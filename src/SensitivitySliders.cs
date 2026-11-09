using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine.UI;

namespace SliderSens
{
    /// <summary>One of the game's sensitivity sliders, with the range and snap flags the game gave it.</summary>
    internal sealed class SensitivitySlider
    {
        internal readonly SliderOption Option;
        internal float Min;
        internal float Max;
        internal bool SnapOnKeyboard;
        internal bool SnapOnGamepad;

        internal SensitivitySlider(SliderOption option) => Option = option;

        internal Slider Slider => Option == null ? null : Option.Slider;
    }

    /// <summary>
    /// The settings menus' sensitivity sliders and what the mod does with them. A slider counts as
    /// one as soon as the game calls <c>SliderOption.SetSensitivityValue</c> on it, which the
    /// settings menu does for all four (mouse, mouse aim, controller, controller aim) when it opens.
    /// </summary>
    internal static class SensitivitySliders
    {
        private static readonly List<SensitivitySlider> Known = new List<SensitivitySlider>();

        /// <summary>The slider whose saved value the settings menu is loading (<c>SliderOption.Initialize</c>), or null.</summary>
        internal static SliderOption Loading;

        /// <summary>
        /// Points <see cref="Sensitivity"/> at the game's own conversions between slider position
        /// and sensitivity.
        /// </summary>
        internal static void UseGameConversions()
        {
            MethodInfo toValue = AccessTools.Method(typeof(SliderOption), "RemapSliderValueToValueMiddleLinear",
                new[] { typeof(float), typeof(float), typeof(float), typeof(float) });
            if (toValue == null || !toValue.IsStatic || toValue.ReturnType != typeof(float))
                throw new MissingMethodException("SliderOption", "RemapSliderValueToValueMiddleLinear(float, float, float, float)");
            Sensitivity.ToValue = (Func<float, float, float, float, float>)Delegate.CreateDelegate(
                typeof(Func<float, float, float, float, float>), toValue);
            Sensitivity.ToPosition = SliderOption.RemapValueSliderValueMiddleLinear;
        }

        /// <summary>The entry for <paramref name="option"/>, created or updated with the game's arguments.</summary>
        internal static SensitivitySlider Register(SliderOption option, float min, float max, bool snapOnKeyboard, bool snapOnGamepad)
        {
            SensitivitySlider entry = Find(option);
            if (entry == null)
            {
                Known.RemoveAll(s => s.Option == null); // destroyed menus
                entry = new SensitivitySlider(option);
                Known.Add(entry);
                KeepOnOneLine(option.label);
            }
            entry.Min = min;
            entry.Max = max;
            entry.SnapOnKeyboard = snapOnKeyboard;
            entry.SnapOnGamepad = snapOnGamepad;
            return entry;
        }

        /// <summary>
        /// <c>0.00x</c> is a character longer than the game's <c>0.0x</c>, and the game's label box
        /// is narrow enough that a longer label wraps the x onto a second line. The label is kept on one
        /// line instead, at the game's own font size; it may run a little past its box, into the gap
        /// the menu leaves around it. Only looks: if it fails, the label wraps as before.
        /// </summary>
        private static void KeepOnOneLine(TMP_Text label)
        {
            if (label == null) return;
            try
            {
                if (label.textWrappingMode == TextWrappingModes.Normal)
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                else if (label.textWrappingMode == TextWrappingModes.PreserveWhitespace)
                    label.textWrappingMode = TextWrappingModes.PreserveWhitespaceNoWrap;
                // On one line, any other overflow mode would cut the text short instead.
                if (label.overflowMode != TextOverflowModes.Overflow)
                    label.overflowMode = TextOverflowModes.Overflow;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Sensitivity label: couldn't keep it on one line ({e.Message}).");
            }
        }

        internal static SensitivitySlider Find(SliderOption option)
        {
            if (option == null) return null;
            foreach (SensitivitySlider s in Known)
                if (ReferenceEquals(s.Option, option))
                    return s;
            return null;
        }

        internal static bool TryGet(Slider slider, out SensitivitySlider found)
        {
            found = null;
            if (slider == null) return false;
            foreach (SensitivitySlider s in Known)
                if (s.Option != null && ReferenceEquals(s.Option.Slider, slider))
                {
                    found = s;
                    return true;
                }
            return false;
        }

        /// <summary>
        /// Stands in for <c>SliderOption.SetSensitivityValue</c>: the game's own steps (snap on
        /// mouse and keyboard, the slider's curve), then the value on the 0.01 grid and a label
        /// with two decimal places.
        /// </summary>
        /// <returns>The sensitivity the game stores.</returns>
        internal static float SetValue(SliderOption option, float min, float max, bool snapOnKeyboard, bool snapOnGamepad)
        {
            Register(option, min, max, snapOnKeyboard, snapOnGamepad);
            Slider slider = option.Slider;
            bool loading = ReferenceEquals(Loading, option);

            // The game's snap to the middle, skipped while the menu loads the saved value so a
            // value near 1.0x that was set on a gamepad isn't reset just by opening the menu.
            if (!loading && (InputManager.UsingKeyboard ? snapOnKeyboard : snapOnGamepad))
            {
                float position = slider.value;
                option.valueWithoutNotify = Sensitivity.Snaps(position) ? Sensitivity.SnapPosition : position;
            }

            float read = Sensitivity.Value(slider.value, min, max);
            float value = loading ? Sensitivity.FromLoaded(read, min, max) : Sensitivity.Round(read, min, max);
            option.SetValueText(string.Format(Sensitivity.LabelFormat, value));
            return value;
        }

        /// <summary>
        /// Moves <paramref name="s"/> one fine-adjust step. The slider is moved like a drag, so the
        /// game itself stores the setting, updates the label and plays its slider sound.
        /// </summary>
        internal static void Step(SensitivitySlider s, float delta, bool snaps)
        {
            Slider slider = s.Slider;
            if (slider == null) return;
            float current = Sensitivity.Value(slider.value, s.Min, s.Max);
            float target = Sensitivity.Step(current, delta, s.Min, s.Max, snaps);
            float position = Sensitivity.Position(target, s.Min, s.Max);
            if (position != slider.value)
                slider.value = position;
        }
    }
}
