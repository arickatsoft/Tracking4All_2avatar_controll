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
        FitFightCameraRecovery probe=new FitFightCameraRecovery(2);
        check(!probe.begin(false,false) && probe.attempt()==0,"Unavailable callback waits during normal handoff");
        check(probe.begin(false,true) && probe.attempt()==1,"Bounded probe can recover a missing availability callback");
        check(!probe.begin(true,true),"Probe and availability callback cannot open twice");
        check(probe.failed(1) && !probe.begin(false,true),"Probe waits for failed device closure");
        check(probe.deviceClosed(1) && probe.begin(false,true),"Closed device can retry despite stale unavailability");
        check(probe.failed(2) && !probe.deviceClosed(2) && !probe.begin(true,true),"Probes respect retry limit");
        FitFightCameraRecovery stopped=new FitFightCameraRecovery(3);stopped.stop();
        check(!stopped.begin(false,true),"Delayed probe after dispose cannot open camera");
        // 1.23: a transient configuration error must not cost 60 fps for the whole session.
        check(FitFightCameraRecovery.fpsForAttempt(1,60,4)==60 && FitFightCameraRecovery.fpsForAttempt(2,60,4)==60
            && FitFightCameraRecovery.fpsForAttempt(3,60,4)==60,"Requested 60 fps is retried after transient failures");
        check(FitFightCameraRecovery.fpsForAttempt(4,60,4)==30,"Only the final attempt falls back to 30 fps");
        check(FitFightCameraRecovery.retryDelayMs(1)==350 && FitFightCameraRecovery.retryDelayMs(3)==950,"Retry delay grows");
        System.out.println("PASS "+checks+" camera ownership/recovery checks; physical Android capture still needs phone verification.");
    }
}
