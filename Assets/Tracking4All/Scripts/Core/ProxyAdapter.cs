// ProxyAdapter
// (C) 2024-2025 G8gaming Ltd.
using System.Collections;
using UnityEngine;

namespace Tracking4All
{
    /// <summary>
    /// Wraps a IProvider<IDENTIFIER_FROM, FROM_DATA> and hooks onto its lifecycle to convert to <IDENTIFIER_TO, DATA>.
    /// </summary>
    /// <typeparam name="IDENTIFIER_FROM"></typeparam>
    /// <typeparam name="FROM_DATA"></typeparam>
    /// <typeparam name="IDENTIFIER_TO"></typeparam>
    /// <typeparam name="DATA"></typeparam>
    public abstract class ProxyAdapter<IDENTIFIER_FROM, FROM_DATA, IDENTIFIER_TO, DATA> : AdapterBase<IProvider<IDENTIFIER_FROM, FROM_DATA>, IDENTIFIER_TO, DATA>
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
        private Coroutine ticker;
        private bool hooked = false;

        protected ProxyAdapter(int groupSize, IProvider<IDENTIFIER_FROM, FROM_DATA> dataSource, float aliveThreshold = -1) : base(groupSize, aliveThreshold)
        {
            this.DataSource = dataSource;
            if (Tracking4All.Instance == null)
            {
                Logger.LogError("Proxy Adapter was created before Tracking4All was ready, only construct adapters after the instance exists.");
                return;
            }
            if (!hooked) Tracking4All.Instance.StartCoroutine(Hook()); // allows delayed hooking into data sources that are not yet active.
        }

        /// <summary>
        /// Fully dispose and unhook. Should only be called before this is destroyed.
        /// </summary>
        public void DisposeAll()
        {
            for (int g = 0; g < GroupSize; ++g)
            {
                Dispose(g);
            }
            Unhook();
        }

        private void Update(int group)
        {
            Update(group, DataSource);
        }
        private IEnumerator Ticker()
        {
            while (true)
            {
                for (int g = 0; g < GroupSize; ++g)
                {
                    base.Tick(g);
                }
                yield return null;
            }
        }

        private IEnumerator Hook()
        {
            yield return new WaitUntil(() => DataSource != null && DataSource.Lifecycle != null);

            DataSource.Lifecycle.OnUpdated += Lifecycle_OnUpdated;
            DataSource.Lifecycle.OnReady += Lifecycle_OnReady;
            DataSource.Lifecycle.OnDisposed += Lifecycle_OnDisposing;

            ticker = Tracking4All.Instance.StartCoroutine(Ticker());

            hooked = true;
        }
        private void Unhook()
        {
            DataSource.Lifecycle.OnUpdated -= Lifecycle_OnUpdated;
            DataSource.Lifecycle.OnReady -= Lifecycle_OnReady;
            DataSource.Lifecycle.OnDisposed -= Lifecycle_OnDisposing;

            if (Tracking4All.Instance) Tracking4All.Instance.StopCoroutine(ticker);

            hooked = false;
        }

        private void Lifecycle_OnUpdated(int group)
        {
            Update(group);
        }
        private void Lifecycle_OnDisposing(int group)
        {
            Dispose(group);
        }
        private void Lifecycle_OnReady(int group)
        {
            Ready(group);
        }

        /// <summary>
        /// Can be used where the different between the data source and this provider is just an identifier.
        /// <para>The identifier must also have the same length. Otherwise, manually convert.</para>
        /// </summary>
        /// <param name="group"></param>
        /// <param name="provider"></param>
        protected void PassthroughConversion(int group, IProvider<IDENTIFIER_FROM, DATA> provider)
        {
            for (int i = 0; i < DataCount; ++i)
            {
                Set(group, i, provider.Get(group, i));
            }
        }
    }

}