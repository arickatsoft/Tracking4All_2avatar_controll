namespace FitFight.Practice
{
    /// <summary>One clock for visible and spoken instructions. Ten setup seconds + 65 test seconds.</summary>
    public sealed class PunchRecordingGuide
    {
        public const double Duration = 75;
        public readonly struct Cue
        {
            public readonly double at;
            public readonly string label, speech;
            public Cue(double at, string label, string speech)
            { this.at=at; this.label=label; this.speech=speech; }
        }
        private static readonly Cue[] cues = {
            new Cue(0, "SHOW SHOULDERS, HIPS AND HANDS", "Boxing test started. Step back until your shoulders, hips, hands and elbows are visible. Keep your knees soft and fists near your chin, like the avatar. Hold that guard until the countdown finishes."),
            new Cue(7, "STARTING IN 3", "Three"),
            new Cue(8, "STARTING IN 2", "Two"),
            new Cue(9, "STARTING IN 1", "One"),
            new Cue(10, "HOLD STILL · FIGHTING GUARD", "Hold your fighting guard still. Fists near your chin, knees soft."),
            new Cue(14, "HOLD GUARD", "Keep your guard. The punches start next."),
            new Cue(18, "LEFT PUNCHES · SLOW", "Throw two slow left punches toward the camera. Return to guard after each."),
            new Cue(26, "LEFT PUNCHES · FAST", "Throw four quick left punches. Return to guard after each."),
            new Cue(34, "RIGHT PUNCHES · SLOW", "Throw two slow right punches toward the camera. Return to guard after each."),
            new Cue(42, "RIGHT PUNCHES · FAST", "Throw four quick right punches. Return to guard after each."),
            new Cue(50, "ALTERNATE LEFT / RIGHT", "Alternate left and right for six punches. Return each fist to guard."),
            new Cue(58, "START A PUNCH, THEN WITHDRAW", "Start a punch, then pull it back before extending fully. Repeat."),
            new Cue(65, "RAISE AND LOWER YOUR HANDS", "Raise both hands above your head, then lower them. Do not punch."),
            new Cue(70, "STEP OUT, THEN RETURN", "Step out of the camera view briefly, then return.")
        };
        private int last = -1;
        private static int Index(double elapsed)
        {
            for(int i=cues.Length-1;i>0;i--) if(elapsed>=cues[i].at) return i;
            return 0;
        }
        public static string Label(double elapsed) => cues[Index(elapsed)].label;
        public void Reset() { last=-1; }
        public bool TryAdvance(double elapsed, out Cue cue)
        {
            int index=Index(elapsed); cue=cues[index];
            // A slow frame skips expired instructions instead of building a speech backlog.
            if(elapsed>=Duration || index<=last) return false;
            last=index;return true;
        }
    }
}
