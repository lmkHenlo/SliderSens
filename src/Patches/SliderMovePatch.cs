using System;
using HarmonyLib;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SliderSens.Patches
{
    // Unity's own Left/Right on a selected slider (Slider.OnMove, 10% of the slider) is held back
    // while the arrow keys or the gamepad step a sensitivity slider, so one press never moves
    // two things. Every other move goes through untouched.
    [HarmonyPatch(typeof(Slider), nameof(Slider.OnMove))]
    internal static class SliderMovePatch
    {
        private static bool Prefix(Slider __instance, AxisEventData __0)
        {
            if (__0 == null || (__0.moveDir != MoveDirection.Left && __0.moveDir != MoveDirection.Right))
                return true;
            try
            {
                return !(KeyboardFineAdjust.OwnsArrowKeys() || GamepadFineAdjust.Owns(__instance));
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
