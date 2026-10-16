using System;
using System.Globalization;
using UnityEngine;

namespace Tracking4All
{
    /// <summary>Legacy Python-compatible pose payload. A frame must contain both complete sets.</summary>
    internal static class PoseWireFormat
    {
        public static bool TryParse(string message, out Vector3[] world, out Vector3[] screen)
        {
            world = screen = null;
            if (string.IsNullOrEmpty(message) || message.Length > 16384) return false;
            var parts = message.Split(new[] { "*||*" }, StringSplitOptions.None);
            if (parts.Length != 2) return false;
            Vector3[] parsedWorld, parsedScreen;
            if (!TryLandmarks(parts[0], out parsedWorld) || !TryLandmarks(parts[1], out parsedScreen))
                return false;
            world = parsedWorld;
            screen = parsedScreen;
            return true;
        }

        private static bool TryLandmarks(string text, out Vector3[] points)
        {
            points = null;
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length != 33) return false;
            var result = new Vector3[33];
            ulong seen = 0;
            foreach (var line in lines)
            {
                var values = line.Split('|');
                int index;
                float x, y, z;
                if (values.Length != 4 || !int.TryParse(values[0], out index) || index < 0 || index >= 33
                    || (seen & (1UL << index)) != 0 || !TryFloat(values[1], out x)
                    || !TryFloat(values[2], out y) || !TryFloat(values[3], out z)) return false;
                seen |= 1UL << index;
                result[index] = new Vector3(x, y, z);
            }
            points = result;
            return true;
        }

        private static bool TryFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
