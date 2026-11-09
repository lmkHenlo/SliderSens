using HarmonyLib;

namespace SliderSens.Patches
{
    // Each settings menu gets the arrow-key handler once its sliders are set up.
    [HarmonyPatch(typeof(SettingsMenu), "Start")]
    internal static class SettingsMenuPatch
    {
        private static void Postfix(SettingsMenu __instance) => KeyboardFineAdjust.AddTo(__instance);
    }
}
