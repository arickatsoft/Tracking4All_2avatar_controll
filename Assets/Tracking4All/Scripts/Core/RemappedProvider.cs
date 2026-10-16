// RemappedProvider
// (C) 2024-2025 G8gaming Ltd.
using System;
using System.Collections.Generic;

namespace Tracking4All
{
    public class ReactiveRemappedProvider<IDENTIFIER, DATA> : IProvider<IDENTIFIER, DATA>, IProviderLifecycle, IDisposable
        where IDENTIFIER : System.Enum
        where DATA : new()
    {
        // implement through
        private RemappedProvider<IDENTIFIER, DATA> provider
        {
            get
            {
                if (settings.Mirror) return mirrored;
                return standard;
            }
        }

        private ISolutionSettings settings;
        private RemappedProvider<IDENTIFIER, DATA> standard;
        private RemappedProvider<IDENTIFIER, DATA> mirrored;

        public ReactiveRemappedProvider(ISolutionSettings settings,
            Dictionary<int, (IProvider<IDENTIFIER, DATA>, int)> standard,
            Dictionary<int, (IProvider<IDENTIFIER, DATA>, int)> mirrored)
        {
            this.settings = settings;
            this.standard = new(standard);
            this.mirrored = new(mirrored);
        }

        public void Ready()
        {
            standard.Ready();
            mirrored.Ready();
        }
        public void Dispose()
        {
            standard.Dispose();
            mirrored.Dispose();
        }


        public event GroupEvent OnReady
        {
            add
            {
                ((IProviderLifecycle)standard).OnReady += value;
            }

            remove
            {
                ((IProviderLifecycle)standard).OnReady -= value;
            }
        }

        public event GroupEvent OnDisposed
        {
            add
            {
                ((IProviderLifecycle)standard).OnDisposed += value;
            }

            remove
            {
                ((IProviderLifecycle)standard).OnDisposed -= value;
            }
        }

        public event GroupEvent OnResumed
        {
            add
            {
                ((IProviderLifecycle)standard).OnResumed += value;
            }

            remove
            {
                ((IProviderLifecycle)standard).OnResumed -= value;
            }
        }

        public event GroupEvent OnUpdated
        {
            add
            {
                ((IProviderLifecycle)standard).OnUpdated += value;
            }

            remove
            {
                ((IProviderLifecycle)standard).OnUpdated -= value;
            }
        }

        public event GroupEvent OnTimedOut
        {
            add
            {
                ((IProviderLifecycle)standard).OnTimedOut += value;
            }

            remove
            {
                ((IProviderLifecycle)standard).OnTimedOut -= value;
            }
        }

        public IProviderLifecycle Lifecycle => ((IProvider<IDENTIFIER, DATA>)provider).Lifecycle;

        public int GroupSize => ((IProvider<IDENTIFIER, DATA>)provider).GroupSize;

        public int DataCount => ((IProvider<IDENTIFIER, DATA>)provider).DataCount;

        public DATA Get(int group, IDENTIFIER index)
        {
            return ((IProvider<IDENTIFIER, DATA>)provider).Get(group, index);
        }

        public DATA Get(int group, int index)
        {
            return ((IProvider<IDENTIFIER, DATA>)provider).Get(group, index);
        }

        public bool IsReady(int group)
        {
            return ((IProviderLifecycle)standard).IsReady(group);
        }

        public bool IsDisposed(int group)
        {
            return ((IProviderLifecycle)standard).IsDisposed(group);
        }

        public bool IsAlive(int group)
        {
            return ((IProviderLifecycle)standard).IsAlive(group);
        }
    }

