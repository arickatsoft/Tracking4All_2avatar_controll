// IProviderLifecycle
// (C) 2024-2025 G8gaming Ltd.
using System.Text;

namespace Tracking4All
{

    public delegate void GroupEvent(int group);

    public interface IProviderLifecycle
    {
        public int GroupSize { get; }

        public event GroupEvent OnReady;
        public event GroupEvent OnDisposed;
        public event GroupEvent OnResumed;
        public event GroupEvent OnUpdated;
        public event GroupEvent OnTimedOut;

        public bool IsReady(int group);
        public bool IsDisposed(int group);
        public bool IsAlive(int group);

        public bool IsTimedOut(int group)
        {
            return !IsAlive(group);
        }

#if UNITY_EDITOR
        public void EDITORONLY_GetStateStringRepresentation(ref StringBuilder state)
        {
            if (state == null || state.Capacity != 2 * GroupSize - 1)
            {
                state = new StringBuilder(2 * GroupSize - 1);
                state.Append(new string('|', state.Capacity));
            }

            int g = 0;
            for (int c = 0; c < state.Capacity; ++c)
            {
                if (c % 2 == 0)
                {
                    g = c / 2;
                    if (IsAlive(g)) state[c] = 'A';
                    else if (IsDisposed(g)) state[c] = 'D';
                    else if (IsReady(g)) state[c] = 'R';
                    else state[c] = '?';
                }
                else
                {
                    state[c] = '|';
                }
            }

        }
#endif
    }
}