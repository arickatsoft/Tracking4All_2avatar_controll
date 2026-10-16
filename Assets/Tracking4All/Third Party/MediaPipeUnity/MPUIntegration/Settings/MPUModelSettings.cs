namespace Tracking4All
{
    /// <summary>
    /// Shared mpu model settings.
    /// </summary>
    [System.Serializable]
    public abstract class MPUModelSettings : ISettingsContainer
    {
        public FloatSetting minDetectionConfidence; // .5
        public FloatSetting minTrackingConfidence; // .5

        public BoolSetting perspectiveFlip; // true,  relevant to front facing mobile cameras and whenever you wanna flip
        public BoolSetting mirror;

        public virtual void RegisterMenuSettings()
        {
            SettingsManager.Instance.AddSetting(minDetectionConfidence);
            SettingsManager.Instance.AddSetting(minTrackingConfidence);
            SettingsManager.Instance.AddSetting(perspectiveFlip);
            SettingsManager.Instance.AddSetting(mirror);
        }
        public virtual void DeregisterMenuSettings()
        {
            SettingsManager.Instance.RemoveSetting(minDetectionConfidence);
            SettingsManager.Instance.RemoveSetting(minTrackingConfidence);
            SettingsManager.Instance.RemoveSetting(perspectiveFlip);
            SettingsManager.Instance.RemoveSetting(mirror);
        }

        public virtual void Hook(ISettingsUser restartable)
        {
            minDetectionConfidence.Hook(restartable);
            minTrackingConfidence.Hook(restartable);
            perspectiveFlip.Hook(restartable);
            mirror.Hook(restartable);
        }
    }
    /// <summary>
    /// Holistic lite specific model settings.
    /// </summary>
    [System.Serializable]
    public class MPUHolisticPHModelSettings : MPUModelSettings
    {
        public bool smoothLandmarks = true;
        public BoolSetting enableSegmentation;
        public bool smoothSegmentation = false;

        public EnumSetting<Mediapipe.Unity.Sample.Holistic.HolisticPHTrackingGraph.ModelComplexity> modelComplexity;

        public override void RegisterMenuSettings()
        {
            base.RegisterMenuSettings();

            SettingsManager.Instance.AddSetting(modelComplexity);
            SettingsManager.Instance.AddSetting(enableSegmentation);
        }

        public override void DeregisterMenuSettings()
        {
            base.DeregisterMenuSettings();

            SettingsManager.Instance.RemoveSetting(modelComplexity);
            SettingsManager.Instance.RemoveSetting(enableSegmentation);
        }

        public override void Hook(ISettingsUser restartable)
        {
            base.Hook(restartable);

            modelComplexity.Hook(restartable);
            enableSegmentation.Hook(restartable);
        }
    }
    /// <summary>
    /// Holistic specific model settings.
    /// </summary>
    [System.Serializable]
    public class MPUHolisticModelSettings : MPUModelSettings
    {
        public bool smoothLandmarks = true;
        public BoolSetting enableSegmentation;
        public bool smoothSegmentation = false;

        public EnumSetting<Mediapipe.Unity.Sample.Holistic.HolisticTrackingGraph.ModelComplexity> modelComplexity;

        public override void RegisterMenuSettings()
        {
            base.RegisterMenuSettings();

            SettingsManager.Instance.AddSetting(modelComplexity);
            SettingsManager.Instance.AddSetting(enableSegmentation);
        }

        public override void DeregisterMenuSettings()
        {
            base.DeregisterMenuSettings();

            SettingsManager.Instance.RemoveSetting(modelComplexity);
            SettingsManager.Instance.RemoveSetting(enableSegmentation);
        }

        public override void Hook(ISettingsUser restartable)
        {
            base.Hook(restartable);

            modelComplexity.Hook(restartable);
            enableSegmentation.Hook(restartable);
        }
    }
    /// <summary>
    /// Pose specific model settings.
    /// </summary>
    [System.Serializable]
    public class MPUPoseModelSettings : MPUModelSettings
    {
        public bool smoothLandmarks = true;
        public BoolSetting enableSegmentation;
        public bool smoothSegmentation = false;

        public EnumSetting<Mediapipe.Unity.Sample.PoseTracking.PoseTrackingGraph.ModelComplexity> modelComplexity;

        public override void RegisterMenuSettings()
        {
            base.RegisterMenuSettings();

            SettingsManager.Instance.AddSetting(modelComplexity);
            SettingsManager.Instance.AddSetting(enableSegmentation);
        }

        public override void DeregisterMenuSettings()
        {
            base.DeregisterMenuSettings();

            SettingsManager.Instance.RemoveSetting(modelComplexity);
            SettingsManager.Instance.RemoveSetting(enableSegmentation);
        }
        public override void Hook(ISettingsUser restartable)
        {
            base.Hook(restartable);

            enableSegmentation.Hook(restartable);
            modelComplexity.Hook(restartable);
        }
    }
    /// <summary>
    /// Hand specific model settings.
    /// </summary>
    [System.Serializable]
    public class MPUHandModelSettings : MPUModelSettings
    {
        public EnumSetting<Mediapipe.Unity.Sample.HandTracking.HandTrackingGraph.ModelComplexity> modelComplexity;

        public override void RegisterMenuSettings()
        {
            base.RegisterMenuSettings();

            SettingsManager.Instance.AddSetting(modelComplexity);
        }

        public override void DeregisterMenuSettings()
        {
            base.DeregisterMenuSettings();

            SettingsManager.Instance.RemoveSetting(modelComplexity);
        }

        public override void Hook(ISettingsUser restartable)
        {
            base.Hook(restartable);

            modelComplexity.Hook(restartable);
        }
    }
    /// <summary>
    /// Face specific model settings.
    /// </summary>
    [System.Serializable]
    public class MPUFaceModelSettings : MPUModelSettings
    {
        public FloatSetting minPresenceConfidence;
        public BoolSetting outputBlendshapes;
        public BoolSetting outputFaceMatrix;

        public override void RegisterMenuSettings()
        {
            base.RegisterMenuSettings();
            SettingsManager.Instance.AddSetting(minPresenceConfidence);
            SettingsManager.Instance.AddSetting(outputBlendshapes);
            SettingsManager.Instance.AddSetting(outputFaceMatrix);
        }

        public override void DeregisterMenuSettings()
        {
            base.DeregisterMenuSettings();
            SettingsManager.Instance.RemoveSetting(minPresenceConfidence);
            SettingsManager.Instance.RemoveSetting(outputBlendshapes);
            SettingsManager.Instance.RemoveSetting(outputFaceMatrix);
        }
        public override void Hook(ISettingsUser restartable)
        {
            base.Hook(restartable);

            outputBlendshapes.Hook(restartable);
            outputFaceMatrix.Hook(restartable);
        }
    }
}