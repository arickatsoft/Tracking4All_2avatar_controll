using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// An assortment of helper functions.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Flips the right and left landmark for the input.
        /// </summary>
        /// <param name="landmark"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static PoseLandmarks Flip(this PoseLandmarks landmark)
        {
            switch (landmark)
            {
                case PoseLandmarks.NOSE:
                    return PoseLandmarks.NOSE;

                case PoseLandmarks.LEFT_EYE_INNER:
                    return PoseLandmarks.RIGHT_EYE_INNER;
                case PoseLandmarks.RIGHT_EYE_INNER:
                    return PoseLandmarks.LEFT_EYE_INNER;

                case PoseLandmarks.LEFT_EYE:
                    return PoseLandmarks.RIGHT_EYE;
                case PoseLandmarks.RIGHT_EYE:
                    return PoseLandmarks.LEFT_EYE;

                case PoseLandmarks.LEFT_EYE_OUTER:
                    return PoseLandmarks.RIGHT_EYE_OUTER;
                case PoseLandmarks.RIGHT_EYE_OUTER:
                    return PoseLandmarks.LEFT_EYE_OUTER;

                case PoseLandmarks.LEFT_EAR:
                    return PoseLandmarks.RIGHT_EAR;
                case PoseLandmarks.RIGHT_EAR:
                    return PoseLandmarks.LEFT_EAR;

                case PoseLandmarks.MOUTH_LEFT:
                    return PoseLandmarks.MOUTH_RIGHT;
                case PoseLandmarks.MOUTH_RIGHT:
                    return PoseLandmarks.MOUTH_LEFT;

                case PoseLandmarks.LEFT_SHOULDER:
                    return PoseLandmarks.RIGHT_SHOULDER;
                case PoseLandmarks.RIGHT_SHOULDER:
                    return PoseLandmarks.LEFT_SHOULDER;

                case PoseLandmarks.LEFT_ELBOW:
                    return PoseLandmarks.RIGHT_ELBOW;
                case PoseLandmarks.RIGHT_ELBOW:
                    return PoseLandmarks.LEFT_ELBOW;

                case PoseLandmarks.LEFT_WRIST:
                    return PoseLandmarks.RIGHT_WRIST;
                case PoseLandmarks.RIGHT_WRIST:
                    return PoseLandmarks.LEFT_WRIST;

                case PoseLandmarks.LEFT_PINKY:
                    return PoseLandmarks.RIGHT_PINKY;
                case PoseLandmarks.RIGHT_PINKY:
                    return PoseLandmarks.LEFT_PINKY;

                case PoseLandmarks.LEFT_INDEX:
                    return PoseLandmarks.RIGHT_INDEX;
                case PoseLandmarks.RIGHT_INDEX:
                    return PoseLandmarks.LEFT_INDEX;

                case PoseLandmarks.LEFT_THUMB:
                    return PoseLandmarks.RIGHT_THUMB;
                case PoseLandmarks.RIGHT_THUMB:
                    return PoseLandmarks.LEFT_THUMB;

                case PoseLandmarks.LEFT_HIP:
                    return PoseLandmarks.RIGHT_HIP;
                case PoseLandmarks.RIGHT_HIP:
                    return PoseLandmarks.LEFT_HIP;

                case PoseLandmarks.LEFT_KNEE:
                    return PoseLandmarks.RIGHT_KNEE;
                case PoseLandmarks.RIGHT_KNEE:
                    return PoseLandmarks.LEFT_KNEE;

                case PoseLandmarks.LEFT_ANKLE:
                    return PoseLandmarks.RIGHT_ANKLE;
                case PoseLandmarks.RIGHT_ANKLE:
                    return PoseLandmarks.LEFT_ANKLE;

                case PoseLandmarks.LEFT_HEEL:
                    return PoseLandmarks.RIGHT_HEEL;
                case PoseLandmarks.RIGHT_HEEL:
                    return PoseLandmarks.LEFT_HEEL;

                case PoseLandmarks.LEFT_FOOT:
                    return PoseLandmarks.RIGHT_FOOT;
                case PoseLandmarks.RIGHT_FOOT:
                    return PoseLandmarks.LEFT_FOOT;

                default:
                    throw new ArgumentOutOfRangeException(nameof(landmark), landmark, null);
            }
        }
        /// <summary>
        /// Flips the right and left handedness for the input.
        /// </summary>
        /// <param name="handedness"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Handedness Flip(this Handedness handedness)
        {
            switch (handedness)
            {
                case Handedness.LEFT:
                    return Handedness.RIGHT;
                case Handedness.RIGHT:
                    return Handedness.LEFT;

                default:
                    throw new ArgumentOutOfRangeException(nameof(handedness), handedness, null);
            }
        }
        public static PoseJoints Flip(this PoseJoints joint)
        {
            switch (joint)
            {
                case PoseJoints.Hips:
                    return PoseJoints.Hips;
                case PoseJoints.Spine:
                    return PoseJoints.Spine;
                case PoseJoints.Chest:
                    return PoseJoints.Chest;
                case PoseJoints.Neck:
                    return PoseJoints.Neck;
                case PoseJoints.Head:
                    return PoseJoints.Head;

                case PoseJoints.RightShoulder:
                    return PoseJoints.LeftShoulder;
                case PoseJoints.RightUpperArm:
                    return PoseJoints.LeftUpperArm;
                case PoseJoints.RightLowerArm:
                    return PoseJoints.LeftLowerArm;
                case PoseJoints.RightHand:
                    return PoseJoints.LeftHand;

                case PoseJoints.LeftShoulder:
                    return PoseJoints.RightShoulder;
                case PoseJoints.LeftUpperArm:
                    return PoseJoints.RightUpperArm;
                case PoseJoints.LeftLowerArm:
                    return PoseJoints.RightLowerArm;
                case PoseJoints.LeftHand:
                    return PoseJoints.RightHand;

                case PoseJoints.RightUpperLeg:
                    return PoseJoints.LeftUpperLeg;
                case PoseJoints.RightLowerLeg:
                    return PoseJoints.LeftLowerLeg;
                case PoseJoints.RightFoot:
                    return PoseJoints.LeftFoot;

                case PoseJoints.LeftUpperLeg:
                    return PoseJoints.RightUpperLeg;
                case PoseJoints.LeftLowerLeg:
                    return PoseJoints.RightLowerLeg;
                case PoseJoints.LeftFoot:
                    return PoseJoints.RightFoot;

                default:
                    throw new ArgumentException("Unknown joint provided");
            }
        }
        public static FaceJoints Flip(this FaceJoints joint)
        {
            switch (joint)
            {
                default:
                    return FaceJoints.Head;
            }
        }
        public static HumanBodyBones Flip(this HumanBodyBones bone)
        {
            switch (bone)
            {
                // Head and center bones (no flip required)
                case HumanBodyBones.Head:
                case HumanBodyBones.Hips:
                case HumanBodyBones.Spine:
                case HumanBodyBones.Chest:
                case HumanBodyBones.Neck:
                case HumanBodyBones.UpperChest:
                    return bone;

                // Left to Right
                case HumanBodyBones.LeftShoulder: return HumanBodyBones.RightShoulder;
                case HumanBodyBones.LeftUpperArm: return HumanBodyBones.RightUpperArm;
                case HumanBodyBones.LeftLowerArm: return HumanBodyBones.RightLowerArm;
                case HumanBodyBones.LeftHand: return HumanBodyBones.RightHand;
                case HumanBodyBones.LeftThumbProximal: return HumanBodyBones.RightThumbProximal;
                case HumanBodyBones.LeftThumbIntermediate: return HumanBodyBones.RightThumbIntermediate;
                case HumanBodyBones.LeftThumbDistal: return HumanBodyBones.RightThumbDistal;
                case HumanBodyBones.LeftIndexProximal: return HumanBodyBones.RightIndexProximal;
                case HumanBodyBones.LeftIndexIntermediate: return HumanBodyBones.RightIndexIntermediate;
                case HumanBodyBones.LeftIndexDistal: return HumanBodyBones.RightIndexDistal;
                case HumanBodyBones.LeftMiddleProximal: return HumanBodyBones.RightMiddleProximal;
                case HumanBodyBones.LeftMiddleIntermediate: return HumanBodyBones.RightMiddleIntermediate;
                case HumanBodyBones.LeftMiddleDistal: return HumanBodyBones.RightMiddleDistal;
                case HumanBodyBones.LeftRingProximal: return HumanBodyBones.RightRingProximal;
                case HumanBodyBones.LeftRingIntermediate: return HumanBodyBones.RightRingIntermediate;
                case HumanBodyBones.LeftRingDistal: return HumanBodyBones.RightRingDistal;
                case HumanBodyBones.LeftLittleProximal: return HumanBodyBones.RightLittleProximal;
                case HumanBodyBones.LeftLittleIntermediate: return HumanBodyBones.RightLittleIntermediate;
                case HumanBodyBones.LeftLittleDistal: return HumanBodyBones.RightLittleDistal;

                case HumanBodyBones.LeftUpperLeg: return HumanBodyBones.RightUpperLeg;
                case HumanBodyBones.LeftLowerLeg: return HumanBodyBones.RightLowerLeg;
                case HumanBodyBones.LeftFoot: return HumanBodyBones.RightFoot;
                case HumanBodyBones.LeftToes: return HumanBodyBones.RightToes;

                // Right to Left
                case HumanBodyBones.RightShoulder: return HumanBodyBones.LeftShoulder;
                case HumanBodyBones.RightUpperArm: return HumanBodyBones.LeftUpperArm;
                case HumanBodyBones.RightLowerArm: return HumanBodyBones.LeftLowerArm;
                case HumanBodyBones.RightHand: return HumanBodyBones.LeftHand;
                case HumanBodyBones.RightThumbProximal: return HumanBodyBones.LeftThumbProximal;
                case HumanBodyBones.RightThumbIntermediate: return HumanBodyBones.LeftThumbIntermediate;
                case HumanBodyBones.RightThumbDistal: return HumanBodyBones.LeftThumbDistal;
                case HumanBodyBones.RightIndexProximal: return HumanBodyBones.LeftIndexProximal;
                case HumanBodyBones.RightIndexIntermediate: return HumanBodyBones.LeftIndexIntermediate;
                case HumanBodyBones.RightIndexDistal: return HumanBodyBones.LeftIndexDistal;
                case HumanBodyBones.RightMiddleProximal: return HumanBodyBones.LeftMiddleProximal;
                case HumanBodyBones.RightMiddleIntermediate: return HumanBodyBones.LeftMiddleIntermediate;
                case HumanBodyBones.RightMiddleDistal: return HumanBodyBones.LeftMiddleDistal;
                case HumanBodyBones.RightRingProximal: return HumanBodyBones.LeftRingProximal;
                case HumanBodyBones.RightRingIntermediate: return HumanBodyBones.LeftRingIntermediate;
                case HumanBodyBones.RightRingDistal: return HumanBodyBones.LeftRingDistal;
                case HumanBodyBones.RightLittleProximal: return HumanBodyBones.LeftLittleProximal;
                case HumanBodyBones.RightLittleIntermediate: return HumanBodyBones.LeftLittleIntermediate;
                case HumanBodyBones.RightLittleDistal: return HumanBodyBones.LeftLittleDistal;

                case HumanBodyBones.RightUpperLeg: return HumanBodyBones.LeftUpperLeg;
                case HumanBodyBones.RightLowerLeg: return HumanBodyBones.LeftLowerLeg;
                case HumanBodyBones.RightFoot: return HumanBodyBones.LeftFoot;
                case HumanBodyBones.RightToes: return HumanBodyBones.LeftToes;

                // Default: No flip or unsupported bones
                default:
                    return bone;
            }
        }
        public static FaceBlendshape Flip(this FaceBlendshape input)
        {
            switch (input)
            {
                case FaceBlendshape.BROW_DOWN_LEFT: return FaceBlendshape.BROW_DOWN_RIGHT;
                case FaceBlendshape.BROW_DOWN_RIGHT: return FaceBlendshape.BROW_DOWN_LEFT;

                case FaceBlendshape.BROW_OUTER_UP_LEFT: return FaceBlendshape.BROW_OUTER_UP_RIGHT;
                case FaceBlendshape.BROW_OUTER_UP_RIGHT: return FaceBlendshape.BROW_OUTER_UP_LEFT;

                case FaceBlendshape.CHEEK_SQUINT_LEFT: return FaceBlendshape.CHEEK_SQUINT_RIGHT;
                case FaceBlendshape.CHEEK_SQUINT_RIGHT: return FaceBlendshape.CHEEK_SQUINT_LEFT;

                case FaceBlendshape.EYE_BLINK_LEFT: return FaceBlendshape.EYE_BLINK_RIGHT;
                case FaceBlendshape.EYE_BLINK_RIGHT: return FaceBlendshape.EYE_BLINK_LEFT;

                case FaceBlendshape.EYE_LOOK_DOWN_LEFT: return FaceBlendshape.EYE_LOOK_DOWN_RIGHT;
                case FaceBlendshape.EYE_LOOK_DOWN_RIGHT: return FaceBlendshape.EYE_LOOK_DOWN_LEFT;

                case FaceBlendshape.EYE_LOOK_IN_LEFT: return FaceBlendshape.EYE_LOOK_IN_RIGHT;
                case FaceBlendshape.EYE_LOOK_IN_RIGHT: return FaceBlendshape.EYE_LOOK_IN_LEFT;

                case FaceBlendshape.EYE_LOOK_OUT_LEFT: return FaceBlendshape.EYE_LOOK_OUT_RIGHT;
                case FaceBlendshape.EYE_LOOK_OUT_RIGHT: return FaceBlendshape.EYE_LOOK_OUT_LEFT;

                case FaceBlendshape.EYE_LOOK_UP_LEFT: return FaceBlendshape.EYE_LOOK_UP_RIGHT;
                case FaceBlendshape.EYE_LOOK_UP_RIGHT: return FaceBlendshape.EYE_LOOK_UP_LEFT;

                case FaceBlendshape.EYE_SQUINT_LEFT: return FaceBlendshape.EYE_SQUINT_RIGHT;
                case FaceBlendshape.EYE_SQUINT_RIGHT: return FaceBlendshape.EYE_SQUINT_LEFT;

                case FaceBlendshape.EYE_WIDE_LEFT: return FaceBlendshape.EYE_WIDE_RIGHT;
                case FaceBlendshape.EYE_WIDE_RIGHT: return FaceBlendshape.EYE_WIDE_LEFT;

                case FaceBlendshape.JAW_LEFT: return FaceBlendshape.JAW_RIGHT;
                case FaceBlendshape.JAW_RIGHT: return FaceBlendshape.JAW_LEFT;

                case FaceBlendshape.MOUTH_DIMPLE_LEFT: return FaceBlendshape.MOUTH_DIMPLE_RIGHT;
                case FaceBlendshape.MOUTH_DIMPLE_RIGHT: return FaceBlendshape.MOUTH_DIMPLE_LEFT;

                case FaceBlendshape.MOUTH_FROWN_LEFT: return FaceBlendshape.MOUTH_FROWN_RIGHT;
                case FaceBlendshape.MOUTH_FROWN_RIGHT: return FaceBlendshape.MOUTH_FROWN_LEFT;

                case FaceBlendshape.MOUTH_LEFT: return FaceBlendshape.MOUTH_RIGHT;
                case FaceBlendshape.MOUTH_RIGHT: return FaceBlendshape.MOUTH_LEFT;

                case FaceBlendshape.MOUTH_LOWER_DOWN_LEFT: return FaceBlendshape.MOUTH_LOWER_DOWN_RIGHT;
                case FaceBlendshape.MOUTH_LOWER_DOWN_RIGHT: return FaceBlendshape.MOUTH_LOWER_DOWN_LEFT;

                case FaceBlendshape.MOUTH_PRESS_LEFT: return FaceBlendshape.MOUTH_PRESS_RIGHT;
                case FaceBlendshape.MOUTH_PRESS_RIGHT: return FaceBlendshape.MOUTH_PRESS_LEFT;

                case FaceBlendshape.MOUTH_SMILE_LEFT: return FaceBlendshape.MOUTH_SMILE_RIGHT;
                case FaceBlendshape.MOUTH_SMILE_RIGHT: return FaceBlendshape.MOUTH_SMILE_LEFT;

                case FaceBlendshape.MOUTH_STRETCH_LEFT: return FaceBlendshape.MOUTH_STRETCH_RIGHT;
                case FaceBlendshape.MOUTH_STRETCH_RIGHT: return FaceBlendshape.MOUTH_STRETCH_LEFT;

                case FaceBlendshape.MOUTH_UPPER_UP_LEFT: return FaceBlendshape.MOUTH_UPPER_UP_RIGHT;
                case FaceBlendshape.MOUTH_UPPER_UP_RIGHT: return FaceBlendshape.MOUTH_UPPER_UP_LEFT;

                case FaceBlendshape.NOSE_SNEER_LEFT: return FaceBlendshape.NOSE_SNEER_RIGHT;
                case FaceBlendshape.NOSE_SNEER_RIGHT: return FaceBlendshape.NOSE_SNEER_LEFT;

                // No flip case
                default: return input;
            }
        }

        /// <summary>
        /// Check if the interface provider's underlying interface is null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="i"></param>
        /// <returns></returns>
        public static bool IsNull<T>(this InterfaceProvider<T> i)
        where T : class
        {
            return i.Null;
        }
        public static T Nullable<T>(this InterfaceProvider<T> i)
            where T : class
        {
            return i.Null ? null : i.Provider;
        }

        public static int GetLength(System.Type t)
        {
            return System.Enum.GetValues(t).Length;
        }
        public static string[] GetNames(System.Type t)
        {
            return System.Enum.GetNames(t);
        }
        public static float GetTime()
        {
            return Tracking4All.GetElapsedTime();
        }

        public static float DeltaTime(float last)
        {
            return GetTime() - last;
        }

        public static Landmark Average(params Landmark[] landmarks)
        {
            Landmark middle = default;
            for (int i = 0; i < landmarks.Length; ++i)
            {
                middle.Position += landmarks[i].Position;
                middle.Visibility += landmarks[i].Visibility;
            }

            middle.Position /= landmarks.Length;
            middle.Visibility /= landmarks.Length;

            return middle;
        }
        public static Vector3 Average(params Vector3[] landmarks)
        {
            Vector3 middle = default;
            for (int i = 0; i < landmarks.Length; ++i)
            {
                middle += landmarks[i];
            }

            middle /= landmarks.Length;

            return middle;
        }

        public static NormalizedLandmark Average(params NormalizedLandmark[] landmarks)
        {
            NormalizedLandmark middle = default;
            for (int i = 0; i < landmarks.Length; ++i)
            {
                middle.Position += landmarks[i].Position;
                middle.Visibility += landmarks[i].Visibility;
            }

            middle.Position /= landmarks.Length;
            middle.Visibility /= landmarks.Length;

            return middle;
        }

        public static Vector3 CalculateNormal(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 u = p2 - p1;
            Vector3 v = p3 - p1;
            Vector3 n = new Vector3((u.y * v.z - u.z * v.y), (u.z * v.x - u.x * v.z), (u.x * v.y - u.y * v.x));
            float nl = Mathf.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]);
            return new Vector3(n[0] / nl, n[1] / nl, n[2] / nl);
        }

        public static T GetInterface<T>(this GameObject go, bool includingInactive = false) where T : class
        {
            if (go == null) return null;

            Component[] components = go.GetComponents(typeof(Component));
            foreach (var c in components)
            {
                if (c is T && (includingInactive || c.gameObject.activeInHierarchy))
                {
                    return c as T;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns all children of a transform, including itself (at index 0).
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static Transform[] GetAllChildrenAndSelf(this Transform parent)
        {
            return parent.GetComponentsInChildren<Transform>();
        }
        /// <summary>
        /// Returns all children of a transform, excluding itself. Little more expensive do not call per frame or some crap.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static Transform[] GetAllChildrenAndNotSelf(this Transform parent)
        {
            List<Transform> transforms = new List<Transform>(parent.GetComponentsInChildren<Transform>());
            transforms.Remove(parent);
            return transforms.ToArray();
        }

        public static Vector3 GetTranslation(this Matrix4x4 orientationMatrix)
        {
            return new Vector3(orientationMatrix[0, 3], orientationMatrix[1, 3], orientationMatrix[2, 3]);
        }
        public static Vector3 GetRotationAngles(this Matrix4x4 orientationMatrix)
        {
            double tolerance = Double.Epsilon * 10.0;

            double eul1, eul2, eul3;

            if (Math.Abs(orientationMatrix[0, 0]) < tolerance && Math.Abs(orientationMatrix[1, 0]) < tolerance)
            {
                eul1 = 0;
                eul2 = Math.Atan2(-orientationMatrix[2, 0], orientationMatrix[0, 0]);
                eul3 = Math.Atan2(-orientationMatrix[1, 2], orientationMatrix[1, 1]);
            }
            else
            {
                eul1 = Math.Atan2(orientationMatrix[1, 0], orientationMatrix[0, 0]);
                double sp = Math.Sin(eul1);
                double cp = Math.Cos(eul1);
                eul2 = Math.Atan2(-orientationMatrix[2, 0], cp * orientationMatrix[0, 0] + sp * orientationMatrix[1, 0]);
                eul3 = Math.Atan2(sp * orientationMatrix[0, 2] - cp * orientationMatrix[1, 2], cp * orientationMatrix[1, 1] - sp * orientationMatrix[0, 1]);
            }

            return new Vector3(
                (float)eul1,
                (float)eul2,
                (float)eul3
            );
        }
    }
}