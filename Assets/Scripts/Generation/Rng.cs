using System;
using System.Collections.Generic;

namespace Flicker.Generation
{
    // Seeded PRNG (mulberry32). All generation randomness flows through one instance,
    // passed explicitly, so a seed always reproduces the same block.
    // Never use UnityEngine.Random in generation code: its global state is shared with gameplay.
    public sealed class Rng
    {
        private uint state;

        public Rng(int seed) => state = unchecked((uint)seed);

        public uint NextUInt()
        {
            unchecked
            {
                state += 0x6D2B79F5u;
                uint z = state;
                z = (z ^ (z >> 15)) * (z | 1u);
                z ^= z + (z ^ (z >> 7)) * (z | 61u);
                return z ^ (z >> 14);
            }
        }

        // Uniform int in [minInclusive, maxExclusive). Returns min for an empty or single-value range.
        public int Range(int minInclusive, int maxExclusive)
        {
            long span = (long)maxExclusive - minInclusive;
            if (span <= 1) return minInclusive;
            ulong r = ((ulong)NextUInt() << 32) | NextUInt();
            return (int)(minInclusive + (long)(r % (ulong)span));
        }

        // Uniform float in [0, 1).
        public float Value() => (NextUInt() >> 8) * (1f / 16777216f);

        public bool Chance(float p) => Value() < p;

        public T Pick<T>(IReadOnlyList<T> items) => items[Range(0, items.Count)];

        // Index picked proportionally to weight. -1 when no weight is positive.
        public int PickWeighted(IReadOnlyList<float> weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++)
                if (weights[i] > 0f) total += weights[i];
            if (total <= 0f) return -1;

            float roll = Value() * total;
            int last = -1;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f) continue;
                last = i;
                if (roll < weights[i]) return i;
                roll -= weights[i];
            }
            return last; // float rounding can leave a sliver at the end
        }

        // Seed for a retry attempt. Attempt 0 is the seed itself; later attempts are
        // well-mixed (splitmix32 finalizer), so a seed always resolves to the same block.
        public static int Derive(int seed, int attempt)
        {
            if (attempt == 0) return seed;
            unchecked
            {
                uint z = (uint)seed + (uint)attempt * 0x9E3779B9u;
                z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
                z = (z ^ (z >> 13)) * 0xC2B2AE35u;
                z ^= z >> 16;
                return (int)z;
            }
        }
    }
}
