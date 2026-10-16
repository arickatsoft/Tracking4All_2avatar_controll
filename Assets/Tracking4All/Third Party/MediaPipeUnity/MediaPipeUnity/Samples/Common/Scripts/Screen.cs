// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using Mediapipe.Unity.Experimental;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace Mediapipe.Unity
{
    public class Screen : MonoBehaviour
    {        
        [SerializeField] private RawImage _screen;

        private ImageSource _imageSource;

        public Texture texture
        {
            get => _screen.texture;
            set => _screen.texture = value;
        }

        public UnityEngine.Rect uvRect
        {
            set => _screen.uvRect = value;
        }

        private void OnEnable()
        {
            StartCoroutine(DrawQueueLoop());
        }
        private void OnDisable()
        {
            StopCoroutine(DrawQueueLoop());
        }

        public void Initialize(ImageSource imageSource)
        {
            _imageSource = imageSource;

            Resize(_imageSource.textureWidth, _imageSource.textureHeight);
            Rotate(_imageSource.rotation.Reverse());
            ResetUvRect(RunningMode.Async);
            texture = imageSource.GetCurrentTexture();
        }

        public void Resize(int width, int height)
        {
            _screen.rectTransform.sizeDelta = new Vector2(width, height);
        }

        public void Rotate(RotationAngle rotationAngle)
        {
            _screen.rectTransform.localEulerAngles = rotationAngle.GetEulerAngles();
        }

        private struct TextureQueueElement
        {
            public TextureFrame frame;
            public float time;
        }
        private Queue<TextureQueueElement> queue = new();
        private float artificialLatency;
        public void ReadSync(TextureFrame textureFrame, float artificialLatency)
        {
            if(artificialLatency>=0) this.artificialLatency = artificialLatency;

            if (artificialLatency <= 0)
            {
                Read(textureFrame);
            }
            else
            {
                this.artificialLatency = artificialLatency;
                if (queue.Count > 100)
                {
                    Logger.LogInfo("Quite a large queue, either increase this or decrease latency...");
                    return;
                }

                queue.Enqueue(new TextureQueueElement() { frame = textureFrame, time = Time.timeSinceLevelLoad });
            }
        }
        private System.Collections.IEnumerator DrawQueueLoop()
        {
            while (true)
            {
                yield return null;

                while (queue.Count > 0 && Time.timeSinceLevelLoad - queue.Peek().time >= artificialLatency)
                {
                    TextureQueueElement textureFrameInformation = queue.Dequeue();
                    TextureFrame textureFrame = textureFrameInformation.frame;

                    Read(textureFrame);

                    textureFrame.WaitUntilReleased();
                }
            }
        }
        private void Read(TextureFrame frame)
        {
            if (!(texture is Texture2D))
            {
                texture = new Texture2D(_imageSource.textureWidth, _imageSource.textureHeight, TextureFormat.RGBA32, false);
                ResetUvRect(RunningMode.Sync);
            }
            frame.CopyTexture(texture);
        }

        private void ResetUvRect(RunningMode runningMode)
        {
            var rect = new UnityEngine.Rect(0, 0, 1, 1);

            if (_imageSource.isVerticallyFlipped && runningMode == RunningMode.Async)
            {
                // In Async mode, we don't need to flip the screen vertically since the image will be copied on CPU.
                rect = FlipVertically(rect);
            }

            if (_imageSource.isFrontFacing)
            {
                // Flip the image (not the screen) horizontally.
                // It should be taken into account that the image will be rotated later.
                var rotation = _imageSource.rotation;

                if (rotation == RotationAngle.Rotation0 || rotation == RotationAngle.Rotation180)
                {
                    rect = FlipHorizontally(rect);
                }
                else
                {
                    rect = FlipVertically(rect);
                }
            }

            uvRect = rect;
        }

        private UnityEngine.Rect FlipHorizontally(UnityEngine.Rect rect)
        {
            return new UnityEngine.Rect(1 - rect.x, rect.y, -rect.width, rect.height);
        }

        private UnityEngine.Rect FlipVertically(UnityEngine.Rect rect)
        {
            return new UnityEngine.Rect(rect.x, 1 - rect.y, rect.width, -rect.height);
        }
    }
}
