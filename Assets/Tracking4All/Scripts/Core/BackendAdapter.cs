// BackendAdapter
// (C) 2024-2025 G8gaming Ltd.
using System.Collections;
using UnityEngine;

namespace Tracking4All
{
    public abstract class BackendAdapter<FROM_DATA, IDENTIFIER, DATA> : AdapterBase<FROM_DATA, IDENTIFIER, DATA>
    where IDENTIFIER : System.Enum
    where DATA : new()
    {
        private CoroutineGroup cors;
        private MonoBehaviour owner;

        protected BackendAdapter(int groupSize, MonoBehaviour owner, float aliveThreshold = -1) : base(groupSize, aliveThreshold)
        {
            this.owner = owner;
            cors = new CoroutineGroup(groupSize, owner);
        }

        public void ReadyAll()
        {
            for (int g = 0; g < GroupSize; ++g)
            {
                Ready(g);
            }
        }
        public new void Ready(int group)
        {
            if (IsReady(group)) return;

            base.Ready(group);

            cors.StartAtIndex(group, Ticker(group));
        }

        public new void Update(int group, FROM_DATA from)
        {
            base.Update(group, from);
        }

        public new void Dispose(int group)
        {
            if (IsDisposed(group)) return;

            base.Dispose(group);

            if (owner == null)
            {
                Logger.LogError("Adapter should be disposed before destroying adapter owner.", this);
            }

            cors.StopAtIndex(group);
        }
        public void DisposeAll()
        {
            for (int g = 0; g < GroupSize; ++g)
            {
                Dispose(g);
            }
        }

        private IEnumerator Ticker(int group)
        {
            while (IsReady(group))
            {
                base.Tick(group);
                yield return null;
            }
        }
    }

}