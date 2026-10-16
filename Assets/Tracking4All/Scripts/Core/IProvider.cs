// IProvider
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{

    /// <summary>
    /// Provides DATA by IDENTIFIER.
    /// </summary>
    /// <typeparam name="IDENTIFIER"></typeparam>
    /// <typeparam name="DATA"></typeparam>
    public interface IProvider<IDENTIFIER, DATA>
        where IDENTIFIER : System.Enum
        where DATA : new()
    {
        public IProviderLifecycle Lifecycle { get; }

        public int GroupSize { get; }
        public int DataCount { get; }

        public DATA Get(int group, IDENTIFIER index);
        public DATA Get(int group, int index);
    }
}