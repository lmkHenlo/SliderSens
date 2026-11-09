using System;
using HarmonyLib;

namespace SliderSens.Patches
{
    // SliderOption.SetSensitivityValue is the one method behind all four sensitivity sliders: it
    // snaps the slider, reads the sensitivity from it, writes the label and returns the value the
    // settings menu stores. This prefix does the same with the value on the 0.01 grid and a label
    // with two decimal places. Parameters are bound by position (min, max, snap on keyboard,
    // snap on gamepad), so renamed parameters don't matter.
    [HarmonyPatch(typeof(SliderOption), nameof(SliderOption.SetSensitivityValue))]
    internal static class SensitivityValuePatch
    {
        private static bool Prefix(SliderOption __instance, float __0, float __1, bool __2, bool __3, ref float __result)
        {
            if (!Plugin.Running) return true;
            try
            {
                __result = SensitivitySliders.SetValue(__instance, __0, __1, __2, __3);
                return false;
            }
            catch (Exception e)
            {
                // The game's own method runs instead, now and for the rest of the session.
                Plugin.Stop("Sensitivity sliders", e);
                return true;
            }
        }
    }
}
