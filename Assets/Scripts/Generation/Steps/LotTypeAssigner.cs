using System.Collections.Generic;
using UnityEngine;

namespace Flicker.Generation
{
    // Picks each lot's type by weighted chance among the types whose filler fits it.
    // Rules: parks never touch other parks; every block gets at least minHouses houses.
    public static class LotTypeAssigner
    {
        private static readonly LotType[] Pickable = { LotType.House, LotType.TallBuilding, LotType.Parking, LotType.Park };
        private static readonly LotType[] Fallback = { LotType.TallBuilding, LotType.Parking, LotType.Park };

        // Returns: False when the house minimum can't be met (the generator then retries).
        public static bool Assign(IReadOnlyList<Lot> lots, Rng rng, BlockGenSettings s)
        {
            var weights = new float[Pickable.Length];
            foreach (var lot in lots)
            {
                for (int i = 0; i < Pickable.Length; i++)
                    weights[i] = Allowed(Pickable[i], lot, lots, s) ? s.WeightOf(Pickable[i]) : 0f;

                int pick = rng.PickWeighted(weights);
                lot.Type = pick >= 0 ? Pickable[pick] : FallbackType(lot, lots, s);
            }

            int houses = 0;
            foreach (var lot in lots)
                if (lot.Type == LotType.House) houses++;

            var house = LotFillers.Get(LotType.House);
            while (houses < s.minHouses)
            {
                Lot largest = null;
                foreach (var lot in lots)
                    if (lot.Type != LotType.House && house.CanFill(lot, s) && (largest == null || Area(lot) > Area(largest)))
                        largest = lot;
                if (largest == null) return false;
                largest.Type = LotType.House;
                houses++;
            }
            return true;
        }

        private static bool Allowed(LotType type, Lot lot, IReadOnlyList<Lot> lots, BlockGenSettings s) =>
            LotFillers.Get(type).CanFill(lot, s) && (type != LotType.Park || !TouchesPark(lot, lots));

        private static LotType FallbackType(Lot lot, IReadOnlyList<Lot> lots, BlockGenSettings s)
        {
            foreach (var type in Fallback)
                if (Allowed(type, lot, lots, s)) return type;
            return LotType.Parking; // every lot is at least lotMinSize, which parking accepts by default
        }

        private static bool TouchesPark(Lot lot, IReadOnlyList<Lot> lots)
        {
            var grown = new RectInt(lot.Rect.xMin - 1, lot.Rect.yMin - 1, lot.Rect.width + 2, lot.Rect.height + 2);
            foreach (var other in lots)
                if (other != lot && other.Type == LotType.Park && grown.Overlaps(other.Rect)) return true;
            return false;
        }

        private static int Area(Lot lot) => lot.Rect.width * lot.Rect.height;
    }
}
