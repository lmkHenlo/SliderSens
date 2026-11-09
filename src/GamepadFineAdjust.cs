using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SliderSens
{
    /// <summary>
    /// Left and Right on an active sensitivity slider step it by 0.01, or 0.1 with either trigger
    /// held, in place of the game's own 5% steps. Runs at the start of the game's
    /// <c>MenuNavigation.Update</c>, which is what moves sliders on a gamepad.
    /// </summary>
    internal static class GamepadFineAdjust
    {
        internal const float Step = Sensitivity.FineStep;
        internal const float TriggerStep = 0.1f;

        /// <summary>Set by the plugin: the patch is in, and Gamepad.FineAdjust is on.</summary>
        internal static Func<bool> IsOn = () => false;

        // MenuNavigation's private fields, to hold back its own Left/Right step on a sensitivity slider.
        private static FieldInfo _previousDirection;
        private static int _horizontalBits;
        private static AccessTools.FieldRef<MenuNavigation, float> _autoShiftTimer;

        private static MenuNavigation _navigation;
        private static Slider _slider;
        private static bool _engaged;
        private static HoldRepeat _repeat;
        private static bool _failed;

        /// <summary>
        /// Finds the fields this needs in <c>MenuNavigation</c>. Throws if a game update changed
        /// them, so the patch is never applied without them.
        /// </summary>
        internal static void FindGameFields()
        {
            _previousDirection = AccessTools.Field(typeof(MenuNavigation), "previousNavigationDirection");
            if (_previousDirection == null || !_previousDirection.FieldType.IsEnum)
                throw new MissingFieldException("MenuNavigation", "previousNavigationDirection");
            _horizontalBits = Convert.ToInt32(Enum.Parse(_previousDirection.FieldType, "Horizontal"));
            if (_horizontalBits == 0)
                throw new MissingFieldException(_previousDirection.FieldType.FullName, "Horizontal");
            _autoShiftTimer = AccessTools.FieldRefAccess<MenuNavigation, float>("autoShiftTimer");
        }

        /// <summary>True while SliderSens owns Left/Right on <paramref name="slider"/>.</summary>
        internal static bool Owns(Slider slider) => _engaged && slider != null && ReferenceEquals(slider, _slider);

        /// <summary>Once a frame for every enabled <c>MenuNavigation</c>, before its own update.</summary>
        internal static void BeforeNavigationUpdate(MenuNavigation navigation)
        {
            if (_failed || !IsOn())
            {
                if (ReferenceEquals(navigation, _navigation)) Release();
                return;
            }
            try
            {
                Update(navigation);
            }
            catch (Exception e)
            {
                _failed = true;
                Release();
                Plugin.Log.LogError($"Gamepad fine adjustment is off for this session after an error: {e}");
            }
        }

        private static void Update(MenuNavigation navigation)
        {
            // The same checks the game makes before it moves a slider, so this only ever acts
            // where the game itself would.
            SensitivitySlider target = null;
            Gamepad gamepad = null;
            if (navigation.CanUpdate())
            {
                gamepad = InputManager.CurrentGamepad;
                ControllerSelectable selected = navigation.Selected;
                if (gamepad != null && selected != null && selected.IsSlider && selected.IsActiveAtAll &&
                    selected.IsEffectivelyInteractable())
                    SensitivitySliders.TryGet(selected.AsSlider, out target);
            }

            if (target == null)
            {
                if (ReferenceEquals(navigation, _navigation) || _navigation == null) Release();
                return;
            }
            if (!ReferenceEquals(navigation, _navigation) || !ReferenceEquals(target.Slider, _slider))
            {
                Release();
                _navigation = navigation;
                _slider = target.Slider;
            }

            int direction = Horizontal(gamepad);
            bool trigger = gamepad.leftTrigger.isPressed || gamepad.rightTrigger.isPressed;

            // Left and Right on a sensitivity slider are always SliderSens's steps; the game's own
            // 5% step is held back.
            _engaged = true;
            if (direction != 0)
                HoldBackGameStep(navigation);
            if (_repeat.Tick(direction, Time.unscaledDeltaTime))
                SensitivitySliders.Step(target, direction * (trigger ? TriggerStep : Step), target.SnapOnGamepad);
        }

        /// <summary>Left and Right as the game reads them: the left stick or the d-pad, left first.</summary>
        private static int Horizontal(Gamepad gamepad)
        {
            if (gamepad.leftStick.left.isPressed || gamepad.dpad.left.isPressed) return -1;
            if (gamepad.leftStick.right.isPressed || gamepad.dpad.right.isPressed) return 1;
            return 0;
        }

        /// <summary>
        /// The game counts a direction as pressed when it wasn't held last frame, or once its repeat
        /// timer runs out. Marking Left/Right as held and keeping the timer full stops both.
        /// </summary>
        private static void HoldBackGameStep(MenuNavigation navigation)
        {
            int previous = Convert.ToInt32(_previousDirection.GetValue(navigation));
            if ((previous & _horizontalBits) != _horizontalBits)
                _previousDirection.SetValue(navigation, Enum.ToObject(_previousDirection.FieldType, previous | _horizontalBits));
            _autoShiftTimer(navigation) = HoldRepeat.FirstDelay;
        }

        private static void Release()
        {
            _engaged = false;
            _navigation = null;
            _slider = null;
            _repeat.Reset();
        }
    }
}
