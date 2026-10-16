// MPUHolisticPHTrackingSolution
// (C) 2024-2025 G8gaming Ltd.
using Mediapipe;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample.Holistic;
using System.Collections;
using UnityEngine;

namespace Tracking4All
{
    public class MPUHolisticTrackingSolution : MPUImageSourceSolution<HolisticTrackingGraph, MPUHolisticModelSettings>,
        ILandmarkProvider<MPPoseLandmarks>, INormalizedLandmarkProvider<MPPoseLandmarks>,
        ILandmarkProvider<MPHandLandmarks>, INormalizedLandmarkProvider<MPHandLandmarks>,
        ISegmentationDataProvider
    {
        [SerializeField] private DetectionAnnotationController _poseDetectionAnnotationController;
        [SerializeField] private HolisticLandmarkListAnnotationController _holisticAnnotationController;
        [SerializeField] private NormalizedRectAnnotationController _poseRoiAnnotationController;

        private MPUPoseLandmarkAdapter poseLandmarks;
        private MPUPoseNormalizedLandmarkAdapter normalizedPoseLandmarks;
        private MPUHandLandmarkAdapter handLandmarks;
        private MPUHandNormalizedLandmarkAdapter normalizedHandLandmarks;
        private MPUSegementationDataAdapter segmentation;



        protected override void Awake()
        {
            base.Awake();

            poseLandmarks = new MPUPoseLandmarkAdapter(this, this, 1);
            normalizedPoseLandmarks = new MPUPoseNormalizedLandmarkAdapter(this, this, 1);
            handLandmarks = new MPUHandLandmarkAdapter(this, this, Helpers.GetLength(typeof(Handedness)), true);
            normalizedHandLandmarks = new MPUHandNormalizedLandmarkAdapter(this, this, Helpers.GetLength(typeof(Handedness)), true);
            segmentation = new MPUSegementationDataAdapter(1, this, GetImageSource());
        }

        protected override void OnStartRun()
        {
            if (!runningMode.IsSynchronous())
            {
                graphRunner.OnPoseLandmarksOutput += OnPoseLandmarksOutput;
                graphRunner.OnPoseWorldLandmarksOutput += OnPoseWorldLandmarksOutput;

                graphRunner.OnLeftHandLandmarksOutput += OnLeftHandLandmarksOutput;
                graphRunner.OnRightHandLandmarksOutput += OnRightHandLandmarksOutput;

                // TODO: customize holistic to give the landmarks
                // graphRunner.OnRightHandWorldLandmarksOutput += OnRightHandWorldLandmarksOutput;
                // graphRunner.OnLeftHandWorldLandmarksOutput += OnLeftHandWorldLandmarksOutput;
                // 

                graphRunner.OnFaceLandmarksOutput += OnFaceLandmarksOutput;

                graphRunner.OnSegmentationMaskOutput += OnSegmentationMaskOutput;
            }

            var imageSource = GetImageSource();
            if (_poseDetectionAnnotationController) SetupAnnotationController(_poseDetectionAnnotationController, imageSource);
            if (_holisticAnnotationController) SetupAnnotationController(_holisticAnnotationController, imageSource);
            if (_poseRoiAnnotationController) SetupAnnotationController(_poseRoiAnnotationController, imageSource);
        }

        private void OnFaceLandmarksOutput(object sender, OutputStream<NormalizedLandmarkList>.OutputEventArgs e)
        {

        }

        protected override void ReadyToRun()
        {
            base.ReadyToRun();

            poseLandmarks.ReadyAll();
            normalizedPoseLandmarks.Hook(VideoSourceProperties);
            normalizedPoseLandmarks.ReadyAll();
            handLandmarks.ReadyAll();
            normalizedHandLandmarks.ReadyAll();
            segmentation.ReadyAll();
        }
        public override void Stop()
        {
            base.Stop();

            poseLandmarks.DisposeAll();
            normalizedPoseLandmarks.DisposeAll();
            handLandmarks.DisposeAll();
            normalizedHandLandmarks.DisposeAll();
            segmentation.DisposeAll();
        }

        private void UpdateSegmentation(Mediapipe.ImageFrame value)
        {
            if (value == null) return;

            //Update adapters
            segmentation.Update(0, value);
            value.Dispose();
        }
        private void UpdatePoseWorldLandmarks(LandmarkList value)
        {
            if (value == null) return;

            // Update adapters
            poseLandmarks.Update(0, value);
        }
        private void UpdatePoseNormalizedLandmarks(NormalizedLandmarkList value)
        {
            if (value == null) return;

            normalizedPoseLandmarks.Update(0, value);
        }
        private void UpdateHandWorldLandmarks(Handedness hand, LandmarkList value)
        {
            if (value == null) return;

            // Update adapters
            handLandmarks.Update((int)hand, value);
        }
        private void UpdateHandNormalizedLandmarks(Handedness hand, NormalizedLandmarkList value)
        {
            if (value == null) return;

            normalizedHandLandmarks.Update((int)hand, value);
        }

        #region POSE
        private void OnPoseWorldLandmarksOutput(object sender, OutputStream<LandmarkList>.OutputEventArgs e)
        {
            Packet<LandmarkList> packet = e.packet;
            LandmarkList value = packet == null ? default : packet.Get(LandmarkList.Parser);
            if (value == null || value.Landmark == null) return;
            // poseLandmarks.Update(0, value);
            UpdatePoseWorldLandmarks(value);
        }
        private void OnPoseLandmarksOutput(object sender, OutputStream<NormalizedLandmarkList>.OutputEventArgs e)
        {
            var packet = e.packet;
            var value = packet == null ? default : packet.Get(NormalizedLandmarkList.Parser);
            if (_holisticAnnotationController != null) _holisticAnnotationController.DrawPoseLandmarkListLater(value);
            if (value == null || value.Landmark == null) return;

            UpdatePoseNormalizedLandmarks(value);
        }
        private void OnPoseRoiOutput(object sender, OutputStream<NormalizedRect>.OutputEventArgs e)
        {
            var packet = e.packet;
            var value = packet == null ? default : packet.Get(NormalizedRect.Parser);
            if (_poseRoiAnnotationController != null) _poseRoiAnnotationController.DrawLater(value);
        }
        private void OnPoseDetectionOutput(object sender, OutputStream<Detection>.OutputEventArgs e)
        {
            var packet = e.packet;
            var value = packet == null ? default : packet.Get(Detection.Parser);
            if (_poseDetectionAnnotationController != null) _poseDetectionAnnotationController.DrawLater(value);
        }

        private void OnSegmentationMaskOutput(object sender, OutputStream<Mediapipe.ImageFrame>.OutputEventArgs e)
        {
            var packet = e.packet;
            var value = packet == null ? default : packet.Get();

            UpdateSegmentation(value);
            // _segmentationMaskAnnotationController.DrawLater(value);
        }
        #endregion

        #region HANDS
        private void OnLeftHandLandmarksOutput(object stream, OutputStream<NormalizedLandmarkList>.OutputEventArgs eventArgs)
        {
            var packet = eventArgs.packet;
            var value = packet == null ? default : packet.Get(NormalizedLandmarkList.Parser);
            if (_holisticAnnotationController != null) _holisticAnnotationController.DrawLeftHandLandmarkListLater(value);

            if (value == null) return;

            // normalizedHandLandmarks.Update((int)Handedness.LEFT, value);
            UpdateHandNormalizedLandmarks(Handedness.LEFT, value);
        }
        private void OnRightHandLandmarksOutput(object stream, OutputStream<NormalizedLandmarkList>.OutputEventArgs eventArgs)
        {
            var packet = eventArgs.packet;
            var value = packet == null ? default : packet.Get(NormalizedLandmarkList.Parser);
            if (_holisticAnnotationController != null) _holisticAnnotationController.DrawRightHandLandmarkListLater(value);

            if (value == null) return;

            // normalizedHandLandmarks.Update((int)Handedness.RIGHT, value);
            UpdateHandNormalizedLandmarks(Handedness.RIGHT, value);
        }

        private void OnLeftHandWorldLandmarksOutput(object sender, OutputStream<LandmarkList>.OutputEventArgs e)
        {
            var packet = e.packet;
            var value = packet == null ? default : packet.Get(LandmarkList.Parser);
            if (value == null) return;

            UpdateHandWorldLandmarks((Mirror ? Handedness.RIGHT : Handedness.LEFT), value);
        }
        private void OnRightHandWorldLandmarksOutput(object sender, OutputStream<LandmarkList>.OutputEventArgs e)
        {
            var packet = e.packet;
            var value = packet == null ? default : packet.Get(LandmarkList.Parser);
            if (value == null) return;

            UpdateHandWorldLandmarks((Mirror ? Handedness.LEFT : Handedness.RIGHT), value);
        }


        #endregion

        protected override IEnumerator WaitForNextValue()
        {
            var task = graphRunner.WaitNextAsync();
            yield return new WaitUntil(() => task.IsCompleted);


            var result = task.Result;

            UpdateSegmentation(result.segmentationMask);

            UpdatePoseWorldLandmarks(result.poseWorldLandmarks);
            UpdatePoseNormalizedLandmarks(result.poseLandmarks);

            UpdateHandNormalizedLandmarks(Handedness.LEFT, result.leftHandLandmarks);
            // UpdateHandWorldLandmarks((Mirror ? Handedness.RIGHT : Handedness.LEFT), result.leftHandWorldLandmarks);// To do
            UpdateHandNormalizedLandmarks(Handedness.RIGHT, result.rightHandLandmarks);
            // UpdateHandWorldLandmarks((Mirror ? Handedness.LEFT : Handedness.RIGHT), result.rightHandWorldLandmarks);// To do

            if (_holisticAnnotationController) _holisticAnnotationController.DrawNow(null, result.poseLandmarks, result.leftHandLandmarks, result.rightHandLandmarks);
        }
        protected override void AddTextureFrameToInputStream(TextureFrame textureFrame)
        {
            graphRunner.AddTextureFrameToInputStream(textureFrame);
        }
        protected override void UpdateModel(MPUHolisticModelSettings modelSettings)
        {
            graphRunner.modelComplexity = modelSettings.modelComplexity.Value;
            graphRunner.smoothLandmarks = modelSettings.smoothLandmarks;
            graphRunner.enableSegmentation = modelSettings.enableSegmentation.Value;
            graphRunner.smoothSegmentation = modelSettings.smoothSegmentation;

            graphRunner.minDetectionConfidence = modelSettings.minDetectionConfidence.Value;
            graphRunner.minTrackingConfidence = modelSettings.minTrackingConfidence.Value;
        }



        SegmentationData IProvider<SegmentationType, SegmentationData>.Get(int group, SegmentationType index)
        {
            return segmentation.Get(group, (int)index);
        }

        SegmentationData IProvider<SegmentationType, SegmentationData>.Get(int group, int index)
        {
            return segmentation.Get(group, index);
        }

        IProviderLifecycle IProvider<SegmentationType, SegmentationData>.Lifecycle => segmentation.Lifecycle;

        int IProvider<SegmentationType, SegmentationData>.GroupSize => segmentation.GroupSize;

        int IProvider<SegmentationType, SegmentationData>.DataCount => segmentation.DataCount;

        IProviderLifecycle IProvider<MPPoseLandmarks, Landmark>.Lifecycle => poseLandmarks.Lifecycle;

        int IProvider<MPPoseLandmarks, Landmark>.GroupSize => poseLandmarks.GroupSize;

        int IProvider<MPPoseLandmarks, Landmark>.DataCount => poseLandmarks.DataCount;

        Landmark IProvider<MPPoseLandmarks, Landmark>.Get(int group, MPPoseLandmarks index)
        {
            return poseLandmarks.Get(group, index);
        }

        Landmark IProvider<MPPoseLandmarks, Landmark>.Get(int group, int index)
        {
            return poseLandmarks.Get(group, index);
        }

        IProviderLifecycle IProvider<MPPoseLandmarks, NormalizedLandmark>.Lifecycle => normalizedPoseLandmarks.Lifecycle;

        int IProvider<MPPoseLandmarks, NormalizedLandmark>.GroupSize => normalizedPoseLandmarks.GroupSize;

        int IProvider<MPPoseLandmarks, NormalizedLandmark>.DataCount => normalizedPoseLandmarks.DataCount;

        NormalizedLandmark IProvider<MPPoseLandmarks, NormalizedLandmark>.Get(int group, MPPoseLandmarks index)
        {
            return normalizedPoseLandmarks.Get(group, index);
        }

        NormalizedLandmark IProvider<MPPoseLandmarks, NormalizedLandmark>.Get(int group, int index)
        {
            return normalizedPoseLandmarks.Get(group, index);
        }



        IProviderLifecycle IProvider<MPHandLandmarks, Landmark>.Lifecycle => handLandmarks.Lifecycle;
        int IProvider<MPHandLandmarks, Landmark>.GroupSize => handLandmarks.GroupSize;
        int IProvider<MPHandLandmarks, Landmark>.DataCount => handLandmarks.DataCount;
        Landmark IProvider<MPHandLandmarks, Landmark>.Get(int group, MPHandLandmarks index)
        {
            return handLandmarks.Get(group, index);
        }
        Landmark IProvider<MPHandLandmarks, Landmark>.Get(int group, int index)
        {
            return handLandmarks.Get(group, index);
        }

        IProviderLifecycle IProvider<MPHandLandmarks, NormalizedLandmark>.Lifecycle => normalizedHandLandmarks.Lifecycle;
        int IProvider<MPHandLandmarks, NormalizedLandmark>.GroupSize => normalizedHandLandmarks.GroupSize;
        int IProvider<MPHandLandmarks, NormalizedLandmark>.DataCount => normalizedHandLandmarks.DataCount;
        NormalizedLandmark IProvider<MPHandLandmarks, NormalizedLandmark>.Get(int group, MPHandLandmarks index)
        {
            return normalizedHandLandmarks.Get(group, index);
        }
        NormalizedLandmark IProvider<MPHandLandmarks, NormalizedLandmark>.Get(int group, int index)
        {
            return normalizedHandLandmarks.Get(group, index);
        }
    }
}