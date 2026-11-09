using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace SliderSens
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.slidersens";
        public const string Name = "SliderSens";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;

        /// <summary>True once the sensitivity patches are in, until an error turns them off.</summary>
        internal static bool Running;

        // ---- Config -------------------------------------------------------
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> KeyboardFineAdjustSetting;
        internal static ConfigEntry<bool> GamepadFineAdjustSetting;

        /// <summary>
        /// The 0.01 steps. Applied first and all together: if one fails, both are removed again and
        /// the game keeps its own sliders.
        /// </summary>
        internal static readonly PatchModule[] CorePatches =
        {
            new PatchModule(typeof(Patches.SliderLoadPatch), "Saved sensitivity on load"),
            new PatchModule(typeof(Patches.SensitivityValuePatch), "Sensitivity sliders: 0.01 steps"),
        };

        /// <summary>Holds back Unity's own Left/Right slider step while a fine step owns it.</summary>
        internal static readonly PatchModule MovePatch =
            new PatchModule(typeof(Patches.SliderMovePatch), "Arrow keys: the game's own slider step held back");

        /// <summary>The arrow keys on the sensitivity sliders. Optional: if it fails, only the arrow keys are off.</summary>
        internal static readonly PatchModule KeyboardPatch =
            new PatchModule(typeof(Patches.SettingsMenuPatch), "Arrow keys: fine adjustment");

        /// <summary>The gamepad fine steps. Optional: if it fails, only the gamepad steps are off.</summary>
        internal static readonly PatchModule GamepadPatch =
            new PatchModule(typeof(Patches.MenuNavigationPatch), "Gamepad: fine steps");

        private void Awake()
        {
            Log = Logger;
            BindConfig(Config);

            if (!Enabled.Value)
            {
                Log.LogInfo($"{Name} {Version} loaded. Off (General.Enabled = false): the game's own sensitivity sliders.");
                return;
            }

            var harmony = new Harmony(Guid);
            if (!ApplyCore(harmony, Log))
                return;
            Log.LogInfo($"{Name} {Version} loaded. Sensitivity sliders in steps of {Sensitivity.FineStep:0.00}.");
            ApplyFineAdjust(harmony, KeyboardFineAdjustSetting.Value, GamepadFineAdjustSetting.Value, Log);
        }

        /// <summary>
        /// Applies the 0.01 steps, or nothing: if a patch fails, the ones already applied are
        /// removed again and the game runs as if the mod weren't installed.
        /// </summary>
        /// <returns>True if every core patch was applied.</returns>
        internal static bool ApplyCore(Harmony harmony, ManualLogSource log)
        {
            try
            {
                SensitivitySliders.UseGameConversions();
            }
            catch (Exception e)
            {
                log.LogError($"[FAILED] The game's sensitivity slider curve: {e.Message}");
                log.LogError($"{Name} is off: the game's sensitivity sliders have changed. The game keeps its own sliders.");
                return false;
            }

            bool allApplied = true;
            foreach (PatchModule patch in CorePatches)
                allApplied &= patch.TryApply(harmony, log);
            if (allApplied)
            {
                Running = true;
                return true;
            }

            try
            {
                harmony.UnpatchSelf();
                log.LogError($"{Name} is off: a patch failed (see [FAILED] above), so every patch was removed " +
                             "again. The game keeps its own sensitivity sliders.");
            }
            catch (Exception e)
            {
                log.LogError($"{Name} is off: a patch failed (see [FAILED] above), and removing the others " +
                             $"failed too: {e}");
            }
            return false;
        }

        /// <summary>Applies the arrow keys and the gamepad steps, each on its own.</summary>
        internal static void ApplyFineAdjust(Harmony harmony, bool keyboard, bool gamepad, ManualLogSource log)
        {
            KeyboardFineAdjust.IsOn = () => false;
            GamepadFineAdjust.IsOn = () => false;

            bool moveGuard = (keyboard || gamepad) && MovePatch.TryApply(harmony, log);

            if (!keyboard)
                log.LogInfo("Arrow keys off (Keyboard.FineAdjust = false).");
            else if (!moveGuard)
                log.LogWarning("Arrow keys are off: without the patch above, one key press could move a slider twice. " +
                               "The 0.01 steps and the rest of the mod work as before.");
            else if (KeyboardPatch.TryApply(harmony, log))
                KeyboardFineAdjust.IsOn = () => Running;
            else
                log.LogWarning("Arrow keys are off: their patch failed (see [FAILED] above). The 0.01 steps and the " +
                               "rest of the mod work as before.");

            if (!gamepad)
            {
                log.LogInfo("Gamepad fine steps off (Gamepad.FineAdjust = false).");
                return;
            }
            try
            {
                GamepadFineAdjust.FindGameFields();
            }
            catch (Exception e)
            {
                log.LogError($"[FAILED] {GamepadPatch.Label}: the game's menu navigation has changed ({e.Message})");
                log.LogWarning("Gamepad fine steps are off. The 0.01 steps and the rest of the mod work as before.");
                return;
            }
            if (GamepadPatch.TryApply(harmony, log))
                GamepadFineAdjust.IsOn = () => Running;
            else
                log.LogWarning("Gamepad fine steps are off: their patch failed (see [FAILED] above). The 0.01 " +
                               "steps and the rest of the mod work as before.");
        }

        /// <summary>
        /// An error while the game runs: the game's own sensitivity sliders take over for the rest
        /// of the session. Values already set stay as they are.
        /// </summary>
        internal static void Stop(string label, Exception e)
        {
            if (!Running) return;
            Running = false;
            Log.LogError($"{Name} is off for this session after an error in \"{label}\"; the game's own sensitivity " +
                         $"sliders take over: {e}");
        }

        internal static void BindConfig(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true,
                "Sensitivity sliders in steps of 0.01, with two decimal places on screen. Off = the game's own " +
                "sliders, with one decimal place.");

            KeyboardFineAdjustSetting = config.Bind("Keyboard", "FineAdjust", true,
                "The Left and Right arrow keys step the sensitivity slider under the mouse pointer (or, if there " +
                "isn't one, the one you last clicked) by 0.01; hold Shift for 0.1. Off = the arrow keys do what " +
                "they do in the game.");

            GamepadFineAdjustSetting = config.Bind("Gamepad", "FineAdjust", true,
                "On a gamepad, Left and Right move a sensitivity slider by 0.01; hold either trigger for 0.1. Off = " +
                "the game's own steps (5% of the slider).");
        }
    }

    /// <summary>One Harmony patch class and the name it's logged under.</summary>
    internal sealed class PatchModule
    {
        internal readonly Type Type;
        internal readonly string Label;

        internal PatchModule(Type type, string label)
        {
            Type = type;
            Label = label;
        }

        /// <returns>True if the patch was applied.</returns>
        internal bool TryApply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                var patched = harmony.CreateClassProcessor(Type).Patch();
                if (patched == null || patched.Count == 0)
                {
                    log.LogError($"[FAILED] {Label}: no game method to patch");
                    return false;
                }
                log.LogInfo($"[ok] {Label}");
                return true;
            }
            catch (Exception e)
            {
                log.LogError($"[FAILED] {Label}: {e}");
                return false;
            }
        }
    }
}
