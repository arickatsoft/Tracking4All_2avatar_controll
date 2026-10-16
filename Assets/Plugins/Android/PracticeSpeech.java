package com.fitfight.practice;

import android.content.Context;
import android.media.AudioAttributes;
import android.os.Handler;
import android.os.Looper;
import android.os.SystemClock;
import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;
import java.util.Locale;
import java.util.ArrayDeque;

/** All engine operations run on Android's UI thread; no Unity-frame blocking or stale queues. */
public final class PracticeSpeech {
    private final Handler main = new Handler(Looper.getMainLooper());
    private TextToSpeech engine;
    private volatile int status; // 0 initializing, 1 ready, -1 unavailable
    private boolean closed;
    private String pending;
    private long pendingAt;
    private int utterance,pendingId;
    private final ArrayDeque<String> events=new ArrayDeque<>();
    private synchronized void event(String type,String id) {
        if(events.size()>=128) events.removeFirst();
        events.addLast(type+"|"+id+"|"+SystemClock.elapsedRealtimeNanos());
    }
    public synchronized String[] drainEvents() { String[] values=events.toArray(new String[0]);events.clear();return values; }
    public long clockNanos() { return SystemClock.elapsedRealtimeNanos(); }

    public PracticeSpeech(Context context) {
        Context application = context.getApplicationContext();
        main.post(() -> {
            if (closed) return;
            engine = new TextToSpeech(application, result -> main.post(() -> initialized(result)));
        });
        main.postDelayed(() -> { if (!closed && status == 0) { status = -1; pending = null; } }, 10000);
    }
    private void initialized(int result) {
        if (closed || engine == null) return;
        if (result != TextToSpeech.SUCCESS) { status = -1; pending = null; return; }
        int language = engine.setLanguage(Locale.US);
        if (language < TextToSpeech.LANG_AVAILABLE) { status = -1; pending = null; return; }
        engine.setSpeechRate(1.0f);
        engine.setAudioAttributes(new AudioAttributes.Builder()
            .setUsage(AudioAttributes.USAGE_MEDIA).setContentType(AudioAttributes.CONTENT_TYPE_SPEECH).build());
        engine.setOnUtteranceProgressListener(new UtteranceProgressListener() {
            @Override public void onStart(String id) { event("start",id); }
            @Override public void onDone(String id) { event("done",id); }
            @Override public void onError(String id) { event("error",id);status = -1; }
            @Override public void onStop(String id,boolean interrupted) { event("stop",id); }
        });
        status = 1;
        String next = pending; pending = null;
        if (next != null && SystemClock.elapsedRealtime() - pendingAt < 4000) speakReady(next,pendingId);
    }
    public int getStatus() { return status; }
    public synchronized int speak(String text) {
        final int id=++utterance;event("enqueue","practice-"+id);
        final long requested = SystemClock.elapsedRealtime();
        main.post(() -> {
            if (closed || status < 0 || SystemClock.elapsedRealtime() - requested >= 4000) return;
            if (status == 0) { pending = text; pendingId=id;pendingAt = requested; return; }
            speakReady(text,id);
        });
        return id;
    }
    private void speakReady(String text,int id) {
        if (engine.speak(text, TextToSpeech.QUEUE_FLUSH, null, "practice-" + id) == TextToSpeech.ERROR)
            status = -1;
    }
    public void stop() {
        main.post(() -> { pending = null; if (!closed && engine != null) engine.stop(); });
    }
    public void shutdown() {
        main.post(() -> {
            closed = true; pending = null;
            if (engine != null) { engine.stop(); engine.shutdown(); engine = null; }
        });
    }
}
