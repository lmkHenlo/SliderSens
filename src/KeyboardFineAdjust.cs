using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SliderSens
{
    /// <summary>
    /// The arrow keys on a settings menu's sensitivity sliders: Left and Right step the slider
    /// under the mouse pointer (or, failing that, the one last clicked) by 0.01, or 0.1 with Shift.
    /// Added to each settings menu, so it only runs while the menu is open.
    /// </summary>
    internal sealed class KeyboardFineAdjust : MonoBehaviour
    {
        internal const float Step = Sensitivity.FineStep;
        internal const float ShiftStep = 0.1f;

        /// <summary>Set by the plugin: the patches this needs are in, and Keyboard.FineAdjust is on.</summary>
        internal static Func<bool> IsOn = () => false;

        private static readonly List<KeyboardFineAdjust> Open = new List<KeyboardFineAdjust>();
        private static bool _failed;

        private SettingsMenu _menu;
        private HoldRepeat _repeat;

        internal static void AddTo(SettingsMenu menu)
        {
            if (menu == null || menu.GetComponent<KeyboardFineAdjust>() != null) return;
            menu.gameObject.AddComponent<KeyboardFineAdjust>()._menu = menu;
        }

        private void OnEnable() => Open.Add(this);

        private void OnDisable()
        {
            Open.Remove(this);
            _repeat.Reset();
        }

        private void Update()
        {
            if (_failed || !IsOn())
                return;
            try
            {
                Keyboard keyboard = Keyboard.current;
                SensitivitySlider target = keyboard != null && InputManager.UsingKeyboard ? Target() : null;
                if (target == null)
                {
                    _repeat.Reset();
                    return;
                }
                int direction = (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0);
                if (!_repeat.Tick(direction, Time.unscaledDeltaTime))
                    return;
                float step = keyboard.shiftKey.isPressed ? ShiftStep : Step;
                SensitivitySliders.Step(target, direction * step, target.SnapOnKeyboard);
            }
            catch (Exception e)
            {
                _failed = true;
                Plugin.Log.LogError($"Keyboard fine adjustment is off for this session after an error: {e}");
            }
        }

        /// <summary>
        /// True while the arrow keys belong to a sensitivity slider. The game's own arrow-key step
        /// (Unity's 10% of the slider) is held back then, on every slider, so one key press never
        /// moves two things.
        /// </summary>
        internal static bool OwnsArrowKeys()
        {
            if (_failed || !IsOn() || !InputManager.UsingKeyboard) return false;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !(keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed)) return false;
            foreach (KeyboardFineAdjust menu in Open)
                if (menu.Target() != null)
                    return true;
            return false;
        }

        /// <summary>The sensitivity slider under the mouse pointer, else the selected one, else null.</summary>
        private SensitivitySlider Target()
        {
            if (_menu == null) return null;

            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null)
            {
                // Typing in a text box: the arrow keys move the text cursor, nothing else.
                TMP_InputField input = selected.GetComponent<TMP_InputField>();
                if (input != null && input.isFocused) return null;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 pointer = mouse.position.ReadValue();
                foreach (SliderOption option in MenuSliders())
                {
                    SensitivitySlider s = Usable(option);
                    if (s != null && IsUnder(s.Slider, pointer))
                        return s;
                }
            }

            if (selected != null)
            {
                Slider slider = selected.GetComponent<Slider>();
                foreach (SliderOption option in MenuSliders())
                {
                    SensitivitySlider s = Usable(option);
                    if (s != null && ReferenceEquals(s.Slider, slider))
                        return s;
                }
            }
            return null;
        }

        private IEnumerable<SliderOption> MenuSliders()
        {
            yield return _menu.mouseSensitivity;
            yield return _menu.mouseAimSensitivity;
            yield return _menu.controllerSensitivity;
            yield return _menu.controllerAimSensitivity;
        }

        private static SensitivitySlider Usable(SliderOption option)
        {
            SensitivitySlider s = SensitivitySliders.Find(option);
            if (s == null) return null;
            Slider slider = s.Slider;
            return slider != null && slider.IsActive() && slider.IsInteractable() ? s : null;
        }

        private static bool IsUnder(Slider slider, Vector2 pointer)
        {
            RectTransform rect = slider.transform as RectTransform;
            if (rect == null) return false;
            Canvas canvas = slider.GetComponentInParent<Canvas>();
            if (canvas != null) canvas = canvas.rootCanvas;
            Camera camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(rect, pointer, camera);
        }
    }
}
