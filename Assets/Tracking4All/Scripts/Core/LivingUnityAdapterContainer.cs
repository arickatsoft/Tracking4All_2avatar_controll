// LivingUnityAdapterContainer
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    public abstract class LivingUnityAdapterContainer : MonoBehaviour
    {
        public enum UpdateTiming { Update, LateUpdate }
        [SerializeField] private UpdateTiming updateTiming;

        protected virtual void Awake()
        {
            CreateAdapters();
        }
        protected virtual void OnEnable()
        {
            ReadyAdapters();
        }
        protected virtual void OnDisable()
        {
            DisposeAdapters();
        }

        protected virtual void Update()
        {
            switch (updateTiming)
            {
                case UpdateTiming.Update:
                    TickAdapters();
                    break;
            }
        }
        protected virtual void LateUpdate()
        {
            switch (updateTiming)
            {
                case UpdateTiming.LateUpdate:
                    TickAdapters();
                    break;
            }
        }

        protected abstract void CreateAdapters();
        protected abstract void TickAdapters();
        protected abstract void ReadyAdapters();
        protected abstract void DisposeAdapters();
    }

}