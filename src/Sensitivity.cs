using System;
using System.Collections.Generic;

namespace SliderSens
{
    /// <summary>
    /// The sensitivity numbers: the 0.01 grid, the game's own slider curve and its snap to 1.0x.
    /// Only maths on floats, so every case can be checked without the game running.
    /// </summary>
    internal static class Sensitivity
    {
        /// <summary>Decimal places on screen and in the setting. The game shows one.</summary>
        internal const int Decimals = 2;

        /// <summary>Grid steps per 1.0x (10 ^ <see cref="Decimals"/>).</summary>
        internal const int StepsPerUnit = 100;

        /// <summary>The finest step: 0.01x.</summary>
        internal const float FineStep = 0.01f;

        /// <summary>The label, as the game's own <c>"{0:0.0}x"</c> with one more place.</summary>
        internal const string LabelFormat = "{0:0.00}x";

        /// <summary>The value at the middle of every sensitivity slider (the game's own "middle" argument).</summary>
        internal const float Middle = 1f;

        /// <summary>
        /// Where the game snaps a slider to, and how close it must be (<c>SliderOption.SnapToMiddle</c>):
        /// half way, from 0.45 to 0.55 of the slider.
        /// </summary>
        internal const float SnapPosition = 0.5f;
        internal const float SnapTolerance = 0.05f;

        /// <summary>
        /// A loaded value this close to the 0.01 grid was set on it. The slider's round trip moves a
        /// value by about a millionth, so anything further away was set some other way and is kept.
        /// </summary>
        internal const float GridTolerance = 5e-5f;

        /// <summary>The game's slider position (0-1) to sensitivity: <c>SliderOption.RemapSliderValueToValueMiddleLinear</c>.</summary>
        internal static Func<float, float, float, float, float> ToValue;

        /// <summary>The game's sensitivity to slider position: <c>SliderOption.RemapValueSliderValueMiddleLinear</c>.</summary>
        internal static Func<float, float, float, float, float> ToPosition;

        // The snap zone's edges for each (min, max): the nearest grid values the game doesn't snap.
        private static readonly Dictionary<long, float[]> Edges = new Dictionary<long, float[]>();

        /// <summary>The nearest value on the 0.01 grid, kept between <paramref name="min"/> and <paramref name="max"/>.</summary>
        internal static float Round(float value, float min, float max)
        {
            if (float.IsNaN(value))
                value = Middle;
            double steps = Math.Round((double)value * StepsPerUnit, MidpointRounding.AwayFromZero);
            // Both sides are whole numbers a float holds exactly, so the division gives the float
            // nearest to the decimal (1.23f for 123 / 100).
            float rounded = (float)steps / StepsPerUnit;
            if (rounded < min) return min;
            if (rounded > max) return max;
            return rounded;
        }

        /// <summary>
        /// A saved value as the settings menu loads it: put back on the grid if it was set on it,
        /// otherwise left exactly as the game reads it (a value dragged without the mod, or typed in).
        /// </summary>
        internal static float FromLoaded(float value, float min, float max)
        {
            float rounded = Round(value, min, max);
            return Math.Abs(value - rounded) <= GridTolerance ? rounded : value;
        }

        /// <summary>The slider position the game shows <paramref name="value"/> at.</summary>
        internal static float Position(float value, float min, float max) => ToPosition(value, min, max, Middle);

        /// <summary>The sensitivity the game reads from slider position <paramref name="position"/>.</summary>
        internal static float Value(float position, float min, float max) => ToValue(position, min, max, Middle);

        /// <summary>True if the game's mouse-and-keyboard snap would turn <paramref name="value"/> into 1.0x.</summary>
        internal static bool InSnapZone(float value, float min, float max) =>
            Snaps(Position(value, min, max));

        /// <summary>The game's own test (<c>SliderOption.Snap</c>): within the tolerance of the middle.</summary>
        internal static bool Snaps(float position) =>
            FloatExtensions.Approximately(position, SnapPosition, SnapTolerance);

        /// <summary>
        /// The value one fine-adjust step from <paramref name="current"/>. With <paramref name="snaps"/>
        /// (the game's mouse-and-keyboard snap), the snap zone around 1.0x counts as a single step:
        /// stepping into it lands on 1.00x, stepping on from 1.00x lands on the zone's far edge.
        /// Without it, every value on the grid can be reached.
        /// </summary>
        internal static float Step(float current, float delta, float min, float max, bool snaps)
        {
            float from = Round(current, min, max);
            float target = Round(current + delta, min, max);
            if (!snaps || target == Middle || !InSnapZone(target, min, max))
                return target;

            float[] edges = SnapZoneEdges(min, max);
            if (delta > 0f)
                return from < Middle ? Middle : edges[1];
            return from > Middle ? Middle : edges[0];
        }

        /// <summary>
        /// The largest grid value below 1.0x and the smallest above it that the game doesn't snap,
        /// worked out with the game's own conversion and test.
        /// </summary>
        internal static float[] SnapZoneEdges(float min, float max)
        {
            long key = ((long)Math.Round(min * StepsPerUnit) << 32) | (uint)Math.Round(max * StepsPerUnit);
            if (Edges.TryGetValue(key, out float[] edges))
                return edges;

            int middle = (int)Math.Round(Middle * StepsPerUnit);
            int lowest = (int)Math.Round(min * StepsPerUnit);
            int highest = (int)Math.Round(max * StepsPerUnit);
            float below = min, above = max;
            for (int k = middle - 1; k >= lowest; k--)
            {
                float v = (float)k / StepsPerUnit;
                if (!InSnapZone(v, min, max)) { below = v; break; }
            }
            for (int k = middle + 1; k <= highest; k++)
            {
                float v = (float)k / StepsPerUnit;
                if (!InSnapZone(v, min, max)) { above = v; break; }
            }
            edges = new[] { below, above };
            Edges[key] = edges;
            return edges;
        }
    }
}
