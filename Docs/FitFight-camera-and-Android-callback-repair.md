# Camera composition and Android tracking repair

## September 11 follow-up checks

Per-leg reliability now follows the mirrored output-joint mapping. Disabling stabilization no longer marks both legs unreliable and forces the standing fallback. Partial-pose restoration orders bones by hierarchy depth so a late parent rotation cannot overwrite a restored child.

Editor C# compilation passed and all 1,019 standalone leg-filter/fallback checks passed. Live kick testing remains pending: the Unity bridge was not listening and ADB reported the phone offline. The saved Android compiler response predates the existing `Assets/BodyTracking/Runtime` additions and fails to include those source files/dependencies; its failure is not a verified Android source defect. A fresh Unity Android build is required to regenerate the compilation inputs. No Android APK was built or installed in this follow-up.

## Verified scaled-shoe correction and tracked standing fallback

Live Editor inspection established Adam_Big has scale (2,2,2). With the old BakeMesh call, TransformPoint reported sole Y=-0.8852564 while independently skinning source vertices with bone matrices gave Y=-0.6461739: a 0.239-unit error. BakeMesh(mesh, true) compensates renderer scale before TransformPoint. This is now used in both court calibration and contact correction. After correction, baked and independently skinned sole measurements agree within floating-point precision for Adam and Remy.

Tracking4All now exposes per-leg confidence to the avatar presentation. When legs are unseen, the guard graph supplies the pelvis and missing legs while measured upper-body bone rotations are restored. Reliable legs retain tracking, including lifts and kicks. Fallback ankles are level and use standing foot orientations. Root contact persists instead of being undone each Update, and the guard's stage anchor follows the grounded root.

Clearance is now approximately 1.6 mm for these characters. Editor contact measurements for both selections matched that clearance. The final captured view showed Adam moving his arm with the webcam while retaining the standing lower body. Full-body kick behavior and Android execution have not been exercised in this pass.

Sun/fill intensities are 1.25/0.38. Shadow bias/normal bias are reduced to 0.008/0.015 to reduce detached contact shadows. The visual still uses the existing lower-detail character assets, not the reference's meshes. C# Editor/Android compilation and 1,019 leg-filter/fallback checks passed before final dead-code cleanup.

## Follow-up from the 16:01 screenshot

Removed the five-degree preset pitch from `Pose Tracking Solution Adam` in `Assets/Assets/current.unity`, retaining its 180-degree facing direction. FitFight also requests upright TransformTargets coordinates for its scene: tracking-source pitch/roll no longer rotate the complete pose, while yaw, translation, scale and measured joint motion remain available.

Ground contact and initial court calibration now sample triangle-referenced shoe vertices rather than every vertex in the imported mesh. Contact is rechecked through Camera.onPreCull in addition to LateUpdate so later transform writers cannot leave a vertical gap at render time. These are corrective changes based on code inspection; a new live capture is still required to confirm the screenshot's floating behavior is resolved.

## Ground contact during tracking

Ground correction now translates the root downward as well as upward until the lowest shoe reaches the court. Previously it could resolve penetration but never remove a floating gap. The signed correction is undone before the next tracking update to avoid accumulating offsets. Relative foot height is preserved for a visible kick; this is a supporting-foot constraint, not a jump simulation.

With leg stabilization enabled (the default), missing or non-finite knee/ankle/heel/toe landmarks now produce a slightly bent standing leg instead of accepting an invalid first frame or holding the last kick indefinitely. Visibility checks follow mirrored landmark selection. Reliable landmarks reacquire immediately, then use the existing motion deadband. The opposite visible leg is unaffected by fallback. Missing-foot behavior uses an inferred standing pose; it cannot reconstruct an unseen kick.

The leg filter/fallback suite passes 1,019 checks, covering missing initial legs, loss during a kick, opposite-leg independence and reacquisition. Live phone ground contact still needs verification after rebuilding.

## Contrast correction from now.png

The supplied screenshot showed bleached paving, weak shadow contrast and silver hair highlights. This pass reduces sun intensity to 1.1 and fill to 0.30, restores shadow strength to 0.85, and lowers ambient illumination while retaining camera-side fill. It supersedes the brighter-light values below.

Paving albedo is warm brown-gray, with antialiased narrow joints, grain and distance-faded procedural normal relief. The fence uses darker steel and a taller lower wall with raised panel separators. Cloud coverage is reduced. Hair material instances disable specular highlights/reflections and lower texture-driven gloss as well as scalar smoothness. Selected-player view and stationary camera behavior remain unchanged.

Editor and Android C# compilation passed. Shader rendering and the resulting contrast have not been verified in a new live capture. Existing meshes and textures still limit fidelity compared with the reference. Restart Play mode or rebuild Android to load this pass.

