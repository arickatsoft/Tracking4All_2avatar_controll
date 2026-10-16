using System;
internal static class FitFightFramingTests
{
    static void Main()
    {
        int checks = 0;
        var random = new Random(31);
        // Aspect ratios are layout stress cases, not assertions of measured device performance.
        foreach (float aspect in new[] { 9f/19.5f, 9f/20f, 9f/16f, 19.5f/9f, 20f/9f })
        foreach (float top in new[] { .50f, .62f, .70f })
        {
            float tanY = (float)Math.Tan(24 * Math.PI / 180), tanX = tanY * aspect;
            for (int pose = 0; pose < 500; pose++)
            {
                var points = new float[8,3];
                float distance = 3.6f;
                var fit = new FitFightFramingMath.FitWindow(tanX, tanY, .86f, .92f, .84f, top);
                for (int p = 0; p < 8; p++)
                {
                    points[p,0] = (float)(random.NextDouble() * 5 - 2.5);
                    points[p,1] = (float)(random.NextDouble() * 4 - 2);
                    points[p,2] = (float)(random.NextDouble() * 6 - 3);
                    distance = Math.Max(distance, FitFightFramingMath.RequiredDistance(points[p,0], points[p,1], points[p,2], tanX, tanY, .86f, .86f, .84f, top));
                    fit.Include(points[p,0], points[p,1], points[p,2]);
                }
                float fittedDistance = fit.Distance;
                if (fittedDistance > distance + .0001f) throw new Exception("Offset fit zoomed farther than centered fit");
                for (int p = 0; p < 8; p++)
                {
                    float z = points[p,2] + distance;
                    float x = points[p,0] / (z * tanX), y = points[p,1] / (z * tanY);
                    if (z <= 0 || x < -.8601f || x > .8601f || y < -.8401f || y > top + .0001f)
                        throw new Exception("Fighter point clipped at aspect " + aspect);
                    checks++;
                    z = points[p,2] + fittedDistance;
                    x = (points[p,0] - fit.OffsetX(fittedDistance)) / (z * tanX);
                    y = (points[p,1] - fit.OffsetY(fittedDistance)) / (z * tanY);
                    if (z <= 0 || x < -.8601f || x > .9201f || y < -.8401f || y > top + .0001f)
                        throw new Exception("Offset fighter point clipped at aspect " + aspect);
                    checks++;
                }
            }
        }
        Console.WriteLine("PASS: " + checks + " projected corner checks across portrait/landscape framing cases");
    }
}
