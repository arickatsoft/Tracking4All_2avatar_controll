package com.fitfight.camera;
import java.lang.reflect.*;
import java.nio.ByteBuffer;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.locks.ReentrantLock;

/** Real production Java/JNI methods with a host-built converter; no Android camera emulation. */
public final class CameraLeaseTest {
    private static int checks;
    private static void check(boolean value,String message) { if(!value) throw new AssertionError(message);checks++; }
    private static Field field(String name) throws Exception { Field f=FitFightCamera.class.getDeclaredField(name);f.setAccessible(true);return f; }
    public static void main(String[] args) throws Exception {
        FitFightCamera camera=new FitFightCamera(null);
        int first=camera.beginConsumer();
        ByteBuffer oldPixels=ByteBuffer.allocateDirect(16);oldPixels.put(0,(byte)77);field("rgba").set(camera,oldPixels);
        int second=camera.beginConsumer();
        check(second>first && field("rgba").get(camera)==null,"New consumer invalidates only the camera's buffer reference");
        check(oldPixels.get(0)==77,"Previous inference's owned direct buffer remains alive after model switch");
        check(camera.waitForFrame(0,512,first)==null,"Cancelled consumer cannot acquire a new frame");
        ReentrantLock lock=(ReentrantLock)field("pixelsLock").get(camera);
        CountDownLatch entered=new CountDownLatch(1),release=new CountDownLatch(1);
        Thread busy=new Thread(() -> { lock.lock();try { entered.countDown();release.await(); } catch(InterruptedException e) { throw new RuntimeException(e); } finally { lock.unlock(); } });
        busy.start();entered.await();long start=System.nanoTime();int[] preview=camera.takeFrame();long elapsed=System.nanoTime()-start;release.countDown();busy.join();
        check(preview==null && elapsed<20_000_000,"Preview pickup never waits for conversion or a camera image");
        Method convert=FitFightCamera.class.getDeclaredMethod("convert",long.class,ByteBuffer.class,int.class,ByteBuffer.class,int.class,ByteBuffer.class,int.class,int.class,int.class,int.class,int.class,ByteBuffer.class,int.class,int.class);
        convert.setAccessible(true);long converter=field("converter").getLong(camera);
        ByteBuffer y=ByteBuffer.allocateDirect(8),u=ByteBuffer.allocateDirect(3),v=ByteBuffer.allocateDirect(3),rgba=ByteBuffer.allocateDirect(16);
        for(int i=0;i<8;i++) y.put(i,(byte)81);u.put(1,(byte)90);v.put(1,(byte)240);u.position(1);v.position(1);
        int code=(int)convert.invoke(null,converter,y,4,u.slice(),2,v.slice(),2,1,2,2,90,rgba,2,2);
        check(code==0 && (rgba.get(0)&255)>250 && (rgba.get(1)&255)<3 && (rgba.get(2)&255)<3 && (rgba.get(3)&255)==255,"Java direct-buffer slices reach JNI with correct RGBA channel order");
        ByteBuffer shortOutput=ByteBuffer.allocateDirect(3);
        check((int)convert.invoke(null,converter,y,4,u.slice(),2,v.slice(),2,1,2,2,90,shortOutput,2,2)!=0,"JNI rejects insufficient direct-buffer capacity");
        camera.close();check(field("converter").getLong(camera)==0,"Close destroys converter after consumer cancellation");
        camera.close();check(field("converter").getLong(camera)==0,"Repeated close remains safe");
        System.out.println("PASS "+checks+" host Java/JNI lease, nonblocking preview and conversion checks; physical Camera2 still requires a phone.");
    }
}
