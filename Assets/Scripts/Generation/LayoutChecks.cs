using System.Collections.Generic;
using UnityEngine;

namespace Flicker.Generation
{
    // Reachability and corridor-width checks used by fillers and the validator.
    public static class LayoutChecks
    {
        private static readonly Vector2Int[] Neighbours =
            { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        // BFS walking distance from start over walkable cells (4-neighbour). -1 = unreachable or not walkable.
        public static int[,] Distances(BlockLayout l, Vector2Int start)
        {
            var dist = new int[l.Width, l.Height];
            for (int x = 0; x < l.Width; x++)
                for (int y = 0; y < l.Height; y++)
                    dist[x, y] = -1;
            if (!l.IsWalkable(start)) return dist;

            var queue = new Queue<Vector2Int>();
            dist[start.x, start.y] = 0;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var d in Neighbours)
                {
                    var n = c + d;
                    if (!l.IsWalkable(n) || dist[n.x, n.y] >= 0) continue;
                    dist[n.x, n.y] = dist[c.x, c.y] + 1;
                    queue.Enqueue(n);
                }
            }
            return dist;
        }

        // True when some fully walkable 2x2 square contains c, i.e. c is not in a 1-wide gap.
        public static bool Covered2x2(BlockLayout l, Vector2Int c)
        {
            for (int ox = -1; ox <= 0; ox++)
                for (int oy = -1; oy <= 0; oy++)
                {
                    var o = new Vector2Int(c.x + ox, c.y + oy);
                    if (l.IsWalkable(o) && l.IsWalkable(o + Vector2Int.right) &&
                        l.IsWalkable(o + Vector2Int.up) && l.IsWalkable(o + Vector2Int.one))
                        return true;
                }
            return false;
        }

        // Every walkable cell in the region is Covered2x2 (squares may extend outside the region).
        public static bool AllCovered2x2(BlockLayout l, RectInt region)
        {
            for (int x = region.xMin; x < region.xMax; x++)
                for (int y = region.yMin; y < region.yMax; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (l.IsWalkable(c) && !Covered2x2(l, c)) return false;
                }
            return true;
        }

        // Every walkable cell in the region can be reached, moving only inside the region,
        // from a walkable cell on one of the given edges. A region with no walkable cells passes.
        public static bool RegionConnectedToEdge(BlockLayout l, RectInt region, Sides edges)
        {
            bool Inside(Vector2Int c) => region.Contains(c) && l.IsWalkable(c);

            var seen = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            foreach (var c in EdgeCells(region, edges))
                if (Inside(c) && seen.Add(c)) queue.Enqueue(c);

            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var d in Neighbours)
                {
                    var n = c + d;
                    if (Inside(n) && seen.Add(n)) queue.Enqueue(n);
                }
            }

            for (int x = region.xMin; x < region.xMax; x++)
                for (int y = region.yMin; y < region.yMax; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (l.IsWalkable(c) && !seen.Contains(c)) return false;
                }
            return true;
        }

        public static IEnumerable<Vector2Int> EdgeCells(RectInt r, Sides edges)
        {
            if ((edges & Sides.South) != 0) for (int x = r.xMin; x < r.xMax; x++) yield return new Vector2Int(x, r.yMin);
            if ((edges & Sides.North) != 0) for (int x = r.xMin; x < r.xMax; x++) yield return new Vector2Int(x, r.yMax - 1);
            if ((edges & Sides.West) != 0) for (int y = r.yMin; y < r.yMax; y++) yield return new Vector2Int(r.xMin, y);
            if ((edges & Sides.East) != 0) for (int y = r.yMin; y < r.yMax; y++) yield return new Vector2Int(r.xMax - 1, y);
        }
    }
}
