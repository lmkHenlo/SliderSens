using System;
using HarmonyLib;

namespace SliderSens.Patches
{
    // SliderOption.Initialize puts a saved value into a slider when the settings menu opens, and
    // calls SetSensitivityValue for it. Marking that call lets SensitivitySliders.SetValue keep the
    // saved value as it is, instead of treating it like a drag.
    [HarmonyPatch(typeof(SliderOption), nameof(SliderOption.Initialize))]
    internal static class SliderLoadPatch
    {
        private static void Prefix(SliderOption __instance) => SensitivitySliders.Loading = __instance;

        private static Exception Finalizer(Exception __exception)
        {
            SensitivitySliders.Loading = null;
            return __exception;
        }
    }
}
