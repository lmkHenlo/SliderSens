# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and version numbers use Thunderstore's `Major.Minor.Patch` format.

1.0.0 is the first public release. Version 0.1.0 was a pre-release test build that was never published. Dates are in US Central time (UTC−06:00).

## [Unreleased]

## [1.0.0] - 2026-11-09

### Added

- The four sensitivity sliders under **Settings > Controls > Camera** (Sensitivity and Aim Sensitivity, for Mouse and Keyboard and for Controller) go in steps of 0.01 and show two decimal places: `1.23x` instead of `1.2x`. The value shown is exactly the value the game uses and saves.
- **Arrow keys:** Left and Right move the sensitivity slider under the mouse pointer (or the one last clicked) by 0.01; with Shift, by 0.1. Holding a key repeats it with the game's menu timing.
- **Gamepad:** on a sensitivity slider, Left and Right move it by 0.01 instead of the game's 5% steps; with either trigger held, by 0.1.
- Settings in `BepInEx/config/com.slidersens.cfg`: `General.Enabled`, `Keyboard.FineAdjust` and `Gamepad.FineAdjust`.

### Changed

- A value near 1.0x set on a gamepad isn't reset to 1.0x when the game sets up its settings menu, and a value set without the mod isn't rounded just by opening the menu.
- The sensitivity labels stay on one line.

The range (0.10x to 5.00x), the default (1.00x), the slider's curve and the game's snap to 1.00x with mouse and keyboard are unchanged.
