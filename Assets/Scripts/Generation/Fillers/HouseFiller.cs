using System.Collections.Generic;
using UnityEngine;

namespace Flicker.Generation
{
    // An enterable house. Seen from its front side F (an open side of the lot):
    //   lot edge on F : fence with a 2-wide gate
    //   yard          : strip of yardDepth (fence row included)
    //   footprint     : the rest; outer walls, rooms split by RoomSplitter, 2-wide door gaps, front door facing the yard
    // Closed lot sides get fences; open non-front sides let the house wall face the alley directly.
    public sealed class HouseFiller : ILotFiller
    {
        public LotType Type => LotType.House;

        public bool CanFill(Lot lot, BlockGenSettings s)
        {
            foreach (var f in lot.Open.Each())
                if (Fits(lot, f, s.yardDepthRange.x, s)) return true;
            return false;
        }

        public void Fill(Lot lot, BlockLayout layout, Rng rng, BlockGenSettings s)
        {
            var fronts = new List<Sides>();
            foreach (var f in lot.Open.Each())
                if (Fits(lot, f, s.yardDepthRange.x, s)) fronts.Add(f);
            Sides front = rng.Pick(fronts);

            int maxYard = s.yardDepthRange.y - 1;
            while (maxYard > s.yardDepthRange.x && !Fits(lot, front, maxYard, s)) maxYard--;
            int yardDepth = rng.Range(s.yardDepthRange.x, maxYard + 1);

            var house = new House { Lot = lot, Front = front };
            FillerUtil.FenceClosedSides(lot, layout);

            RectInt inner = Inner(lot);
            house.Yard = RectSides.Edge(inner, front, yardDepth);
            house.Footprint = RectSides.Shrink(inner, front, yardDepth);

            // Yard, fenced on the street-facing edge and on any open side edge.
            layout.Fill(house.Yard, CellType.Yard);
            layout.Fill(RectSides.Edge(house.Yard, front, 1), CellType.Fence);
            foreach (var side in (RectSides.Perpendicular(front) & lot.Open).Each())
                layout.Fill(RectSides.Edge(house.Yard, side, 1), CellType.Fence);

            // House shell and rooms.
            layout.Fill(house.Footprint, CellType.Wall);
            var floor = new RectInt(house.Footprint.xMin + 1, house.Footprint.yMin + 1, house.Footprint.width - 2, house.Footprint.height - 2);
            layout.Fill(floor, CellType.HouseFloor);
            house.Rooms = RoomSplitter.Split(floor, layout, rng, s);
            OpenInteriorDoors(house, layout, rng, s);

            Vector2Int inward = RectSides.Inward(front);
            house.FrontDoor = OpenPair(layout, rng, RectSides.Edge(house.Footprint, front, 1),
                c => layout[c + inward] == CellType.HouseFloor && layout[c - inward] == CellType.Yard, CellType.HouseFloor);
            house.Gate = OpenPair(layout, rng, RectSides.Edge(house.Yard, front, 1),
                c => layout[c + inward] == CellType.Yard, CellType.Yard);

            layout.Houses.Add(house);
        }

        // The lot minus its fence ring on closed sides.
        private static RectInt Inner(Lot lot)
        {
            var r = lot.Rect;
            foreach (var side in (~lot.Open & (Sides.South | Sides.North | Sides.West | Sides.East)).Each())
                r = RectSides.Shrink(r, side, 1);
            return r;
        }

        private static bool Fits(Lot lot, Sides front, int yardDepth, BlockGenSettings s)
        {
            var footprint = RectSides.Shrink(Inner(lot), front, yardDepth);
            int min = s.roomMinInner + 2;
            return footprint.width >= min && footprint.height >= min;
        }

        // Connects every room through a random spanning tree of doors, plus some extra doors for loops.
        private static void OpenInteriorDoors(House house, BlockLayout layout, Rng rng, BlockGenSettings s)
        {
            var walls = new List<(int a, int b, RectInt span)>();
            for (int i = 0; i < house.Rooms.Count; i++)
                for (int j = i + 1; j < house.Rooms.Count; j++)
                    if (SharedWall(house.Rooms[i], house.Rooms[j], out var span))
                        walls.Add((i, j, span));

            var connected = new bool[house.Rooms.Count];
            var used = new bool[walls.Count];
            if (connected.Length > 0) connected[0] = true;
            while (true)
            {
                var frontier = new List<int>();
                for (int w = 0; w < walls.Count; w++)
                    if (connected[walls[w].a] != connected[walls[w].b]) frontier.Add(w);
                if (frontier.Count == 0) break;

                int pick = rng.Pick(frontier);
                used[pick] = true;
                connected[walls[pick].a] = connected[walls[pick].b] = true;
                house.Doors.Add(OpenPair(layout, rng, walls[pick].span, _ => true, CellType.HouseFloor));
            }

            for (int w = 0; w < walls.Count; w++)
                if (!used[w] && rng.Chance(s.extraDoorChance))
                    house.Doors.Add(OpenPair(layout, rng, walls[w].span, _ => true, CellType.HouseFloor));
        }

        // Rooms separated by exactly one wall line, overlapping by at least 2 cells along it.
        // span is the run of wall cells with floor on both sides.
        private static bool SharedWall(RectInt a, RectInt b, out RectInt span)
        {
            span = default;
            if (a.xMax + 1 == b.xMin || b.xMax + 1 == a.xMin)
            {
                int x = a.xMax + 1 == b.xMin ? a.xMax : b.xMax;
                int y0 = Mathf.Max(a.yMin, b.yMin), y1 = Mathf.Min(a.yMax, b.yMax);
                if (y1 - y0 < 2) return false;
                span = new RectInt(x, y0, 1, y1 - y0);
                return true;
            }
            if (a.yMax + 1 == b.yMin || b.yMax + 1 == a.yMin)
            {
                int y = a.yMax + 1 == b.yMin ? a.yMax : b.yMax;
                int x0 = Mathf.Max(a.xMin, b.xMin), x1 = Mathf.Min(a.xMax, b.xMax);
                if (x1 - x0 < 2) return false;
                span = new RectInt(x0, y, x1 - x0, 1);
                return true;
            }
            return false;
        }

        // Opens a random 2-cell gap along a 1-thick line, at a spot where both cells pass the check.
        private static RectInt OpenPair(BlockLayout layout, Rng rng, RectInt line, System.Func<Vector2Int, bool> ok, CellType open)
        {
            var cells = RectSides.LineCells(line);
            var starts = new List<int>();
            for (int i = 0; i + 1 < cells.Count; i++)
                if (ok(cells[i]) && ok(cells[i + 1])) starts.Add(i);
            if (starts.Count == 0) return default; // the validator rejects the block if this ever leaves a house shut

            int start = rng.Pick(starts);
            var a = cells[start];
            var b = cells[start + 1];
            layout[a] = open;
            layout[b] = open;
            return new RectInt(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Abs(a.x - b.x) + 1, Mathf.Abs(a.y - b.y) + 1);
        }
    }
}
