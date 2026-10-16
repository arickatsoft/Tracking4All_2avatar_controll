// CoroutineGroup
// (C) 2024-2025 G8gaming Ltd.
namespace Tracking4All
{
    using System.Collections;
    using UnityEngine;

    public class CoroutineGroup
    {
        private MonoBehaviour owner;
        private Coroutine[] coroutines; // Array to manage coroutines by index.

        public CoroutineGroup(int size, MonoBehaviour owner)
        {
            coroutines = new Coroutine[size];
            this.owner = owner;
        }

        public void StartAtIndex(int index, IEnumerator routine)
        {
            if (index < 0 || index >= coroutines.Length)
            {
                UnityEngine.Debug.LogError($"Index {index} is out of bounds for CoroutineGroup.");
                return;
            }

            // Stop any coroutine currently running at this index.
            if (coroutines[index] != null)
            {
                owner.StopCoroutine(coroutines[index]);
            }

            // Start the new coroutine and store it at the specified index.
            coroutines[index] = owner.StartCoroutine(RunCoroutine(index, routine));
        }
        public void StopAtIndex(int index)
        {
            if (index < 0 || index >= coroutines.Length)
            {
                UnityEngine.Debug.LogError($"Index {index} is out of bounds for CoroutineGroup.");
                return;
            }

            if (coroutines[index] != null)
            {
                owner.StopCoroutine(coroutines[index]);
                coroutines[index] = null;
            }
        }

        public void StopAll()
        {
            for (int i = 0; i < coroutines.Length; i++)
            {
                if (coroutines[i] != null)
                {
                    owner.StopCoroutine(coroutines[i]);
                    coroutines[i] = null;
                }
            }
        }

        private IEnumerator RunCoroutine(int index, IEnumerator routine)
        {
            yield return owner.StartCoroutine(routine);
            coroutines[index] = null;
        }

        public bool IsRunning(int index)
        {
            if (index < 0 || index >= coroutines.Length)
            {
                UnityEngine.Debug.LogError($"Index {index} is out of bounds for CoroutineGroup.");
                return false;
            }

            return coroutines[index] != null;
        }

        public int Size()
        {
            return coroutines.Length;
        }
    }

}