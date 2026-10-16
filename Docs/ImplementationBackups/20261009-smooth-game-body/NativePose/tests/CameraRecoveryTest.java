import com.fitfight.camera.FitFightCameraRecovery;

public final class CameraRecoveryTest {
    private static int checks;
    private static void check(boolean ok,String label) { if(!ok) throw new AssertionError(label);checks++; }
    public static void main(String[] args) {
        FitFightCameraRecovery r=new FitFightCameraRecovery(3);
        check(r.begin() && r.attempt()==1,"Initial open");
        check(!r.begin(),"No concurrent open");
        check(r.opened(1),"Camera opens");
        check(r.configuring(1),"Session may configure");
        check(!r.begin(),"Availability callback cannot reopen a streaming device");
        check(r.failed(1),"Disconnect begins recovery");
        check(!r.configuring(1),"Late configuration cannot restart the disconnected session");
        check(!r.begin(),"Wait for old device to close before retry");
        check(!r.failed(1),"Duplicate error does not duplicate recovery");
        check(r.deviceClosed(1),"Closed device allows retry");
        check(!r.deviceClosed(1) && !r.failed(1),"Late callbacks after closure cannot duplicate recovery");
        check(r.begin() && r.attempt()==2,"Second open");
        check(!r.opened(1) && !r.failed(1) && !r.deviceClosed(1),"Old generation cannot change new owner");
        check(r.opened(2) && r.failed(2),"Second failure");
        check(r.deviceClosed(2) && r.begin(),"Last retry");
        check(r.failed(3) && !r.canRetry(),"Bounded failures expose fallback");
        check(!r.deviceClosed(3) && !r.begin(),"No infinite reopen loop");
        FitFightCameraRecovery pending=new FitFightCameraRecovery(3);
        pending.begin();pending.stop();
        check(!pending.opened(1) && !pending.failed(1) && !pending.begin(),"Late opening callback after dispose cannot resurrect camera");
        FitFightCameraRecovery closing=new FitFightCameraRecovery(3);
        closing.begin();closing.opened(1);closing.failed(1);closing.stop();
        check(!closing.deviceClosed(1) && !closing.begin(),"Closing callback after dispose cannot schedule retry");
        System.out.println("PASS "+checks+" camera ownership/recovery checks; physical Android capture still needs phone verification.");
    }
}
