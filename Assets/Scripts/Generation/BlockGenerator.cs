using System;
using UnityEngine;

namespace Flicker.Generation
{
    public sealed class BlockGenerationException : Exception
    {
        public int Seed { get; }
        public BlockGenerationException(int seed, string message) : base(message) => Seed = seed;
    }

    // seed → block. Pipeline: roll size → street frame → BSP lots + alleys → lot types → fill lots → validate.
    // A rejected attempt retries with Rng.Derive(seed, attempt), so a seed always resolves to the same block.
    public static class BlockGenerator
    {
        public static BlockLayout Generate(int seed, BlockGenSettings s)
        {
            string invalid = s.Validate();
            if (invalid != null) throw new BlockGenerationException(seed, $"Block seed {seed}: invalid settings: {invalid}");

            string last = null;
            for (int attempt = 0; attempt < s.maxAttempts; attempt++)
            {
                if (TryGenerate(seed, attempt, s, out var layout, out last)) return layout;
                Debug.LogWarning($"Block seed {seed} attempt {attempt} rejected: {last}");
            }
            throw new BlockGenerationException(seed, $"Block seed {seed}: no valid layout in {s.maxAttempts} attempts (last: {last})");
        }

        public static bool TryGenerate(int seed, int attempt, BlockGenSettings s, out BlockLayout layout, out string failure)
        {
            var rng = new Rng(Rng.Derive(seed, attempt));
            layout = new BlockLayout(rng.Range(s.widthRange.x, s.widthRange.y), rng.Range(s.heightRange.x, s.heightRange.y), seed, attempt);

            BlockFrame.Lay(layout, rng, s);
            layout.Lots.AddRange(LotSplitter.Split(layout, rng, s));

            if (!LotTypeAssigner.Assign(layout.Lots, rng, s))
            {
                failure = $"fewer than {s.minHouses} lots fit a house";
                return false;
            }

            foreach (var lot in layout.Lots)
                LotFillers.Get(lot.Type).Fill(lot, layout, rng, s);

            failure = LayoutValidator.Validate(layout) ?? CanisterPlacer.Place(layout, rng, s);
            return failure == null;
        }
    }
}
