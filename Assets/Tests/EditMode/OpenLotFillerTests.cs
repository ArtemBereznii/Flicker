using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Flicker.Generation.Tests
{
    public class OpenLotFillerTests
    {
        private const int Seeds = 200;
        private static readonly RectInt LotRect = new RectInt(5, 5, 14, 12);
        private const Sides Open = Sides.South | Sides.East;

        private static ILotFiller[] Fillers => new ILotFiller[] { new ParkingFiller(), new ParkFiller() };

        private static (BlockLayout layout, Lot lot) Fill(ILotFiller filler, int seed, RectInt rect, Sides open)
        {
            var s = TestSettings.Default();
            var (layout, lot) = FillerHarness.Make(rect, open, seed);
            Assert.IsTrue(filler.CanFill(lot, s), $"{filler.Type} CanFill {rect}");
            filler.Fill(lot, layout, new Rng(seed), s);
            return (layout, lot);
        }

        [Test]
        public void OpenLot_PropsInsideLotOnWalkableGround()
        {
            foreach (var filler in Fillers)
                for (int seed = 0; seed < Seeds; seed++)
                {
                    var (l, lot) = Fill(filler, seed, LotRect, Open);
                    var ground = filler.Type == LotType.Parking ? CellType.Parking : CellType.Park;
                    var owner = new int[l.Width, l.Height];
                    foreach (var p in l.Props)
                        foreach (var c in p.Footprint.allPositionsWithin)
                        {
                            Assert.IsTrue(lot.Rect.Contains(c), $"{filler.Type} seed {seed} prop {p.Kind} outside lot");
                            Assert.AreEqual(ground, l[c], $"{filler.Type} seed {seed} prop {p.Kind} on {l[c]}");
                            Assert.AreEqual(0, owner[c.x, c.y]++, $"{filler.Type} seed {seed} props overlap at {c}");
                        }
                    FillerHarness.AssertNoEmpty(l, lot.Rect, $"{filler.Type} seed {seed}");
                }
        }

        [Test]
        public void OpenLot_StillConnectedAnd2Wide()
        {
            foreach (var filler in Fillers)
                for (int seed = 0; seed < Seeds; seed++)
                {
                    var (l, lot) = Fill(filler, seed, LotRect, Open);
                    Assert.IsTrue(LayoutChecks.RegionConnectedToEdge(l, lot.Rect, lot.Open), $"{filler.Type} seed {seed}");
                    Assert.IsTrue(LayoutChecks.AllCovered2x2(l, lot.Rect), $"{filler.Type} seed {seed}");
                }
        }

        [Test]
        public void OpenLot_ClosedSidesFenced()
        {
            foreach (var filler in Fillers)
            {
                var (l, _) = Fill(filler, 1, LotRect, Open);
                Assert.AreEqual(CellType.Fence, l[LotRect.xMin, LotRect.yMin + 5]);      // west
                Assert.AreEqual(CellType.Fence, l[LotRect.xMin + 5, LotRect.yMax - 1]);  // north
            }
        }

        [Test]
        public void Parking_HasCarsUsually()
        {
            int withCars = 0;
            for (int seed = 0; seed < Seeds; seed++)
                if (Fill(new ParkingFiller(), seed, LotRect, Open).layout.Props.Any(p => p.Kind == PropKind.Car)) withCars++;
            Assert.Greater(withCars, Seeds * 0.8f);
        }

        [Test]
        public void Parking_CarsUseCarFootprint()
        {
            var (l, _) = Fill(new ParkingFiller(), 3, LotRect, Open);
            foreach (var p in l.Props.Where(p => p.Kind == PropKind.Car))
            {
                var size = p.Footprint.size;
                Assert.IsTrue(size == new Vector2Int(2, 4) || size == new Vector2Int(4, 2), $"car {size}");
            }
        }

        [Test]
        public void Park_HasTreesUsually()
        {
            int withTrees = 0;
            for (int seed = 0; seed < Seeds; seed++)
                if (Fill(new ParkFiller(), seed, LotRect, Open).layout.Props.Any(p => p.Kind == PropKind.Tree)) withTrees++;
            Assert.Greater(withTrees, Seeds * 0.8f);
        }

        [Test]
        public void Park_HasBenchesSometimes()
        {
            int withBenches = 0;
            for (int seed = 0; seed < Seeds; seed++)
                if (Fill(new ParkFiller(), seed, LotRect, Open).layout.Props.Any(p => p.Kind == PropKind.Bench)) withBenches++;
            Assert.Greater(withBenches, Seeds * 0.5f);
        }

        [Test]
        public void OpenLots_RejectTooSmall()
        {
            var (_, lot) = FillerHarness.Make(new RectInt(5, 5, 7, 12), Open);
            Assert.IsFalse(new ParkingFiller().CanFill(lot, TestSettings.Default()));
            Assert.IsFalse(new ParkFiller().CanFill(lot, TestSettings.Default()));
        }
    }
}
