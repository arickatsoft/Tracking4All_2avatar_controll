using UnityEngine;
using FitFight.Practice;
using FitFight.BodyTracking;

// A short spoken guide deliberately independent of session/video/CSV recording.
public sealed class QuickPoseCheck : MonoBehaviour
{
    private PracticeSpeech speech;
    private float started;
    private int next;
    private bool running;
    public string Prompt { get; private set; }
    private void OnEnable() { BodyTrackingSelection.Changed += Stop; }
    public void Stop() { running=false; Prompt=null; speech?.Stop(); }
    private static readonly float[] Times = { 0, 8, 14, 20, 26, 32, 38, 44, 50, 56, 62 };
    private static readonly string[] Cues = {
        "Sanity check. No recording. Stand upright and still, with both feet visible, for calibration.",
        "Raise both arms to the sides, then lower them.",
        "Raise both arms overhead, then lower them.",
        "Bend and straighten your left elbow.",
        "Bend and straighten your right elbow.",
        "Bend your torso forward, then stand straight.",
        "Lean your torso left and right.",
        "Lift your left knee, then lower it.",
        "Lift your right knee, then lower it.",
        "Throw a few quick left and right punches.",
        "Check complete. Compare missed movements, steadiness, and delay. Choose another engine in Tracking, then repeat the sanity check."
    };
    public void Begin()
    {
        speech ??= new PracticeSpeech(); speech.Stop();
        started=Time.unscaledTime; next=0; running=true;
    }
    private void Update()
    {
        if (!running || next >= Times.Length) return;
        speech.CheckStatus();
        if(next==1)
        {
            var pose=FindFirstObjectByType<RTMPosePoseProvider>();
            if(pose != null && pose.isActiveAndEnabled && !pose.Calibrated)
            {
                Prompt="Stand upright and still · "+pose.PipelineStatus;
                if(Time.unscaledTime-started > 40) { speech.Speak("Calibration is not ready. Check that your whole body is visible, then repeat the sanity check."); Stop(); }
                return;
            }
            // The first movement starts after calibration; slow initialization cannot consume the guide.
            if(pose != null && pose.isActiveAndEnabled) started=Time.unscaledTime-Times[1];
        }
        if (Time.unscaledTime-started < Times[next]) return;
        Prompt=Cues[next]; speech.Speak(Cues[next++]);
        if(next == Times.Length) running=false;
    }
    private void OnDisable() { BodyTrackingSelection.Changed -= Stop; Stop(); }
    private void OnDestroy() { speech?.Dispose(); speech=null; }
}
