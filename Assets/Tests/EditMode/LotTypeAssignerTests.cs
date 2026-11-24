using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Flicker.Generation.Tests
{
    public class LotTypeAssignerTests
    {
        private const int Seeds = 300;

        private static (List<Lot> lots, bool ok) Run(int seed, BlockGenSettings s = null)
        {
            s ??= TestSettings.Default();
            var rng = new Rng(seed);
            var layout = new BlockLayout(rng.Range(s.widthRange.x, s.widthRange.y), rng.Range(s.heightRange.x, s.heightRange.y), seed, 0);
            BlockFrame.Lay(layout, rng, s);
            var lots = LotSplitter.Split(layout, rng, s);
            bool ok = LotTypeAssigner.Assign(lots, rng, s);
            return (lots, ok);
        }

        private static bool Touch(Lot a, Lot b)
        {
            var grown = new RectInt(a.Rect.xMin - 1, a.Rect.yMin - 1, a.Rect.width + 2, a.Rect.height + 2);
            return grown.Overlaps(b.Rect);
        }

        [Test]
        public void Assign_MeetsMinHouses()
        {
            int okCount = 0;
            for (int seed = 0; seed < Seeds; seed++)
            {
                var (lots, ok) = Run(seed);
                if (!ok) continue;
                okCount++;
                Assert.GreaterOrEqual(lots.Count(l => l.Type == LotType.House), 3, $"seed {seed}");
            }
            Assert.Greater(okCount, Seeds * 0.9f, "most blocks should meet the house minimum");
        }

        [Test]
        public void Assign_TypesFitTheirLots()
        {
            var s = TestSettings.Default();
            for (int seed = 0; seed < Seeds; seed++)
                foreach (var lot in Run(seed).lots)
                {
                    Assert.AreNotEqual(LotType.Unassigned, lot.Type, $"seed {seed} {lot.Rect}");
                    Assert.IsTrue(LotFillers.Get(lot.Type).CanFill(lot, s), $"seed {seed} {lot.Type} on {lot.Rect}");
                }
        }

        [Test]
        public void Assign_NoTouchingParks()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                var parks = Run(seed).lots.Where(l => l.Type == LotType.Park).ToList();
                for (int i = 0; i < parks.Count; i++)
                    for (int j = i + 1; j < parks.Count; j++)
                        Assert.IsFalse(Touch(parks[i], parks[j]), $"seed {seed} parks {parks[i].Rect} {parks[j].Rect}");
            }
        }

        [Test]
        public void Assign_TallOnlyOnLargeLots()
        {
            var s = TestSettings.Default();
            for (int seed = 0; seed < Seeds; seed++)
                foreach (var lot in Run(seed).lots.Where(l => l.Type == LotType.TallBuilding))
                    Assert.GreaterOrEqual(Mathf.Min(lot.Rect.width, lot.Rect.height), s.tallMinSide, $"seed {seed}");
        }

        [Test]
        public void Assign_UsesEveryType()
        {
            var seen = new HashSet<LotType>();
            for (int seed = 0; seed < Seeds; seed++)
                foreach (var lot in Run(seed).lots) seen.Add(lot.Type);
            CollectionAssert.IsSupersetOf(seen, new[] { LotType.House, LotType.TallBuilding, LotType.Parking, LotType.Park });
        }

        [Test]
        public void Assign_Deterministic()
        {
            for (int seed = 0; seed < 20; seed++)
                CollectionAssert.AreEqual(Run(seed).lots.Select(l => l.Type), Run(seed).lots.Select(l => l.Type));
        }

        [Test]
        public void Assign_ReturnsFalseWhenImpossible()
        {
            var s = TestSettings.Default();
            s.minHouses = 999;
            Assert.IsFalse(Run(1, s).ok);
        }
    }
}
