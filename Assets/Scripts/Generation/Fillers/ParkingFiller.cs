using System.Collections.Generic;
using UnityEngine;

namespace Flicker.Generation
{
    // Open asphalt with parked cars: a row backed onto each fenced side, plus a middle row when the lot
    // is deep enough. Cars block light and bullets, so a parking lot is open ground full of hard cover.
    public sealed class ParkingFiller : ILotFiller
    {
        private const int MiddleRowMinDepth = 12;

        public LotType Type => LotType.Parking;

        public bool CanFill(Lot lot, BlockGenSettings s) => Mathf.Min(lot.Rect.width, lot.Rect.height) >= s.parkingMinSide;

        public void Fill(Lot lot, BlockLayout layout, Rng rng, BlockGenSettings s)
        {
            layout.Fill(lot.Rect, CellType.Parking);
            FillerUtil.FenceClosedSides(lot, layout);

            foreach (var slot in Slots(lot))
                if (rng.Chance(s.carChance))
                    PropPlacer.TryPlace(layout, lot, new PropPlacement(PropKind.Car, slot), lot.Open);
        }

        private static IEnumerable<RectInt> Slots(Lot lot)
        {
            var r = lot.Rect;
            var closed = ~lot.Open & (Sides.South | Sides.North | Sides.West | Sides.East);

            // Rows nose-in against each fence, cars side by side (step 2, so gaps are 0 or a full 2-wide slot).
            foreach (var side in closed.Each())
            {
                bool horizontalRow = side == Sides.South || side == Sides.North;
                var row = RectSides.Edge(RectSides.Shrink(r, side, 1), side, 4);
                if (horizontalRow)
                    for (int x = row.xMin + 1; x + 2 <= row.xMax - 1; x += 2)
                        yield return new RectInt(x, row.yMin, 2, 4);
                else
                    for (int y = row.yMin + 1; y + 2 <= row.yMax - 1; y += 2)
                        yield return new RectInt(row.xMin, y, 4, 2);
            }

            // A middle row along the long axis, centered, when there is room for aisles on both sides.
            if (r.width >= r.height && r.height >= MiddleRowMinDepth)
            {
                int y = r.yMin + r.height / 2 - 2;
                for (int x = r.xMin + 3; x + 2 <= r.xMax - 3; x += 2)
                    yield return new RectInt(x, y, 2, 4);
            }
            else if (r.height > r.width && r.width >= MiddleRowMinDepth)
            {
                int x = r.xMin + r.width / 2 - 2;
                for (int y = r.yMin + 3; y + 2 <= r.yMax - 3; y += 2)
                    yield return new RectInt(x, y, 4, 2);
            }
        }
    }
}
