using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Flicker.Generation.Tests
{
    public class HouseFillerTests
    {
        private static readonly RectInt LotRect = new RectInt(5, 5, 14, 16);

        private static (BlockLayout layout, Lot lot, House house) FillHouse(int seed, RectInt rect, Sides open)
        {
            var s = TestSettings.Default();
            var (layout, lot) = FillerHarness.Make(rect, open, seed);
            var filler = new HouseFiller();
            Assert.IsTrue(filler.CanFill(lot, s), $"CanFill {rect} {open}");
            filler.Fill(lot, layout, new Rng(seed), s);
            Assert.AreEqual(1, layout.Houses.Count);
            return (layout, lot, layout.Houses[0]);
        }

        [Test]
        public void House_CanFill_RejectsTinyLot()
        {
            var (_, lot) = FillerHarness.Make(new RectInt(5, 5, 7, 7), Sides.South);
            Assert.IsFalse(new HouseFiller().CanFill(lot, TestSettings.Default()));
        }

        [Test]
        public void House_CanFill_RejectsLotWithNoOpenSide()
        {
            var (_, lot) = FillerHarness.Make(LotRect, Sides.None);
            Assert.IsFalse(new HouseFiller().CanFill(lot, TestSettings.Default()));
        }

        [Test]
        public void House_AllWalkableInLotReachableFromGate()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var (l, lot, _) = FillHouse(seed, LotRect, Sides.South);
                FillerHarness.AssertNoEmpty(l, lot.Rect, $"seed {seed}");
                Assert.IsTrue(LayoutChecks.RegionConnectedToEdge(l, lot.Rect, Sides.South), $"seed {seed}");
            }
        }

        [Test]
        public void House_OnlyHouseCellTypesInLot()
        {
            var allowed = new[] { CellType.Fence, CellType.Wall, CellType.HouseFloor, CellType.Yard };
            for (int seed = 0; seed < 50; seed++)
            {
                var (l, lot, _) = FillHouse(seed, LotRect, Sides.South);
                for (int x = lot.Rect.xMin; x < lot.Rect.xMax; x++)
                    for (int y = lot.Rect.yMin; y < lot.Rect.yMax; y++)
                        CollectionAssert.Contains(allowed, l[x, y], $"seed {seed} cell {x},{y}");
            }
        }

        [Test]
        public void House_DoorsAndGateTwoWide()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var (l, lot, house) = FillHouse(seed, LotRect, Sides.South);
                Assert.IsTrue(LayoutChecks.AllCovered2x2(l, lot.Rect), $"seed {seed}");
                foreach (var d in house.Doors.Append(house.FrontDoor))
                {
                    Assert.AreEqual(2, d.width * d.height, $"seed {seed} door {d}");
                    foreach (var c in d.allPositionsWithin) Assert.AreEqual(CellType.HouseFloor, l[c], $"seed {seed} door {d}");
                }
                Assert.AreEqual(2, house.Gate.width * house.Gate.height, $"seed {seed} gate");
                foreach (var c in house.Gate.allPositionsWithin) Assert.AreEqual(CellType.Yard, l[c], $"seed {seed} gate");
            }
        }

        [Test]
        public void House_RoomsMeetMinimum()
        {
            var s = TestSettings.Default();
            for (int seed = 0; seed < 200; seed++)
            {
                var (_, _, house) = FillHouse(seed, LotRect, Sides.South);
                Assert.Greater(house.Rooms.Count, 0);
                for (int i = 0; i < house.Rooms.Count; i++)
                {
                    var r = house.Rooms[i];
                    Assert.GreaterOrEqual(Mathf.Min(r.width, r.height), s.roomMinInner, $"seed {seed} room {r}");
                    for (int j = i + 1; j < house.Rooms.Count; j++)
                        Assert.IsFalse(r.Overlaps(house.Rooms[j]), $"seed {seed} rooms overlap");
                }
            }
        }

        [Test]
        public void House_BigLotsGetSeveralRooms()
        {
            int multi = 0;
            for (int seed = 0; seed < 100; seed++)
                if (FillHouse(seed, new RectInt(3, 3, 16, 16), Sides.South).house.Rooms.Count > 1) multi++;
            Assert.Greater(multi, 90);
        }

        [Test]
        public void House_FrontFacesOpenSide()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var (_, lot, house) = FillHouse(seed, LotRect, Sides.South);
                Assert.AreEqual(Sides.South, house.Front);
                Assert.AreEqual(lot.Rect.yMin, house.Gate.yMin, $"seed {seed} gate not on the lot's bottom row");
            }
        }

        [TestCase(Sides.South)] [TestCase(Sides.North)] [TestCase(Sides.West)] [TestCase(Sides.East)]
        [TestCase(Sides.South | Sides.East)] [TestCase(Sides.North | Sides.West)]
        public void House_WorksForEveryFrontSide(Sides open)
        {
            for (int seed = 0; seed < 60; seed++)
            {
                var (l, lot, house) = FillHouse(seed, new RectInt(6, 6, 13, 13), open);
                Assert.AreNotEqual(Sides.None, house.Front & open, $"seed {seed}");
                FillerHarness.AssertNoEmpty(l, lot.Rect, $"seed {seed}");
                Assert.IsTrue(LayoutChecks.RegionConnectedToEdge(l, lot.Rect, open), $"seed {seed}");
                Assert.IsTrue(LayoutChecks.AllCovered2x2(l, lot.Rect), $"seed {seed}");
            }
        }

        [Test]
        public void Tall_OnlyBuildingsAndAlleys()
        {
            foreach (var (l, lot) in TallLots())
            {
                int buildings = 0, alleys = 0;
                foreach (var c in lot.Rect.allPositionsWithin)
                {
                    if (l[c] == CellType.Building) buildings++;
                    else if (l[c] == CellType.Alley) alleys++;
                    else Assert.Fail($"seed {l.Seed} lot {lot.Rect}: {l[c]} at {c}");
                }
                Assert.Greater(buildings, 0, $"seed {l.Seed} no building");
                Assert.Greater(alleys, 0, $"seed {l.Seed} no alley");
            }
        }

        [Test]
        public void Tall_BuildingsHaveAlleysAroundThem()
        {
            // Every side of every building faces walkable ground (an alley inside the lot or the one outside it).
            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var (l, lot) in TallLots())
                foreach (var c in lot.Rect.allPositionsWithin)
                {
                    if (l[c] != CellType.Building) continue;
                    foreach (var d in dirs)
                    {
                        var n = c + d;
                        Assert.IsTrue(l[n] == CellType.Building || l.IsWalkable(n), $"seed {l.Seed} lot {lot.Rect}: building at {c} touches {l[n]}");
                    }
                }
        }

        [Test]
        public void Tall_AlleysConnectedAndTwoWide()
        {
            foreach (var (l, lot) in TallLots())
            {
                Assert.IsTrue(LayoutChecks.RegionConnectedToEdge(l, lot.Rect, lot.Open), $"seed {l.Seed} lot {lot.Rect} {lot.Open}");
                Assert.IsTrue(LayoutChecks.AllCovered2x2(l, lot.Rect), $"seed {l.Seed} lot {lot.Rect}");
            }
        }

        [Test]
        public void Tall_BuildingsWithinSizeLimits()
        {
            var s = TestSettings.Default();
            foreach (var (l, lot) in TallLots())
                foreach (var b in GreedyRects.Merge(l.Width, l.Height, (x, y) => l[x, y] == CellType.Building))
                    Assert.GreaterOrEqual(Mathf.Min(b.width, b.height), s.tallBuildingMinSide, $"seed {l.Seed} building {b}");
        }

        [Test]
        public void Tall_BigLotsUsuallyGetSeveralBuildings()
        {
            int several = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                var (l, lot) = FillerHarness.Make(new RectInt(5, 5, 16, 16), Sides.South, seed);
                new TallBuildingFiller().Fill(lot, l, new Rng(seed), TestSettings.Default());
                if (CountComponents(l, lot.Rect, CellType.Building) >= 2) several++;
            }
            Assert.Greater(several, 50);
        }

        private static System.Collections.Generic.IEnumerable<(BlockLayout, Lot)> TallLots()
        {
            var s = TestSettings.Default();
            var rects = new[] { new RectInt(5, 5, 10, 10), new RectInt(4, 4, 16, 12), new RectInt(3, 3, 12, 22) };
            var opens = new[] { Sides.South, Sides.North | Sides.East, Sides.West, Sides.South | Sides.North | Sides.West | Sides.East };
            for (int seed = 0; seed < 60; seed++)
            {
                var (l, lot) = FillerHarness.Make(rects[seed % rects.Length], opens[seed % opens.Length], seed);
                var filler = new TallBuildingFiller();
                Assert.IsTrue(filler.CanFill(lot, s));
                filler.Fill(lot, l, new Rng(seed), s);
                yield return (l, lot);
            }
        }

        private static int CountComponents(BlockLayout l, RectInt region, CellType type)
        {
            var seen = new System.Collections.Generic.HashSet<Vector2Int>();
            int count = 0;
            foreach (var start in region.allPositionsWithin)
            {
                if (l[start] != type || !seen.Add(start)) continue;
                count++;
                var stack = new System.Collections.Generic.Stack<Vector2Int>();
                stack.Push(start);
                while (stack.Count > 0)
                {
                    var c = stack.Pop();
                    foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    {
                        var n = c + d;
                        if (region.Contains(n) && l[n] == type && seen.Add(n)) stack.Push(n);
                    }
                }
            }
            return count;
        }

        [Test]
        public void Tall_RejectsSmallLot()
        {
            var (_, lot) = FillerHarness.Make(new RectInt(5, 5, 9, 14), Sides.South);
            Assert.IsFalse(new TallBuildingFiller().CanFill(lot, TestSettings.Default()));
        }

        [Test]
        public void FenceClosedSides_FencesOnlyClosedEdges()
        {
            var (l, lot) = FillerHarness.Make(new RectInt(5, 5, 8, 8), Sides.South | Sides.East);
            FillerUtil.FenceClosedSides(lot, l);
            Assert.AreEqual(CellType.Fence, l[5, 8]);   // west edge
            Assert.AreEqual(CellType.Fence, l[8, 12]);  // north edge
            Assert.AreEqual(CellType.Empty, l[8, 5]);   // south edge stays open
            Assert.AreEqual(CellType.Empty, l[12, 8]);  // east edge stays open
        }

        [Test]
        public void PropPlacer_RejectsPropThatCutsTheLot_AndRollsBack()
        {
            // An 8-wide, 3-tall park strip open on the west: a car standing across it would cut it in two.
            var (l, lot) = FillerHarness.Make(new RectInt(5, 5, 8, 3), Sides.West);
            l.Fill(lot.Rect, CellType.Park);
            Assert.IsFalse(PropPlacer.TryPlace(l, lot, new PropPlacement(PropKind.Car, new RectInt(8, 5, 2, 3)), Sides.West));
            Assert.AreEqual(0, l.Props.Count);
            Assert.IsTrue(PropPlacer.TryPlace(l, lot, new PropPlacement(PropKind.TrashCan, new RectInt(12, 7, 1, 1)), Sides.West));
            Assert.AreEqual(1, l.Props.Count);
        }

        [Test]
        public void PropPlacer_RejectsPropOutsideLotOrOnBlocking()
        {
            var (l, lot) = FillerHarness.Make(new RectInt(5, 5, 8, 8), Sides.South);
            l.Fill(lot.Rect, CellType.Park);
            l[8, 8] = CellType.Fence;
            Assert.IsFalse(PropPlacer.TryPlace(l, lot, new PropPlacement(PropKind.TrashCan, new RectInt(4, 6, 1, 1)), Sides.South));
            Assert.IsFalse(PropPlacer.TryPlace(l, lot, new PropPlacement(PropKind.TrashCan, new RectInt(8, 8, 1, 1)), Sides.South));
            Assert.AreEqual(0, l.Props.Count);
        }

        [Test]
        public void Fillers_RegistryOrdered()
        {
            var types = LotFillers.All.Select(f => f.Type).ToList();
            for (int i = 1; i < types.Count; i++) Assert.Less(types[i - 1], types[i]);
            Assert.IsInstanceOf<HouseFiller>(LotFillers.Get(LotType.House));
            Assert.IsInstanceOf<TallBuildingFiller>(LotFillers.Get(LotType.TallBuilding));
        }
    }
}
