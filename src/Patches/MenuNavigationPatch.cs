using HarmonyLib;

namespace SliderSens.Patches
{
    // MenuNavigation.Update moves the selected slider on a gamepad. On a sensitivity slider the
    // prefix makes SliderSens's step first, and holds back the game's own step.
    [HarmonyPatch(typeof(MenuNavigation), "Update")]
    internal static class MenuNavigationPatch
    {
        private static void Prefix(MenuNavigation __instance) => GamepadFineAdjust.BeforeNavigationUpdate(__instance);
    }
}
