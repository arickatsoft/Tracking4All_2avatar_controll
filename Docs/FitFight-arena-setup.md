# Full-body street arena presentation

Implemented 2026-09-08 for the existing Unity player-selection flow. Target devices supplied by the user: iPhone 11, Samsung Galaxy S21 and Galaxy S24. No hardware performance claim has been verified.

## Run

Open the project with Unity 6000.2.9f1, open `Assets/Tracking4All/Scenes/Pose Avatar Tracking/AvatarTracking_new.unity`, and enter Play Mode. Select either fighter as before. `PlayerSelectionManager.SetPlayerActive` configures the presentation automatically; no Inspector wiring is required for that scene. The court and HUD are runtime objects, so they appear in Play Mode rather than being baked into the scene file.

## Changes

- `FitFightArenaCamera`: both player cameras use 48-degree vertical FOV and a slightly elevated, sideways-offset view toward the opponent. Camera distance fits both skinned-renderer bounds plus movement padding into the current screen safe area. Space is reserved above the fighters for the HUD. It widens immediately when necessary and moves inward slowly. It runs after the existing camera-rig scripts to apply the final camera position. Existing fighter positions and skeletal scales remain intact.
- `FitFightStreetArena`: tiled warm-concrete ground and a surrounding steel fence, built as three combined meshes. A warm directional sun and cool ambient fill follow the reference's daylight direction. The plain cyan camera background supplies the sky color. This is procedural 3D scenery, not a photorealistic replacement of the supplied image.
- Existing named Environment/Plane renderers are hidden while the new court is active. Their colliders and scripts remain unchanged. Previously enabled directional lights and ambient settings are restored when the presentation is destroyed.
- `FitFightArenaPresentation` / `FitFightRoundedPanel`: rounded dark HUD nameplates, green local-fighter and red opponent accents, safe-area-aware anchors and actual Adam/Remy labels. Accents are identity stripes, not simulated health. The match/health system needs a separate integration before these can represent health.
- The named Preview Camera is disabled for the clean fight composition. This does not stop the front-camera tracking source. Its prior enabled state is restored when presentation is destroyed.
- `Assets/Resources/FitFightStreet.mat` retains an explicit built-in material reference for mobile builds instead of relying on a runtime-only shader lookup.

## Verification completed

- Compiled and executed the actual standalone framing math: 60,000 corner projections passed across five portrait/landscape aspect ratios and three top-clearance cases.
- Roslyn syntax parsing: zero syntax errors in six changed/new C# files.
- This does not verify Unity API compilation, actual skinned mesh bounds, GPU rendering, touch interaction or device frame rate.

Repeat the math check:

```sh
mcs -out:/tmp/fitfight-framing-tests.exe Assets/Scripts/FitFightFramingMath.cs Tools/Tests/FitFightFramingTests.cs
mono /tmp/fitfight-framing-tests.exe
```

## Required visual/device checks

Unity Editor is still unavailable on this computer at the known location. Run the scene on the target devices and inspect both player selections, portrait/landscape rotation, feet/hand clearance during kicks, notch/status-bar clearance, fence placement, ground contact and shadows. Verify that no legacy script later re-enables the debug preview or a competing camera. Confirm that the authored skinned-renderer bounds enclose animated limbs; the fitter cannot guarantee coverage of geometry outside reported bounds. Profile CPU/GPU/inference on iPhone 11 before increasing shadow or material complexity.

The camera keeps reported full-body bounds in frame; it does not guarantee unobstructed limb visibility when one fighter overlaps the other. The 18-degree side offset reduces that overlap, and should be tuned in actual gameplay footage. Runtime ambient/light changes are scene-wide. The original ground colliders were preserved to avoid altering gameplay physics; verify their alignment with the new visual floor.

To undo this pass, restore PlayerSelectionManager.cs from `Docs/FitFight-arena-original-scripts.zip` and remove the five new presentation/math/graphic scripts (and their .meta files), the Resources material, and the standalone framing test. Earlier UDP and selector improvements are separate.
