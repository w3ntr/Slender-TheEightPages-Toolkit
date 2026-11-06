# Slender: The Eight Pages Toolkit

[![MelonLoader](https://img.shields.io/badge/MelonLoader-0.5.4-blueviolet.svg)](https://github.com/LavaGang/MelonLoader)
[![Game Version](https://img.shields.io/badge/Game-v0.9.1-green.svg)]()
[![Platform](https://img.shields.io/badge/Platform-Windows-blue.svg)]()

## Overview

Slender Toolkit is a comprehensive runtime modification for **Slender: The Eight Pages (v0.9.1)**, built on top of **MelonLoader v0.5.4**. The toolkit provides a suite of gameplay-altering modules — ranging from camera manipulation and entity visualization to graphics reconfiguration and player state control — designed for exploration, analysis, and extended interaction with the game's systems. It is explicitly **not** intended for speedrunning; the project targets players who wish to study the game's mechanics, traverse its environment freely, or experiment with its rendering and logic layers.

The mod was engineered against the constraints of the legacy Unity runtime (targeting .NET Framework 3.5) on which Slender 0.9.1 operates. Many modern Unity APIs — including `Cursor.lockState`, `SceneManager`, and `Texture2D.whiteTexture` — are unavailable in this environment. Consequently, the toolkit relies heavily on reflection, Harmony-based IL patching, and direct Windows API interop to achieve behaviour that would otherwise require first-party engine support.

## Table of Contents

- [Modules](#modules)
- [Installation](#installation)
- [Usage](#usage)
- [Technical Notes](#technical-notes)
- [Known Issues](#known-issues)
- [Contributing](#contributing)

## Modules

### FreeCam

A detachable, freely controllable camera that operates independently of the player's `CharacterMotor` and `MouseLook` components. When activated (default: `F3`), the module instantiates a new `GameObject` tagged `MainCamera`, copies the rendering configuration from the original camera via `Camera.CopyFrom`, and disables the original camera's `enabled` flag. The camera responds to `WASD` for horizontal translation, `Shift` for a configurable speed multiplier, and `Space`/`E` / `Q`/`Ctrl` for vertical movement. Rotation is driven by raw `Input.GetAxis("Mouse X/Y")` with a configurable sensitivity scalar, clamped to ±89° on the pitch axis to prevent gimbal flip.

A critical sub-feature is **flashlight reattachment**. The player's flashlight is a `Light` component of type `Spot` parented to the player's camera object. When FreeCam activates, the module locates this light via `Object.FindObjectsOfType(typeof(Light))`, filters by `LightType.Spot` and `intensity > 0`, detaches it from its parent (`transform.parent = null`), and re-parents it to the FreeCam object each frame. On deactivation, the original parent and transform state are restored.

### PageESP

A runtime entity scanner that locates all page collectibles in the active scene. The module iterates over every `MonoBehaviour` instance, filtering by type name (`PickupPage`, `Page`, `Collectible`) and, as a fallback, by object name containing `"page"`, `"paper"`, or `"note"`. Objects identified as spawners (name containing `"PageController"` or `"SpawnPages"`, or type `SpawnPages`) are explicitly excluded to prevent teleportation to non-interactive coordinator objects.

Each discovered page is represented by a translucent cyan `Cube` primitive (or a wireframe mesh when wireframe mode is enabled) rendered via a custom `Material` backed by the `Hidden/Internal-Colored` shader. The material's `_ZTest` property is toggled between `8` (always pass — through walls) and `4` (standard depth test) depending on the "Through Walls" setting. Distance labels are drawn in screen space using `Camera.WorldToScreenPoint`, offset below each marker.

**Teleportation** (default: `F`, requires PageESP active) locates the nearest page by Euclidean distance from the player object — identified via `CharacterMotor` lookup rather than `PlayerScript`, since the latter resides on the camera object — sets `transform.position` to the page's `bounds.center`, and invokes `CharacterMotor.SetVelocity(Vector3.zero)` to cancel residual momentum.

### GraphicsModule

A real-time renderer configuration interface. The module captures the original values of `RenderSettings.ambientLight`, `RenderSettings.fogColor`, `RenderSettings.fogDensity`, and `RenderSettings.fog` at initialization and restores them on deactivation. **Day Mode** overrides these values with configurable brightness and fog parameters, additionally switching `RenderSettings.fogMode` to `FogMode.Linear` and exposing `fogStartDistance` / `fogEndDistance` sliders.

Quick presets ("Clear Day", "Overcast", "Reset to Original") apply preconfigured value sets. The module operates entirely through Unity's built-in `RenderSettings` static API — no shader replacement or post-processing pipeline manipulation is performed, which means games that override these values via custom scripts each frame may require additional Harmony patching (not currently implemented for Slender 0.9.1, as the game does not override render settings at runtime).

### PlayerModule (GodMode + Scene Restart)

A player-state enforcement module. When GodMode is active (default toggle: `F7`), the module writes to the `PlayerScript` fields `sanity`, `caught`, `lost`, `stamina`, and `maxstam` every frame via reflection. Values are only written when they deviate from their target states (`100` for sanity/stamina/maxstam, `false` for caught/lost), and a rate-limited log message is emitted at most once per 2 seconds to avoid console flooding.

**Scene restart** (default: `R`) invokes `Application.LoadLevel(Application.loadedLevel)`. Due to the game's scene layout — scene index 0 contains the intro/credits sequence, which briefly renders a Slender Man silhouette on load — a single-frame visual artifact may appear during restart. This is a known limitation of the legacy `Application.LoadLevel` API and the game's scene authoring; no workaround is currently implemented.

### SpeedHack

A movement speed multiplier implemented via Harmony **postfix patching** of `CharacterMotor.MaxSpeedInDirection(Vector3)`. Direct field mutation of `CharacterMotorMovement.maxForwardSpeed` was attempted initially but proved ineffective: the game's `FPSInputController.Update` method re-derives movement input each frame, and the legacy Unity struct marshalling behaviour caused field writes to be lost. The postfix approach intercepts the return value of `MaxSpeedInDirection` — the method that computes the magnitude of desired horizontal velocity — and multiplies it by `speedMultiplier` before it reaches `GetDesiredHorizontalVelocity`. This guarantees correct behaviour regardless of input pipeline internals.

The patch is applied once at `OnApplicationStart` and removed at `OnApplicationQuit`. The multiplier is a `public static` field on `PlayerModule` and can be adjusted at runtime.

### StaminaUI

A draggable on-screen stamina readout. When enabled (default toggle: `F9`), the module renders a semi-transparent black rectangle with centered text displaying `stamina` / `maxstam` values read from `PlayerScript` each frame via reflection. The UI element supports click-and-drag repositioning: `EventType.MouseDown` within the element's `Rect` captures the cursor offset, `EventType.MouseDrag` updates the position, and `EventType.MouseUp` releases the drag state. Text color transitions from green to red when stamina drops below `30`.

### Cursor Management

A dual-layer cursor unlock system required because Slender 0.9.1 aggressively re-locks the cursor via `Screen.lockCursor = true` in `PlayerScript.Update` every frame. The solution combines:

1. **A Harmony prefix patch** on `Screen.set_lockCursor` that intercepts every setter call, forces `value = false` when the toolkit menu is open, and returns `false` to suppress the original setter invocation. A re-entrancy guard (`isInsidePatch`) prevents infinite recursion when the patch itself writes to `Screen.showCursor`.
2. **A Windows API layer** (`user32.dll` `ShowCursor` and `ClipCursor`) that manipulates the OS-level cursor visibility counter. Because Windows uses a reference-counted visibility model, the module calls `ShowCursor(true)` ten times on menu open to overcome any negative count established by the game, and compensates with matching `ShowCursor(false)` calls on menu close.

### FPS Counter

A frame-rate readout rendered via `OnGUI`, computed from a rolling 0.5-second accumulator. The counter's screen position is draggable using the same event-driven mechanism as `StaminaUI`. V-Sync toggle is exposed via the toolkit menu and maps to `QualitySettings.vSyncCount`.

## Installation

The modification requires a working MelonLoader installation. MelonLoader must be installed into the root directory of Slender: The Eight Pages v0.9.1 **prior** to using this toolkit. Once MelonLoader is operational (verified by the appearance of the MelonLoader console window on game launch), place the compiled `SlenderToolkit.dll` file into the `Mods` subdirectory. Place `0Harmony.dll` (version 2.9.0) into the same `Mods` directory. On the first launch, MelonLoader will load both assemblies; the toolkit logs its initialization sequence to the MelonLoader console. No additional configuration files are required.

Directory structure:

```
Slender/
├── MelonLoader/
├── Mods/
│    └── SlenderToolkit.dll
│
└── Slender.exe
```

## Usage

The toolkit interface is accessed by pressing `Insert`, which toggles a draggable `GUILayout.Window` containing tabbed modules (Freecam, FPS, FOV, Triggers, Graphics, Pages). When the menu is open, the cursor lock state is released and the cursor is made visible via the combined Harmony + WinAPI system described above; when the menu is closed, the game's native cursor management is restored without interference from the mod.

### Global Hotkeys

| Key | Action |
|-----|--------|
| `Insert` | Toggle toolkit menu |
| `F1` | Toggle PageESP |
| `F` | Teleport to nearest page *(PageESP must be active)* |
| `F3` | Toggle FreeCam |
| `F7` | Toggle GodMode |
| `F8` | Toggle SpeedHack (×2) |
| `F9` | Toggle StaminaUI |
| `R` | Reload current scene |

### Module Notes

- **FreeCam** — `WASD` for horizontal movement, `Shift` for speed multiplier, `Space` / `E` for ascend, `Q` / `Left Ctrl` for descend. Mouse movement controls camera rotation.
- **PageESP** — When active, pages are highlighted with cyan markers. Distance labels appear below each marker. Press `F` to teleport to the nearest detected page. The `PageController` / `SpawnPages` spawner object is automatically excluded from detection and teleportation targets.
- **GraphicsModule** — The Day Mode toggle applies ambient brightness and fog overrides. Sliders adjust values in real time. The *Reset to Original* button restores all `RenderSettings` to their values captured at module initialization.
- **FPS counter and StaminaUI** — Both elements are repositionable via click-and-drag. Positions are not persisted between sessions.

## Technical Notes

- **Reflection over direct typing:** Because `CharacterMotor`, `CharacterMotorMovement`, `PlayerScript`, and related types are defined in the game's `Assembly-CSharp.dll` — which is not referenced at compile time — all interactions with these types use `object` storage and `Type.GetField` / `Type.GetMethod` resolution. This avoids compile-time dependency on the game assembly while preserving full runtime access.
- **Struct marshalling:** `CharacterMotorMovement` is a class (reference type) in Slender 0.9.1, as confirmed via `Type.IsValueType == false`. Direct field mutation through reflection therefore persists without requiring reassignment to the parent `CharacterMotor.movement` field. This was verified via runtime diagnostics logging field values each second.
- **Legacy Unity API substitutions:** `Application.LoadLevel(int)` replaces `SceneManager.LoadScene`; `Object.FindObjectsOfType(typeof(T)) as T[]` replaces the generic `FindObjectsOfType<T>()`; `transform.parent = null` replaces `SetParent(null)`; manual `Texture2D` construction replaces `Texture2D.whiteTexture`.
- **Cursor patch re-entrancy:** The `isInsidePatch` boolean in `CursorPatch` is set to `true` before any write to `Screen.showCursor` and reset to `false` immediately after, preventing the patch from recursively intercepting its own writes.

## Known Issues

- **Scene restart artifact:** Reloading scene index 0 briefly renders a Slender Man silhouette for a single frame due to the intro scene's authoring. No workaround is currently implemented.
- **9th page detection:** The module consistently detects 9 page-like objects in the scene, one of which is an empty/non-interactive object located below the map geometry. This is believed to be a development artifact in the original game build.
- **StaminaUI position persistence:** UI element positions are not saved between sessions.

## Contributing

This project is fully open-source. Anyone is welcome to fork the repository, make their own changes, and submit pull requests — improvements, bug fixes, and new features are all appreciated.
