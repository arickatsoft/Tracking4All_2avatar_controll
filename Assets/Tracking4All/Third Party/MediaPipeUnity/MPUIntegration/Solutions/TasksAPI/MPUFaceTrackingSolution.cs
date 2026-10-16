using System;
using System.Collections;
using Mediapipe;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;
using Tracking4All;
using UnityEngine;
using UnityEngine.Rendering;

public class MPUFaceTrackingSolution : MPUVisionTaskApiRunner<FaceLandmarker, MPUFaceModelSettings>,
    IFaceDataProvider
{
    // NOTE: adapted largely from the face tracking & mesh solution sample written by homuler.

    //TODO: depreciate a lot of stuff that is not used.

    [SerializeField] private FaceLandmarkerResultAnnotationController _faceLandmarkerResultAnnotationController;

    private Mediapipe.Unity.Experimental.TextureFramePool _textureFramePool;

    public readonly FaceLandmarkDetectionConfig config = new FaceLandmarkDetectionConfig();
    private bool outputBlendshapes;
    private bool outputTransformationMatrix;

    private MPUFaceDataAdapter faceDataAdapter;

    protected override void Awake()
    {
        base.Awake();

        faceDataAdapter = new(this, this, 1);
    }

    protected void ReadyToRun()
    {
        faceDataAdapter.ReadyAll();
    }
    public override void Stop()
    {
        base.Stop();
        _textureFramePool?.Dispose();
        _textureFramePool = null;

        faceDataAdapter.DisposeAll();
    }

    protected void OnStartRun()
    {

    }
    protected void OnDetectionComplete()
    {

    }

    protected override IEnumerator Run()
    {
        config.OutputFaceBlendshapes = outputBlendshapes;
        config.OutputFacialTransformationMatrixes = outputTransformationMatrix;

        Debug.Log($"Delegate = {config.Delegate}");
        Debug.Log($"Running Mode = {config.RunningMode}");
        Debug.Log($"NumFaces = {config.NumFaces}");
        Debug.Log($"MinFaceDetectionConfidence = {config.MinFaceDetectionConfidence}");
        Debug.Log($"MinFacePresenceConfidence = {config.MinFacePresenceConfidence}");
        Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");
        Debug.Log($"OutputFaceBlendshapes = {config.OutputFaceBlendshapes}");
        Debug.Log($"OutputFacialTransformationMatrixes = {config.OutputFacialTransformationMatrixes}");

        yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

        var options = config.GetFaceLandmarkerOptions(config.RunningMode == Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnFaceLandmarkDetectionOutput : null);
        taskApi = FaceLandmarker.CreateFromOptions(options);
        var imageSource = GetImageSource();

        yield return imageSource.Play();

        if (!imageSource.isPrepared || imageSource.textureWidth <= 16)
        {
            Tracking4All.Logger.LogInfo("Stopped solution execution process due to image source failure...(" + imageSource.textureWidth + ")", imageSource);
            yield break;
        }

        ReadyToRun();

        // Use RGBA32 as the input format.
        // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so maybe the following code needs to be fixed.
        _textureFramePool = new Mediapipe.Unity.Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

        // NOTE: The screen will be resized later, keeping the aspect ratio.
        screen.Initialize(imageSource);

        if (_faceLandmarkerResultAnnotationController != null) SetupAnnotationController(_faceLandmarkerResultAnnotationController, imageSource);

        var transformationOptions = imageSource.GetTransformationOptions();
        var flipHorizontally = transformationOptions.flipHorizontally;
        var flipVertically = transformationOptions.flipVertically;
        var imageProcessingOptions = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: (int)transformationOptions.rotationAngle);

        OnStartRun();

        AsyncGPUReadbackRequest req = default;
        var waitUntilReqDone = new WaitUntil(() => req.done);
        var result = FaceLandmarkerResult.Alloc(options.numFaces);

        while (true)
        {
            if (isPaused)
            {
                yield return new WaitWhile(() => isPaused);
            }

            if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
            {
                yield return new WaitForEndOfFrame();
                continue;
            }

            if (imageSource.isPlaying == false)
            {
                yield break;
            }

            // Copy current image to TextureFrame
            req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            yield return waitUntilReqDone;

            if (req.hasError)
            {
                Debug.LogError($"Failed to read texture from the image source, exiting...");
                break;
            }

            var image = textureFrame.BuildCPUImage();
            // livestream only
            taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
            OnDetectionComplete();

            textureFrame.Release();
        }
    }

    private void OnFaceLandmarkDetectionOutput(FaceLandmarkerResult result, Image image, long timestamp)
    {
        if (_faceLandmarkerResultAnnotationController != null) _faceLandmarkerResultAnnotationController.DrawLater(result);

        if (result.faceBlendshapes != null && result.faceBlendshapes.Count > 0)
        {
            faceDataAdapter.Update(0, Tuple.Create(result.faceBlendshapes[0], result.facialTransformationMatrixes[0]));
        }
    }
    protected override void UpdateModel(MPUFaceModelSettings modelSettings)
    {
        outputBlendshapes = modelSettings.outputBlendshapes.Value;
        outputTransformationMatrix = modelSettings.outputFaceMatrix.Value;
        config.MinFaceDetectionConfidence = modelSettings.minDetectionConfidence.Value;
        config.MinTrackingConfidence = modelSettings.minTrackingConfidence.Value;
        config.MinFacePresenceConfidence = modelSettings.minPresenceConfidence.Value;
    }



    // Provider implementation
    IProviderLifecycle IProvider<FaceDataIdentifier, FaceData>.Lifecycle => faceDataAdapter.Lifecycle;
    int IProvider<FaceDataIdentifier, FaceData>.GroupSize => faceDataAdapter.GroupSize;
    int IProvider<FaceDataIdentifier, FaceData>.DataCount => faceDataAdapter.DataCount;

    FaceData IProvider<FaceDataIdentifier, FaceData>.Get(int group, FaceDataIdentifier index)
    {
        return faceDataAdapter.Get(group, index);
    }
    FaceData IProvider<FaceDataIdentifier, FaceData>.Get(int group, int index)
    {
        return faceDataAdapter.Get(group, index);
    }
}