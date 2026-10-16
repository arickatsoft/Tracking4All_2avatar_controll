using System;
using Tracking4All;
using UnityEngine;

public static class LegMotionDeadbandTests
{
    private static int checks;
    private static void Check(bool condition, string label)
    {
        checks++;
        if (!condition) throw new Exception(label);
    }

    public static void Main()
    {
        var sample = new Vector3[8];
        var accepted = new Vector3[8];
        Check(LegMotionDeadband.Filter(sample, accepted, 0, .03f, false), "Initial pose");
        for (int i = 0; i < 1000; i++)
        {
            sample[1] = new Vector3((float)Math.Sin(i) * .01f, 0, 0);
            Check(!LegMotionDeadband.Filter(sample, accepted, 0, .03f, true), "Stationary jitter must not move leg");
        }
        for (int i = 1; i <= 6; i++)
        {
            sample[1] = new Vector3(i * .005f, 0, 0);
            bool moved = LegMotionDeadband.Filter(sample, accepted, 0, .029f, true);
            Check(moved == (i == 6), "Slow motion must accumulate against accepted pose");
        }
        sample[0] = new Vector3(0, .2f, 0);
        sample[1] = new Vector3(0, .45f, .35f);
        Check(LegMotionDeadband.Filter(sample, accepted, 0, .03f, true), "Kick must pass on first sample");
        Check(accepted[1].y == .45f && accepted[0].y == .2f, "Whole leg updated coherently");
        Check(accepted[5].y == 0f, "Other leg remains unchanged");
        sample[0] = sample[1] = Vector3.zero;
        Check(LegMotionDeadband.Filter(sample, accepted, 0, .03f, true), "Return from kick must pass");
        sample[2] = new Vector3(0, float.NaN, 0);
        Check(!LegMotionDeadband.Filter(sample, accepted, 0, .03f, true), "Invalid point must not poison accepted pose");
        sample[2] = new Vector3(0, .01f, 0);
        Check(LegMotionDeadband.Filter(sample, accepted, 0, .03f, false), "Reacquisition must reset hold");
        Check(LegMotionDeadband.Filter(sample, accepted, 0, 0f, true), "Zero threshold disables holding");
        accepted[5] = new Vector3(0, .6f, .4f);
        LegMotionDeadband.StandingFallback(accepted, 0, .8f, Vector3.up, Vector3.forward);
        Check(accepted[1].y == -.8f && accepted[2].y == accepted[3].y, "Missing first-frame leg has level grounded foot");
        Check(accepted[5].y == .6f, "Fallback must preserve opposite visible kick");
        LegMotionDeadband.StandingFallback(accepted, 4, .8f, Vector3.up, Vector3.forward);
        Check(accepted[5].y == -.8f, "Losing landmarks during a kick returns to standing");
        sample[0] = new Vector3(0, -.3f, .2f);
        sample[1] = new Vector3(0, -.4f, .5f);
        Check(LegMotionDeadband.Filter(sample, accepted, 0, .03f, false), "Visible kick reacquires immediately after fallback");
        Check(!LegMotionDeadband.IsFinite(new Vector3(float.PositiveInfinity, 0, 0)), "Infinite landmarks are invalid");
        Console.WriteLine("Leg motion checks passed: " + checks);
    }
}