    // TODO: usage could be simplified using builder pattern.
    /// <summary>
    /// Allows you to remap provider groups including separation and merging.
    /// <para>Manually Ready ideally before and uses and manually Dispose when all users are done (otherwise you may miss callbacks).</para>
    /// </summary>
    /// <typeparam name="IDENTIFIER"></typeparam>
    /// <typeparam name="DATA"></typeparam>
    public class RemappedProvider<IDENTIFIER, DATA> : IProvider<IDENTIFIER, DATA>, IProviderLifecycle, IDisposable
        where IDENTIFIER : System.Enum
        where DATA : new()
    {
        // Remapping: {public group, (IProvider, provider group)}
        // allows you to access IProvider.group by public group through this provider.
        // automatically fully manages the conversion

        public Dictionary<int, (ProviderWrapper, int)> Remapping { get; private set; }
        public Dictionary<(ProviderWrapper, int), int> InverseMapping { get; private set; }
        private bool readied = false;

        /// <summary>
        /// Remap a single provider using the remapping.
        /// </summary>
        /// <param name="wrap"></param>
        /// <param name="remapping">As {new group access, access from provider}</param>
        public RemappedProvider(IProvider<IDENTIFIER, DATA> wrap, Dictionary<int, int> remapping)
        {
            Remapping = new();
            InverseMapping = new();

            foreach (var kv in remapping)
            {
                Remapping.Add(kv.Key, (new ProviderWrapper(this, wrap), kv.Value));
                InverseMapping.Add(Remapping[kv.Key], kv.Key);
            }

            DataCount = wrap.DataCount;
        }
        /// <summary>
        /// Remap multiple providers to a single provider using the remapping.
        /// </summary>
        /// <param name="remapping">As {new group access, (provider, group access from provider)}</param>
        public RemappedProvider(Dictionary<int, (IProvider<IDENTIFIER, DATA>, int)> remapping)
        {
            InverseMapping = new();
            this.Remapping = new();

            foreach (var kv in remapping)
            {
                Remapping.Add(kv.Key, (new ProviderWrapper(this, kv.Value.Item1), kv.Value.Item2));
                InverseMapping.Add(Remapping[kv.Key], kv.Key);
            }

            DataCount = remapping[0].Item1.DataCount;
        }

        /// <summary>
        /// Ready all hooks.
        /// </summary>
        public void Ready()
        {
            if (readied) return;

            foreach (var kv in Remapping)
            {
                kv.Value.Item1.Ready();
            }

            readied = true;
        }
        /// <summary>
        /// Dispose all hooks.
        /// </summary>
        public void Dispose()
        {
            if (!readied) return;

            foreach (var kv in Remapping)
            {
                kv.Value.Item1.Dispose();
            }

            readied = false;
        }

        public bool TryGetMapping(int group, out ProviderWrapper wrapper, out int wrapperAccess)
        {
            if (Remapping.ContainsKey(group) == false)
            {
                wrapper = null;
                wrapperAccess = group;
                return false;
            }

            wrapper = Remapping[group].Item1;
            wrapperAccess = Remapping[group].Item2;
            return true;
        }
        private bool TryGetInverseMapping(ProviderWrapper wrapper, int wrapperAccess, out int group)
        {
            if (InverseMapping.ContainsKey((wrapper, wrapperAccess)) == false)
            {
                group = wrapperAccess;
                return false;
            }

            group = InverseMapping[(wrapper, wrapperAccess)];
            return true;
        }

        public IProviderLifecycle Lifecycle => this;

        public int GroupSize => Remapping.Count;
        public int DataCount { get; private set; }

        public DATA Get(int group, IDENTIFIER index)
        {
            if (!TryGetMapping(group, out ProviderWrapper wrapper, out group))
            {
                throw new KeyNotFoundException($"Internal group {group} does not exist.");
            }

            return wrapper.Wrap.Get(group, index);
        }
        public DATA Get(int group, int index)
        {
            if (!TryGetMapping(group, out ProviderWrapper wrapper, out group))
            {
                throw new KeyNotFoundException($"Internal group {group} does not exist.");
            }

            return wrapper.Wrap.Get(group, index);
        }

        public bool IsAlive(int group)
        {
            if (!TryGetMapping(group, out ProviderWrapper wrapper, out group))
            {
                throw new KeyNotFoundException($"Internal group {group} does not exist.");
            }

            return wrapper.Wrap.Lifecycle.IsAlive(group);
        }
        public bool IsDisposed(int group)
        {
            if (!TryGetMapping(group, out ProviderWrapper wrapper, out group))
            {
                throw new KeyNotFoundException($"Internal group {group} does not exist.");
            }

            return wrapper.Wrap.Lifecycle.IsDisposed(group);
        }
        public bool IsReady(int group)
        {
            if (!TryGetMapping(group, out ProviderWrapper wrapper, out group))
            {
                throw new KeyNotFoundException($"Internal group {group} does not exist.");
            }

            return wrapper.Wrap.Lifecycle.IsReady(group);
        }

        // Publicly we subscribe to these, then these are invoked through remapped callbacks of the wrapped provider.

        private event GroupEvent onReady;
        public event GroupEvent OnReady
        {
            add
            {
                // auto ready behavior also exists in AdapterBase
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

        private void Lifecycle_OnReady(ProviderWrapper provider, int group)
        {
            if (!TryGetInverseMapping(provider, group, out group))
            {
                return;
            }

            onReady?.Invoke(group);
        }
        private void Lifecycle_OnResumed(ProviderWrapper provider, int group)
        {
            if (!TryGetInverseMapping(provider, group, out group))
            {
                return;
            }
            OnResumed?.Invoke(group);
        }
        private void Lifecycle_OnUpdated(ProviderWrapper provider, int group)
        {
            if (!TryGetInverseMapping(provider, group, out group))
            {
                return;
            }
            OnUpdated?.Invoke(group);
        }
        private void Lifecycle_OnTimedOut(ProviderWrapper provider, int group)
        {
            if (!TryGetInverseMapping(provider, group, out group))
            {
                return;
            }
            OnTimedOut?.Invoke(group);
        }
        private void Lifecycle_OnDisposing(ProviderWrapper provider, int group)
        {
            if (!TryGetInverseMapping(provider, group, out group))
            {
                return;
            }
            OnDisposed?.Invoke(group);
        }

        public class ProviderWrapper : IDisposable
        {
            public IProvider<IDENTIFIER, DATA> Wrap { get; private set; }
            private RemappedProvider<IDENTIFIER, DATA> Remapper;

            private bool readied = false;

            public ProviderWrapper(RemappedProvider<IDENTIFIER, DATA> remapper, IProvider<IDENTIFIER, DATA> wrap)
            {
                Wrap = wrap;
                Remapper = remapper;
            }

            public void Ready()
            {
                if (readied) return;

                Wrap.Lifecycle.OnDisposed += Lifecycle_OnDisposing;
                Wrap.Lifecycle.OnTimedOut += Lifecycle_OnTimedOut;
                Wrap.Lifecycle.OnUpdated += Lifecycle_OnUpdated;
                Wrap.Lifecycle.OnResumed += Lifecycle_OnResumed;
                Wrap.Lifecycle.OnReady += Lifecycle_OnReady;

                readied = true;
            }
            public void Dispose()
            {
                if (Wrap == null) return;
                if (!readied) return;

                Wrap.Lifecycle.OnDisposed -= Lifecycle_OnDisposing;
                Wrap.Lifecycle.OnTimedOut -= Lifecycle_OnTimedOut;
                Wrap.Lifecycle.OnUpdated -= Lifecycle_OnUpdated;
                Wrap.Lifecycle.OnResumed -= Lifecycle_OnResumed;
                Wrap.Lifecycle.OnReady -= Lifecycle_OnReady;

                readied = false;
            }

            private void Lifecycle_OnReady(int group)
            {
                Remapper.Lifecycle_OnReady(this, group);
            }
            private void Lifecycle_OnResumed(int group)
            {
                Remapper.Lifecycle_OnResumed(this, group);
            }
            private void Lifecycle_OnUpdated(int group)
            {
                Remapper.Lifecycle_OnUpdated(this, group);
            }
            private void Lifecycle_OnTimedOut(int group)
            {
                Remapper.Lifecycle_OnTimedOut(this, group);
            }
            private void Lifecycle_OnDisposing(int group)
            {
                Remapper.Lifecycle_OnDisposing(this, group);
            }
        }
    }
}