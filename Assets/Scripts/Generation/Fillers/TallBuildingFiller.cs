using UnityEngine;

namespace Flicker.Generation
{
    // A cluster of tall, unenterable buildings with alleys between and around them: blind corners and
    // narrow passages, the flashlight's worst case.
    //   - A 2-wide alley runs along every closed side, so buildings never back onto a neighbour's fence.
    //   - The rest is BSP-split into buildings of tallBuildingMinSide..MaxSide, separated by alleys that
    //     run edge to edge, so every passage connects to the ring or to the street-side alley.
    public sealed class TallBuildingFiller : ILotFiller
    {
        private const float OptionalSplitChance = 0.5f;

        public LotType Type => LotType.TallBuilding;

        public bool CanFill(Lot lot, BlockGenSettings s) => Mathf.Min(lot.Rect.width, lot.Rect.height) >= s.tallMinSide;

        public void Fill(Lot lot, BlockLayout layout, Rng rng, BlockGenSettings s)
        {
            layout.Fill(lot.Rect, CellType.Alley);

            var core = lot.Rect;
            int ring = s.alleyWidthRange.x;
            foreach (var side in (~lot.Open & (Sides.South | Sides.North | Sides.West | Sides.East)).Each())
                core = RectSides.Shrink(core, side, ring);

            SplitBuildings(core, layout, rng, s);
        }

        private static void SplitBuildings(RectInt r, BlockLayout layout, Rng rng, BlockGenSettings s)
        {
            int gap = rng.Range(s.alleyWidthRange.x, s.alleyWidthRange.y);
            bool mustSplit = r.width > s.tallBuildingMaxSide || r.height > s.tallBuildingMaxSide;
            if (mustSplit && Mathf.Max(r.width, r.height) < 2 * s.tallBuildingMinSide + gap)
                gap = s.alleyWidthRange.x; // an oversized building is worse than a narrower alley
            int minSplit = 2 * s.tallBuildingMinSide + gap;
            bool canX = r.width >= minSplit, canY = r.height >= minSplit;

            if ((!canX && !canY) || (!mustSplit && !rng.Chance(OptionalSplitChance)))
            {
                layout.Fill(r, CellType.Building);
                return;
            }

            bool splitX = canX && canY ? (r.width == r.height ? rng.Chance(0.5f) : r.width > r.height) : canX;
            int length = splitX ? r.width : r.height;
            int low = rng.Range(s.tallBuildingMinSide, length - gap - s.tallBuildingMinSide + 1);

            if (splitX)
            {
                SplitBuildings(new RectInt(r.xMin, r.yMin, low, r.height), layout, rng, s);
                SplitBuildings(new RectInt(r.xMin + low + gap, r.yMin, r.width - low - gap, r.height), layout, rng, s);
            }
            else
            {
                SplitBuildings(new RectInt(r.xMin, r.yMin, r.width, low), layout, rng, s);
                SplitBuildings(new RectInt(r.xMin, r.yMin + low + gap, r.width, r.height - low - gap), layout, rng, s);
            }
        }
    }
}
