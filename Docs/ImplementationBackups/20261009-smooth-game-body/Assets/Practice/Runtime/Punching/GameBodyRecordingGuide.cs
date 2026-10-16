namespace FitFight.Practice
{
    /// <summary>One 164-second game-mode run. Cues label evaluation windows, never force actions.</summary>
    public sealed class GameBodyRecordingGuide
    {
        public const double Duration=164;
        public const double MaxCalibrationPause=16;
        public static readonly PunchRecordingGuide.Cue[] Cues = {
            new PunchRecordingGuide.Cue(0,"GAME BODY · STEP BACK","Step back. Keep your whole body and raised hands visible. Leave room to kick. Follow my voice."),
            new PunchRecordingGuide.Cue(7,"STARTING IN 3","Three"),
            new PunchRecordingGuide.Cue(8,"STARTING IN 2","Two"),
            new PunchRecordingGuide.Cue(9,"STARTING IN 1","One"),
            new PunchRecordingGuide.Cue(10,"CALIBRATE · STAND STILL","Stand straight with arms down. Hold still."),
            new PunchRecordingGuide.Cue(16,"FIGHTING GUARD · HOLD","Take fighting guard. Knees soft, fists near your chin. Hold."),
            new PunchRecordingGuide.Cue(22,"SLOW PUNCHES · LEFT / RIGHT","Two slow left punches, then two right. Return to guard each time."),
            new PunchRecordingGuide.Cue(30,"FAST PUNCHES · ALTERNATE","Six quick alternating punches. Left, right. Return to guard."),
            new PunchRecordingGuide.Cue(40,"BOTH ARMS FORWARD · BEND ELBOWS","Extend both arms forward. Bend both elbows, then straighten. Repeat twice."),
            new PunchRecordingGuide.Cue(48,"BOTH ARMS OVERHEAD · LOWER","Raise both arms overhead, pause, then lower. Repeat twice."),
            new PunchRecordingGuide.Cue(56,"BOTH ARMS TO SIDES · LOWER","Stretch both arms to the sides. Bend the elbows, straighten, then lower."),
            new PunchRecordingGuide.Cue(64,"HEAD TURN · NOD","Keep your body still. Turn your head left, right, center. Nod twice."),
            new PunchRecordingGuide.Cue(74,"TORSO FORWARD BENDS","Bend forward comfortably, then stand tall. Repeat twice. Keep feet down."),
            new PunchRecordingGuide.Cue(84,"TORSO SIDE BENDS · TWIST","Lean left, then right. Then twist your shoulders left and right."),
            new PunchRecordingGuide.Cue(96,"SQUAT · HOLD · STAND","Three comfortable squats. Pause at the bottom, then stand. Keep both feet down."),
            new PunchRecordingGuide.Cue(108,"LEFT KNEE LIFT · HOLD","Back to guard. Lift your left knee slowly, hold, then lower. Do not strike."),
            new PunchRecordingGuide.Cue(114,"RIGHT KNEE LIFT · HOLD","Lift your right knee slowly, hold, then lower. Do not strike."),
            new PunchRecordingGuide.Cue(120,"KNEE STRIKES · LEFT / RIGHT","Drive your left knee forward twice, then your right twice. Return each time."),
            new PunchRecordingGuide.Cue(130,"LEFT FRONT KICKS","Two controlled left front kicks. Lift the knee, extend, fold, then lower."),
            new PunchRecordingGuide.Cue(138,"RIGHT FRONT KICKS","Two controlled right front kicks. Lift, extend, fold, then lower."),
            new PunchRecordingGuide.Cue(146,"SIDE KICKS · LEFT / RIGHT","One low left side kick, then one right. Return to guard after each."),
            new PunchRecordingGuide.Cue(156,"QUIET GUARD · RECOVERY","Hold your fighting guard still. Relax. The test is nearly finished.")
        };
        private int last=-1;
        private static int Index(double elapsed)
        { for(int i=Cues.Length-1;i>0;i--) if(elapsed>=Cues[i].at) return i;return 0; }
        public static string Label(double elapsed) => Cues[Index(elapsed)].label;
        public void Reset() { last=-1; }
        public bool TryAdvance(double elapsed,out PunchRecordingGuide.Cue cue)
        {
            int index=Index(elapsed);cue=Cues[index];
            if(elapsed>=Duration || index<=last) return false;
            last=index;return true;
        }
    }
}
