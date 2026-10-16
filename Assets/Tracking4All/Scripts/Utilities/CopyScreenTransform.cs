// CopyScreenTransform
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;
using UnityEngine.UI;

namespace Tracking4All
{
    public class CopyScreenTransform : MonoBehaviour
    {
        [SerializeField] private string settingAssociation = "MPUPose Segmentation";

        [SerializeField] protected RectTransform source;
        [SerializeField] protected RawImage sourceImage;

        [SerializeField] protected RectTransform destination;
        [SerializeField] protected RawImage destinationImage;

        private void LateUpdate()
        {
            if (SettingsManager.HasSetting(settingAssociation))
            {
                if (SettingsManager.GetBool(settingAssociation, false) == false)
                {
                    if (destinationImage.color != Color.clear) destinationImage.color = Color.clear;
                    return;
                }
                else
                {
                    if (destinationImage.color != Color.white) destinationImage.color = Color.white;
                }
            }

            destination.sizeDelta = source.sizeDelta;
            destinationImage.uvRect = sourceImage.uvRect;
            destination.localRotation = source.localRotation;
        }
    }
}