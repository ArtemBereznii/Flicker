using UnityEngine;

namespace Flicker.Generation
{
    // Lays the fixed frame of a block, from the bottom up: road, barrier row, sidewalk (with the car spot),
    // then the inner area (left Empty for lots). Tall-building perimeter seals the other three sides.
    public static class BlockFrame
    {
        public static void Lay(BlockLayout layout, Rng rng, BlockGenSettings s)
        {
            int w = layout.Width, h = layout.Height, p = s.perimeterThickness;
            int barrierRow = s.roadRows;
            int sidewalkMin = barrierRow + 1;
            int innerMin = sidewalkMin + s.sidewalkDepth;

            layout.Fill(new RectInt(0, 0, w, s.roadRows), CellType.Road);
            layout.Fill(new RectInt(0, barrierRow, w, h - barrierRow), CellType.Building);
            layout.Fill(new RectInt(p, barrierRow, w - 2 * p, 1), CellType.Barrier);
            layout.Fill(new RectInt(p, sidewalkMin, w - 2 * p, s.sidewalkDepth), CellType.Sidewalk);

            layout.InnerArea = new RectInt(p, innerMin, w - 2 * p, h - p - innerMin);
            layout.Fill(layout.InnerArea, CellType.Empty);

            int carX = rng.Range(p, w - p - s.carSpotWidth + 1);
            layout.CarSpot = new RectInt(carX, sidewalkMin, s.carSpotWidth, s.sidewalkDepth);
        }
    }
}
