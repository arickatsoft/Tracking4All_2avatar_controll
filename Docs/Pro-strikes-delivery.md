# Pro strikes and punch fixes — app 1.21

APK `Builds/FitFight-pro-strikes.apk` — **1.21, version code 14**, 286,764,695 bytes, SHA-256
`024d895fc7369845d2828d23a988ac21cb0770451713120314e5238e72c45963`. Build 0 errors / 753 warnings, signature verified.
Source backups: `Docs/ImplementationBackups/20261010-v121/`. Phone sessions A/B/C are archived under `Logs/MediaPipe/20261010-{A,B,C}/input/`.

## Issues from the A/B/C videos — cause and fix

| Seen in video | Cause found in the recording | Fix |
|---|---|---|
| Hand lifts up when returning from slow punches (B, C, A) | The animated punch ends after ~0.25 s; the arm was then handed back to tracking while the real arm was still out, and a forward arm projects upward from the floor camera (fist to 1.76 m for ~0.4 s) | The punching arm stays on the punch line while the player's arm is out and returns along it, never upward (monotonic, ≤1.5 s) |
| Left punches detected during right punches (B) | Support-hand drift committed at 0.2 wrist travel | Commit only at **0.46 torso-lengths** of upward travel from the last quiet guard (real punches peak 0.57–1.14, false 0.20–0.46) |
| Start-and-withdraw counted as a punch (B, C) | Same early commit | Same rule; before commit the avatar arm *follows* the real arm, so a withdrawn start looks withdrawn |
| Arms forward rendered as arms up (A) | Tracked arms copied the floor-view upward projection | No overhead arms in boxing: height above the shoulder becomes forward reach. Raised arms are treated as punches, as requested; only arms out to the sides remain a gesture |
| Front kick looked like a sideways leg bend (A) | Foot went straight to knee height with a 90° bent knee and flat foot | Real teep: high chamber, extension into the bag's midsection with a nearly straight leg, hips set back + trunk counter-lean, ankle neutral (ball of foot), re-chamber, plant |

Also: punches are blocked while a kick is executing (balance arm swings committed in A), downward hand travel never counts (torso bends, kick swings), a depth-only spike no longer resets a hand mid-punch, and a stalled start re-arms immediately (it previously locked out the next real punch). The punch target is re-aimed at commit (swinging bag).

**Animation.** Boxing punches are now procedural: guard stance + hips/trunk rotation into the punch (cross 28°, jab 12°, small weight shift) + a fully extended arm aimed from the rotated shoulder. The authored punch clips were removed from the boxing body because the left clip crouched the avatar with its head on the bag. Side kicks: high inward chamber, 60° hip turn, lean away, sole toward the target. Knees: slight lean back. Visual captures: `Logs/Practice/boxing-*-{contact,hold,return}.png`, `game-{front,side,knee,generic}-*.png`, `game-overhead.png`.

**Detection on your sessions (replayed):** B and C now match every cue exactly — LL / LLLL / RR / RRRR / LRLRL, no left during right punches, withdrawn starts removed. A: LLRR, alternating LRLRLR, no punches during kicks, bends or squats. The 1.16 and 1.17 fast blocks become clean alternations (the old detector committed single right strokes twice). Median added commit delay ~38–47 ms; the avatar starts moving on the first frame as before.

## Latency/infrastructure

Worker clock resynced every frame (fixes the −73990/74000 ms summary fields). New per-frame JSONL fields: `thermalStatus`, `thermalHeadroom`, `performanceHint`, `pipelineDepth` (camera HAL), `displayRefreshHz`. New options (Diagnostics → Tracking): *Android performance hint* (ADPF hint session for the inference thread, default **on**), *Camera RECORD template* (A/B, default off), 120 Hz now also requests a 120 Hz display mode. Exposure-cap brightness loop damped (no ISO overshoot). Exposure cap stays Auto.

## Verification

Host: 39 new 1.21 checks (detector replays of A, B, C, 1.16 + animation math), strike-contact 20, game-body 109, smooth 57, boxing-priority 93, October replay 76, MediaPipe boxing 24, punch 167, full-body 70, frame-driven pipeline, engine comparison, Java host 86 — all pass. Scenes: 8 synthetic kicks each land once on the bag body (0.27–0.38 m above its bottom); 1.16 and 1.17 full replays: all 12 main punch decisions hit, knee holds non-scoring, foot step 0.00 cm; punch scene passes, returning fist stays below the head. Tests encoding the old policies were updated with the reason in comments: partial 0.35 lift, stalled-preparation timing, one withdrawn stroke in the 7 Oct session, raised arms rendered forward.

Not verified: phone feel, thermal behaviour with the hint, whether the RECORD template or 120 Hz help on this device.

## Phone test

1. `adb install -r Builds/FitFight-pro-strikes.apk`, fully close/reopen. Same floor placement and lighting.
2. **Run A:** GAME BODY TEST · 2:44 with defaults. Check punches/kicks look right and the hand never lifts on return.
3. **Run B:** BOXING TEST with defaults — include slow punches, fast right-only punches and start-and-withdraw.
4. Optional (one change per run, BOXING TEST): *Camera RECORD template* on; then off + 120 Hz (Samsung: Settings › Display › Motion smoothness = Adaptive).

Send JSONL + summaries + screen video per run.
