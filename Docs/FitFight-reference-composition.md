# Reference composition correction — 2026-09-09

The supplied first.png showed three actual defects: the framed camera reference was a preview camera instead of the game camera; the old road was still rendered; and missing pose input left the avatars in their bind/T-pose.

Changes:
- Resolve the real game camera and keep preview cameras disabled. Commit framing immediately before camera culling, after legacy rig updates.
- Use humanoid joints with a body margin, rather than the oversized imported animation bounds, to fit both fighters. The offset view puts Remy on the left and Adam opposite him. Position both fighters around the existing midpoint at a defined body-relative spacing, align their feet, and face them toward each other.
- Use the existing fighting-idle clip when pose updates are absent or stale. Disable its root motion and hold the last staged/tracked root position while untracked, so the idle animation cannot walk Adam out of the court. Fresh tracked joint data takes precedence. This adds pose freshness reporting to AvatarJointProvider/Avatar; physical live-camera tracking still needs device validation.
- Hide old opaque MeshRenderer scenery across the current scene, excluding avatar descendants and UI, rather than relying on an Environment object name. Keep original colliders intact. Reassert renderer hiding after legacy scripts run and restore original enabled renderers on teardown.
- Build a closer three-sided steel court. Surface shader provides concrete tile joints, subtle grain and court lines without overlapping seam geometry. A procedural sky shader and warm directional light replace the flat cyan scene. No reference-image characters are baked into the background.
- Add the required CanvasRenderer to rounded HUD cards. Preserve and reparent the actual Menu button into the HUD canvas so the card does not obscure it; its listeners remain intact.

Verification:
- Unity 6000.6 compiled the changes and completed domain reloads.
- Ran the actual game in Play Mode repeatedly and inspected captured game-view renders while fixing incorrect camera selection, oversized animation bounds and idle root-motion drift.
- Final preview: Docs/FitFight-reference-preview.png. This is an actual Unity game-view screenshot, not an AI mockup. It uses the existing character assets and their material quality.
- Both bodies and shoes are visible in the narrow portrait view; fighters stay on the court and use the authored guard idle. The old road is absent and Menu is legible.
- Returned the Editor to Edit Mode after checking. The helper Tools > FitFight > Capture presentation preview captures the current Play Mode game view to /tmp and restores the selector after two seconds.
- No iPhone/Samsung deployment or two-device tracking test was performed. The color strips remain fighter identity accents, not an implemented health/scoring system.

The setup is applied at runtime by the existing player selector. Original serialized scene assets were not overwritten. A snapshot of the earlier FitFight presentation scripts is in Docs/FitFight-reference-originals.zip.
