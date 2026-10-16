// FaceEnhancementModifier
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    public class FaceDataEnhancementModifier : Modifier, IFaceDataModifier
    {
        public enum SyncEyesBlinkMode { None, Average, PreferClosed, PreferOpen, Smart }

        [SerializeField]
        private float headPositionSmoothSpeed = 5f;
        [SerializeField]
        private float headRotationSmoothSpeed = 10f;
        [SerializeField]
        private float eyeSmoothSpeed = 30f;
        [SerializeField]
        private float expressionSmoothSpeed = 100f;
        [SerializeField]
        private AnimationCurve blinkCurve;
        [SerializeField]
        private float[] blinkMinMax = new float[] { .1f, .35f }; // Pick bounds depending on your environmental conditions
        [SerializeField]
        private SyncEyesBlinkMode syncEyeBlinks = SyncEyesBlinkMode.PreferOpen;

        private OneEuroFilterFloat eyeDeltaSmoothed = new OneEuroFilterFloat(1f);
        private Vector3 targetPosition, targetRotationAngles;
        private Vector3 lastPosition, lastRotationAngles;

        private Vector3 smoothPosition;
        private Quaternion smoothLookRot;

        private static readonly FaceBlendshape[] AllKeys = (FaceBlendshape[])System.Enum.GetValues(typeof(FaceBlendshape));

        public void Modify(int dataIndex, ref FaceData current, ref FaceData target, ref bool stayAlive, float deltaTime)
        {
            targetPosition = target.GetTranslation();
            targetRotationAngles = target.GetRotationAngles();

            if (!Enabled)
            {
                current = target;
                return;
            }

            Matrix4x4 matrix = target.FaceOrientationMatrix;

            Quaternion next = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
            smoothLookRot = Quaternion.Slerp(smoothLookRot, next, deltaTime * headRotationSmoothSpeed);
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(smoothLookRot);

            Vector3 nextPos = matrix.GetColumn(3);
            if (smoothPosition == Vector3.zero) smoothPosition = nextPos;
            else smoothPosition = Vector3.Lerp(smoothPosition, nextPos, deltaTime * headPositionSmoothSpeed);

            matrix.SetColumn(0, rotationMatrix.GetColumn(0));
            matrix.SetColumn(1, rotationMatrix.GetColumn(1));
            matrix.SetColumn(2, rotationMatrix.GetColumn(2));
            matrix.SetColumn(3, smoothPosition);

            current.Set(matrix);

            BlendshapeEnhancement(current, target, deltaTime);

            lastPosition = current.GetTranslation();
            lastRotationAngles = current.GetRotationAngles();
        }

        private void BlendshapeEnhancement(FaceData current, FaceData target, float dt)
        {
            Vector3 newPosition = current.GetTranslation();

            // Experimentally derived values to improve visual quality during head translation and rotation.
            // Resistance to uglyness when moving the head around.
            float s = ((newPosition - lastPosition) / dt).magnitude;

            float m2 = Mathf.InverseLerp(.2f, .1f, s);
            float d = Mathf.Abs(Quaternion.Angle(Quaternion.Euler(targetRotationAngles), Quaternion.Euler(lastRotationAngles)));
            float m = Mathf.InverseLerp(10, 3, d);

            // Use data to apply to model
            foreach (FaceBlendshape k in AllKeys)
            {
                float confidence = Mathf.Min(m, m2); // take the worse of head rotation and translation
                confidence = Mathf.Clamp(confidence, .1f, 1f);

                float v = current.GetBlendshape(k);
                switch (k)
                {
                    case FaceBlendshape.EYE_BLINK_LEFT:
                    case FaceBlendshape.EYE_BLINK_RIGHT:
                        if (confidence > .5f)
                            v = Mathf.Lerp(current.GetBlendshape(k),
                                blinkCurve.Evaluate(Mathf.InverseLerp(blinkMinMax[0], blinkMinMax[1], target.GetBlendshape(k))), // convert to the new range and map to curve
                                Time.deltaTime * eyeSmoothSpeed * confidence);
                        break;
                    case FaceBlendshape.EYE_SQUINT_LEFT:
                    case FaceBlendshape.EYE_SQUINT_RIGHT:
                        if (confidence > .5f)
                            v = Mathf.Lerp(current.GetBlendshape(k), target.GetBlendshape(k), Time.deltaTime * eyeSmoothSpeed * confidence);
                        break;
                    case FaceBlendshape.BROW_DOWN_LEFT:
                    case FaceBlendshape.BROW_DOWN_RIGHT:
                    case FaceBlendshape.BROW_INNER_UP:
                    case FaceBlendshape.BROW_OUTER_UP_LEFT:
                    case FaceBlendshape.BROW_OUTER_UP_RIGHT:
                        v = Mathf.Lerp(current.GetBlendshape(k), target.GetBlendshape(k), Time.deltaTime * eyeSmoothSpeed * confidence);
                        break;
                    default:
                        v = Mathf.Lerp(current.GetBlendshape(k), target.GetBlendshape(k), Time.deltaTime * expressionSmoothSpeed * confidence);
                        break;
                }

                current.Set(k, v);
            }

            // To improve visual behaviour optionally allow synchronization of the eyes.
            // Usually recommended in nonuniform/not ideal lighting conditions.
            if (syncEyeBlinks != SyncEyesBlinkMode.None)
            {
                float v1 = current.GetBlendshape(FaceBlendshape.EYE_BLINK_LEFT), v2 = current.GetBlendshape(FaceBlendshape.EYE_BLINK_RIGHT);
                float a = (v1 + v2) / 2f;
                switch (syncEyeBlinks)
                {
                    case SyncEyesBlinkMode.Average:
                        a = (v1 + v2) / 2f;

                        current.Set(FaceBlendshape.EYE_BLINK_LEFT, a);
                        current.Set(FaceBlendshape.EYE_BLINK_RIGHT, a);
                        break;

                    case SyncEyesBlinkMode.PreferClosed:
                        a = Mathf.Min(v1, v2);

                        current.Set(FaceBlendshape.EYE_BLINK_LEFT, a);
                        current.Set(FaceBlendshape.EYE_BLINK_RIGHT, a);
                        break;

                    case SyncEyesBlinkMode.PreferOpen:
                        a = Mathf.Max(v1, v2);

                        current.Set(FaceBlendshape.EYE_BLINK_LEFT, a);
                        current.Set(FaceBlendshape.EYE_BLINK_RIGHT, a);
                        break;

                    case SyncEyesBlinkMode.Smart:
                        float de = eyeDeltaSmoothed.Filter(Mathf.Abs(v1 - v2), Time.deltaTime);

                        if (de > .2f)
                        {
                            current.Set(FaceBlendshape.EYE_BLINK_LEFT, v1);
                            current.Set(FaceBlendshape.EYE_BLINK_RIGHT, v2);
                        }
                        else
                        {
                            // eye open behaviour
                            a = Mathf.Max(v1, v2);

                            current.Set(FaceBlendshape.EYE_BLINK_LEFT, a);
                            current.Set(FaceBlendshape.EYE_BLINK_RIGHT, a);
                        }
                        break;

                }
            }
        }
    }
}