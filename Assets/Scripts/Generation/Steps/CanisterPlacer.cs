using System.Collections.Generic;
using UnityEngine;

namespace Flicker.Generation
{
    // Places the fuel canisters after validation, from the spawn hints: inside the block (never on the
    // sidewalk), at least canisterMinCarDistance walking cells from the car, and at least canisterMinSpacing
    // walking cells from each other. Fails the attempt when the block has no room for all of them.
    public static class CanisterPlacer
    {
        // Returns: Null on success, otherwise a short reason (the generator retries the seed).
        public static string Place(BlockLayout layout, Rng rng, BlockGenSettings s)
        {
            layout.Canisters.Clear();
            if (s.canisterCount == 0) return null;

            var candidates = new List<Vector2Int>();
            foreach (var hint in layout.SpawnHints)
                if (hint.Zone != Zone.Sidewalk && hint.Distance >= s.canisterMinCarDistance)
                    candidates.Add(hint.Cell);

            for (int i = 0; i < s.canisterCount; i++)
            {
                if (candidates.Count == 0)
                    return $"room for only {i} of {s.canisterCount} fuel canisters";

                var pick = rng.Pick(candidates);
                layout.Canisters.Add(pick);

                // Keep only candidates far enough (walking) from the canister just placed.
                var dist = LayoutChecks.Distances(layout, pick);
                candidates.RemoveAll(c => dist[c.x, c.y] < s.canisterMinSpacing);
            }
            return null;
        }
    }
}
