# SliderSens: Review status

This tracks the code review of SliderSens 1.0.0, from the investigation of the game's sliders through the first look at it in game, and where each finding stands in the current code.

**Disclosure:** the same assistant that wrote the code did the review and the fixes. An independent reviewer, or simply a playtest, is worth more than another self-review.

---

## Project summary

A BepInEx 5 plugin (`SliderSens.dll`, GUID `com.slidersens`) for *Super Battle Golf*. The game runs on Unity (Mono). The plugin puts the game's four sensitivity sliders (mouse, mouse aim, controller, controller aim) on a 0.01 grid, shows two decimal places, and adds arrow-key and gamepad-trigger steps of 0.01. It keeps the game's range (0.1 to 5), default (1), slider curve and mouse-and-keyboard snap to 1. It is client-side only: it sends nothing over the network and needs nothing from the host or other players.

`src/Plugin.cs` reads the config and applies the patches. The two the 0.01 steps need are applied first, all or nothing; the arrow keys and the gamepad steps are applied after, each on its own. The parts are:
- **Value patch** (`Patches/SensitivityValuePatch.cs`): a prefix that stands in for `SliderOption.SetSensitivityValue`, inside a try/catch that hands back to the game's method on any error.
- **Load patch** (`Patches/SliderLoadPatch.cs`): a prefix and finalizer on `SliderOption.Initialize` that mark the call loading a saved value.
- **Sliders** (`SensitivitySliders.cs`): the game's steps with the value rounded to 0.01 and a two-decimal label; keeping saved values on load; moving a slider one step the way a drag does.
- **Maths** (`Sensitivity.cs`): the grid, the game's curve and snap test (its own methods, through delegates), the snap stretch as one step.
- **Arrow keys** (`KeyboardFineAdjust.cs`, added to each settings menu by `Patches/SettingsMenuPatch.cs`) and **Unity's own arrow-key step held back** (`Patches/SliderMovePatch.cs`).
- **Gamepad steps** (`GamepadFineAdjust.cs`, run by `Patches/MenuNavigationPatch.cs`).
- **Supporting code:** `HoldRepeat.cs`, step on press then repeat while held, with the game's menu timing.

**Build:** `build.ps1` (PowerShell 7, no .NET SDK needed) and `SliderSens.csproj` (`dotnet build`). The release DLL was built with `build.ps1`'s compile step (see below).

---

## Build / verification status

