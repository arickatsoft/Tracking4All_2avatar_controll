// Standalone test harness; compile with Assets/Scripts/PoseWireFormat.cs.
using System;
using System.Globalization;
using System.Text;
using Tracking4All;
namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
}
internal static class PoseWireFormatTests
{
    private static int checks;
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception(label);
        checks++;
    }
    private static bool Parse(string value)
    {
        UnityEngine.Vector3[] world, screen;
        return PoseWireFormat.TryParse(value, out world, out screen);
    }
    private static void Main()
    {
        var section = new StringBuilder();
        for (int i = 0; i < 33; i++) section.Append(i).Append("|0.125|-0.5|1.25e-2\n");
        string part = section.ToString();
        string packet = part + "*||*" + part;
        foreach (var locale in new[] { "en-US", "de-DE", "fr-FR", "he-IL" })
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
            UnityEngine.Vector3[] world, screen;
            Check(PoseWireFormat.TryParse(packet, out world, out screen), locale);
            Check(world.Length == 33 && screen.Length == 33 && world[32].x == .125f
                && screen[0].y == -.5f, "Coordinate values " + locale);
        }
        Check(!Parse(null), "null");
        Check(!Parse(""), "empty");
        Check(!Parse(part), "missing screen section");
        Check(!Parse(packet.Replace("32|", "31|")), "duplicate indices");
        Check(!Parse(packet.Replace("32|", "33|")), "out of range");
        Check(!Parse(packet.Replace("32|0.125|-0.5|1.25e-2\n", "")), "missing landmarks");
        foreach (string bad in new[] { "NaN", "Infinity", "-Infinity", "1e99", "0,125", "abc" })
            Check(!Parse(packet.Replace("0.125", bad)), "invalid number " + bad);
        Check(!Parse(new string('x', 16385)), "oversized");
        Check(Parse(packet.Replace("\n", "\r\n")), "Python CRLF");
        Check(Parse(packet.Replace("0.125", "0")), "missing-point sentinel accepted");
        Console.WriteLine("PASS: " + checks + " pose wire format checks");
    }
}
