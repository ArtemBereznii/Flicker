using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Flicker.Generation.Tests
{
    public class CanisterPlacerTests
    {
        private const int ManySeeds = 500;
        private static readonly Dictionary<int, BlockLayout> Cache = new Dictionary<int, BlockLayout>();

        private static BlockLayout Gen(int seed)
        {
            if (!Cache.TryGetValue(seed, out var l)) Cache[seed] = l = BlockGenerator.Generate(seed, TestSettings.Default());
            return l;
        }

        [Test]
        public void ManySeeds_PlaceConfiguredCountOnFreeInnerCells()
        {
            var s = TestSettings.Default();
            for (int seed = 0; seed < ManySeeds; seed++)
            {
                var l = Gen(seed);
                Assert.AreEqual(s.canisterCount, l.Canisters.Count, $"seed {seed}");
                foreach (var c in l.Canisters)
                {
                    Assert.IsTrue(l.IsWalkable(c), $"seed {seed} {c} not walkable or under a prop");
                    Assert.AreNotEqual(CellType.Sidewalk, l[c], $"seed {seed} {c} on the sidewalk");
                    Assert.IsFalse(l.CarSpot.Contains(c), $"seed {seed} {c} on the car spot");
                }
                Assert.AreEqual(l.Canisters.Count, l.Canisters.Distinct().Count(), $"seed {seed} duplicate canister");
            }
        }

        [Test]
        public void ManySeeds_FarFromCar()
        {
            var s = TestSettings.Default();
            for (int seed = 0; seed < ManySeeds; seed++)
            {
                var l = Gen(seed);
                foreach (var c in l.Canisters)
                    Assert.GreaterOrEqual(l.DistanceFromCar[c.x, c.y], s.canisterMinCarDistance, $"seed {seed} {c}");
            }
        }

        [Test]
        public void ManySeeds_SpreadApartByWalkingDistance()
        {
            var s = TestSettings.Default();
            for (int seed = 0; seed < ManySeeds; seed++)
            {
                var l = Gen(seed);
                for (int i = 0; i < l.Canisters.Count; i++)
                {
                    var dist = LayoutChecks.Distances(l, l.Canisters[i]);
                    for (int j = i + 1; j < l.Canisters.Count; j++)
                    {
                        var o = l.Canisters[j];
                        Assert.GreaterOrEqual(dist[o.x, o.y], s.canisterMinSpacing, $"seed {seed} canisters {i},{j}");
                    }
                }
            }
        }

        [Test]
        public void SameSeed_SameCanisters()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var a = BlockGenerator.Generate(seed, TestSettings.Default());
                var b = BlockGenerator.Generate(seed, TestSettings.Default());
                CollectionAssert.AreEqual(a.Canisters, b.Canisters, $"seed {seed}");
                Assert.AreEqual(a.ExitCar, b.ExitCar, $"seed {seed}");
            }
        }

        [Test]
        public void ZeroCanisters_Allowed()
        {
            var s = TestSettings.Default();
            s.canisterCount = 0;
            var l = BlockGenerator.Generate(3, s);
            Assert.AreEqual(0, l.Canisters.Count);
        }

        [Test]
        public void ImpossibleSpacing_RejectsEveryAttempt()
        {
            var s = TestSettings.Default();
            s.canisterMinCarDistance = 10000;
            s.maxAttempts = 2;
            Assert.Throws<BlockGenerationException>(() => BlockGenerator.Generate(5, s));
        }
    }
}
