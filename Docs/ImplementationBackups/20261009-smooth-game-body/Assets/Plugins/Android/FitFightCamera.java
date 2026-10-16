package com.fitfight.camera;

import android.app.Activity;
import android.content.Context;
import android.graphics.ImageFormat;
import android.hardware.camera2.*;
import android.media.Image;
import android.media.ImageReader;
import android.os.Handler;
import android.os.HandlerThread;
import android.os.SystemClock;
import android.util.Range;
import android.util.Size;
import java.nio.ByteBuffer;
import java.util.Arrays;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;

/** One latest CPU frame. Open/retry/close all run on the camera handler. */
public final class FitFightCamera {
    private final Activity activity;
    private final FitFightCameraRecovery recovery=new FitFightCameraRecovery(3);
    private final CountDownLatch shutdown=new CountDownLatch(1);
    private final StringBuilder trace=new StringBuilder();
    private HandlerThread thread;
    private Handler handler;
    private CameraManager manager;
    private CameraManager.AvailabilityCallback availability;
    private CameraDevice camera;
    private CameraCaptureSession session;
    private ImageReader reader;
    private volatile boolean closed;
    private volatile String error="",status="Waiting for Android camera";
    private boolean available,openPending,reportedFrame;
    private String selected;
    private Size size;
    private Range<Integer>[] ranges;
    private int rotation,sensorRotation,requestedFps,fps,sequence;
    private boolean front;
    private int[] latest;
    private long arrival;
    public FitFightCamera(Activity activity) { this.activity=activity; }
    public String getError() { return error; }
    public String getStatus() { return status; }
    public synchronized String getTrace() { return trace.toString(); }
    private synchronized void note(String message) {
        status=message;
        trace.append(SystemClock.elapsedRealtime()).append(" ").append(message).append("\n");
        if(trace.length()>4096) trace.delete(0,trace.indexOf("\n",trace.length()-3072)+1);
        android.util.Log.i("FitFightCamera",message);
    }
    public void start(String preferredId,boolean preferFront,int requestedFps) {
        this.requestedFps=requestedFps;
        thread=new HandlerThread("FitFightCamera");thread.start();handler=new Handler(thread.getLooper());
        handler.post(() -> {
            if(closed) { finishShutdown();return; }
            try {
                manager=(CameraManager)activity.getSystemService(Context.CAMERA_SERVICE);
                for(String id:manager.getCameraIdList()) if(id.equals(preferredId)) selected=id;
                if(selected==null) for(String id:manager.getCameraIdList()) {
                    Integer facing=manager.getCameraCharacteristics(id).get(CameraCharacteristics.LENS_FACING);
                    if(facing!=null && (facing==CameraCharacteristics.LENS_FACING_FRONT)==preferFront) { selected=id;break; }
                }
                if(selected==null) selected=manager.getCameraIdList()[0];
                CameraCharacteristics c=manager.getCameraCharacteristics(selected);
                front=c.get(CameraCharacteristics.LENS_FACING)==CameraCharacteristics.LENS_FACING_FRONT;
                sensorRotation=c.get(CameraCharacteristics.SENSOR_ORIENTATION);
                Size[] sizes=c.get(CameraCharacteristics.SCALER_STREAM_CONFIGURATION_MAP).getOutputSizes(ImageFormat.YUV_420_888);
                size=sizes[0];double score=Double.MAX_VALUE;
                for(Size candidate:sizes) {
                    double s=Math.abs(candidate.getWidth()-640)+Math.abs(candidate.getHeight()-480)
                        +Math.abs(candidate.getWidth()/(double)candidate.getHeight()-4.0/3)*1000;
                    if(s<score) { size=candidate;score=s; }
                }
                ranges=c.get(CameraCharacteristics.CONTROL_AE_AVAILABLE_TARGET_FPS_RANGES);
                note("Selected camera "+selected+" front="+front+" size="+size+"; waiting for release");
                availability=new CameraManager.AvailabilityCallback() {
                    public void onCameraAvailable(String id) {
                        if(!id.equals(selected) || closed) return;
                        available=true;handler.postDelayed(() -> tryOpen(),250);
                    }
                    public void onCameraUnavailable(String id) { if(id.equals(selected)) available=false; }
                };
                manager.registerAvailabilityCallback(availability,handler);
                // Unity may release its previous WebCamTexture asynchronously.
                handler.postDelayed(() -> {
                    if(!closed && camera==null && !openPending && sequence==0 && error.isEmpty()) {
                        error="Camera2 unavailable: previous camera did not release";note(error);
                    }
                },7000);
            } catch(Exception e) { if(!closed) { error=e.toString();note(error); } }
        });
    }
    private void tryOpen() {
        if(closed || !available || !error.isEmpty() || !recovery.begin()) return;
        final int generation=recovery.attempt();
        fps=generation==1?requestedFps:Math.min(requestedFps,30);
        Range<Integer> chosen=null;int score=Integer.MAX_VALUE;
        if(ranges!=null) for(Range<Integer> range:ranges) {
            int s=Math.abs(range.getUpper()-fps)*10+Math.abs(range.getLower()-fps);
            if(s<score) { score=s;chosen=range; }
        }
        final Range<Integer> target=chosen;
        if(target!=null) fps=target.getUpper();
        note("Opening attempt "+generation+" fps="+target);reportedFrame=false;
        try {
            reader=ImageReader.newInstance(size.getWidth(),size.getHeight(),ImageFormat.YUV_420_888,3);
            reader.setOnImageAvailableListener(this::receive,handler);openPending=true;
            manager.openCamera(selected,new CameraDevice.StateCallback() {
                public void onOpened(CameraDevice device) {
                    if(generation!=recovery.attempt()) { device.close();return; }
                    openPending=false;
                    if(closed || !recovery.opened(generation)) { device.close();return; }
                    camera=device;
                    note("Opened attempt "+generation);
                    try {
                        device.createCaptureSession(Arrays.asList(reader.getSurface()),new CameraCaptureSession.StateCallback() {
                            public void onConfigured(CameraCaptureSession capture) {
                                if(closed || camera!=device || !recovery.configuring(generation)) { capture.close();return; }
                                session=capture;
                                try {
                                    CaptureRequest.Builder request=device.createCaptureRequest(CameraDevice.TEMPLATE_PREVIEW);
                                    request.addTarget(reader.getSurface());
                                    request.set(CaptureRequest.CONTROL_AF_MODE,CaptureRequest.CONTROL_AF_MODE_CONTINUOUS_VIDEO);
                                    if(target!=null) request.set(CaptureRequest.CONTROL_AE_TARGET_FPS_RANGE,target);
                                    capture.setRepeatingRequest(request.build(),null,handler);note("Streaming attempt "+generation);
                                } catch(Exception e) { failed(device,generation,e.toString()); }
                            }
                            public void onConfigureFailed(CameraCaptureSession capture) { failed(device,generation,"Camera2 configuration failed"); }
                        },handler);
                    } catch(Exception e) { failed(device,generation,e.toString()); }
                }
                public void onDisconnected(CameraDevice device) { openPending=false;failed(device,generation,"Camera2 disconnected"); }
                public void onError(CameraDevice device,int code) { openPending=false;failed(device,generation,"Camera2 error "+code); }
                public void onClosed(CameraDevice device) {
                    if(camera==device) camera=null;note("Closed attempt "+generation);
                    if(closed) finishShutdown();
                    else if(recovery.deviceClosed(generation)) handler.postDelayed(() -> tryOpen(),350);
                }
            },handler);
        } catch(Exception e) {
            openPending=false;recovery.failed(generation);releaseSurfaces();note(e.toString()+" attempt "+generation);
            if(recovery.deviceClosed(generation)) handler.postDelayed(() -> tryOpen(),350);else error=e.toString();
        }
    }
    private void failed(CameraDevice device,int generation,String reason) {
        if(device!=null) device.close();
        if(closed || !recovery.failed(generation)) return;
        if(camera==null) camera=device;
        note(reason+" attempt "+generation+" frames="+sequence);releaseSurfaces();
        synchronized(this) { latest=null; }
        if(!recovery.canRetry()) error=reason+" after "+generation+" attempts";
    }
    private void releaseSurfaces() {
        if(session!=null) { session.close();session=null; }
        if(reader!=null) { reader.close();reader=null; }
    }
    private void receive(ImageReader source) {
        if(closed || source!=reader) return;
        try(Image image=source.acquireLatestImage()) {
            if(image==null) return;
            long at=SystemClock.elapsedRealtimeNanos();
            int display=activity.getWindowManager().getDefaultDisplay().getRotation()*90;
            rotation=(sensorRotation+(front?display:-display)+360)%360;
            int sw=image.getWidth(),sh=image.getHeight();
            int uw=rotation%180==0?sw:sh,uh=rotation%180==0?sh:sw;
            float ratio=Math.min(1f,512f/Math.max(uw,uh));
            int w=Math.max(1,Math.round(uw*ratio)),h=Math.max(1,Math.round(uh*ratio));
            int[] frame=new int[8+w*h];
            frame[0]=w;frame[1]=h;frame[2]=rotation;frame[3]=front?1:0;frame[6]=++sequence;frame[7]=fps;
            Image.Plane[] p=image.getPlanes();ByteBuffer y=p[0].getBuffer(),u=p[1].getBuffer(),v=p[2].getBuffer();
            int yp=y.position(),up=u.position(),vp=v.position();
            for(int row=0;row<h;row++) for(int col=0;col<w;col++) {
                int ox=Math.min(uw-1,(int)((col+.5f)*uw/w)),oy=Math.min(uh-1,(int)((row+.5f)*uh/h));
                int sx=FitFightCameraPixels.sourceX(ox,oy,sw,sh,rotation),sy=FitFightCameraPixels.sourceY(ox,oy,sw,sh,rotation);
                int yy=y.get(yp+sy*p[0].getRowStride()+sx*p[0].getPixelStride())&255;
                int uu=u.get(up+(sy/2)*p[1].getRowStride()+(sx/2)*p[1].getPixelStride())&255;
                int vv=v.get(vp+(sy/2)*p[2].getRowStride()+(sx/2)*p[2].getPixelStride())&255;
                frame[8+row*w+col]=FitFightCameraPixels.rgba(yy,uu,vv);
            }
            frame[5]=(int)((SystemClock.elapsedRealtimeNanos()-at)/1000);
            synchronized(this) { latest=frame;arrival=at; }
            if(!reportedFrame) { reportedFrame=true;note("First frame attempt "+recovery.attempt()+" rotation="+rotation+" rgba="+w+"x"+h); }
        } catch(Exception e) { if(!closed) failed(camera,recovery.attempt(),e.toString()); }
    }
    public synchronized int[] takeFrame() {
        if(latest==null) return null;
        int[] value=latest;latest=null;
        value[4]=(int)Math.min(Integer.MAX_VALUE,(SystemClock.elapsedRealtimeNanos()-arrival)/1000);return value;
    }
    private void finishShutdown() { shutdown.countDown();if(thread!=null) thread.quitSafely(); }
    public void close() {
        closed=true;if(handler==null) return;
        handler.post(() -> {
            recovery.stop();note("Close requested");
            if(manager!=null && availability!=null) manager.unregisterAvailabilityCallback(availability);
            releaseSurfaces();synchronized(this) { latest=null; }
            if(camera!=null) camera.close();else if(!openPending) finishShutdown();
            // A late onOpened must still close the pending device. Keep its
            // callback handler alive until onClosed, including after the wait.
        });
        try { shutdown.await(1200,TimeUnit.MILLISECONDS); }
        catch(InterruptedException e) { Thread.currentThread().interrupt(); }
    }
}