## Follow-up: brighter fighters and stationary selected-player camera

- Selecting Adam now frames behind Adam; selecting Remy frames behind Remy. HUD names follow the selected foreground fighter. This supersedes the fixed Remy viewpoint described in the earlier reference pass below.
- The gameplay camera locks its world position and rotation after staging. It only reframes on player selection or a viewport/safe-area change, with 6% extra distance for movement. Tracking no longer drives pan, orbit or zoom. Large movements can still leave a fixed camera's field of view.
- Court rotation and lighting are set on selection instead of following fighter movement every frame.
- Sun intensity is 1.35, shadow strength 0.65, with a shadow-free camera-side fill at 0.65 and brighter ambient light.
- Editor and Android C# compilation passed for this follow-up. Live appearance and device behavior have not been verified; a new Android build is required.

## Android failure observed on the connected Galaxy S21

The installed app displayed its webcam preview but failed before starting pose inference:

```
NotSupportedException: To marshal a managed method, please add an attribute named
'MonoPInvokeCallback' to the method definition.
Mediapipe.Unity.OutputStream<Detection>::InvokeIfOutputStreamFound
```

The callback already had the attribute, but lived inside a generic class. `OutputStream.cs` now registers a static callback on the non-generic `OutputStreamCallbackDispatcher`. A global ID routes each native packet to its managed stream, across different packet types and graph instances. The delegate is rooted, disposed streams are removed, and exceptions are caught at the native boundary. GPU inference remains enabled as before.

## Camera and scene

- Fighter separation increased from `0.62` to `0.82` times the taller fighter's height.
- The latest reference pass uses a 34-degree offset behind Remy, 36-degree vertical field of view, and 5-degree downward pitch. Both input selections retain Remy on the left and Adam on the right.
- Framing now solves camera distance and horizontal/vertical offsets together. Padded joint volumes fit below the open sky band and above the bottom margin, avoiding unnecessary zoom-out from empty bounding-box corners.
- The idle stance is narrowed with a two-bone leg solve preserving segment lengths and foot orientation. Fresh tracked poses bypass this adjustment.
- HUD cards sit lower in the safe area with blue-gray rounded backgrounds and rounded identity stripes. These stripes are decorative; this pass does not implement health simulation.
- Hair material instances receive brown/dark-brown tints and lower gloss. The original character meshes, hairstyles and texture detail remain unchanged.
- Warm directional light casts shadows toward the foreground/right. A revised procedural cloud sky and combined distant greenery mesh complement the thinner fence and warm paving.
- The idle animation is aligned toward the opponent instead of inheriting the imported clip's sideways heading. Live tracking remains free to turn the torso.
- Fence moved farther away and lowered; thinner wires, warmer textured paving, and more blue sky bring the court closer to the reference composition.
- Android Dynamic Batching is disabled in Player Settings.

`FitFight-rear-camera-preview.png` is an earlier 40-degree Editor preview, BEFORE the latest reference pass above. It was captured with Remy's tracking temporarily disabled; temporary changes were reverted by stopping Play mode. It does not show the latest UI and is not evidence of Android tracking accuracy.

## Tracking lifecycle repairs

- Disabled MediaPipe solutions now stop and reset their running/wait state and remove lifecycle subscriptions.
- Restart uses the solution's own ready image source instead of waiting indefinitely for the sample's global source to dispose.
- The image-source run coroutine is cleared when stopped.
- Pose adapters reconcile source readiness after a stop/restart occurring between frames, and avoid duplicate subscriptions.
- Low-visibility shoulder detections do not animate a body from an empty-room estimate. Previously accepted legs are held when their knee or ankle loses visibility.

## Verification

- Editor play tests exercised Remy → Adam → Remy. Each selected puppet received updates and reported a fresh pose; the unselected source was stopped.
- Earlier idle framing was captured and inspected: both full bodies visible, with Remy on the left and Adam on the right. The latest visual pass has not been rendered for inspection.
- Latest Editor and Android C# compilation passed. Standalone framing tests passed 120,000 projected-point checks, including asymmetric framing offsets and comparison against the earlier centered fit.
- The standalone leg filter passed 1,014 assertions.
- Android IL2CPP generated `ReversePInvokeWrapper_OutputStreamCallbackDispatcher_Receive` for the new callback. A development APK built with zero errors, installed successfully, and launched on the connected Galaxy S21. Final phone logs/screenshot validation was blocked by automatic approval review's usage limit; landmark output after this repair is not yet verified.
- The installed `Builds/FitFight-tracking-debug.apk` predates the latest visual pass. Rebuild to include the new camera, stance, HUD, lighting and sky. Runtime presentation is created after selecting a fighter in Tracking4All; the stopped Scene view retains the original serialized environment.
