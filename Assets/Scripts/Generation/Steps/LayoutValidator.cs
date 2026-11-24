using UnityEngine;

namespace Flicker.Generation
{
    // The soft-lock guard. A block is valid only if every walkable cell can be reached from the car spot
    // and nothing narrower than 2 tiles exists. On success it records walking distances and spawn hints.
    public static class LayoutValidator
    {
        // Returns: Null when valid, otherwise a short reason (logged with the seed by the generator).
        public static string Validate(BlockLayout layout)
        {
            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                    if (layout[x, y] == CellType.Empty) return $"empty cell at {x},{y}";

            foreach (var c in layout.CarSpot.allPositionsWithin)
                if (!layout.IsWalkable(c)) return $"car spot blocked at {c}";

            var owner = new bool[layout.Width, layout.Height];
            foreach (var p in layout.Props)
                foreach (var c in p.Footprint.allPositionsWithin)
                {
                    if (!layout.InBounds(c) || !layout[c].IsWalkable()) return $"{p.Kind} off walkable ground at {c}";
                    if (owner[c.x, c.y]) return $"props overlap at {c}";
                    owner[c.x, c.y] = true;
                }

            var start = new Vector2Int(layout.CarSpot.xMin + layout.CarSpot.width / 2, layout.CarSpot.yMin + layout.CarSpot.height / 2);
            var dist = LayoutChecks.Distances(layout, start);
            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (!layout.IsWalkable(c)) continue;
                    if (dist[x, y] < 0) return $"unreachable {layout[c]} at {c}";
                    if (!LayoutChecks.Covered2x2(layout, c)) return $"1-wide gap at {c}";
                }

            layout.DistanceFromCar = dist;
            layout.SpawnHints.Clear();
            for (int y = 0; y < layout.Height; y++)
                for (int x = 0; x < layout.Width; x++)
                {
                    var c = new Vector2Int(x, y);
                    var zone = layout[c].ZoneOf();
                    if (dist[x, y] > 0 && zone.HasValue && !layout.CarSpot.Contains(c))
                        layout.SpawnHints.Add(new SpawnHint(c, zone.Value, dist[x, y]));
                }
            return null;
        }
    }
}
