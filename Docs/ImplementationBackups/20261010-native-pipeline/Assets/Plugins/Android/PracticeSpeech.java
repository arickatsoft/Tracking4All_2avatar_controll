package com.fitfight.practice;

import android.content.Context;
import android.media.AudioAttributes;
import android.os.Handler;
import android.os.Looper;
import android.os.SystemClock;
import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;
import java.util.Locale;

/** All engine operations run on Android's UI thread; no Unity-frame blocking or stale queues. */
public final class PracticeSpeech {
    private final Handler main = new Handler(Looper.getMainLooper());
    private TextToSpeech engine;
    private volatile int status; // 0 initializing, 1 ready, -1 unavailable
    private boolean closed;
    private String pending;
    private long pendingAt;
    private int utterance;

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
            @Override public void onStart(String id) { }
            @Override public void onDone(String id) { }
            @Override public void onError(String id) { status = -1; }
        });
        status = 1;
        String next = pending; pending = null;
        if (next != null && SystemClock.elapsedRealtime() - pendingAt < 4000) speakReady(next);
    }
    public int getStatus() { return status; }
    public void speak(String text) {
        final long requested = SystemClock.elapsedRealtime();
        main.post(() -> {
            if (closed || status < 0 || SystemClock.elapsedRealtime() - requested >= 4000) return;
            if (status == 0) { pending = text; pendingAt = requested; return; }
            speakReady(text);
        });
    }
    private void speakReady(String text) {
        if (engine.speak(text, TextToSpeech.QUEUE_FLUSH, null, "practice-" + (++utterance)) == TextToSpeech.ERROR)
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
