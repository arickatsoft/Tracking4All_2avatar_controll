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

/** Single-owner Camera2 capture. One replaceable CPU frame; preview never blocks analysis. */
public final class FitFightCamera {
    private final Activity activity;
    private HandlerThread thread;
    private Handler handler;
    private CameraDevice camera;
    private CameraCaptureSession session;
    private ImageReader reader;
    private volatile boolean closed;
    private volatile String error = "";
    private int rotation, sensorRotation, fps, sequence;
    private boolean front;
    private int[] latest;
    private long arrival;
    public FitFightCamera(Activity activity) { this.activity = activity; }
    public String getError() { return error; }
    public void start(String preferredId, boolean preferFront, int requestedFps) {
        thread = new HandlerThread("FitFightCamera"); thread.start(); handler = new Handler(thread.getLooper());
        handler.post(() -> {
            try {
                CameraManager manager = (CameraManager)activity.getSystemService(Context.CAMERA_SERVICE);
                String selected = null;
                for (String id : manager.getCameraIdList()) if (id.equals(preferredId)) selected = id;
                if (selected == null) for (String id : manager.getCameraIdList()) {
                    Integer facing = manager.getCameraCharacteristics(id).get(CameraCharacteristics.LENS_FACING);
                    if (facing != null && (facing == CameraCharacteristics.LENS_FACING_FRONT) == preferFront) { selected = id; break; }
                }
                if (selected == null) selected = manager.getCameraIdList()[0];
                CameraCharacteristics c = manager.getCameraCharacteristics(selected);
                front = c.get(CameraCharacteristics.LENS_FACING) == CameraCharacteristics.LENS_FACING_FRONT;
                int display = activity.getWindowManager().getDefaultDisplay().getRotation() * 90;
                sensorRotation=c.get(CameraCharacteristics.SENSOR_ORIENTATION);
                rotation = (sensorRotation + (front ? display : -display) + 360) % 360;
                Size[] sizes = c.get(CameraCharacteristics.SCALER_STREAM_CONFIGURATION_MAP).getOutputSizes(ImageFormat.YUV_420_888);
                Size best = sizes[0]; double score = Double.MAX_VALUE;
                for (Size size : sizes) {
                    // Preserve the camera's 4:3 field of view while bounding conversion work.
                    double s = Math.abs(size.getWidth()-640) + Math.abs(size.getHeight()-480) + Math.abs(size.getWidth()/(double)size.getHeight()-4.0/3)*1000;
                    if (s < score) { best=size; score=s; }
                }
                fps=requestedFps;
                Range<Integer>[] ranges=c.get(CameraCharacteristics.CONTROL_AE_AVAILABLE_TARGET_FPS_RANGES);
                Range<Integer> chosen=null; int fpsScore=Integer.MAX_VALUE;
                if(ranges!=null) for(Range<Integer> range:ranges) {
                    int s=Math.abs(range.getUpper()-requestedFps)*10 + Math.abs(range.getLower()-requestedFps);
                    if(s<fpsScore) { fpsScore=s;chosen=range; }
                }
                final Range<Integer> target=chosen;
                reader=ImageReader.newInstance(best.getWidth(),best.getHeight(),ImageFormat.YUV_420_888,3);
                reader.setOnImageAvailableListener(this::receive,handler);
                manager.openCamera(selected,new CameraDevice.StateCallback() {
                    public void onOpened(CameraDevice device) {
                        if(closed) { device.close();return; }
                        camera=device;
                        try {
                            device.createCaptureSession(Arrays.asList(reader.getSurface()),new CameraCaptureSession.StateCallback() {
                                public void onConfigured(CameraCaptureSession capture) {
                                    if(closed) { capture.close();return; }
                                    session=capture;
                                    try {
                                        CaptureRequest.Builder request=device.createCaptureRequest(CameraDevice.TEMPLATE_PREVIEW);
                                        request.addTarget(reader.getSurface());
                                        request.set(CaptureRequest.CONTROL_AF_MODE,CaptureRequest.CONTROL_AF_MODE_CONTINUOUS_VIDEO);
                                        if(target!=null) request.set(CaptureRequest.CONTROL_AE_TARGET_FPS_RANGE,target);
                                        capture.setRepeatingRequest(request.build(),null,handler);
                                    } catch(Exception e) { error=e.toString(); }
                                }
                                public void onConfigureFailed(CameraCaptureSession capture) { error="Camera2 configuration failed"; }
                            },handler);
                        } catch(Exception e) { error=e.toString(); }
                    }
                    public void onDisconnected(CameraDevice device) { device.close();error="Camera2 disconnected"; }
                    public void onError(CameraDevice device,int code) { device.close();error="Camera2 error "+code; }
                },handler);
            } catch(Exception e) { error=e.toString(); }
        });
    }
    private void receive(ImageReader source) {
        if(closed) return;
        try(Image image=source.acquireLatestImage()) {
            if(image==null) return;
            long at=SystemClock.elapsedRealtimeNanos();
            int display=activity.getWindowManager().getDefaultDisplay().getRotation()*90;
            rotation=(sensorRotation+(front?display:-display)+360)%360;
            int sw=image.getWidth(),sh=image.getHeight();
            int uw=rotation%180==0?sw:sh, uh=rotation%180==0?sh:sw;
            float ratio=Math.min(1f,512f/Math.max(uw,uh));
            int w=Math.max(1,Math.round(uw*ratio)),h=Math.max(1,Math.round(uh*ratio));
            int[] frame=new int[8+w*h];
            frame[0]=w;frame[1]=h;frame[2]=rotation;frame[3]=front?1:0;frame[6]=++sequence;
            Image.Plane[] p=image.getPlanes();ByteBuffer y=p[0].getBuffer(),u=p[1].getBuffer(),v=p[2].getBuffer();
            int yp=y.position(),up=u.position(),vp=v.position();
            for(int row=0;row<h;row++) for(int col=0;col<w;col++) {
                int ox=Math.min(uw-1,(int)((col+.5f)*uw/w)),oy=Math.min(uh-1,(int)((row+.5f)*uh/h));
                int sx=FitFightCameraPixels.sourceX(ox,oy,sw,sh,rotation);
                int sy=FitFightCameraPixels.sourceY(ox,oy,sw,sh,rotation);
                int yy=y.get(yp+sy*p[0].getRowStride()+sx*p[0].getPixelStride())&255;
                int uu=u.get(up+(sy/2)*p[1].getRowStride()+(sx/2)*p[1].getPixelStride())&255;
                int vv=v.get(vp+(sy/2)*p[2].getRowStride()+(sx/2)*p[2].getPixelStride())&255;
                frame[8+row*w+col]=FitFightCameraPixels.rgba(yy,uu,vv);
            }
            frame[5]=(int)((SystemClock.elapsedRealtimeNanos()-at)/1000);
            synchronized(this) { latest=frame;arrival=at; }
        } catch(Exception e) { if(!closed) error=e.toString(); }
    }
    public synchronized int[] takeFrame() {
        if(latest==null) return null;
        int[] value=latest;latest=null;
        value[4]=(int)Math.min(Integer.MAX_VALUE,(SystemClock.elapsedRealtimeNanos()-arrival)/1000);
        return value;
    }
    public void close() {
        closed=true;
        if(handler==null) return;
        CountDownLatch done=new CountDownLatch(1);
        handler.post(() -> {
            try {
                if(session!=null) session.close();if(camera!=null) camera.close();if(reader!=null) reader.close();
                synchronized(this) { latest=null; }
            } finally { done.countDown(); }
        });
        try { done.await(700,TimeUnit.MILLISECONDS); } catch(InterruptedException e) { Thread.currentThread().interrupt(); }
        thread.quitSafely();handler=null;
    }
}
