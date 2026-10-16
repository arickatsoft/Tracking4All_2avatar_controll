# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Tracking4All is a Unity-based real-time motion tracking framework that integrates MediaPipe for body pose, hand, and face tracking. It's designed for applications like FitFight (a motion-controlled game) and avatar animation.

**Unity Version**: 6000.0 or compatible (check ProjectVersion.txt)
**Product Name**: Tracking4AllUnity

## Build and Run Commands

### Opening the Project
Open the project in Unity Editor:
```bash
# The project root contains .sln files and Unity project structure
# Open with Unity Hub or directly via Unity Editor
```

### Building
Unity builds are typically done through the Editor (File > Build Settings), but can be automated:
```bash
# Build for specific platform (example for macOS/iOS)
# Unity builds are typically done via Editor UI or Unity Cloud Build
```

### Running Tests
Unity uses its own test framework accessible via Window > General > Test Runner in the Editor.

## Architecture Overview

### Core System Architecture

The codebase follows a **Provider-Adapter-Modifier** pattern:

1. **Providers** (`IProvider<IDENTIFIER, DATA>`) - Source data from tracking systems
   - Located in `Assets/Tracking4All/Scripts/Core/`
   - Examples: `LandmarkProvider`, `NormalizedLandmarkProvider`
   - Emit lifecycle events: `OnReady`, `OnUpdated`, `OnDisposed`, `OnTimedOut`

2. **Adapters** (`AdapterBase<FROM_DATA, IDENTIFIER, DATA>`) - Transform and route data
   - Located in `Assets/Tracking4All/Scripts/Core/`
   - `SolutionAdapter` - Base for solution-specific adapters
   - `ProxyAdapter` - Forwards data with optional remapping
   - `UnityAdapter` - Bridges to Unity components
   - Handle timeout logic and data validation

3. **Modifiers** (`IModifier<T>`) - Filter/transform data streams
   - Located in `Assets/Tracking4All/Scripts/Modifiers/`
   - Organized in stacks (`ModifierStack`)
   - Examples: `EuroSmooth`, `SimpleSmooth`, `PresenseCutoff`, `Freeze`
   - Applied to landmark, normalized landmark, joint, and face data

4. **Solutions** (`SolutionBase`) - Complete tracking pipelines
   - Located in `Assets/Tracking4All/Scripts/Solutions/`
   - Currently implements: `PoseSolution`
   - Each solution integrates providers, adapters, and modifiers
   - Implements `ISolutionSettings` for configuration

### Data Flow

```
MediaPipe Graph → LivingAdapter → Provider → ModifierStack → SolutionAdapter → Unity Components
```

1. MediaPipe processes camera input and generates tracking data
2. `LivingAdapter` containers manage MediaPipe graph lifecycle
3. Raw data flows through provider interfaces
4. Modifier stacks apply smoothing, filtering, transformation
5. Solution adapters route to specific use cases (pose, hands, face)
6. Unity components consume processed data for visualization/game logic

### Key Components

- **Tracking4All.cs** - Singleton manager, handles FPS tracking, screen orientation, provider lifecycle
- **AdapterBase.cs** - Base class for all adapters, manages groups, timeouts, lifecycle events
- **LivingUnityAdapterContainer** - Unity MonoBehaviour lifecycle integration
- **RemappedProvider** - Maps one identifier set to another (e.g., MediaPipe landmarks → custom landmarks)

### MediaPipe Integration

Located in `Assets/Tracking4All/Scripts/MediaPipe/`:
- Adapters for pose (`MPPoseNormalizedLandmarkAdapter`), hands (`MPHandLandmarkAdapter`), face
- Identifier mappings (`MPPoseLandmarks`, `MPHandLandmarks`)
- Living adapters manage MediaPipe graph execution context

### FitFight Game Module

Located in `Assets/Tracking4All/Scripts/FitFight/`:
- Collision detection using MediaPipe landmarks
- Hit feedback and notification systems
- Player controls and input handling
- Sound management

### Visualization

