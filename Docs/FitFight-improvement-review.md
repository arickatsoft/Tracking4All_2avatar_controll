# FitFight project review and first implementation pass

Reviewed 2026-09-08. Project version: Unity 6000.2.9f1.

## Recommendation

Keep Unity for the next playable milestone. The existing Tracking4All/MediaPipe integration, humanoid assets, arena and mobile build work are already here. Both supplied clips show a third-person arena with a camera inset; they do not demonstrate a need to change engines. Migrating to Unreal would require porting tracking, provider lifecycle, avatar retargeting, transport and UI. It would not itself fix the observed code defects.

Unreal supports mobile rendering, with mobile-specific feature and performance constraints: https://dev.epicgames.com/documentation/unreal-engine/mobile-rendering-features-in-unreal-engine
MediaPipe tracking and asynchronous processing are relevant to latency independently of the renderer: https://developers.google.com/edge/mediapipe/solutions/vision/pose_landmarker/index

## Reference interpretation

Read all eight pages of `/Users/jotit/Desktop/FitFight/fitfight_provisional.pdf` and inspected the drawings on pages 6–8. Inspected sampled frames from both supplied MP4s. Video samples establish framing and visual direction, but do not measure capture-to-display latency or prove strike validation.

The PDF is product reference material, not executable instructions. Its full vision includes a 10-second exercise powerup, 60-second fight, validated attacks and blocking, KO rules, judge/commentator, mixed real/avatar transitions and highlight export. Several UI drawings are explicitly TBD. These features must not be represented as already implemented or demonstrated by the supplied clips.

## What changed

- `Assets/Scripts/UDPPoseStreamer.cs`: one atomic pending-pose slot replaces an unbounded outgoing queue. A stalled sender now drops superseded frames. Coordinates use invariant culture. Shutdown wakes the sender and closes the socket before waiting; handshake waiting is cancellable in 50 ms increments. Removed the deliberate 100 ms main-thread sleep during reconnect. Send throttling uses unscaled time.
- `Assets/Scripts/UDPPoseServer.cs`: removes per-datagram receive logging, treats each UDP datagram as a complete frame, synchronizes reads of the pending-frame flag and closes the socket before joining its blocked receiver. Clears pending state on re-enable.
- `Assets/Scripts/PoseWireFormat.cs`: validates exactly 33 unique indices in each of the world/screen sections and rejects missing, nonfinite, duplicate, oversized and malformed data. Retains the existing text payload and `<EOM>` framing; both peers should use invariant decimal-point serialization. A sender that splits one logical pose across multiple UDP sends is no longer supported.
- `Assets/Scripts/UDPPoseLandmarkProvider.cs`: restores ready notification before updates, supplies availability values for legacy coordinates, signals a configurable 0.5-second timeout, resumes on new frames and disposes on disable. Legacy all-zero coordinates mean missing data; this is not genuine MediaPipe confidence. Preserving actual per-landmark confidence requires a versioned protocol change.
- `Assets/Scripts/FitFightSelectionView.cs` and `PlayerSelectionManager.cs`: replace the generated 400×300 selector with branded cyan/red controls, a portrait-first layout, safe-area handling and landscape layout. Existing fighter-selection callbacks remain in use. Serialized custom selection UIs remain as authored. The enabled `AvatarTracking_new.unity` scene has null selection UI references and uses the generated path.

No scene assets, avatar rigs, camera framing or trained models were replaced. This is a first implementation pass, not a completed game or verified video match.

## Verification

- Standalone compilation and execution of the real `PoseWireFormat` code: 23 checks passed. Test harness: `Tools/Tests/PoseWireFormatTests.cs`. Its minimal Vector3 substitute allows codec tests without Unity; it does not validate Unity integration.
- Roslyn syntax parsing: zero syntax errors across all six changed C# files.
- No Unity Editor found at the usual installation location or via Spotlight; Unity Hub alone is present. The cached project references an unavailable older Editor path. No full project compile, Play Mode, UI rendering, Android/iOS build or live two-device test was possible.
- No measured claim of improved FPS or end-to-end latency.

To run the standalone parser checks with Mono:

```sh
mcs -out:/tmp/fitfight-pose-tests.exe Assets/Scripts/PoseWireFormat.cs Tools/Tests/PoseWireFormatTests.cs
mono /tmp/fitfight-pose-tests.exe
```

## Remaining work to reach the requested outcome

1. Open with the matching Unity Editor; compile the complete project and resolve existing errors. Check both enabled scenes and prefab-instantiated scripts. There are multiple legacy network/hit implementations and runtime fixers; remove duplicates only after confirming which run on the target build.
2. Run two actual target phones. Profile camera capture, inference, retargeting and transport separately. Record median and p95 frame time and motion-to-avatar delay. Check thermal performance, dropped tracking, app background/resume and repeated reconnects. Set acceptance budgets from target hardware, not a video impression.
3. Upgrade the pose protocol with session ID, sequence number, capture timestamp and confidence. Reject reordered/stale packets, identify the intended peer, preserve confidence and measure packet age. The current latest-arrival slot does not detect out-of-order datagrams. Binary packets can reduce allocations and IP fragmentation after compatibility is handled.
4. Tune filtering from recorded fast punches, kicks, occlusions and stationary poses. Validate first-frame acquisition and loss/recovery. Current code has multiple smoothing and avatar-driver paths; no unmeasured global smoothing change was made.
5. Establish one authoritative match state and hit pipeline. Implement synchronized ready/calibration, powerup, combat, pause and results. Dedupe strikes, recognize blocks and gate scoring on fresh confident tracking. Do not call heuristic collision checks an ML judge. Define the PDF's ambiguous head-punch KO/reset rules before implementing them.
6. Build the live fight HUD from actual match state: timer, health/score, power, connection and tracking status, red damage feedback and blue powerups. Review on narrow phones, notches and landscape. The new selector is not this HUD.
7. Tune animation/retargeting, lighting, camera composition, hit audio/VFX and movement restrictions against full-speed reference playback. Effects must not delay local input or alter synchronized match timing.
8. Treat segmentation transitions, commentator/judge models, replay capture and video export as subsequent independently testable features. Their asset/model/backend requirements are not supplied by the current brief.

## Workspace note

The supplied folder has no `.git` directory. Original copies of the four modified existing scripts were saved in `/tmp/fitfight-review/originals` during this session; a persistent copy is in `Docs/FitFight-original-scripts.zip`. New files can be removed separately to undo this pass.