| Check | Result |
|---|---|
| Release build against the real game DLLs + BepInEx 5.4.23.5 | Passes. 0 errors, 0 warnings. PowerShell 7 isn't available in the build environment, so `build.ps1`'s compile step was run with the same Roslyn options and references from a .NET 8 host (the .NET 8.0.133 SDK's Roslyn). The build is deterministic: two builds are byte-for-byte identical. |
| `dotnet build -c Release` of `SliderSens.csproj` | **Not run.** The build environment can't reach NuGet, which `netstandard2.1` needs for its reference pack. The project file is MaxFOVSlider's and VCTextLobby's with the names changed, and both of those build this way. |
| Patch targets in the real `GameAssembly.dll` | `SliderOption.SetSensitivityValue(float, float, bool, bool)` exists once and is called only by the settings menu's four sensitivity rows, each with 0.1 and 5. `SliderOption.Initialize(Action, float)`, `SettingsMenu.Start`, `MenuNavigation.Update`, `MenuNavigation.previousNavigationDirection` (a flags enum, `Horizontal = 24`) and `MenuNavigation.autoShiftTimer` (float) are as the code expects. The stored values are written only by the four rows and the field defaults, and read only by `AxisSetting.ScaleInput` (no rounding or clamping) and the settings menu. |
| Harmony on the real methods (Mono 6.8, BepInEx's HarmonyX 2.9) | All five patches apply, from the source build and from the release DLL, and nothing else is patched. |
| End to end, with the engine stood in for | The game's own `SliderOption`, `Slider` and settings-menu wiring, run with test copies of two Unity assemblies (so `UnityEngine.Object` can start without the engine, and two engine calls do nothing) and stubs for the label text, the slider's visuals, its sound and the input device. The unmodded game first, then the mod: loading, dragging, all 491 grid values through the real slider, fine steps through the slider's own change event. |
| Unit tests | All 491 grid values against the game's own curve (round trip, snap stretch, text round trip, label); steps in both directions with and without the snap; hold-to-repeat timing; the gamepad hold-back. 2,125 checks in all pass. |
| Error paths | An exception in the value code is caught, the game's own method takes over for the session, the arrow keys and gamepad steps stop, and one line is logged. A core patch that can't be applied removes the other again and logs `[FAILED]`. |
| Other mods | BigLobby, MaxFOVSlider 0.2.1 (and its tilt-shift build), SBGVR (with UUVR) and VCTextLobby, inspected for patches on or calls into the methods SliderSens uses. Only MaxFOVSlider shares one: a prefix on `SettingsMenu.Start`, next to SliderSens's postfix. |
| Default config | Written by the plugin's own `BindConfig` under BepInEx 5.4.23.5; matches the README. |
| In-game test | **Partly done.** The two-decimal labels look right under **Settings > Controls** (findings 21 and 22), the arrow keys and the gamepad behave as described, a value survives a restart, and turning it off gives the game's own sliders back. An earlier three-decimal build had shown the `x` wrapping onto a second line (finding 21). **Not yet:** the rest of the test plan (other mods, VR, triggers during a match). **Everything else: not yet.** See [Open risks](#open-risks-to-confirm-in-game). |

---

## Review findings and their status

| # | Finding | Status now |
|---|---|---|
| 1 | Showing more decimals alone would only be cosmetic: the game stores wherever the slider ended up, which is often not what the label says | **Fixed by design.** The value is rounded to 0.01 first, and the label is written from that same value, so what's shown is what's stored. |
| 2 | A float can't hold most decimals exactly (0.01 isn't a float) | **Fixed.** Values are rounded to whole hundredths in double precision, then divided as floats, which gives the float nearest the decimal every time. All 491 values survive the slider's round trip and a text round trip unchanged. |
| 3 | The vanilla snap (5% around the middle, mouse and keyboard) makes 0.91x to just under 1.4x unreachable | **Kept, as decided.** The game's own snap test and position run unchanged. 48 grid values still become 1.00x on mouse and keyboard; a gamepad reaches all of them. |
| 4 | With the snap kept, every 0.01 arrow-key step near 1.00x would snap back, so the keys would get stuck there | **Fixed.** The snap stretch is worked out with the game's own conversion and test, and counts as one step: into it lands on 1.00x, out of it lands on 0.90x or 1.40x. Stepping from 0.10 to 5.00 and back never gets stuck. |
| 5 | The settings menu's own load (`Initialize`) also snaps, so with mouse and keyboard it would reset a value near 1.0x set on a gamepad, every time the menu is set up | **Fixed.** The load is marked, and while it runs there's no snap. |
| 6 | Rounding on load would change values set without the mod (1.58731 to 1.59), just by opening the menu | **Fixed.** On load, a value is put back on the grid only if it's within 0.00005 of it (float noise from the slider's round trip is about a millionth); anything else is kept exactly as the game reads it, and its label shows it rounded until the slider is moved. |
| 7 | Unity's own Left/Right on a selected slider (`Slider.OnMove`, 10%) would move a slider as well as the arrow-key step | **Fixed.** Held back while an arrow key belongs to a sensitivity slider. Up and Down, and Left and Right at any other time, go through. |
| 8 | On a gamepad, the game's own 5% step would come with every SliderSens step | **Fixed.** On a sensitivity slider, Left and Right are marked as already held in `MenuNavigation`'s own state, and its repeat timer kept full, so it sees no press. |
| 9 | The obvious place to drop the game's gamepad step is its compiler-generated `<Update>g__GetCurrentNavigation\|35_3`, whose name can change with any rebuild and whose return type is a private enum | **Avoided.** The hold-back uses two ordinary fields by name instead. If they're gone, only the trigger steps are off, with `[FAILED]` in the log. |
| 10 | The curve conversion (`RemapSliderValueToValueMiddleLinear`) is private and could be renamed | **Handled.** It's looked up and checked (static, four floats, returns float) before anything is patched. If it's gone, the mod stays off and says so. |
| 11 | The arrow keys while typing in a text box | **Fixed.** Nothing happens while a text box has the keyboard. |
| 12 | Polling keys from the plugin's own `Update` depends on BepInEx's manager object staying alive | **Avoided.** The key handler is a component on each settings menu, so it runs exactly while a menu is open. |
| 13 | Could the triggers or Shift already do something in the settings menu? | **Checked in code.** The game reads the triggers directly only in the main menu (intro skip), the match setup hole list and the player customisation menu. Its input actions bind them to Aim and Swing in the gameplay map, and bind nothing to Shift. Nothing in the settings menu or menu navigation reads them. See open risk 4. |
| 14 | An exception thrown from a prefix inside the settings menu would break it | **Fixed.** Every prefix catches everything. The value prefix hands back to the game's own method for the rest of the session; the key and gamepad handlers switch themselves off. |
| 15 | Moving a slider for a fine step must store the setting the way the game does | **Done.** The step moves the slider (`Slider.value`), which fires the slider's own change event; the game's own handler then stores the setting, writes the label and plays the slider sound. |
| 16 | The label uses the player's number format | **Accepted.** Same as the game's own label (`string.Format` with the current culture), so a comma locale shows `1,234x` as the game shows `1,2x`. |
| 17 | "Client-side only" must hold | **Confirmed.** The DLL references no networking code (no Mirror, no Steam). It only changes the local settings menu. |
| 18 | The builds must not copy the DLL into a mod manager profile | **Done.** `SliderSens.csproj` has no copy step; `build.ps1` writes only to `bin/`. |
| 19 | While the pointer is over a sensitivity slider, Left and Right are taken from every other slider and from Unity's left/right navigation | **Accepted.** One press should only ever move one thing. Documented under Known issues. |
| 20 | While Left or Right is held on a sensitivity slider, the game's repeat for Up and Down is paused too (it's one timer) | **Accepted.** Only while both are held at once. |
| 21 | Playtest (three decimal places): the game's label box fits `1.0x` but not `1.000x`, so the `x` wrapped onto a second line under the number | **Fixed.** Each sensitivity label is kept on one line (TextMeshPro `NoWrap`, and `Overflow` if it was set to cut text short), at the game's own font size. From the screenshot there's room: at 2560 x 1440 the six characters took about 90 pixels, centred in a gap of about 100 pixels between the end of the slider and the edge of the panel; `0.00x` is a character shorter. If setting it fails, a warning is logged and the label wraps as before. Not yet tested again in game. |
| 22 | Changed to two decimal places. Steps of 0.001 behind a two-decimal label would bring back the problem of finding 1: `0.30x` on screen for a stored 0.303 | **Fixed.** The grid, the label and the steps changed together: steps of 0.01, labels `0.00x`. The arrow keys move 0.01 (0.1 with Shift; Ctrl is no longer used), and the triggers 0.1 (left) and 0.01 (right), since changed (finding 23). Values already saved to three places are kept and shown rounded (finding 6) until the slider is moved, when they step onto the grid. |
| 23 | Playtest request: the gamepad should move 0.01 both ways | **Superseded by 24.** First read as both triggers stepping 0.01. |
| 24 | Playtest: Left and Right on a gamepad still moved by the game's 0.09 | **Fixed.** On a sensitivity slider, Left and Right now always step 0.01, in place of the game's 5% step; either trigger held makes it 0.1, like Shift on the keyboard, so the whole range is still quick to cross. Confirmed in game. |

