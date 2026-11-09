# SliderSens

[![license](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](LICENSE)
[![standard-readme compliant](https://img.shields.io/badge/readme%20style-standard-brightgreen.svg?style=flat-square)](https://github.com/RichardLitt/standard-readme)

Adds a second decimal place to the sensitivity sliders in Super Battle Golf. Client-side: only you need it.

The game's four sensitivity sliders (mouse, mouse aim, controller and controller aim) show their value to one decimal place, and can't be set much finer than that. With SliderSens they go in steps of 0.01 and show two decimal places, from 0.10x to 5.00x, and the game stores and uses exactly the value shown. The range, the default of 1.00x, the slider's curve and the game's own snap to 1.00x stay as they are. The arrow keys, or Left and Right on a gamepad, move a slider by 0.01 at a time. It only changes your own settings: nothing is sent over the network, and the host and other players don't need the mod.

## Table of Contents

- [Background](#background)
- [Install](#install)
- [Usage](#usage)
- [How it works](#how-it-works)
- [Known issues](#known-issues)
- [Building from source](#building-from-source)
- [Verification](#verification)
- [Test plan](#test-plan)
- [Files](#files)
- [API](#api)
- [Maintainers](#maintainers)
- [Contributing](#contributing)
- [License](#license)

## Background

Each of the game's sensitivity settings is a plain number that multiplies how far the camera turns for a given mouse movement or stick push (`GameSettings.ControlSettings.AxisSetting.ScaleInput`). The number itself has full precision. What limits it is the slider in front of it:

| | The game | With SliderSens |
|---|---|---|
| Range | 0.1x to 5.0x | 0.10x to 5.00x |
| Default | 1.0x, in the middle of the slider | 1.00x, in the middle of the slider |
| Slider curve | Left half 0.1x to 1.0x, right half 1.0x to 5.0x | The same |
| Shown on screen | One decimal place (`1.2x`) | Two decimal places (`1.23x`) |
| Stored | Wherever the slider ended up, often not what the label says (0.19x shows as `0.2x`) | Exactly the value shown, on a 0.01 grid |
| Mouse drag | One pixel at a time: on a 300-pixel slider, about 0.006x a pixel on the left half and 0.027x on the right | The same, then rounded to 0.01 |
| Gamepad | 5% of the slider a press: 0.09x on the left half, 0.4x on the right | 0.01 a press, 0.1 with either trigger held |
| Keyboard | Unity's own Left and Right on a selected slider: 10% of the slider, or on to the next control | 0.01 a press, 0.1 with Shift |
| Snap | With mouse and keyboard, anything from 0.91x to just under 1.4x snaps to 1.0x | The same |

The snap is the game's way of making 1.0x easy to hit with a mouse: any slider position within 5% of the middle becomes exactly 1.0x. SliderSens keeps it, so with mouse and keyboard 48 of the 491 values between 0.10x and 5.00x (0.91x to 0.99x and 1.01x to 1.39x) still become 1.00x. A gamepad doesn't snap, in the game or with the mod, so it reaches every value.

The game runs on Unity (Mono). SliderSens is a BepInEx 5 plugin; the HarmonyX library that ships with BepInEx applies its patches at runtime, without changing any game files.

### See Also

- [BepInEx](https://github.com/BepInEx/BepInEx), the mod loader
- [Harmony](https://github.com/pardeike/Harmony), the runtime patching library (BepInEx ships the HarmonyX fork)
- [REVIEW.md](REVIEW.md), the code review and the open risks still to confirm in game
- [CHANGELOG.md](CHANGELOG.md), the changes in each release

## Install

### Dependencies

- [BepInEx 5](https://github.com/BepInEx/BepInEx) for Windows x64. On Thunderstore this is `BepInEx-BepInExPack-5.4.2305`.
- **Client-side only.** Only the players who want finer sensitivity need the mod. It works in any lobby, whoever hosts and whatever they have installed, and other players see no difference.

### With a mod manager

Install `SliderSens` from Thunderstore with r2modman or another Thunderstore mod manager. BepInEx is installed with it as a dependency.

### By hand

Install BepInEx 5 into the game folder and start the game once. Then copy the DLL into the plugins folder:

```
Super Battle Golf/
└── BepInEx/
    └── plugins/
        └── SliderSens/
            └── SliderSens.dll
```

## Usage

Open **Settings > Controls**. Under **Camera**, the **Sensitivity** and **Aim Sensitivity** sliders for **Mouse and Keyboard** and for **Controller** now show two decimal places, and whatever they show is what the game uses.

- **Mouse:** drag the slider close to the value you want, then fine-tune with the arrow keys.
- **Arrow keys:** with the mouse pointer over a sensitivity slider (or, if it isn't over one, after clicking one), **Left** and **Right** move it by 0.01. Hold **Shift** for 0.1. Holding a key repeats it, with the game's own menu timing: once, then after 0.4 seconds ten times a second.
- **Gamepad:** move to a sensitivity slider as usual. **Left** and **Right** move it by 0.01. Hold either **trigger** for steps of 0.1.

The game saves the values in its own settings file, so they stay after a restart.

With mouse and keyboard, the game's snap to 1.00x still works: a value from 0.91x to 1.39x becomes 1.00x. The arrow keys count that stretch as a single step, so from 0.90x one press up gives 1.00x and the next gives 1.40x, and the same on the way down. A gamepad doesn't snap, so to set a value in that stretch, use a gamepad. SliderSens keeps it when the game next sets up its settings menu (see [How it works](#how-it-works)), as long as you don't move that slider with the mouse or the arrow keys.

The settings live in `BepInEx/config/com.slidersens.cfg`, which is created on first launch. With r2modman you can also edit them under **Config editor**. Changes apply the next time the game starts. These are the defaults:

```ini
[Gamepad]
## Left and Right on a sensitivity slider: 0.01; either trigger 0.1.
FineAdjust = true

[General]
## Off = the game's own sliders, with one decimal place.
Enabled = true

[Keyboard]
## Left and Right on a sensitivity slider: 0.01; Shift 0.1.
FineAdjust = true
```

To check that the mod loaded, open `BepInEx/LogOutput.log`. It should show `[ok] Saved sensitivity on load`, `[ok] Sensitivity sliders: 0.01 steps`, `SliderSens 1.0.0 loaded. Sensitivity sliders in steps of 0.01.`, then `[ok] Arrow keys: the game's own slider step held back`, `[ok] Arrow keys: fine adjustment` and `[ok] Gamepad: fine steps`.

**Removing it.** Turn `General.Enabled` off or uninstall the mod, and the values you set stay in the game's settings file; the game shows them to one decimal place again. The game's own snap also applies when the game sets up its settings menu with mouse and keyboard, so a value from 0.91x to just under 1.4x then goes back to 1.0x. Values outside that stretch stay as they are.

## How it works

- **The value.** All four sliders go through one game method, `SliderOption.SetSensitivityValue(min, max, snapOnKeyboard, snapOnGamepad)`, called with 0.1 and 5 every time a slider moves. It snaps the slider to the middle (with mouse and keyboard), reads the sensitivity off the slider, writes the label and returns the value the settings menu stores. A prefix does the same steps in the same order with the game's own code (its snap test, and its private `RemapSliderValueToValueMiddleLinear` for the curve), then rounds the value to the nearest 0.01 and writes the label as `0.00x` instead of `0.0x`. The rounded value is the float nearest the decimal (`1.23f`), so it's the same value every time it's worked out. Parameters are bound by position, so renamed parameters don't matter.
- **The label.** The box the game gives each label is sized for `1.0x`, and a longer label can wrap its `x` onto a second line (an earlier build with three decimal places did). When SliderSens first sees a sensitivity slider, it sets its label to stay on one line (TextMeshPro's `NoWrap`, and `Overflow` if the label was set to cut text short), at the game's own font size. If the label is wider than its box, it runs a little past it, into the gap between the slider and the edge of the panel.
- **Setting up the settings menu.** When the game sets up a settings menu (`SettingsMenu.Start`), it puts each saved value into its slider with `SliderOption.Initialize`, which calls `SetSensitivityValue` too. In the game, that call snaps as well, so with mouse and keyboard a saved 1.2x becomes 1.0x. A prefix and finalizer on `Initialize` mark that call, and while it runs SliderSens skips the snap and keeps the saved value as it is: back on the 0.01 grid if it was within 0.00005 of it (the slider's round trip moves a value by about a millionth), otherwise exactly as the game reads it. So a value set some other way (dragged without the mod, or set to three decimal places by an earlier build) isn't rounded just by opening the menu, and one set on a gamepad isn't snapped. Its label shows it rounded to two places until that slider is moved. A value outside 0.1 to 5.0, typed into the game's settings file, is brought back into range by the slider, as in the game.
- **The arrow keys.** A small component is added to each settings menu (a postfix on `SettingsMenu.Start`), so it only runs while the menu is open. Each frame it looks for the sensitivity slider under the mouse pointer (Unity's own rectangle test, with the canvas's camera), or else the selected one, and none while a text box has the keyboard. Left and Right move that slider like a drag would: the game itself then stores the setting, updates the label and plays its slider sound. Where the game would snap, the target is worked out with the game's own conversion and test, so the snap stretch around 1.000x is one step (in from either side lands on 1.00x, out from 1.00x lands on 0.90x or 1.40x) and a key press never gets stuck.
- **Unity's own arrow keys.** On a selected slider, Unity moves it by 10% for Left and Right, or moves on to the next control to the side (`Slider.OnMove`). While an arrow key belongs to a sensitivity slider, a prefix holds that back on every slider, so one press never moves two things. Up and Down, and Left and Right everywhere else, work as before.
- **The gamepad.** The game moves sliders on a gamepad in `MenuNavigation.Update`: 5% of the slider per press of Left or Right, repeating after 0.4 seconds, every 0.1 seconds. A prefix runs first with the game's own checks (the menu is the one in use, the selection is a sensitivity slider that's active and interactable). It makes the step itself (0.01, or 0.1 with either trigger held) and marks Left and Right as already held in `MenuNavigation`'s own state (`previousNavigationDirection`, `autoShiftTimer`), so the game sees no new press and no repeat. The game doesn't snap on a gamepad, and neither do SliderSens's steps.
- **Saving.** Nothing new is saved: the game writes the values to its own settings file (`Config.json` in its save folder) when the settings menu closes, as it always does.
- **All or nothing.** The two patches the 0.01 steps need (`Initialize` and `SetSensitivityValue`) are applied first. If either fails (for example after a game update), the log shows `[FAILED]`, both are removed again, and the game keeps its own sliders. The arrow keys and the gamepad steps are applied after, each on its own; if one fails, only it is off. If the value code throws while the game runs, the game's own method takes over for the rest of the session, and the arrow keys and gamepad steps stop with it; if only the arrow keys or the gamepad steps throw, only they switch off. The log says why.

## Known issues

- **Only partly tested in game.** The labels, the arrow keys, the gamepad, saving and turning it off have been tested in game; the rest of the [test plan](#test-plan) (other mods, triggers during a match, VR) hasn't yet. Everything that doesn't need the Unity engine has been checked (see [Verification](#verification)). Please try the [test plan](#test-plan) and send `BepInEx/LogOutput.log` if anything is off.
- **The game's snap stays.** With mouse and keyboard, 0.91x to 1.39x can't be set: the game makes them 1.00x, as it always has. Use the gamepad for those values, and don't touch that slider with the mouse or arrow keys afterwards.
- **A drag can't hit every value.** The mouse moves the slider a pixel at a time, which is more than 0.01 on the right half. Drag close, then use the arrow keys.
- **The label may be wider than its box.** `0.00x` is one character longer than the game's `0.0x`. It's kept on one line, so if it doesn't fit it runs a few pixels past its box. If it touches the slider or the edge of the panel on your screen, please send a screenshot.
- **The arrow keys belong to the slider under the pointer.** While the mouse pointer is over a sensitivity slider, Left and Right move that slider and nothing else, even if another slider was selected.
- **In VR,** the slider under a VR pointer may not be found. Click the slider first; the arrow keys then work on the selected slider.
- **Config changes need a restart.** Every setting is read when the game starts.

## Building from source

You can build with either of these:

**A. Without the .NET SDK:**

```
pwsh ./build.ps1 -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Super Battle Golf" -BepInExCore "<path to BepInEx\core>"
```

This needs PowerShell 7 (`pwsh`). If you use r2modman, `-BepInExCore` is your profile's `BepInEx\core` folder. The output is `bin/SliderSens.dll`.

**B. With the .NET SDK:**

```
dotnet build -c Release -p:GameDir="..." -p:BepInExDir="<folder containing core\BepInEx.dll>"
```

The output is `bin/Release/SliderSens.dll`. Neither build copies the DLL anywhere; install it yourself.

## Verification

These checks have been run against the game's own assemblies as installed on 9 November 2026 (`GameAssembly.dll` of 24 October 2026, Unity 6000.3.10) and BepInEx 5.4.23.5. Only the first one is an in-game test.

- In game: the two-decimal labels look right under **Settings > Controls**, the arrow keys and the gamepad behave as described, a value survives a restart, and `General.Enabled = false` gives the game's own sliders back (test plan steps 2, 4, 6, 8 and 10). An earlier build with three decimal places had shown the `x` wrapping onto a second line, which the label fix addresses. The rest of the test plan hasn't been tested yet.
- Release build: the compile step of `build.ps1`, run with the same Roslyn calls and options from a .NET 8 host (PowerShell 7 wasn't available in the build environment). No errors and no warnings. Two builds are byte-for-byte identical.
- `dotnet build` (path B): not run, because the build environment couldn't reach NuGet. The project file is MaxFOVSlider's and VCTextLobby's with the names changed.
- IL inspection of the real `GameAssembly.dll`:
  - `SetSensitivityValue` exists once, with the signature above, and is called only by the settings menu's four sensitivity rows, each with 0.1 and 5.
  - The four stored values are written only by those rows (and the defaults of 1), and read only by `ScaleInput` and the settings menu. `ScaleInput` multiplies by them as they are, with no rounding or clamping.
  - `MenuNavigation` steps by 5% of the slider and repeats after 0.4 seconds, every 0.1 seconds; `previousNavigationDirection` and `autoShiftTimer` are as described above.
  - The game reads the gamepad triggers only in the main menu (to skip its intro), the match setup hole list and the player customisation menu: not in the settings menu or menu navigation. No game code reads the arrow keys directly.
- The other mods, inspected the same way: BigLobby and MaxFOVSlider (0.2.1 and the tilt-shift build) patch `SliderOption.SetLimits`, and MaxFOVSlider has a prefix on `SettingsMenu.Start`; SliderSens's postfix on the same method runs alongside it. SBGVR and VCTextLobby touch none of the methods SliderSens patches or reads.
- In a Mono 6.8 test host with BepInEx's own HarmonyX 2.9 and the real game assemblies, 2,125 checks pass. The engine can't run there, so test-only copies of two Unity assemblies let `UnityEngine.Object` start and skip two engine calls, and the label text, the slider's visuals, its sound and which device is in use are stubbed. The game's own `SliderOption`, `Slider` and settings code run as they are.
  - Harmony applies all five patches to the real game methods, and nothing else is patched.
  - The game's own sliders first, without SliderSens: a saved 1.2 or 1.234 becomes 1.0 when the menu sets them up with mouse and keyboard, and the label shows one decimal place.
  - Every one of the 491 values from 0.10 to 5.00, four checks each: through the game's curve to a slider position and back to the same value, the snap stretch, a text round trip at full float precision, and the label.
  - The same 491 values through a real `SliderOption` and `Slider`, set up exactly as the settings menu sets them up: on a gamepad each one is stored and shown exactly; with mouse and keyboard each is stored exactly, or as 1.00 inside the snap stretch (48 values, 0.91 to 0.99 and 1.01 to 1.39).
  - Loading: 1.00, 2.35, 0.10 and 5.00 load exactly; 1.2 is kept; values set some other way (1.58731 dragged without the mod, 0.303 from the three-decimal build) are kept as the game reads them and shown rounded (`1.59x`, `0.30x`); 9 becomes 5.0, as in the game.
  - Steps: 0.01 from 0.10 reaches 5.00 in 490 steps on a gamepad, each exactly the next value; with mouse and keyboard, in both directions, without getting stuck and stopping inside the snap stretch only at 1.00. Shift and trigger steps of 0.1, values set some other way stepping onto the grid, both ends, and steps on the real slider going through its change event to the settings menu's own handler, as a drag does.
  - Holding a key or direction: one step, then repeats at 0.4 seconds and every 0.1 seconds after.
  - The gamepad's hold-back marks Left and Right as held and keeps the repeat timer full, without touching Up and Down.
  - An error while running hands over to the game's own method for the rest of the session, with one log line; a core patch that can't be applied removes the other again.
  - The release DLL: its GUID, name and version, its patches on the five game methods, the five `[ok]` lines, and the sliders end to end. The settings file it writes has the settings and defaults shown under [Usage](#usage).
- The DLL references no networking code: no Mirror, no Steam.

Anything that needs the Unity engine (the menu on screen, the pointer, the keys and the gamepad, the label's width, the settings file the game writes) can only be checked in game.

## Test plan

These checks are done in game:

1. **Load check.** `BepInEx/LogOutput.log` should show five `[ok]` lines and `SliderSens 1.0.0 loaded. Sensitivity sliders in steps of 0.01.`
2. **The labels.** Open **Settings > Controls**: each sensitivity slider shows two decimal places on one line (`1.00x` by default), and `0.10x` and `5.00x` don't touch the slider or the edge of the panel.
3. **Dragging.** Drag a slider: the label moves in steps of 0.01 or more and always shows two decimal places. Near the middle it still snaps to `1.00x`.
4. **Arrow keys.** Point at a slider and press Right: +0.01. Shift+Right: +0.1. Hold Right: one step, then ten a second. From `0.90x`, one Right gives `1.00x` and the next `1.40x`; Left goes back the same way.
5. **Selected slider.** Click a slider, move the pointer away and press Left: the clicked slider moves by 0.01, not 10%.
6. **Gamepad.** On a sensitivity slider, Left and Right move by 0.01, and by 0.1 with either trigger held. Holding Right repeats in steps of 0.01, never the game's 0.09 or 0.4. The game's tab switching on the shoulder buttons still works.
7. **In use.** Set the mouse sensitivity to 1.50 and then 1.51: the camera turns slightly faster.
8. **Saving.** Set a value such as `2.37x`, close the settings, restart the game: it's still `2.37x`. The game's `Config.json` should hold the same number.
9. **The snap stretch on a gamepad.** Set the controller sensitivity to `1.20x` with the gamepad, restart the game and open the settings with the mouse: still `1.20x`.
10. **Turning it off.** Set `General.Enabled = false` and restart: one decimal place again, and the values you set are still used.
11. **Other mods.** With MaxFOVSlider, BigLobby, VCTextLobby and SBGVR installed, everything above should still hold, and the Field of view slider should still go to MaxFOVSlider's limit.

## Files

```
SliderSens.csproj                      SDK build file (references the game's DLLs)
.editorconfig                          formatting and naming conventions
build.ps1                              SDK-free build (PowerShell 7)
REVIEW.md                              code review findings and open risks
CHANGELOG.md                           changes in each release
LICENSE                                MIT license
thunderstore/manifest.json             Thunderstore package metadata
thunderstore/README.md                 README shown on the Thunderstore page (a short, player-facing version)
thunderstore/icon.png                  Thunderstore package icon
src/AssemblyInfo.cs                    assembly version info
src/Plugin.cs                          config, and applying the patches
src/Sensitivity.cs                     the 0.01 grid, the game's curve and snap, the fine steps
src/SensitivitySliders.cs              the sensitivity sliders: what's shown and stored, moving them a step
src/HoldRepeat.cs                      step on press, then repeat while held (the game's menu timing)
src/KeyboardFineAdjust.cs              the arrow keys, added to each settings menu
src/GamepadFineAdjust.cs               the gamepad steps, and holding back the game's own step
src/Patches/SensitivityValuePatch.cs   prefix on SetSensitivityValue: the value and the label
src/Patches/SliderLoadPatch.cs         prefix and finalizer on Initialize: saved values kept on load
src/Patches/SettingsMenuPatch.cs       postfix on SettingsMenu.Start: adds the arrow keys
src/Patches/SliderMovePatch.cs         prefix on Slider.OnMove: Unity's own arrow-key step held back
src/Patches/MenuNavigationPatch.cs     prefix on MenuNavigation.Update: the gamepad steps
```

## API

SliderSens is a plugin, not a library. Other BepInEx plugins can refer to it by its GUID:

```csharp
[BepInDependency(SliderSens.Plugin.Guid, BepInDependency.DependencyFlags.SoftDependency)] // "com.slidersens"
public class MyPlugin : BaseUnityPlugin { }
```

- `SliderSens.Plugin.Guid`, `Name` and `Version` are public constants. There is no other public API.

## Maintainers

lmkHenlo, <ps.meiisbei@gmail.com>

## Contributing

Questions, bug reports and contributions are welcome by email to <ps.meiisbei@gmail.com>. Please attach `BepInEx/LogOutput.log` when reporting a problem, and a screenshot if a label looks wrong.

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/): `feat:`, `fix:`, `perf:`, `refactor:`, `style:`, `docs:` and `chore:`, with `BREAKING CHANGE:` for config settings that are removed or renamed. Changes are recorded in [CHANGELOG.md](CHANGELOG.md), which follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

If you edit this README, please follow the [standard-readme](https://github.com/RichardLitt/standard-readme) specification.

## License

[MIT](LICENSE) © 2026 lmkHenlo
