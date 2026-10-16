using System.Collections;
using Mediapipe;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample.PoseTracking;
using Tracking4All;
using UnityEngine;
using Landmark = Tracking4All.Landmark;
using NormalizedLandmark = Tracking4All.NormalizedLandmark;

public class MPUPoseTrackingSolution : MPUImageSourceSolution<PoseTrackingGraph, MPUPoseModelSettings>,
    ILandmarkProvider<MPPoseLandmarks>, INormalizedLandmarkProvider<MPPoseLandmarks>,
    ISegmentationDataProvider // in this case we can go straight to the standardized provider
{
    // NOTE: adapted largely from the mpu pose tracking solution sample written by homuler.

    //TODO: depreciate a lot of stuff that is not used.

    [SerializeField] private DetectionAnnotationController _poseDetectionAnnotationController;
    [SerializeField] private PoseLandmarkListAnnotationController _poseLandmarksAnnotationController;

    private MPUPoseLandmarkAdapter landmarks;
    private MPUPoseNormalizedLandmarkAdapter normalizedLandmarks;
    private MPUSegementationDataAdapter segmentation;

    protected override void Awake()
    {
        base.Awake();

        landmarks = new MPUPoseLandmarkAdapter(this, this, 1);
        normalizedLandmarks = new MPUPoseNormalizedLandmarkAdapter(this, this, 1);
        segmentation = new MPUSegementationDataAdapter(1, this, GetImageSource());
    }

    protected override void OnStartRun()
    {
        if (!runningMode.IsSynchronous())
        {
            graphRunner.OnPoseDetectionOutput += OnPoseDetectionOutput;
            graphRunner.OnPoseLandmarksOutput += OnPoseLandmarksOutput;
            graphRunner.OnPoseWorldLandmarksOutput += OnPoseWorldLandmarksOutput;
            if (modelSettings.enableSegmentation.Value) graphRunner.OnSegmentationMaskOutput += OnSegmentationMaskOutput;
        }

        var imageSource = GetImageSource();
        if (_poseDetectionAnnotationController) SetupAnnotationController(_poseDetectionAnnotationController, imageSource);
        if (_poseLandmarksAnnotationController) SetupAnnotationController(_poseLandmarksAnnotationController, imageSource);
        // SetupAnnotationController(_segmentationMaskAnnotationController, imageSource);
        // _segmentationMaskAnnotationController.InitScreen(imageSource.textureWidth, imageSource.textureHeight);
    }

    protected override void ReadyToRun()
    {
        base.ReadyToRun();

        landmarks.ReadyAll();
        normalizedLandmarks.Hook(VideoSourceProperties);
        normalizedLandmarks.ReadyAll();
        if (modelSettings.enableSegmentation.Value) segmentation.ReadyAll();
    }
    public override void Stop()
    {
        base.Stop();

        landmarks.DisposeAll();
        normalizedLandmarks.DisposeAll();
        segmentation.DisposeAll();
    }

    // async
    private void OnSegmentationMaskOutput(object sender, OutputStream<Mediapipe.ImageFrame>.OutputEventArgs e)
    {
        var packet = e.packet;
        var value = packet == null ? default : packet.Get();

        UpdateSegmentation(value);
        // _segmentationMaskAnnotationController.DrawLater(value);
    }
    private void OnPoseWorldLandmarksOutput(object sender, OutputStream<LandmarkList>.OutputEventArgs e)
    {
        Packet<LandmarkList> packet = e.packet;
        LandmarkList value = packet == null ? default : packet.Get(LandmarkList.Parser);
        if (value == null || value.Landmark == null) return;

        UpdateWorldLandmarks(value);
    }
    private void OnPoseLandmarksOutput(object sender, OutputStream<NormalizedLandmarkList>.OutputEventArgs e)
    {
        var packet = e.packet;
        var value = packet == null ? default : packet.Get(NormalizedLandmarkList.Parser);
        if (_poseLandmarksAnnotationController) _poseLandmarksAnnotationController.DrawLater(value);
        if (value == null || value.Landmark == null) return;

        UpdateNormalizedLandmarks(value);
    }
    private void OnPoseDetectionOutput(object sender, OutputStream<Detection>.OutputEventArgs e)
    {
        var packet = e.packet;
        var value = packet == null ? default : packet.Get(Detection.Parser);
        if (_poseDetectionAnnotationController) _poseDetectionAnnotationController.DrawLater(value);
    }

    // sync
    protected override IEnumerator WaitForNextValue()
    {
        var task = graphRunner.WaitNextAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        var result = task.Result;

        if (modelSettings.enableSegmentation.Value) UpdateSegmentation(result.segmentationMask);
        UpdateWorldLandmarks(result.poseWorldLandmarks);
        UpdateNormalizedLandmarks(result.poseLandmarks);

        if (_poseDetectionAnnotationController) _poseDetectionAnnotationController.DrawNow(result.poseDetection);
        if (_poseLandmarksAnnotationController) _poseLandmarksAnnotationController.DrawNow(result.poseLandmarks);
        // _segmentationMaskAnnotationController.DrawNow(result.segmentationMask);
    }

    private void UpdateSegmentation(Mediapipe.ImageFrame value)
    {
        if (value == null) return;

        //Update adapters
        segmentation.Update(0, value);
        value.Dispose();
    }
    private void UpdateWorldLandmarks(LandmarkList value)
    {
        if (value == null) return;

        // Update adapters
        landmarks.Update(0, value);
    }
    private void UpdateNormalizedLandmarks(NormalizedLandmarkList value)
    {
        if (value == null) return;

        normalizedLandmarks.Update(0, value);
    }

    protected override void UpdateModel(MPUPoseModelSettings modelSettings)
    {
        graphRunner.modelComplexity = (PoseTrackingGraph.ModelComplexity)modelSettings.modelComplexity.Value;
        graphRunner.smoothLandmarks = modelSettings.smoothLandmarks;
        graphRunner.enableSegmentation = modelSettings.enableSegmentation.Value;
        graphRunner.smoothSegmentation = modelSettings.smoothSegmentation;

        // if set to something high, it may not track at all (btw)
        graphRunner.minDetectionConfidence = modelSettings.minDetectionConfidence.Value;
        graphRunner.minTrackingConfidence = modelSettings.minTrackingConfidence.Value;
    }
    protected override void AddTextureFrameToInputStream(TextureFrame textureFrame)
    {
        graphRunner.AddTextureFrameToInputStream(textureFrame);
    }


    IProviderLifecycle IProvider<MPPoseLandmarks, Landmark>.Lifecycle => landmarks.Lifecycle;

    int IProvider<MPPoseLandmarks, Landmark>.GroupSize => landmarks.GroupSize;

    int IProvider<MPPoseLandmarks, Landmark>.DataCount => landmarks.DataCount;

    Landmark IProvider<MPPoseLandmarks, Landmark>.Get(int group, MPPoseLandmarks index)
    {
        return landmarks.Get(group, index);
    }

    Landmark IProvider<MPPoseLandmarks, Landmark>.Get(int group, int index)
    {
        return landmarks.Get(group, index);
    }

    IProviderLifecycle IProvider<MPPoseLandmarks, NormalizedLandmark>.Lifecycle => normalizedLandmarks.Lifecycle;

    int IProvider<MPPoseLandmarks, NormalizedLandmark>.GroupSize => normalizedLandmarks.GroupSize;

    int IProvider<MPPoseLandmarks, NormalizedLandmark>.DataCount => normalizedLandmarks.DataCount;

    IProviderLifecycle IProvider<SegmentationType, SegmentationData>.Lifecycle => segmentation.Lifecycle;

    int IProvider<SegmentationType, SegmentationData>.GroupSize => segmentation.GroupSize;

    int IProvider<SegmentationType, SegmentationData>.DataCount => segmentation.DataCount;

    NormalizedLandmark IProvider<MPPoseLandmarks, NormalizedLandmark>.Get(int group, MPPoseLandmarks index)
    {
        return normalizedLandmarks.Get(group, index);
    }

    NormalizedLandmark IProvider<MPPoseLandmarks, NormalizedLandmark>.Get(int group, int index)
    {
        return normalizedLandmarks.Get(group, index);
    }

    SegmentationData IProvider<SegmentationType, SegmentationData>.Get(int group, SegmentationType index)
    {
        return segmentation.Get(group, (int)index);
    }

    SegmentationData IProvider<SegmentationType, SegmentationData>.Get(int group, int index)
    {
        return segmentation.Get(group, index);
    }



}