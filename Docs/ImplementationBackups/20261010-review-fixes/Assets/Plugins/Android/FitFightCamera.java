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
import java.nio.ByteOrder;
import java.util.LinkedHashMap;
import java.util.ArrayDeque;
import java.util.concurrent.locks.ReentrantLock;
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
    static { System.loadLibrary("fitfight_pose"); }
    private final ReentrantLock pixelsLock=new ReentrantLock();
    private long converter=createConverter();
    private PendingImage latest;
    private ByteBuffer rgba;
    private int[] preview;
    private final long[] info=new long[16];
    private int previewSequence;
    private volatile boolean workerDriven;
    private int activeConsumer;
    private long latestArrival,previousArrival;
    private double measuredFps;
    private long lastInterval;
    private final ArrayDeque<Long> frameTimes=new ArrayDeque<>();
    private final long[] intervalBins=new long[7];
    private int replacedImages,convertedImages;
    private boolean realtimeSensor;
    private final LinkedHashMap<Long,long[]> captureResults=new LinkedHashMap<>();
    private static final class PendingImage {
        final Image image;final long arrival;final int rotation,sequence;
        PendingImage(Image image,long at,int rotation,int sequence) { this.image=image;arrival=at;this.rotation=rotation;this.sequence=sequence; }
    }
    private static native long createConverter();
    private static native void destroyConverter(long converter);
    private static native int convert(long converter,ByteBuffer y,int ys,ByteBuffer u,int us,ByteBuffer v,int vs,int pixel,
        int sw,int sh,int rotation,ByteBuffer dst,int w,int h);
    public FitFightCamera getSource() { return this; }
    public long clockNanos() { return SystemClock.elapsedRealtimeNanos(); }
    public int beginConsumer() {
        pixelsLock.lock();try {
            workerDriven=true;rgba=null;preview=null;
            // Each new model gets a new direct buffer. The previous worker may
            // still be finishing inference while Unity keeps the camera open.
            return ++activeConsumer;
        } finally { pixelsLock.unlock(); }
    }
    public synchronized long[] getCameraStats() {
        long[] stats=new long[13];stats[0]=sequence;stats[1]=latestArrival;stats[2]=(long)(measuredFps*1000);
        stats[3]=replacedImages;stats[4]=convertedImages;stats[5]=lastInterval;
        System.arraycopy(intervalBins,0,stats,6,7);return stats;
    }
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
        thread=new HandlerThread("FitFightCamera",android.os.Process.THREAD_PRIORITY_DISPLAY);thread.start();handler=new Handler(thread.getLooper());
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
                realtimeSensor=Integer.valueOf(CameraCharacteristics.SENSOR_INFO_TIMESTAMP_SOURCE_REALTIME).equals(
                    c.get(CameraCharacteristics.SENSOR_INFO_TIMESTAMP_SOURCE));
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
                // Some camera services deliver a stale unavailable state during
                // Unity handoff. A bounded open probe lets Camera2 report the real
                // ownership error instead of waiting forever for a notification.
                handler.postDelayed(() -> {
                    if(!closed && recovery.attempt()==0 && camera==null && !openPending && error.isEmpty()) {
                        note("Availability delayed; probing first open");tryOpen(true);
                    }
                },1200);
                // Unity may release its previous WebCamTexture asynchronously.
                handler.postDelayed(() -> {
                    if(!closed && camera==null && !openPending && sequence==0 && error.isEmpty()) {
                        error="Camera2 availability/open timed out";note(error);
                    }
                },7000);
            } catch(Exception e) { if(!closed) { error=e.toString();note(error); } }
        });
    }
    private void tryOpen() {
        tryOpen(false);
    }
    private void tryOpen(boolean probe) {
        if(closed || !error.isEmpty() || !recovery.begin(available,probe)) return;
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
            reader=ImageReader.newInstance(size.getWidth(),size.getHeight(),ImageFormat.YUV_420_888,4);
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
                                    CameraCharacteristics caps=manager.getCameraCharacteristics(selected);
                                    if(has(caps.get(CameraCharacteristics.CONTROL_AF_AVAILABLE_MODES),CaptureRequest.CONTROL_AF_MODE_CONTINUOUS_VIDEO))
                                        request.set(CaptureRequest.CONTROL_AF_MODE,CaptureRequest.CONTROL_AF_MODE_CONTINUOUS_VIDEO);
                                    if(has(caps.get(CameraCharacteristics.CONTROL_AVAILABLE_VIDEO_STABILIZATION_MODES),CaptureRequest.CONTROL_VIDEO_STABILIZATION_MODE_OFF))
                                        request.set(CaptureRequest.CONTROL_VIDEO_STABILIZATION_MODE,CaptureRequest.CONTROL_VIDEO_STABILIZATION_MODE_OFF);
                                    if(has(caps.get(CameraCharacteristics.NOISE_REDUCTION_AVAILABLE_NOISE_REDUCTION_MODES),CaptureRequest.NOISE_REDUCTION_MODE_FAST))
                                        request.set(CaptureRequest.NOISE_REDUCTION_MODE,CaptureRequest.NOISE_REDUCTION_MODE_FAST);
                                    if(has(caps.get(CameraCharacteristics.EDGE_AVAILABLE_EDGE_MODES),CaptureRequest.EDGE_MODE_FAST))
                                        request.set(CaptureRequest.EDGE_MODE,CaptureRequest.EDGE_MODE_FAST);
                                    if(target!=null) request.set(CaptureRequest.CONTROL_AE_TARGET_FPS_RANGE,target);
                                    capture.setRepeatingRequest(request.build(),new CameraCaptureSession.CaptureCallback() {
                                        public void onCaptureCompleted(CameraCaptureSession session,CaptureRequest request,TotalCaptureResult result) {
                                            Long timestamp=result.get(CaptureResult.SENSOR_TIMESTAMP);
                                            Long exposure=result.get(CaptureResult.SENSOR_EXPOSURE_TIME),duration=result.get(CaptureResult.SENSOR_FRAME_DURATION);
                                            Integer iso=result.get(CaptureResult.SENSOR_SENSITIVITY),ae=result.get(CaptureResult.CONTROL_AE_STATE);
                                            Range<Integer> applied=result.get(CaptureResult.CONTROL_AE_TARGET_FPS_RANGE);
                                            if(timestamp!=null) synchronized(FitFightCamera.this) {
                                                captureResults.put(timestamp,new long[]{exposure==null?0:exposure,duration==null?0:duration,
                                                    iso==null?0:iso,ae==null?-1:ae,applied==null?0:applied.getLower(),applied==null?0:applied.getUpper()});
                                                if(captureResults.size()>16) captureResults.remove(captureResults.keySet().iterator().next());
                                            }
                                        }
                                    },handler);note("Streaming attempt "+generation);
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
                    else if(recovery.deviceClosed(generation)) handler.postDelayed(() -> tryOpen(true),350);
                }
            },handler);
        } catch(Exception e) {
            openPending=false;recovery.failed(generation);releaseSurfaces();note(e.toString()+" attempt "+generation);
            if(recovery.deviceClosed(generation)) handler.postDelayed(() -> tryOpen(true),350);else error=e.toString();
        }
    }
    private void failed(CameraDevice device,int generation,String reason) {
        if(device!=null) device.close();
        if(closed || !recovery.failed(generation)) return;
        if(camera==null) camera=device;
        note(reason+" attempt "+generation+" frames="+sequence);releaseSurfaces();
        synchronized(this) { clearLatest(); }
        if(!recovery.canRetry()) error=reason+" after "+generation+" attempts";
    }
    private static boolean has(int[] modes,int value) { if(modes!=null) for(int mode:modes) if(mode==value) return true;return false; }
    // Called under this monitor. A reader owns at most one waiting image and one consumed image.
    private void clearLatest() { if(latest!=null) latest.image.close();latest=null;notifyAll(); }
    private void releaseSurfaces() {
        if(session!=null) { session.close();session=null; }
        pixelsLock.lock();try {
            synchronized(this) { clearLatest();captureResults.clear(); }
            if(reader!=null) { reader.close();reader=null; }
        } finally { pixelsLock.unlock(); }
    }
    private void receive(ImageReader source) {
        if(closed || source!=reader) return;
        try {
            Image image=source.acquireLatestImage();if(image==null) return;
            long at=SystemClock.elapsedRealtimeNanos();
            int display=activity.getWindowManager().getDefaultDisplay().getRotation()*90;
            int upright=(sensorRotation+(front?display:-display)+360)%360;
            synchronized(this) {
                if(closed) { image.close();return; }
                if(latest!=null) { latest.image.close();replacedImages++; }
                if(previousArrival>0 && at>previousArrival) {
                    lastInterval=at-previousArrival;double ms=lastInterval/1e6;
                    int bin=ms<10?0:ms<18?1:ms<25?2:ms<35?3:ms<50?4:ms<100?5:6;intervalBins[bin]++;
                }
                frameTimes.addLast(at);while(frameTimes.size()>2 && frameTimes.peekFirst()<at-1000000000L) frameTimes.removeFirst();
                if(frameTimes.size()>1) measuredFps=(frameTimes.size()-1)*1e9/(at-frameTimes.peekFirst());
                previousArrival=latestArrival=at;
                latest=new PendingImage(image,at,upright,++sequence);notifyAll();
            }
            if(!reportedFrame) { reportedFrame=true;note("First frame attempt "+recovery.attempt()+" rotation="+upright+" native YUV"); }
        } catch(Exception e) { if(!closed) failed(camera,recovery.attempt(),e.toString()); }
    }
    // Single inference consumer. The direct buffer remains unchanged until its next call.
    // Images are closed before returning; inference never holds an ImageReader image.
    public ByteBuffer waitForFrame(int timeoutMs,int maxDimension) throws InterruptedException {
        return waitForFrame(timeoutMs,maxDimension,0);
    }
    public ByteBuffer waitForFrame(int timeoutMs,int maxDimension,int consumer) throws InterruptedException {
        pixelsLock.lock();try {
            if(consumer!=activeConsumer) return null;
            PendingImage next;
            synchronized(this) {
                long deadline=SystemClock.elapsedRealtime()+Math.min(25,Math.max(0,timeoutMs));
                while(latest==null && !closed && error.isEmpty()) {
                    long remaining=deadline-SystemClock.elapsedRealtime();if(remaining<=0) return null;wait(remaining);
                }
                if(closed || latest==null) return null;
                next=latest;latest=null;
            }
            try(Image image=next.image) {
                int sw=image.getWidth(),sh=image.getHeight();
                int uw=next.rotation%180==0?sw:sh,uh=next.rotation%180==0?sh:sw;
                float ratio=Math.min(1f,Math.max(256,Math.min(512,maxDimension))/(float)Math.max(uw,uh));
                int w=Math.max(1,Math.round(uw*ratio)),h=Math.max(1,Math.round(uh*ratio));
                if(rgba==null || rgba.capacity()!=w*h*4) {
                    rgba=ByteBuffer.allocateDirect(w*h*4).order(ByteOrder.LITTLE_ENDIAN);preview=new int[8+w*h];
                }
                long started=SystemClock.elapsedRealtimeNanos();
                Image.Plane[] planes=image.getPlanes();
                int code=convert(converter,planes[0].getBuffer().slice(),planes[0].getRowStride(),
                    planes[1].getBuffer().slice(),planes[1].getRowStride(),planes[2].getBuffer().slice(),planes[2].getRowStride(),
                    planes[1].getPixelStride(),sw,sh,next.rotation,rgba,w,h);
                if(code!=0) throw new IllegalStateException("Native YUV conversion failed: "+code);
                long completed=SystemClock.elapsedRealtimeNanos();
                synchronized(this) {
                    long[] capture=captureResults.get(image.getTimestamp());
                    info[0]=w;info[1]=h;info[2]=next.rotation;info[3]=front?1:0;info[4]=next.arrival;
                    info[5]=started;info[6]=completed;info[7]=next.sequence;info[8]=image.getTimestamp();info[9]=realtimeSensor?1:0;
                    for(int i=0;i<6;i++) info[10+i]=capture==null?(i==3?-1:0):capture[i];
                    convertedImages++;
                }
                return rgba;
            } catch(RuntimeException e) { if(!closed) error=e.toString();throw e; }
        } finally { pixelsLock.unlock(); }
    }
    public static final class Frame {
        private final ByteBuffer pixels;private final long[] metadata;
        Frame(ByteBuffer pixels,long[] metadata) { this.pixels=pixels;this.metadata=metadata; }
        public ByteBuffer getPixels() { return pixels; }
        public long[] getInfo() { return metadata; }
    }
    public Frame waitForInferenceFrame(int timeoutMs,int maxDimension,int consumer) throws InterruptedException {
        pixelsLock.lock();try {
            ByteBuffer pixels=waitForFrame(timeoutMs,maxDimension,consumer);
            return pixels==null?null:new Frame(pixels,info.clone());
        } finally { pixelsLock.unlock(); }
    }
    // Preview copies only at the caller's 15 Hz limit; no camera-sized allocation in receive().
    public int[] takeFrame() throws InterruptedException {
        if(!workerDriven) waitForFrame(0,512); // one startup frame before the inference worker is attached
        if(!pixelsLock.tryLock()) return null;try {
            if(rgba==null || previewSequence==info[7]) return null;
            previewSequence=(int)info[7];
            rgba.position(0);rgba.asIntBuffer().get(preview,8,preview.length-8);
            preview[0]=(int)info[0];preview[1]=(int)info[1];preview[2]=(int)info[2];preview[3]=(int)info[3];
            preview[4]=(int)Math.min(Integer.MAX_VALUE,(SystemClock.elapsedRealtimeNanos()-info[4])/1000);
            preview[5]=(int)((info[6]-info[5])/1000);preview[6]=(int)info[7];preview[7]=fps;return preview;
        } finally { pixelsLock.unlock(); }
    }
    private void finishShutdown() { shutdown.countDown();if(thread!=null) thread.quitSafely(); }
    public void close() {
        closed=true;synchronized(this) { notifyAll(); }
        if(handler==null) { pixelsLock.lock();try { destroyConverter(converter);converter=0; } finally { pixelsLock.unlock(); } return; }
        handler.post(() -> {
            recovery.stop();note("Close requested");
            if(manager!=null && availability!=null) manager.unregisterAvailabilityCallback(availability);
            releaseSurfaces();pixelsLock.lock();try { destroyConverter(converter);converter=0; } finally { pixelsLock.unlock(); }
            if(camera!=null) camera.close();else if(!openPending) finishShutdown();
            // A late onOpened must still close the pending device. Keep its
            // callback handler alive until onClosed, including after the wait.
        });
        try { shutdown.await(1200,TimeUnit.MILLISECONDS); }
        catch(InterruptedException e) { Thread.currentThread().interrupt(); }
    }
}
