// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using UnityEngine;

namespace Mediapipe.Unity.Sample
{
    public abstract class ImageSourceSolution<T> : Solution where T : GraphRunner
    {
        [SerializeField, Range(0f, .5f)] protected float artificialScreenLatency = -1; // only works in sync mode, disabled => 0.
        [SerializeField] protected Screen screen;
        [SerializeField] protected T graphRunner;
        [SerializeField] protected TextureFramePool textureFramePool;

        private Coroutine _coroutine;

        public RunningMode runningMode;

        public virtual bool OptionalWait()
        {
            return false;
        }
        protected virtual void OnDetectionComplete()
        {

        }

        protected virtual void ReadyToRun()
        {

        }

        public long timeoutMillisec
        {
            get => graphRunner.timeoutMillisec;
            set => graphRunner.timeoutMillisec = value;
        }

        public override void Play()
        {
            if (_coroutine != null)
            {
                Stop();
            }
            base.Play();
            _coroutine = StartCoroutine(Run());
        }

        public override void Pause()
        {
            base.Pause();
            // ImageSourceProvider.ImageSource.Pause();
        }

        public override void Resume()
        {
            base.Resume();
            // var _ = StartCoroutine(ImageSourceProvider.ImageSource.Resume());
        }

        public override void Stop()
        {
            base.Stop();
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
            // ImageSourceProvider.ImageSource.Stop();
            graphRunner.Stop();
        }

        protected virtual void OnStartSyncDetection()
        {

        }
        protected virtual ImageSource GetImageSource()
        {
            return ImageSourceProvider.ImageSource;
        }
        private IEnumerator Run()
        {
            var graphInitRequest = graphRunner.WaitForInit(runningMode);
            var imageSource = GetImageSource();

            yield return imageSource.Play();

            if (!imageSource.isPrepared || imageSource.textureWidth<=16)
            {
                Logger.LogInfo("Stopped solution execution process due to image source failure...("+imageSource.textureWidth+")", imageSource);
                yield break;
            }

            ReadyToRun();

            // Use RGBA32 as the input format.
            // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so the following code must be fixed.
            textureFramePool.ResizeTexture(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32);
            SetupScreen(imageSource);

            yield return graphInitRequest;
            if (graphInitRequest.isError)
            {
                Debug.LogError(graphInitRequest.error);
                yield break;
            }

            OnStartRun();
            graphRunner.StartRun(imageSource);

            var waitWhilePausing = new WaitWhile(() => isPaused);
            var wait = new WaitWhile(() => OptionalWait());

            while (true)
            {
                if (isPaused)
                {
                    yield return waitWhilePausing;
                }

                if (!textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                if (imageSource.isPlaying == false)
                {
                    yield break;
                }

                yield return wait;

                // Copy current image to TextureFrame
                ReadFromImageSource(imageSource, textureFrame);
                AddTextureFrameToInputStream(textureFrame);

                yield return new WaitForEndOfFrame();

                if (runningMode.IsSynchronous())
                {
                    RenderCurrentFrame(textureFrame);
                    OnStartSyncDetection();
                    yield return WaitForNextValue();
                }

                OnDetectionComplete();
            }
        }

        protected virtual void SetupScreen(ImageSource imageSource)
        {
            // Make screen optional - if not assigned, just skip display
            if (screen == null)
            {
                Logger.LogWarning("No screen assigned to solution - camera preview will not be displayed", this);
                return;
            }
            
            // NOTE: The screen will be resized later, keeping the aspect ratio.
            screen.Initialize(imageSource);
        }

        protected virtual void RenderCurrentFrame(TextureFrame textureFrame)
        {
            // Make screen optional - if not assigned, just skip rendering
            if (screen == null)
            {
                return;
            }
            
            if (runningMode.IsSynchronous() == false)
            {
                if (artificialScreenLatency > 0)
                {
                    Logger.LogInfo("Artificial screen latency only works in synchronous running mode.", this);
                }
                artificialScreenLatency = 0;
            }

            screen.ReadSync(textureFrame, artificialScreenLatency);
        }

        protected abstract void OnStartRun();

        protected abstract void AddTextureFrameToInputStream(TextureFrame textureFrame);

        protected abstract IEnumerator WaitForNextValue();
    }
}
