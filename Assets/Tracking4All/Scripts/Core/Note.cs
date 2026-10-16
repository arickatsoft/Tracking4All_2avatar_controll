// Note
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
#if UNITY_EDITOR
    [AddComponentMenu("Tools/Note")]
    [ExecuteInEditMode]
    public class Note : MonoBehaviour
    {
        [TextArea(3, 10)]
        [SerializeField]
        private string note = "";
    }
#endif

}