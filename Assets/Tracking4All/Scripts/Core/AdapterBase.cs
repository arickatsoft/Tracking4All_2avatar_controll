// Adapter
// (C) 2024-2025 G8gaming Ltd.
using System;
using UnityEngine;

namespace Tracking4All
{
    public abstract class AdapterBase<FROM_DATA, IDENTIFIER, DATA> : IProvider<IDENTIFIER, DATA>, IProviderLifecycle
        where IDENTIFIER : System.Enum
        where DATA : new()
    {
        private event GroupEvent onReady;
        public event GroupEvent OnReady
        {
            add
            {
                // auto ready behavior also exists in RemappedProvider
                onReady += value;
                for (int g = 0; g < GroupSize; ++g)
                {
                    if (IsReady(g))
                    {
                        value(g);
                    }
                }
            }
            remove
            {
                onReady -= value;
            }
        }
        public event GroupEvent OnDisposed;
        public event GroupEvent OnResumed;
        public event GroupEvent OnUpdated;
        public event GroupEvent OnTimedOut;

        public IProviderLifecycle Lifecycle => this;

        public int GroupSize => table.GroupSize;
        public int DataCount => table.ElementCount;

        private TableGroup<DATA> table;

        private Flags isReady = new(false), timedOut = new(true);

        protected float timeoutThreshold = -1;
        private float[] lastUpdateTimes;

        public AdapterBase(int groupSize, float aliveThreshold = -1)
        {
            table = new(groupSize, Helpers.GetLength(typeof(IDENTIFIER)));

            lastUpdateTimes = new float[groupSize];
            for (int i = 0; i < lastUpdateTimes.Length; ++i)
            {
                lastUpdateTimes[i] = Mathf.NegativeInfinity;
            }

            this.timeoutThreshold = aliveThreshold;
            if (this.timeoutThreshold == -1)
            {
                // Use default value if Tracking4All instance is not yet initialized
                this.timeoutThreshold = Tracking4All.Instance != null ? Tracking4All.Instance.ProviderAliveThreshold : 1.0f;
            }
        }

        protected void Ready(int group)
        {
            if (IsReady(group)) return;
            DoReady(group);
        }
        protected void DoReady(int group)
        {
            isReady.SetFlag(group);
            SelfReady(group);
            onReady?.Invoke(group);
        }
        protected virtual void SelfReady(int group)
        {

        }

        protected void Dispose(int group)
        {
            if (IsDisposed(group)) return;

            if (!IsTimedOut(group) || lastUpdateTimes[group] == Mathf.NegativeInfinity)
            {
                DoTimeout(group);
            }
            DoDispose(group);
        }
        protected void DoDispose(int group)
        {
            isReady.UnsetFlag(group);
            OnDisposed?.Invoke(group);
            SelfDispose(group);
        }
        protected virtual void SelfDispose(int group)
        {

        }

        protected void Tick(int group)
        {
            if (ShouldTimeout(group) && !IsTimedOut(group))
            {
                DoTimeout(group);
            }
        }

        protected void Update(int group, FROM_DATA from)
        {
            if (!IsReady(group)) return;

            LifecycleStateUpdate(group);

            InvokeSelfUpdate(group, from);
        }
        protected void LifecycleStateUpdate(int group)
        {
            lastUpdateTimes[group] = Helpers.GetTime();
        }
        protected void InvokeSelfUpdate(int group, FROM_DATA from)
        {
            if (IsTimedOut(group))
            {
                DoResume(group);
            }

            SelfUpdate(group, from);
            OnUpdated?.Invoke(group);
        }
        protected abstract void SelfUpdate(int group, FROM_DATA from);

        // both managed internally to enforce adapter behaviour timeout/resume meanings
        private void DoTimeout(int group)
        {
            SelfTimeout(group);
            timedOut.SetFlag(group);
            OnTimedOut?.Invoke(group);
        }
        protected virtual void SelfTimeout(int group)
        {

        }
        private void DoResume(int group)
        {
            SelfResume(group);
            timedOut.UnsetFlag(group);
            OnResumed?.Invoke(group);
        }
        protected virtual void SelfResume(int group)
        {

        }

        public DATA Get(int group, IDENTIFIER index)
        {
            if (group < 0 || group >= GroupSize)
            {
                throw new Exception("Group is invalid " + group);
            }

            return table.Get(group, Convert.ToInt32(index));
        }
        public DATA Get(int group, int index)
        {
            if (group < 0 || group >= GroupSize)
            {
                throw new Exception("Group is invalid " + group);
            }

            return table.Get(group, index);
        }
        protected void Set(int group, IDENTIFIER index, DATA data)
        {
            if (group < 0 || group >= GroupSize)
            {
                throw new Exception("Group is invalid " + group);
            }

            table.Set(group, Convert.ToInt32(index), data);
        }
        protected void Set(int group, int index, DATA data)
        {
            if (group < 0 || group >= GroupSize)
            {
                throw new Exception("Group is invalid " + group);
            }

            table.Set(group, index, data);
        }

        protected virtual bool ShouldTimeout(int group)
        {
            return (Helpers.GetTime() - lastUpdateTimes[group]) > timeoutThreshold;
        }

        public bool IsTimedOut(int group)
        {
            return timedOut.IsFlagSet(group);
        }
        public bool IsAnyReady()
        {
            return isReady.AnyFlagSet();
        }
        public bool IsReady(int group)
        {
            if (group < 0 || group >= GroupSize)
            {
                throw new Exception("Group is invalid " + group);
            }

            return isReady.IsFlagSet(group);
        }
        public bool IsDisposed(int group)
        {
            if (group < 0 || group >= GroupSize)
            {
                throw new Exception("Group is invalid " + group);
            }

            return isReady.IsFlagNotSet(group);
        }
        public bool IsAlive(int group)
        {
            if (group < 0 || group >= GroupSize)
            {
                throw new Exception("Group is invalid " + group);
            }

            return IsReady(group) && !timedOut.IsFlagSet(group);
        }

        ~AdapterBase()
        {
            for (int g = 0; g < GroupSize; g++)
            {
                if (!IsDisposed(g))
                {
                    Logger.LogInfo("This adapter was not properly disposed before being destroyed g=" + g, this);
                }
            }
        }
    }
}