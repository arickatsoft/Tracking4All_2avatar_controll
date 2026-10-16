package com.fitfight.camera;

import android.app.Activity;
import android.content.Context;
import android.os.Build;
import android.os.PerformanceHintManager;
import android.os.PowerManager;

/**
 * Thermal state and an Android Dynamic Performance Framework (ADPF) hint session for the pose
 * inference thread. The hint tells the scheduler the per-frame work target so the worker stays on
 * suitably clocked cores; thermal status/headroom are logged so throttling is visible in sessions.
 * Every call degrades to "unavailable" on older Android versions or when the service refuses.
 */
public final class FitFightPerformance {
    private final PowerManager power;
    private final PerformanceHintManager hints;
    private Object session;
    private String status="not requested";
    public FitFightPerformance(Activity activity) {
        power=(PowerManager)activity.getSystemService(Context.POWER_SERVICE);
        hints=Build.VERSION.SDK_INT>=31 ? (PerformanceHintManager)activity.getSystemService(Context.PERFORMANCE_HINT_SERVICE) : null;
    }
    /** 0 none .. 6 shutdown (PowerManager.THERMAL_STATUS_*), -1 unavailable. */
    public int thermalStatus() {
        try { return Build.VERSION.SDK_INT>=29 && power!=null ? power.getCurrentThermalStatus() : -1; }
        catch(Exception e) { return -1; }
    }
    /** Forecast headroom (1.0 = throttling threshold), NaN when unavailable. */
    public float thermalHeadroom(int seconds) {
        try { return Build.VERSION.SDK_INT>=30 && power!=null ? power.getThermalHeadroom(Math.max(0,Math.min(60,seconds))) : Float.NaN; }
        catch(Exception e) { return Float.NaN; }
    }
    /** Must be called on the thread that will report work (the inference worker). */
    public synchronized boolean startHint(long targetNanos) {
        if(Build.VERSION.SDK_INT<31 || hints==null) { status="unavailable (API<31)";return false; }
        try {
            PerformanceHintManager.Session created=hints.createHintSession(new int[]{android.os.Process.myTid()},targetNanos);
            if(created==null) { status="refused by system";return false; }
            session=created;status="active target "+targetNanos/1000000+" ms";return true;
        } catch(Exception e) { status="error: "+e.getMessage();return false; }
    }
    public synchronized void reportWork(long actualNanos) {
        if(Build.VERSION.SDK_INT<31 || session==null || actualNanos<=0) return;
        try { ((PerformanceHintManager.Session)session).reportActualWorkDuration(actualNanos); } catch(Exception ignored) { }
    }
    public synchronized void close() {
        if(Build.VERSION.SDK_INT>=31 && session!=null) try { ((PerformanceHintManager.Session)session).close(); } catch(Exception ignored) { }
        session=null;status="closed";
    }
    public synchronized String getStatus() { return status; }

    private volatile String displayStatus="not requested";
    /** 1.23: Unity's Screen.SetResolution(refreshRate) left the S24 at 60 Hz in two sessions although 120 was
     *  requested. Ask Android directly: pick the highest-refresh mode at the current resolution (or the requested
     *  rate) and set it as the window's preferred display mode. The modes on offer are recorded for diagnosis. */
    public void requestRefresh(final Activity activity,final float hz) {
        if(Build.VERSION.SDK_INT<23) { displayStatus="unavailable (API<23)";return; }
        activity.runOnUiThread(() -> {
            try {
                android.view.Display display=activity.getWindowManager().getDefaultDisplay();
                android.view.Display.Mode current=display.getMode(),best=null;
                StringBuilder modes=new StringBuilder();
                for(android.view.Display.Mode m:display.getSupportedModes()) {
                    modes.append(m.getPhysicalWidth()).append('x').append(m.getPhysicalHeight()).append('@').append(Math.round(m.getRefreshRate())).append(' ');
                    if(m.getPhysicalWidth()!=current.getPhysicalWidth() || m.getPhysicalHeight()!=current.getPhysicalHeight()) continue;
                    if(m.getRefreshRate()>hz+1) continue;
                    if(best==null || m.getRefreshRate()>best.getRefreshRate()) best=m;
                }
                if(best==null) { displayStatus="no mode <= "+Math.round(hz)+" Hz; modes "+modes;return; }
                android.view.WindowManager.LayoutParams p=activity.getWindow().getAttributes();
                p.preferredDisplayModeId=best.getModeId();
                if(Build.VERSION.SDK_INT>=30) p.preferredRefreshRate=best.getRefreshRate();
                activity.getWindow().setAttributes(p);
                displayStatus="requested "+Math.round(best.getRefreshRate())+" Hz (was "+Math.round(current.getRefreshRate())+"); modes "+modes;
            } catch(Exception e) { displayStatus="error: "+e.getMessage(); }
        });
    }
    /** Current display refresh as Android reports it (not Unity's cached value). */
    public float currentRefresh(Activity activity) {
        try { return activity.getWindowManager().getDefaultDisplay().getRefreshRate(); } catch(Exception e) { return -1; }
    }
    public String getDisplayStatus() { return displayStatus; }
}
