namespace FitFight.Practice
{
    /// <summary>Hands-free, observation-only body diagnostic; does not classify movements as ground truth.</summary>
    public sealed class FullBodyRecordingGuide
    {
        public const double Duration = 240;
        private static readonly PunchRecordingGuide.Cue[] Cues = {
            new PunchRecordingGuide.Cue(0, "FULL BODY · STEP BACK", "Full body test, assistance off. Step back until your head, hands and feet are visible."),
            new PunchRecordingGuide.Cue(7, "STARTING IN 3", "Three"),
            new PunchRecordingGuide.Cue(8, "STARTING IN 2", "Two"),
            new PunchRecordingGuide.Cue(9, "STARTING IN 1", "One"),
            new PunchRecordingGuide.Cue(10, "HOLD STILL · ARMS DOWN", "Stand upright with arms down. Hold still for six seconds."),
            new PunchRecordingGuide.Cue(16, "ARMS FORWARD · HOLD · LOWER", "Extend both arms forward at shoulder height. Hold for two seconds, then lower. Repeat slowly."),
            new PunchRecordingGuide.Cue(26, "ARMS OVERHEAD · HOLD · LOWER", "Raise both arms straight above your head. Hold for two seconds, then lower. Repeat."),
            new PunchRecordingGuide.Cue(38, "ARMS TO THE SIDES · T POSE", "Extend both arms to the sides at shoulder height. Hold for two seconds, then lower. Repeat."),
            new PunchRecordingGuide.Cue(50, "BEND AND STRAIGHTEN ELBOWS", "Bend both elbows to bring your hands up. Straighten forward, then sideways. Repeat slowly."),
            new PunchRecordingGuide.Cue(62, "TORSO FORWARD BEND · RETURN", "Bend your torso forward comfortably, pause, then stand upright. Keep your feet still. Repeat slowly."),
            new PunchRecordingGuide.Cue(76, "TORSO SIDE BENDS · LEFT / RIGHT", "Lean your torso left, return upright, then lean right. Keep both feet on the floor. Repeat."),
            new PunchRecordingGuide.Cue(90, "TORSO TWIST · LEFT / RIGHT", "Turn your shoulders left and right while keeping your hips facing forward. Return to center each time."),
            new PunchRecordingGuide.Cue(104, "HEAD TURN · LEFT / RIGHT", "Keep your body still. Turn your head left, then right, then look forward."),
            new PunchRecordingGuide.Cue(114, "LEFT KNEE LIFT · HOLD · LOWER", "Lift your left knee comfortably. Hold for two seconds, then lower. Keep the other foot planted. Repeat."),
            new PunchRecordingGuide.Cue(126, "RIGHT KNEE LIFT · HOLD · LOWER", "Lift your right knee comfortably. Hold for two seconds, then lower. Keep the other foot planted. Repeat."),
            new PunchRecordingGuide.Cue(138, "LEFT LEG · GENTLE FRONT EXTENSION", "Lift your left knee, extend the foot gently forward, bend the knee again, then lower. Repeat slowly."),
            new PunchRecordingGuide.Cue(152, "RIGHT LEG · GENTLE FRONT EXTENSION", "Lift your right knee, extend the foot gently forward, bend the knee again, then lower. Repeat slowly."),
            new PunchRecordingGuide.Cue(166, "LEFT LEG · SIDE LIFT", "Lift your left leg gently to the side, pause, then return. Keep your torso upright. Repeat slowly."),
            new PunchRecordingGuide.Cue(178, "RIGHT LEG · SIDE LIFT", "Lift your right leg gently to the side, pause, then return. Keep your torso upright. Repeat slowly."),
            new PunchRecordingGuide.Cue(190, "SQUAT · HOLD · STAND", "Make a comfortable shallow squat. Hold for two seconds, then stand up. Repeat slowly."),
            new PunchRecordingGuide.Cue(204, "HEEL RAISES · KEEP TOES DOWN", "Raise both heels while keeping your toes on the floor, then lower. Repeat slowly."),
            new PunchRecordingGuide.Cue(216, "RETURN TO NEUTRAL · HOLD STILL", "Stand upright again with your arms down. Hold still so we can check recovery."),
            new PunchRecordingGuide.Cue(224, "STEP OUT · RETURN · HOLD", "Step briefly out of the camera view, then return to the same standing position. Hold still.")
        };
        private int last = -1;
        private static int Index(double elapsed)
        {
            for (int i = Cues.Length - 1; i > 0; i--) if (elapsed >= Cues[i].at) return i;
            return 0;
        }
        public static string Label(double elapsed) => Cues[Index(elapsed)].label;
        public void Reset() { last = -1; }
        public bool TryAdvance(double elapsed, out PunchRecordingGuide.Cue cue)
        {
            int index = Index(elapsed); cue = Cues[index];
            if (elapsed >= Duration || index <= last) return false;
            last = index; return true;
        }
    }
}
