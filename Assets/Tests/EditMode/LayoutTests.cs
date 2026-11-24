using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Flicker.Generation.Tests
{
    public class LayoutTests
    {
        private static BlockLayout Filled(int w, int h, CellType t)
        {
            var l = new BlockLayout(w, h, 0, 0);
            l.Fill(new RectInt(0, 0, w, h), t);
            return l;
        }

        [Test]
        public void IsWalkable_FalseOutsideBoundsAndOnProps()
        {
            var l = Filled(4, 4, CellType.Alley);
            Assert.IsFalse(l.IsWalkable(new Vector2Int(5, 0)));
            Assert.IsFalse(l.IsWalkable(new Vector2Int(-1, 0)));
            l.AddProp(new PropPlacement(PropKind.Bench, new RectInt(0, 0, 2, 1)));
            Assert.IsFalse(l.IsWalkable(new Vector2Int(1, 0)));
            Assert.IsTrue(l.HasProp(new Vector2Int(0, 0)));
            Assert.IsTrue(l.IsWalkable(new Vector2Int(2, 0)));
            Assert.AreEqual(1, l.Props.Count);
            l.RemoveLastProp();
            Assert.IsTrue(l.IsWalkable(new Vector2Int(1, 0)));
            Assert.AreEqual(0, l.Props.Count);
        }

        [Test]
        public void IsWalkable_FalseOnBlockingCells()
        {
            var l = Filled(2, 1, CellType.Alley);
            l[1, 0] = CellType.Wall;
            Assert.IsTrue(l.IsWalkable(new Vector2Int(0, 0)));
            Assert.IsFalse(l.IsWalkable(new Vector2Int(1, 0)));
            Assert.AreEqual(CellType.Wall, l[new Vector2Int(1, 0)]);
        }

        [Test]
        public void GreedyRects_CoversMaskExactlyWithoutOverlap()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var rng = new Rng(seed);
                var mask = new bool[20, 15];
                for (int x = 0; x < 20; x++)
                    for (int y = 0; y < 15; y++)
                        mask[x, y] = rng.Chance(0.4f);

                var count = new int[20, 15];
                foreach (var r in GreedyRects.Merge(20, 15, (x, y) => mask[x, y]))
                    for (int x = r.xMin; x < r.xMax; x++)
                        for (int y = r.yMin; y < r.yMax; y++)
                            count[x, y]++;

                for (int x = 0; x < 20; x++)
                    for (int y = 0; y < 15; y++)
                        Assert.AreEqual(mask[x, y] ? 1 : 0, count[x, y], $"seed {seed} cell {x},{y}");
            }
        }

        [Test]
        public void GreedyRects_SolidRect_IsOneRect()
        {
            var rects = GreedyRects.Merge(6, 3, (x, y) => true);
            Assert.AreEqual(1, rects.Count);
            Assert.AreEqual(new RectInt(0, 0, 6, 3), rects[0]);
        }

        [Test]
        public void Distances_RespectWalls()
        {
            var l = Filled(5, 1, CellType.Alley);
            l[2, 0] = CellType.Wall;
            var d = LayoutChecks.Distances(l, new Vector2Int(0, 0));
            CollectionAssert.AreEqual(new[] { 0, 1, -1, -1, -1 }, new[] { d[0, 0], d[1, 0], d[2, 0], d[3, 0], d[4, 0] });
        }

        [Test]
        public void Covered2x2_OneWideCorridorFails()
        {
            var l = Filled(3, 3, CellType.Wall);
            l.Fill(new RectInt(1, 0, 1, 3), CellType.Alley);
            Assert.IsFalse(LayoutChecks.Covered2x2(l, new Vector2Int(1, 1)));
            Assert.IsFalse(LayoutChecks.AllCovered2x2(l, new RectInt(0, 0, 3, 3)));

            var wide = Filled(3, 3, CellType.Wall);
            wide.Fill(new RectInt(1, 0, 2, 3), CellType.Alley);
            Assert.IsTrue(LayoutChecks.Covered2x2(wide, new Vector2Int(1, 1)));
            Assert.IsTrue(LayoutChecks.AllCovered2x2(wide, new RectInt(0, 0, 3, 3)));
        }

        [Test]
        public void RegionConnectedToEdge_DetectsIsland()
        {
            var region = new RectInt(0, 0, 7, 7);
            var open = Filled(7, 7, CellType.Park);
            Assert.IsTrue(LayoutChecks.RegionConnectedToEdge(open, region, Sides.South));

            var island = Filled(7, 7, CellType.Park);
            foreach (var c in new[] { (2, 2), (3, 2), (4, 2), (2, 3), (4, 3), (2, 4), (3, 4), (4, 4) })
                island[c.Item1, c.Item2] = CellType.Wall;
            Assert.IsFalse(LayoutChecks.RegionConnectedToEdge(island, region, Sides.South));
        }

        [Test]
        public void RegionConnectedToEdge_NeedsWalkableEdgeCell()
        {
            var l = Filled(4, 4, CellType.Park);
            l.Fill(new RectInt(0, 0, 4, 1), CellType.Fence);
            Assert.IsFalse(LayoutChecks.RegionConnectedToEdge(l, new RectInt(0, 0, 4, 4), Sides.South));
            Assert.IsTrue(LayoutChecks.RegionConnectedToEdge(l, new RectInt(0, 0, 4, 4), Sides.North));
        }

        [Test]
        public void SidesEach_FixedOrder()
        {
            CollectionAssert.AreEqual(new[] { Sides.South, Sides.West, Sides.East },
                new List<Sides>((Sides.East | Sides.South | Sides.West).Each()));
        }
    }
}
