#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
pose_ndk="/Applications/Unity/Hub/Editor/6000.6.0f1/PlaybackEngines/AndroidPlayer/NDK"
for pose_abi in arm64-v8a armeabi-v7a; do
  cmake -S NativePose/src -B "NativePose/build/$pose_abi" -DCMAKE_BUILD_TYPE=Release -DCMAKE_TOOLCHAIN_FILE="$pose_ndk/build/cmake/android.toolchain.cmake" -DANDROID_ABI="$pose_abi" -DANDROID_PLATFORM=android-24 -DANDROID_STL=c++_static -Dncnn_DIR="$PWD/NativePose/sdk/ncnn-20260526-android/$pose_abi/lib/cmake/ncnn"
  cmake --build "NativePose/build/$pose_abi" -j 4
  cp "NativePose/build/$pose_abi/libfitfight_pose.so" "Assets/Plugins/Android/$pose_abi/"
done
cmake -S NativePose/src -B NativePose/build/macos -DCMAKE_BUILD_TYPE=Release -DNCNN_SDK="$PWD/NativePose/sdk" '-DCMAKE_OSX_ARCHITECTURES=arm64;x86_64'
cmake --build NativePose/build/macos -j 4
cp NativePose/build/macos/libfitfight_pose.dylib Assets/Plugins/macOS/
codesign --force --sign - Assets/Plugins/macOS/libfitfight_pose.dylib
cp NativePose/converted/Body26.param Assets/BodyTracking/Resources/PoseComparison/NcnnParam.bytes
cp NativePose/converted/Body26.bin Assets/BodyTracking/Resources/PoseComparison/NcnnWeights.bytes
