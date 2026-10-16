package com.fitfight.camera;

/** Ignores stale callbacks and prevents retries before the old device closes. */
public final class FitFightCameraRecovery {
    private final int maximum;
    private int attempt;
    private boolean stopped,opening,closing,streaming;
    public FitFightCameraRecovery(int maximum) { this.maximum=maximum; }
    public int attempt() { return attempt; }
    public boolean canRetry() { return !stopped && attempt<maximum; }
    public boolean begin(boolean available,boolean probe) {
        return (available || probe) && begin();
    }
    public boolean begin() {
        if(!canRetry() || opening || closing || streaming) return false;
        attempt++;opening=true;return true;
    }
    public boolean opened(int generation) {
        if(stopped || generation!=attempt || !opening) return false;
        opening=false;streaming=true;return true;
    }
    public boolean failed(int generation) {
        if(stopped || generation!=attempt || closing || !opening && !streaming) return false;
        opening=streaming=false;closing=true;return true;
    }
    public boolean deviceClosed(int generation) {
        if(generation!=attempt || !closing) return false;
        closing=false;return canRetry();
    }
    public void stop() { stopped=true;opening=streaming=false; }
    public boolean configuring(int generation) { return !stopped && generation==attempt && streaming && !closing; }
}
