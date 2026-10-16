using System.Collections;
using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Handles swapping between menus.
    /// </summary>
    public class DebugMenus : MonoBehaviour
    {
        [SerializeField] private GameObject menuToggle;
        [SerializeField] private GameObject[] windows;

        public bool MenusOpen => menuToggle.activeInHierarchy;

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => Tracking4All.Instance);
            OnMenuDropdownValueChanged(0);
        }

        public void OnPressMenuButton()
        {
            // Called from ugui

            // We are about to close the window.
            if (MenusOpen)
            {
                SettingsManager.Instance.Apply();
            }
            // We are about to open the window.
            else
            {
                SettingsManager.Instance.RefreshDisplay();
            }

            menuToggle.SetActive(!menuToggle.activeSelf);
        }

        public void OnMenuDropdownValueChanged(int index)
        {
            foreach (var w in windows)
            {
                w.SetActive(false);
            }

            windows[index].SetActive(true);
        }
    }
}