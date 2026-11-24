using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Flicker.Generation.Tests
{
    public class LotSplitterTests
    {
        private const int Seeds = 300;

        private static (BlockLayout layout, List<Lot> lots) Run(int seed)
        {
            var s = TestSettings.Default();
            var rng = new Rng(seed);
            var layout = new BlockLayout(rng.Range(s.widthRange.x, s.widthRange.y), rng.Range(s.heightRange.x, s.heightRange.y), seed, 0);
            BlockFrame.Lay(layout, rng, s);
            return (layout, LotSplitter.Split(layout, rng, s));
        }

        [Test]
        public void Split_PartitionsInnerArea()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                var (l, lots) = Run(seed);
                var owners = new int[l.Width, l.Height];
                foreach (var lot in lots)
                    for (int x = lot.Rect.xMin; x < lot.Rect.xMax; x++)
                        for (int y = lot.Rect.yMin; y < lot.Rect.yMax; y++)
                            owners[x, y]++;

                for (int x = l.InnerArea.xMin; x < l.InnerArea.xMax; x++)
                    for (int y = l.InnerArea.yMin; y < l.InnerArea.yMax; y++)
                    {
                        if (l[x, y] == CellType.Alley) Assert.AreEqual(0, owners[x, y], $"seed {seed} alley {x},{y} inside a lot");
                        else
                        {
                            Assert.AreEqual(CellType.Empty, l[x, y], $"seed {seed} {x},{y}");
                            Assert.AreEqual(1, owners[x, y], $"seed {seed} {x},{y} owned by {owners[x, y]} lots");
                        }
                    }
                foreach (var lot in lots)
                    Assert.IsTrue(l.InnerArea.Contains(lot.Rect.min) && l.InnerArea.Contains(lot.Rect.max - Vector2Int.one), $"seed {seed} lot outside inner area");
            }
        }

        [Test]
        public void Split_LotSizesWithinBounds()
        {
            var s = TestSettings.Default();
            int total = 0, withinMax = 0;
            for (int seed = 0; seed < Seeds; seed++)
                foreach (var lot in Run(seed).lots)
                {
                    total++;
                    var r = lot.Rect;
                    Assert.GreaterOrEqual(Mathf.Min(r.width, r.height), s.lotMinSize, $"seed {seed} {r}");
                    Assert.LessOrEqual(Mathf.Max(r.width, r.height), 2 * s.lotMaxSize, $"seed {seed} {r}");
                    if (r.width <= s.lotMaxSize && r.height <= s.lotMaxSize) withinMax++;
                }
            Assert.GreaterOrEqual(withinMax, total * 0.9f, $"{withinMax}/{total} lots within lotMaxSize");
        }

        [Test]
        public void Split_EveryLotHasOpenSide_AndOpenSidesTouchWalkable()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                var (l, lots) = Run(seed);
                foreach (var lot in lots)
                {
                    Assert.AreNotEqual(Sides.None, lot.Open, $"seed {seed} {lot.Rect}");
                    foreach (var side in lot.Open.Each())
                        foreach (var c in OutsideStrip(lot.Rect, side))
                        {
                            var t = l[c];
                            Assert.IsTrue(t == CellType.Sidewalk || t == CellType.Alley, $"seed {seed} lot {lot.Rect} side {side} cell {c} is {t}");
                        }
                }
            }
        }

        [Test]
        public void Split_AlleysConnectedToSidewalk()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                var (l, _) = Run(seed);
                var d = LayoutChecks.Distances(l, l.CarSpot.position);
                for (int x = 0; x < l.Width; x++)
                    for (int y = 0; y < l.Height; y++)
                        if (l[x, y] == CellType.Alley)
                            Assert.GreaterOrEqual(d[x, y], 0, $"seed {seed} alley {x},{y} unreachable");
            }
        }

        [Test]
        public void Split_AlleysAtLeastTwoWide()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                var (l, _) = Run(seed);
                Assert.IsTrue(LayoutChecks.AllCovered2x2(l, l.InnerArea), $"seed {seed}");
            }
        }

        [Test]
        public void Split_ProducesAlleys()
        {
            int withAlleys = 0;
            for (int seed = 0; seed < Seeds; seed++)
            {
                var (l, _) = Run(seed);
                bool any = false;
                for (int x = 0; x < l.Width && !any; x++)
                    for (int y = 0; y < l.Height && !any; y++)
                        any = l[x, y] == CellType.Alley;
                if (any) withAlleys++;
            }
            Assert.Greater(withAlleys, Seeds * 0.95f);
        }

        [Test]
        public void Split_Deterministic()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var a = Run(seed).lots;
                var b = Run(seed).lots;
                Assert.AreEqual(a.Count, b.Count);
                for (int i = 0; i < a.Count; i++)
                {
                    Assert.AreEqual(a[i].Rect, b[i].Rect);
                    Assert.AreEqual(a[i].Open, b[i].Open);
                }
            }
        }

        private static IEnumerable<Vector2Int> OutsideStrip(RectInt r, Sides side)
        {
            switch (side)
            {
                case Sides.South: for (int x = r.xMin; x < r.xMax; x++) yield return new Vector2Int(x, r.yMin - 1); break;
                case Sides.North: for (int x = r.xMin; x < r.xMax; x++) yield return new Vector2Int(x, r.yMax); break;
                case Sides.West: for (int y = r.yMin; y < r.yMax; y++) yield return new Vector2Int(r.xMin - 1, y); break;
                case Sides.East: for (int y = r.yMin; y < r.yMax; y++) yield return new Vector2Int(r.xMax, y); break;
            }
        }
    }
}