---

## Open risks (to confirm in game)

1. **Label width.** Kept on one line now (finding 21), so if it's wider than its box it runs a little past it. Confirm that `0.10x` and `5.00x` don't touch the slider or the panel's edge, and that the number sits level with its slider.
2. **The slider under the pointer.** Found with Unity's `RectangleContainsScreenPoint` and the canvas's camera. If the menu's canvas is drawn in an unusual way (or redirected by a VR mod), the pointer may not be found; clicking the slider first still works.
3. **Whether Unity sends the arrow keys to the slider at all.** The game uses Unity's `InputSystemUIInputModule`. If its move action doesn't include the arrow keys, the hold-back in `Slider.OnMove` simply never runs; if it does, it stops the double move. Either way the arrow-key step itself is read directly from the keyboard.
4. **Triggers during a match.** In the pause menu during a match, the triggers are also Aim and Swing. The game is expected to switch gameplay input off while a menu is open; confirm that holding a trigger in the settings doesn't aim or swing.
5. **The game's settings file.** Confirmed in game: a value survives a restart. Unity's `JsonUtility` writes `Config.json` natively, so its number format couldn't be checked here. Any format that keeps a float's value keeps a grid value; the test plan's saving step confirms it.
6. **Repeats while the game is paused.** Hold-to-repeat uses unscaled time, so it works while the game's time is stopped. The game's own gamepad repeat uses scaled time; if a pause menu stops time, SliderSens's repeats will still run where the game's don't.

## Gaps

- Partly tested in game: the labels, the arrow keys, the gamepad, saving and turning it off. Unity's engine can't run here, so the pointer in VR, the triggers during a match and the settings file's number format are still untested. Compiling, IL inspection, Harmony patching under Mono, the game's own slider and settings code with the engine stood in for, the error paths and the maths could be checked.
- `build.ps1` itself wasn't run, because PowerShell 7 isn't available here. Its compile step was replayed with the same options. `dotnet build` wasn't run either (no NuGet access).
- Security: nothing notable. There's no network access, no file access beyond BepInEx's own config, and no deserialization.
