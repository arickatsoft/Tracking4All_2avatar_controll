// UnityAdapter
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    public abstract class UnityAdapter<IDENTIFIER_FROM, FROM_DATA, IDENTIFIER_TO, DATA> : AdapterBase<IProvider<IDENTIFIER_FROM, FROM_DATA>, IDENTIFIER_TO, DATA>
        where IDENTIFIER_FROM : System.Enum
        where FROM_DATA : new()
        where IDENTIFIER_TO : System.Enum
        where DATA : new()
    {
        /* Take as input IProvider, behave as an IProvider<IDENTIFIER, DATA>.
         * Adapter that must be manually readied.
         * Can be manually disposed, but additionally will auto dispose when the data source disposes.
         */

        protected IProvider<IDENTIFIER_FROM, FROM_DATA> DataSource { get; private set; }

        // Preserve this adapter's lifecycle identity so existing consumers stay subscribed.
        public void ReplaceDataSource(IProvider<IDENTIFIER_FROM, FROM_DATA> source, bool ready)
        {
            if (source == null) throw new System.ArgumentNullException(nameof(source));
            DisposeAll();
            DataSource = source;
            if (ready) ReadyAll();
        }

        private Flags readyDirty = new(false), disposeDirty = new(false);
        private bool hooked;

        protected UnityAdapter(int groupSize, IProvider<IDENTIFIER_FROM, FROM_DATA> dataSource, float aliveThreshold = -1) : base(groupSize, aliveThreshold)
        {
            this.DataSource = dataSource;
            if (dataSource == null)
            {
                Logger.LogError("Data source was null, please input a valid data source!", this);
            }
        }

        /// <summary>
        /// Ready the adapter and hook onto the data source.
        /// <para>NOTE: will automatically be readied and disposed with the data source until manually disposed.</para>
        /// </summary>
        /// <param name="group"></param>
        public new void Ready(int group)
        {
            if (IsReady(group)) return;

            Hook();

            if (DataSource.Lifecycle.IsReady(group)) base.Ready(group); // only immediately ready if source is ready, otherwise we recieve dirty later

            readyDirty.UnsetFlag(group);
        }
        public void ReadyAll()
        {
            for (int g = 0; g < GroupSize; ++g)
            {
                Ready(g);
            }
        }

        /// <summary>
        /// Uniformly tick the adapter.
        /// <para>Should probably run every MonoBehaviour.Update.</para>
        /// </summary>
        /// <param name="group"></param>
        public new void Tick(int group)
        {
            if (!hooked) return;
            // A source may stop and become ready again between two Unity ticks.
            // Consume disposal before updates, then reconcile its current readiness.
            if (disposeDirty.IsFlagSet(group))
            {
                base.Dispose(group);
                disposeDirty.UnsetFlag(group);
            }
            if (!IsReady(group) && DataSource.Lifecycle.IsReady(group))
            {
                base.Ready(group);
                readyDirty.UnsetFlag(group);
            }
            if (IsReady(group))
            {
                // constantly update at tick rate as long as we are not supposed to timeout.
                if (ShouldTimeout(group) == false)
                {
                    base.InvokeSelfUpdate(group, DataSource);
                }

                // dispose if indicated
                if (disposeDirty.IsFlagSet(group))
                {
                    base.Dispose(group); // note that we are still hooked
                    disposeDirty.UnsetFlag(group);
                }
            }
            else
            {
                // ready if indicated
                if (readyDirty.IsFlagSet(group) && DataSource.Lifecycle.IsReady(group))
                {
                    base.Ready(group);
                    readyDirty.UnsetFlag(group);
                }
            }

            base.Tick(group); // handles timeout invocation
        }
        public void TickAll()
        {
            for (int g = 0; g < GroupSize; ++g)
            {
                Tick(g);
            }
        }

        /// <summary>
        /// Dispose the adapter and unhook from the data source.
        /// <para>NOTE: you must manually ready again if desired.</para>
        /// </summary>
        /// <param name="group"></param>
        public new void Dispose(int group)
        {
            Unhook();

            base.Dispose(group);

            disposeDirty.UnsetFlag(group);
        }
        public void DisposeAll()
        {
            for (int g = 0; g < GroupSize; ++g)
            {
                Dispose(g);
            }
        }

        private void Hook()
        {
            if (hooked) return;
            hooked = true;
            DataSource.Lifecycle.OnUpdated += Lifecycle_OnUpdated;
            DataSource.Lifecycle.OnReady += Lifecycle_OnReady;
            DataSource.Lifecycle.OnDisposed += Lifecycle_OnDisposing;
        }
        private void Unhook()
        {
            if (!hooked) return;
            hooked = false;
            DataSource.Lifecycle.OnUpdated -= Lifecycle_OnUpdated;
            DataSource.Lifecycle.OnReady -= Lifecycle_OnReady;
            DataSource.Lifecycle.OnDisposed -= Lifecycle_OnDisposing;
        }

        protected virtual void Lifecycle_OnUpdated(int group)
        {
            base.LifecycleStateUpdate(group); // Lifecycle-wise, behave as though the update source is the DataSource
        }
        private void Lifecycle_OnDisposing(int group)
        {
            if (IsDisposed(group) == false) disposeDirty.SetFlag(group);
        }
        private void Lifecycle_OnReady(int group)
        {
            if (IsReady(group) == false) readyDirty.SetFlag(group);
        }
    }

}
