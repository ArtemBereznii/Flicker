using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Flicker.Generation.Tests
{
    public class BlockGeneratorTests
    {
        private const int ManySeeds = 500;
        private static readonly Dictionary<int, BlockLayout> Cache = new Dictionary<int, BlockLayout>();

        private static BlockLayout Gen(int seed)
        {
            if (!Cache.TryGetValue(seed, out var l)) Cache[seed] = l = BlockGenerator.Generate(seed, TestSettings.Default());
            return l;
        }

        private static int CellHash(BlockLayout l)
        {
            unchecked
            {
                int h = l.Width * 31 + l.Height;
                for (int x = 0; x < l.Width; x++)
                    for (int y = 0; y < l.Height; y++)
                        h = h * 31 + (int)l[x, y];
                foreach (var p in l.Props) h = h * 31 + p.Footprint.GetHashCode() + (int)p.Kind;
                return h;
            }
        }

        [Test]
        public void SameSeed_IdenticalLayout()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var a = BlockGenerator.Generate(seed, TestSettings.Default());
                var b = BlockGenerator.Generate(seed, TestSettings.Default());
                Assert.AreEqual(a.Width, b.Width);
                Assert.AreEqual(a.Height, b.Height);
                Assert.AreEqual(a.Attempt, b.Attempt);
                for (int x = 0; x < a.Width; x++)
                    for (int y = 0; y < a.Height; y++)
                        Assert.AreEqual(a[x, y], b[x, y], $"seed {seed} cell {x},{y}");
                CollectionAssert.AreEqual(a.Props.Select(p => (p.Kind, p.Footprint)), b.Props.Select(p => (p.Kind, p.Footprint)));
                CollectionAssert.AreEqual(a.Lots.Select(l => (l.Rect, l.Type)), b.Lots.Select(l => (l.Rect, l.Type)));
                Assert.AreEqual(a.CarSpot, b.CarSpot);
            }
        }

        [Test]
        public void DifferentSeeds_DifferentLayouts()
        {
            var hashes = new HashSet<int>();
            for (int seed = 1; seed <= 50; seed++) hashes.Add(CellHash(Gen(seed)));
            Assert.GreaterOrEqual(hashes.Count, 49);
        }

        [Test]
        public void ManySeeds_AllResolveAndValidate()
        {
            for (int seed = 0; seed < ManySeeds; seed++)
            {
                var l = Gen(seed);
                Assert.AreEqual(seed, l.Seed);
                Assert.IsNull(LayoutValidator.Validate(l), $"seed {seed}");
            }
        }

        [Test]
        public void ManySeeds_MostResolveFirstTry()
        {
            int firstTry = 0;
            for (int seed = 0; seed < ManySeeds; seed++)
                if (Gen(seed).Attempt == 0) firstTry++;
            Assert.GreaterOrEqual(firstTry, ManySeeds * 0.8f, $"{firstTry}/{ManySeeds} first try");
        }

        [Test]
        public void ManySeeds_HaveVariety()
        {
            var seen = new HashSet<LotType>();
            for (int seed = 0; seed < ManySeeds; seed++)
            {
                var l = Gen(seed);
                Assert.IsFalse(l.Lots.Any(lot => lot.Type == LotType.Unassigned), $"seed {seed}");
                Assert.GreaterOrEqual(l.Houses.Count, 3, $"seed {seed}");
                Assert.AreEqual(l.Lots.Count(lot => lot.Type == LotType.House), l.Houses.Count, $"seed {seed}");
                foreach (var lot in l.Lots) seen.Add(lot.Type);
            }
            CollectionAssert.IsSupersetOf(seen, new[] { LotType.House, LotType.TallBuilding, LotType.Parking, LotType.Park });
        }

        [Test]
        public void CarSpot_IsWalkableAndPropFree()
        {
            for (int seed = 0; seed < ManySeeds; seed++)
            {
                var l = Gen(seed);
                foreach (var c in l.CarSpot.allPositionsWithin)
                {
                    Assert.AreEqual(CellType.Sidewalk, l[c], $"seed {seed}");
                    Assert.IsFalse(l.HasProp(c), $"seed {seed}");
                }
            }
        }

        [TestCase(0)] [TestCase(-1)] [TestCase(int.MinValue)] [TestCase(int.MaxValue)]
        public void ExtremeSeeds_Resolve(int seed)
        {
            var l = BlockGenerator.Generate(seed, TestSettings.Default());
            Assert.AreEqual(seed, l.Seed);
            Assert.IsNull(LayoutValidator.Validate(l));
        }

        [Test]
        public void SmallestBlock_Resolves()
        {
            var s = TestSettings.Default();
            s.widthRange = new Vector2Int(52, 53);
            s.heightRange = new Vector2Int(38, 39);
            for (int seed = 0; seed < 100; seed++)
            {
                var l = BlockGenerator.Generate(seed, s);
                Assert.AreEqual(52, l.Width);
                Assert.AreEqual(38, l.Height);
            }
        }

        [Test]
        public void ImpossibleSettings_Throws()
        {
            var s = TestSettings.Default();
            s.minHouses = 999;
            s.maxAttempts = 3;
            var ex = Assert.Throws<BlockGenerationException>(() => BlockGenerator.Generate(7, s));
            Assert.AreEqual(7, ex.Seed);
            StringAssert.Contains("7", ex.Message);
        }

        [Test]
        public void BlockTooSmallForFrame_ThrowsNamingSeed()
        {
            var s = TestSettings.Default();
            s.heightRange = new Vector2Int(6, 7);
            var ex = Assert.Throws<BlockGenerationException>(() => BlockGenerator.Generate(42, s));
            Assert.AreEqual(42, ex.Seed);
            StringAssert.Contains("42", ex.Message);
        }

        [Test]
        public void NonPositiveLotSize_Throws()
        {
            var s = TestSettings.Default();
            s.lotMinSize = 0;
            Assert.Throws<BlockGenerationException>(() => BlockGenerator.Generate(1, s));
        }

        [Test]
        public void NegativeWidth_Throws()
        {
            var s = TestSettings.Default();
            s.widthRange = new Vector2Int(-10, -5);
            Assert.Throws<BlockGenerationException>(() => BlockGenerator.Generate(1, s));
        }

        [Test]
        public void OneWideAlleys_Throw()
        {
            var s = TestSettings.Default();
            s.alleyWidthRange = new Vector2Int(1, 3);
            Assert.Throws<BlockGenerationException>(() => BlockGenerator.Generate(1, s));
        }

        [Test]
        public void ZeroAttempts_Throws()
        {
            var s = TestSettings.Default();
            s.maxAttempts = 0;
            Assert.Throws<BlockGenerationException>(() => BlockGenerator.Generate(1, s));
        }

        [Test]
        public void DefaultSettings_AreValid()
        {
            Assert.IsNull(TestSettings.Default().Validate());
        }

        [Test]
        public void SpawnHints_DistancesMatchBfs()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var l = Gen(seed);
                Assert.Greater(l.SpawnHints.Count, 100, $"seed {seed}");
                foreach (var h in l.SpawnHints)
                {
                    Assert.AreEqual(l.DistanceFromCar[h.Cell.x, h.Cell.y], h.Distance);
                    Assert.Greater(h.Distance, 0);
                    Assert.IsFalse(l.CarSpot.Contains(h.Cell));
                    Assert.AreEqual(l[h.Cell].ZoneOf(), h.Zone);
                }
                Assert.IsTrue(l.SpawnHints.Any(h => h.Zone == Zone.House), $"seed {seed} no house hints");
            }
        }

        [Test]
        public void Validator_RejectsEmptyCell()
        {
            var l = BlockGenerator.Generate(11, TestSettings.Default());
            var house = l.Houses[0].Rooms[0];
            l[house.xMin, house.yMin] = CellType.Empty;
            Assert.IsNotNull(LayoutValidator.Validate(l));
        }

        [Test]
        public void Validator_RejectsSealedHouse()
        {
            var l = BlockGenerator.Generate(12, TestSettings.Default());
            var house = l.Houses[0];
            l.Fill(house.FrontDoor, CellType.Wall); // the front door is a house's only way in
            Assert.IsNotNull(LayoutValidator.Validate(l), "a house with its front door walled up must be rejected");
        }

        [Test]
        public void Validator_RejectsOneWideGap()
        {
            var l = BlockGenerator.Generate(13, TestSettings.Default());
            // Narrow the sidewalk to one row along its whole length: every sidewalk cell loses its 2x2.
            for (int x = 0; x < l.Width; x++)
                for (int y = l.CarSpot.yMin + 1; y < l.CarSpot.yMax; y++)
                    if (l[x, y] == CellType.Sidewalk) l[x, y] = CellType.Barrier;
            Assert.IsNotNull(LayoutValidator.Validate(l));
        }
    }
}
