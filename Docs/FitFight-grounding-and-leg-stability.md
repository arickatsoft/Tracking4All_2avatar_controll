# Grounding, leg stability, and camera settings

For the subsequent camera refinements and Android native-callback repair, see [the current repair notes](FitFight-camera-and-Android-callback-repair.md). The verification limitations below describe the earlier leg-stability change.

## Changes

- Runtime fighter separation is now 0.62 times the taller avatar's height, down from 1.0. The existing camera still fits both fighters.
- `FitFightFighterGrounding` runs after tracking and the guard pose. It keeps the root upright and measures animated shoe vertices to lift penetrating toe/heel edges above the court. Raised feet remain free to kick. The correction uses reusable buffers and skins only footwear; its mobile cost still needs profiling.
- `PosePuppet` now holds each leg's knee, ankle, heel, and toe together until movement exceeds a small distance relative to that hip. The default threshold is 4% of tracked leg length (roughly 3 cm for a 75 cm leg). Comparisons use the last accepted pose, so slow movement accumulates and kicks pass through without a temporal smoothing delay. Tracking gaps, provider changes, and mirroring changes reset the held pose.
- Rejected duplicate settings rows are removed instead of displaying prefab text such as `Setting 1` and `Option B`. Camera setting names include the backend's GameObject name, separating Adam and Remy controls. Camera dropdowns submit their selected numeric index.
- Selecting an avatar finds inactive pose solutions, releases the other solution before enabling the selected one, and restores the selected puppet's local pose provider. This addresses switching back to an avatar that previously used network tracking. The peer IP controls network communication, not local camera selection.

## Tuning

On both `Puppet-StandardT` and `Puppet-StandardT Remy`, open the `PosePuppet` component's **Leg stability** section. Start with **Stabilize Legs** enabled and **Leg Movement Threshold** at `0.04`. Increase it slightly if standing still still produces visible shaking; decrease it if deliberate small steps are suppressed. This is a distance filter, not a kick classifier.

These source changes take effect after script compilation and a new Play session. Android needs a rebuilt APK.

## Verification and remaining checks

- Editor and Android script compilation checked using the installed Unity Roslyn compiler and the project's generated compiler arguments.
- `Tools/Tests/LegMotionDeadbandTests.cs` runs the actual filter against Unity's managed Vector3 implementation. All 1,014 assertions passed, including standing jitter, accumulated slow movement, immediate kicks, independent legs, return to stance, invalid samples, and reacquisition.
- Live Editor/device verification was blocked by automatic approval review rejecting the Unity connection with a usage-limit error. Camera startup, rendered foot contact, punch contact, and frame time are not yet runtime verified.

When live testing is available, check a fresh launch with each avatar, switch Adam → Remy → Adam without changing camera settings, stand still, step slowly, and kick each leg. Confirm the toes stay above the court and punches can reach. If the camera still needs a menu change, record the now-labeled field and selected camera name; the old placeholder `Option B` did not identify the actual setting.
