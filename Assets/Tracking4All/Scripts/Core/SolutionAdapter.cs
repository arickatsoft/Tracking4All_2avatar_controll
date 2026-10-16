// SolutionAdapter
// (C) 2024-2025 G8gaming Ltd.
using UnityEngine;

namespace Tracking4All
{
    public abstract class SolutionAdapter<IDENTIFIER, DATA> : UnityAdapter<IDENTIFIER, DATA, IDENTIFIER, DATA>
        where IDENTIFIER : System.Enum
        where DATA : new()
    {
        protected IModifier<DATA> DataModifier { get; private set; }

        private Flags keepGroupAlive = new Flags(false);

        protected SolutionAdapter(int groupSize, IProvider<IDENTIFIER, DATA> dataSource, IModifier<DATA> modifier,
            float aliveThreshold = -1) : base(groupSize, dataSource, aliveThreshold)
        {
            DataModifier = modifier;
        }

        protected override void SelfUpdate(int group, IProvider<IDENTIFIER, DATA> from)
        {
            // Update using modifiers.
            DATA currentData, targetData;
            bool stayAlive = false;
            float dt = Time.deltaTime;

            if (DataModifier != null)
            {
                DataModifier.PreCalculate(dt, DataCount);

                for (int i = 0; i < DataCount; ++i)
                {
                    currentData = Get(group, i); // the current data
                    targetData = from.Get(group, i); // the data to target
                    stayAlive = false || stayAlive;

                    // Set the current data using the target data
                    DataModifier.Modify(i, ref currentData, ref targetData, ref stayAlive, dt);

                    Set(group, i, currentData);
                }
            }
            else
            {
                for (int i = 0; i < DataCount; ++i)
                {
                    currentData = Get(group, i);
                    targetData = from.Get(group, i);
                    stayAlive = false || stayAlive;

                    Set(group, i, targetData);
                }
            }

            keepGroupAlive.SetFlag(group, stayAlive);
        }

        protected override bool ShouldTimeout(int group)
        {
            bool baseTimeout = base.ShouldTimeout(group);
            return baseTimeout && keepGroupAlive.IsFlagNotSet(group); // allow the modifier to adjust the lifecycle
        }
    }
}