Located in `Assets/Tracking4All/Scripts/Visualization/`:
- `LandmarkVisualizer` - Base class for rendering landmarks
- `PoseLandmarkVisualizer`, `HandLandmarkVisualizer` - Specific implementations
- `SegmentationVisualizer2D` - For segmentation mask rendering
- `ImageFrameVisualizer2D` - Camera feed display

## Important Patterns

### Identifier Enums
Landmarks are accessed via enum identifiers (e.g., `PoseLandmarks.NOSE`, `PoseLandmarks.LEFT_WRIST`), not indices. This provides type safety and clarity.

### Group-Based Data
Many providers support multiple groups (e.g., left hand + right hand = 2 groups). Use group index to access the correct data set.

### Lifecycle Management
All adapters follow a strict lifecycle:
1. Construction
2. `ReadyAll()` - Initialize and signal readiness
3. `TickAll()` - Update per frame
4. `DisposeAll()` - Clean up resources

Always implement all three phases in custom solutions.

### Timeout Handling
Providers automatically timeout after `providerAliveThreshold` seconds (default 1.0s) without updates. Handle `OnTimedOut` events to pause dependent systems.

## Third-Party Dependencies

- **MediaPipe Unity Plugin** - Located in `Assets/Tracking4All/Third Party/MediaPipeUnity/`
- **VRM Avatar System** - UniVRM for humanoid avatar support
- **TextMesh Pro** - UI text rendering
- **Unity Netcode for GameObjects** - Multiplayer support (package)
- **Unity Input System** - Modern input handling (package)
- **Unity Animation Rigging** - IK and procedural animation (package)

## Development Notes

### Adding New Solutions
1. Extend `SolutionBase` in `Assets/Tracking4All/Scripts/Solutions/`
2. Implement required provider interfaces
3. Create custom adapters in `Solutions/Adapters/`
4. Override `CreateAdapters()`, `DisposeAdapters()`, `ReadyAdapters()`, `TickAdapters()`
5. Serialize provider and modifier stack fields

### Custom Modifiers
1. Implement `IModifier<T>` interface (e.g., `ILandmarkModifier`)
2. Implement `Process()` method for data transformation
3. Add to appropriate modifier stack in inspector

### Working with Landmarks
- **Landmark** - 3D world space coordinates (x, y, z in meters)
- **NormalizedLandmark** - Screen space coordinates (0-1 range, relative to camera frame)
- Use `PerspectiveFlip` and `Mirror` settings in `ISolutionSettings` to adjust coordinate systems

### MediaPipe Models
MediaPipe model files (.tflite) should be in `Assets/StreamingAssets/`. The MediaPipe plugin handles loading automatically.

## Common Gotchas

1. **Execution Order**: `Tracking4All` has `DefaultExecutionOrder(-1000)`, solutions have `-500`. Respect these for proper initialization.
2. **DontDestroyOnLoad**: `Tracking4All` persists across scenes - ensure singleton pattern is maintained.
3. **Thread Safety**: MediaPipe runs on separate threads. Always use Unity main thread for Unity API calls.
4. **Provider Lifecycle**: Subscribers to `OnReady` receive immediate callback if already ready (auto-ready behavior).
5. **Group Indexing**: Group indices are 0-based. Verify group count with `GroupSize` before accessing.

## File Organization

```
Assets/Tracking4All/Scripts/
├── Core/               # Base classes, providers, adapters, lifecycle management
├── Solutions/          # Complete tracking solutions (Pose, Holistic, etc.)
├── Modifiers/          # Data transformation and filtering
├── MediaPipe/          # MediaPipe-specific adapters and identifiers
├── Visualization/      # Rendering landmarks and debug visualization
├── UI/                 # Settings UI and debug menus
├── FitFight/          # Game-specific collision and gameplay logic
├── Avatar/            # Avatar animation and rigging
├── Identifiers/       # Landmark enum definitions
├── Utilities/         # Helper classes and extensions
└── Editor/            # Unity Editor extensions and tools
```

## Performance Considerations

- Target frame rate is set to 120 FPS in `Tracking4All.Awake()`
- Use `EuroSmooth` for balance between smoothness and latency
- `SimpleSmooth` is faster but less responsive
- Timeout thresholds affect responsiveness - tune per use case
- Group-based operations are optimized - avoid per-frame allocations