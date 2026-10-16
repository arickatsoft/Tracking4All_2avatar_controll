// Flags
// (C) 2024-2025 G8gaming Ltd.
using System;

namespace Tracking4All
{
    public class Flags
    {
        public const int FLAG_COUNT = 32;

        private int flags;

        public Flags(bool startFilled)
        {
            if (startFilled) flags = -1;
            else flags = 0;
        }
        public Flags(int rawValue)
        {
            flags = rawValue;
        }

        public int GetRawValue()
        {
            return flags;
        }

        public void SetFlag(int index, bool set)
        {
            if (set) SetFlag(index);
            else UnsetFlag(index);
        }
        public void SetFlag(int index)
        {
            if (index < 0 || index >= 32)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            flags |= (1 << index);
        }
        public void UnsetFlag(int index)
        {
            if (index < 0 || index >= 32)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            flags &= ~(1 << index);
        }

        public bool IsFlagSet(int index)
        {
            if (index >= 32)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (index < 0) return AnyFlagSet();
            return (flags & (1 << index)) != 0;
        }
        public bool IsFlagNotSet(int index)
        {
            if (index >= 32)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (index < 0) return !AnyFlagSet();
            return (flags & (1 << index)) == 0;
        }
        public bool AnyFlagSet()
        {
            return flags != 0;
        }

        public void SetAll()
        {
            flags = -1; // All bits set
        }
        public void UnsetAll()
        {
            flags = 0; // All bits cleared
        }

        public override string ToString()
        {
            return Convert.ToString(flags, 2).PadLeft(32, '0');
        }
    }
}