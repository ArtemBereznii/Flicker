using UnityEngine;

namespace Flicker.Generation
{
    // A small park: grass with scattered trees (block light), benches and trash cans (only block movement).
    // The most open lot type: a breather, but you can be seen from all sides.
    public sealed class ParkFiller : ILotFiller
    {
        private const int TriesPerProp = 10;

        public LotType Type => LotType.Park;

        public bool CanFill(Lot lot, BlockGenSettings s) => Mathf.Min(lot.Rect.width, lot.Rect.height) >= s.parkMinSide;

        public void Fill(Lot lot, BlockLayout layout, Rng rng, BlockGenSettings s)
        {
            layout.Fill(lot.Rect, CellType.Park);
            FillerUtil.FenceClosedSides(lot, layout);

            int area = lot.Rect.width * lot.Rect.height;
            Scatter(PropKind.Tree, area * s.parkTreesPer100 / 100, lot, layout, rng);
            Scatter(PropKind.Bench, area * s.parkBenchesPer100 / 100, lot, layout, rng);
            Scatter(PropKind.TrashCan, area * s.parkTrashPer100 / 100, lot, layout, rng);
        }

        private static void Scatter(PropKind kind, int count, Lot lot, BlockLayout layout, Rng rng)
        {
            for (int i = 0; i < count; i++)
            {
                for (int attempt = 0; attempt < TriesPerProp; attempt++)
                {
                    var size = PropKinds.Size(kind);
                    if (rng.Chance(0.5f)) size = new Vector2Int(size.y, size.x);
                    int x = rng.Range(lot.Rect.xMin, lot.Rect.xMax - size.x + 1);
                    int y = rng.Range(lot.Rect.yMin, lot.Rect.yMax - size.y + 1);
                    if (PropPlacer.TryPlace(layout, lot, new PropPlacement(kind, new RectInt(x, y, size.x, size.y)), lot.Open))
                        break;
                }
            }
        }
    }
}
