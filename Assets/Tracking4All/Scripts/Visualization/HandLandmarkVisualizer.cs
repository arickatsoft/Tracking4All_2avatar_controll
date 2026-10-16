using System.Collections.Generic;
using UnityEngine;

namespace Tracking4All
{
    public class HandLandmarkVisualizer : LandmarkVisualizer<HandLandmarks>
    {
        [SerializeField] protected int groupAccess = 0;
        [SerializeField] private GameObject handednessPrefab;

        private static readonly List<int[]> EDGE_DEFINITIONS = new List<int[]>()
        {
            new int[] { 0, 5, 9, 13, 17, 0 },
            new int[] { 0, 1, 2, 3, 4 },
            new int[] { 5, 6, 7, 8 },
            new int[] { 9, 10, 11, 12 },
            new int[] { 13, 14, 15, 16 },
            new int[] { 17, 18, 19, 20 }
        };

        public override List<int[]> EdgeDefinitions => EDGE_DEFINITIONS;

        private static readonly HashSet<int> IGNORE_INDICES = new HashSet<int>(
            new int[]
            {
                // 1,2,3,4,5,6
            });

        public override HashSet<int> IgnoreIndicies => IGNORE_INDICES;

        public override int Group => (int)groupAccess;

        private Transform handednessIndicator;

        protected override void Start()
        {
            base.Start();

            if (handednessPrefab)
            {
                // Use this transform as parent instead of generated object
                GameObject hand = Instantiate(handednessPrefab, transform, true);
                handednessIndicator = hand.transform;
            }
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (handednessIndicator)
                handednessIndicator.transform.position =
                    (Get(HandLandmarks.WRIST) + Get(HandLandmarks.INDEX1) + Get(HandLandmarks.PINKY1)) / 3f;
        }

        private Vector3 Get(HandLandmarks l)
        {
            return provider.Get((int)groupAccess, l).Position;
        }

        private void OnValidate()
        {
            if (visualizationMode != LandmarkVisualizationMode.Normal)
            {
                UnityEngine.Debug.LogError("Hand solution does not currently support non-normal visualization. The presense and visibility values will always be 0.");
                visualizationMode = LandmarkVisualizationMode.Normal;
            }
        }
    }
}