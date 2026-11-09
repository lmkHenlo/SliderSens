# SliderSens

Adds a second decimal place to the sensitivity sliders in Super Battle Golf. Client-side: only you need it.

Version 1.0.0, tested on the game files of 24 October 2026 with BepInEx 5.4.23.5.

## What it does

### Added

- The four sensitivity sliders (mouse, mouse aim, controller and controller aim) go in steps of **0.01** and show two decimal places: `1.23x` instead of `1.2x`.
- The value you see is exactly the value the game uses and saves. In the game, the label is rounded and the setting is wherever the slider ended up.
- **Arrow keys:** with the mouse pointer over a sensitivity slider, Left and Right move it by 0.01. Hold Shift for 0.1.
- **Gamepad:** on a sensitivity slider, Left and Right move it by 0.01. Hold either trigger for 0.1.
- Settings in `BepInEx/config/com.slidersens.cfg` to turn the arrow keys or the gamepad steps off. See [Settings](#settings).

### Changed

- Nothing else about the sliders:
  - Same range, 0.10x to 5.00x, and the same default, 1.00x, in the middle of the slider.
  - Same feel when you drag: the left half still goes from 0.1x to 1.0x and the right half from 1.0x to 5.0x.
  - With mouse and keyboard, the game still snaps anything from 0.91x to 1.39x to 1.00x.
- A value near 1.0x set with a gamepad isn't reset when the game sets up its settings menu.

## Install

You need [BepInEx 5](https://github.com/BepInEx/BepInEx) (`BepInEx-BepInExPack-5.4.2305`, installed for you by mod managers).

- **With r2modman or another Thunderstore mod manager:** install `SliderSens`.
- **By hand:** copy `SliderSens.dll` into `Super Battle Golf/BepInEx/plugins/SliderSens/`.

Only you need the mod. It works in any lobby, whoever hosts, and other players see no difference.

## Usage

1. Open **Settings > Controls**. The sensitivity sliders are under **Camera**, for **Mouse and Keyboard** and for **Controller**.
2. Drag a slider close to the value you want.
3. Fine-tune it: point at the slider and press Left or Right (Shift for 0.1), or on a gamepad press Left or Right (a trigger for 0.1).
4. Close the settings. The game saves the value, so it's still there after a restart.

How far each input moves a slider:

| Input | Step |
|---|---|
| Left or Right | 0.01 |
| Shift + Left or Right | 0.1 |
| Gamepad: Left or Right | 0.01 |
| Gamepad: either trigger + Left or Right | 0.1 |

Holding a key or direction repeats it: once, then after 0.4 seconds ten times a second.

## Settings

Edit `BepInEx/config/com.slidersens.cfg` (in r2modman: **Config editor**), then restart the game.

| Setting | Default | What it does |
|---|---|---|
| `General.Enabled` | `true` | `false` turns the mod off and gives you the game's own sliders, with one decimal place. |
| `Keyboard.FineAdjust` | `true` | `false` turns the arrow keys off: they then do what they do in the game. |
| `Gamepad.FineAdjust` | `true` | `false` turns the gamepad steps off: Left and Right move in the game's own steps (0.09 below 1.0x, 0.4 above). |

## Good to know

- **Tested in game** with mouse and keyboard and with a controller; not yet in VR or alongside the other mods listed below. If something doesn't work as described, please send `BepInEx/LogOutput.log`, and a screenshot if a label looks wrong.
- **The snap to 1.00x stays.** With mouse and keyboard, the game turns anything from 0.91x to 1.39x into 1.00x, and SliderSens keeps that. The arrow keys jump straight across: from 0.90x, one press up gives 1.00x and the next 1.40x. To set a value in between, use a gamepad, and don't move that slider with the mouse afterwards.
- **Dragging is coarser than 0.01 above 1.0x,** because the slider moves a pixel at a time. Drag close, then use the arrow keys or the gamepad.
- **While the pointer is over a sensitivity slider,** Left and Right move that slider and nothing else.
- **Removing the mod** is safe: your values stay, shown with one decimal place. The game itself turns a value from 0.91x to just under 1.4x back into 1.0x when it next sets up the settings with mouse and keyboard.
- **After a game update,** if the mod can't apply itself fully, it switches itself off instead of half-working. The game then keeps its own sliders, and `BepInEx/LogOutput.log` shows `[FAILED]`. If only the arrow keys or the gamepad steps can't apply, just that part switches off.

## Compatibility

- None of MaxFOVSlider, BigLobby, VCTextLobby or SBGVR changes the sensitivity sliders (checked in their code; not yet tested together in game).
- In VR, click a slider before using the arrow keys on it.
- Client-side only: it changes your own settings, and sends nothing over the network.

## Contact

Questions and bug reports: lmkHenlo, <ps.meiisbei@gmail.com>. Please attach `BepInEx/LogOutput.log`, and a screenshot if a label looks wrong.

## License

MIT © 2026 lmkHenlo
