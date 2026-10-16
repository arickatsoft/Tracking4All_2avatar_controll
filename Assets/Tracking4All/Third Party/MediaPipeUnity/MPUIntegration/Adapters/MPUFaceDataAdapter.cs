// MPUFaceBlendshapeAdapter
// (C) 2024-2025 G8gaming Ltd.
using Mediapipe.Tasks.Components.Containers;
using System;
using UnityEngine;

namespace Tracking4All
{
    public class MPUFaceDataAdapter : BackendAdapter<Tuple<Classifications, Matrix4x4>, FaceDataIdentifier, FaceData>
    {
        private ISolutionSettings settings;

        public MPUFaceDataAdapter(ISolutionSettings settings, MonoBehaviour owner, int groupSize) : base(groupSize, owner)
        {
            this.settings = settings;
        }

        protected override void SelfUpdate(int group, Tuple<Classifications, Matrix4x4> from)
        {
            FaceData face = Get(group, 0);
            bool mirror = settings.Mirror;

            // update blendshape data
            for (int i = 0; i < face.BlendshapeCount; ++i)
            {
                face.Set(i, from.Item1.categories[i].score);
            }

            // update orientation data
            Matrix4x4 m = from.Item2;
            Vector3 c0 = m.GetColumn(0);// right
            Vector3 c1 = m.GetColumn(1);// up
            Vector3 c2 = m.GetColumn(2);// forward
            Vector3 c3 = m.GetColumn(3) * .01f;// cm to m
            m = new Matrix4x4(c0, c1, c2, c3);

            face.Set(
                Matrix4x4.Scale(
                    new Vector3(
                        (settings.PerspectiveFlip ? -1f : 1f) * (mirror ? -1f : 1f),
                        1f,
                        settings.PerspectiveFlip ? 1f : -1f)
                    ) * m
                );
        }

        protected override void SelfTimeout(int group)
        {
            FaceData face = Get(group, 0);

            for (int i = 0; i < DataCount; ++i)
            {
                face.Set(i, 0);
            }
        }
    }
}