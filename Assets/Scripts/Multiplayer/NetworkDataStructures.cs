using System;
using UnityEngine;

namespace Tracking4All.Multiplayer
{
    /// <summary>
    /// Network-serializable data structures for pose landmark transmission
    /// Compatible with your existing networking setup
    /// </summary>
    
    [System.Serializable]
    public struct LandmarkNetworkData
    {
        public Vector3 position;
        public float presence;
        public float visibility;
        
        public LandmarkNetworkData(Vector3 pos, float pres, float vis)
        {
            position = pos;
            presence = pres;
            visibility = vis;
        }
    }
    
    [System.Serializable]
    public struct PoseLandmarksNetworkData
    {
        public LandmarkNetworkData[] landmarks;
        public float timestamp;
        public bool isValid;
        
        public PoseLandmarksNetworkData(int landmarkCount)
        {
            landmarks = new LandmarkNetworkData[landmarkCount];
            timestamp = Time.time;
            isValid = false;
        }
    }
}