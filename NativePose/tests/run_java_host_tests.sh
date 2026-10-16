#!/bin/bash
# Host checks for the Android camera Java classes (no Camera2 device). Requires Unity's OpenJDK.
set -euo pipefail
cd "$(dirname "$0")/../.."
JDK=/Applications/Unity/Hub/Editor/6000.6.0f1/PlaybackEngines/AndroidPlayer/OpenJDK
ANDROID_JAR=/Applications/Unity/Hub/Editor/6000.6.0f1/PlaybackEngines/AndroidPlayer/SDK/platforms/android-36/android.jar
OUT=$(mktemp -d)
clang++ -std=c++17 -shared -o "$OUT/libfitfight_pose.dylib" NativePose/tests/host-shim/fitfight_camera_jni_host.cpp \
  -I"$JDK/include" -I"$JDK/include/darwin" Assets/Plugins/macOS/libfitfight_pose.dylib -Wl,-rpath,"$PWD/Assets/Plugins/macOS"
"$JDK/bin/javac" -nowarn -d "$OUT" -cp "$ANDROID_JAR" Assets/Plugins/Android/FitFightCamera*.java NativePose/tests/Camera*Test.java
for t in com.fitfight.camera.CameraLeaseTest CameraRecoveryTest CameraPixelsTest; do
  "$JDK/bin/java" -Djava.library.path="$OUT" -cp "$OUT:$ANDROID_JAR" $t
done
