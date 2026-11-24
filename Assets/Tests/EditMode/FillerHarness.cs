using UnityEngine;

namespace Flicker.Generation.Tests
{
    // A 40x30 field of alley with one empty lot in it, for testing a single filler.
    public static class FillerHarness
    {
        public static (BlockLayout layout, Lot lot) Make(RectInt rect, Sides open, int seed = 0)
        {
            var layout = new BlockLayout(40, 30, seed, 0);
            layout.Fill(new RectInt(0, 0, 40, 30), CellType.Alley);
            layout.Fill(rect, CellType.Empty);
            var lot = new Lot(rect, open);
            layout.Lots.Add(lot);
            return (layout, lot);
        }

        public static void AssertNoEmpty(BlockLayout l, RectInt r, string context)
        {
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    NUnit.Framework.Assert.AreNotEqual(CellType.Empty, l[x, y], $"{context} cell {x},{y}");
        }
    }
}
