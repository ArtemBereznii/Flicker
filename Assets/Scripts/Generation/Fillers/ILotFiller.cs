using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Flicker.Generation
{
    // Fills one lot of a given type. Fillers are discovered by reflection, so adding a lot type means:
    // a LotType value, one class implementing this, and a weight in BlockGenSettings.
    public interface ILotFiller
    {
        LotType Type { get; }
        bool CanFill(Lot lot, BlockGenSettings s);
        void Fill(Lot lot, BlockLayout layout, Rng rng, BlockGenSettings s);
    }

    public static class LotFillers
    {
        private static ILotFiller[] all;

        // Every filler in the generation assembly, ordered by LotType (deterministic).
        public static IReadOnlyList<ILotFiller> All => all ??= typeof(ILotFiller).Assembly.GetTypes()
            .Where(t => typeof(ILotFiller).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Select(t => (ILotFiller)Activator.CreateInstance(t))
            .OrderBy(f => f.Type)
            .ToArray();

        public static ILotFiller Get(LotType t)
        {
            foreach (var f in All)
                if (f.Type == t) return f;
            throw new ArgumentException($"No lot filler for {t}");
        }
    }

    public static class FillerUtil
    {
        // Fence along every lot edge that does not face the sidewalk or an alley.
        public static void FenceClosedSides(Lot lot, BlockLayout layout)
        {
            foreach (var side in (~lot.Open & (Sides.South | Sides.North | Sides.West | Sides.East)).Each())
                layout.Fill(RectSides.Edge(lot.Rect, side, 1), CellType.Fence);
        }
    }

    public static class PropPlacer
    {
        // Places the prop only if it sits on free walkable ground inside the lot and the lot stays
        // connected to its access edges with no 1-wide gaps. Otherwise nothing changes.
        public static bool TryPlace(BlockLayout layout, Lot lot, PropPlacement p, Sides accessEdges)
        {
            var f = p.Footprint;
            if (f.width <= 0 || f.height <= 0) return false;
            for (int x = f.xMin; x < f.xMax; x++)
                for (int y = f.yMin; y < f.yMax; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (!lot.Rect.Contains(c) || !layout.IsWalkable(c)) return false;
                }

            layout.AddProp(p);
            if (LayoutChecks.AllCovered2x2(layout, lot.Rect) && LayoutChecks.RegionConnectedToEdge(layout, lot.Rect, accessEdges))
                return true;

            layout.RemoveLastProp();
            return false;
        }
    }

    // Side-relative rectangle helpers, so fillers can work the same way for any front side.
    public static class RectSides
    {
        // The strip of the given depth inside r along one side.
        public static RectInt Edge(RectInt r, Sides side, int depth)
        {
            switch (side)
            {
                case Sides.South: return new RectInt(r.xMin, r.yMin, r.width, depth);
                case Sides.North: return new RectInt(r.xMin, r.yMax - depth, r.width, depth);
                case Sides.West: return new RectInt(r.xMin, r.yMin, depth, r.height);
                case Sides.East: return new RectInt(r.xMax - depth, r.yMin, depth, r.height);
                default: throw new ArgumentException(nameof(side));
            }
        }

        // r with n cells removed from one side.
        public static RectInt Shrink(RectInt r, Sides side, int n)
        {
            switch (side)
            {
                case Sides.South: return new RectInt(r.xMin, r.yMin + n, r.width, r.height - n);
                case Sides.North: return new RectInt(r.xMin, r.yMin, r.width, r.height - n);
                case Sides.West: return new RectInt(r.xMin + n, r.yMin, r.width - n, r.height);
                case Sides.East: return new RectInt(r.xMin, r.yMin, r.width - n, r.height);
                default: throw new ArgumentException(nameof(side));
            }
        }

        // Direction pointing from this side into the rect.
        public static Vector2Int Inward(Sides side)
        {
            switch (side)
            {
                case Sides.South: return Vector2Int.up;
                case Sides.North: return Vector2Int.down;
                case Sides.West: return Vector2Int.right;
                case Sides.East: return Vector2Int.left;
                default: throw new ArgumentException(nameof(side));
            }
        }

        public static Sides Perpendicular(Sides side) =>
            side == Sides.South || side == Sides.North ? Sides.West | Sides.East : Sides.South | Sides.North;

        // Cells of a one-cell-thick line, in increasing x or y.
        public static List<Vector2Int> LineCells(RectInt line)
        {
            var cells = new List<Vector2Int>();
            for (int y = line.yMin; y < line.yMax; y++)
                for (int x = line.xMin; x < line.xMax; x++)
                    cells.Add(new Vector2Int(x, y));
            return cells;
        }
    }
}
