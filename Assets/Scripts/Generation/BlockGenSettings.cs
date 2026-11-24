using UnityEngine;

namespace Flicker.Generation
{
    // Every tunable of block generation. Ranges are Vector2Int (x = min inclusive, y = max exclusive).
    // Defaults are the starting point from the design; tune them in play.
    [CreateAssetMenu(menuName = "Flicker/Block Gen Settings")]
    public class BlockGenSettings : ScriptableObject
    {
        [Header("Block")]
        public Vector2Int widthRange = new Vector2Int(52, 69);
        public Vector2Int heightRange = new Vector2Int(38, 47);
        [Min(0)] public int roadRows = 3;
        [Min(1)] public int sidewalkDepth = 3;
        [Min(1)] public int perimeterThickness = 2;
        [Min(0)] public int carSpotWidth = 4;

        [Header("Lots")]
        [Min(1)] public int lotMinSize = 8;
        [Min(0)] public int lotMaxSize = 16;
        public Vector2Int alleyWidthRange = new Vector2Int(2, 4);
        [Range(0f, 1f)] public float alleyChanceTop = 0.9f;
        [Range(0f, 1f)] public float alleyChanceFalloff = 0.6f;

        [Header("Lot types")]
        public float houseWeight = 0.55f;
        public float tallWeight = 0.15f;
        public float parkingWeight = 0.15f;
        public float parkWeight = 0.15f;
        [Min(0)] public int minHouses = 3;
        [Min(0)] public int tallMinSide = 10;
        [Tooltip("Tall-building lots are split into buildings this size, with alleys between and around them.")]
        [Min(1)] public int tallBuildingMinSide = 4;
        [Min(1)] public int tallBuildingMaxSide = 8;
        [Min(0)] public int parkingMinSide = 8;
        [Min(0)] public int parkMinSide = 8;

        [Header("Houses")]
        [Tooltip("Yard depth including the fence row.")]
        public Vector2Int yardDepthRange = new Vector2Int(3, 6);
        [Min(1)] public int roomMinInner = 4;
        [Min(0)] public int roomMaxInner = 9;
        [Range(0f, 1f)] public float extraDoorChance = 0.25f;

        [Header("Props")]
        [Range(0f, 1f)] public float carChance = 0.6f;
        [Min(0)] public int parkTreesPer100 = 6;
        [Min(0)] public int parkBenchesPer100 = 3;
        [Min(0)] public int parkTrashPer100 = 2;

        [Header("Retries")]
        [Min(1)] public int maxAttempts = 20;

        // Null when the settings can produce a block, otherwise what is wrong.
        public string Validate()
        {
            int frameRows = roadRows + 1 + sidewalkDepth + perimeterThickness;
            int minWidth = 2 * perimeterThickness + Mathf.Max(lotMinSize, carSpotWidth);

            if (!Ordered(widthRange)) return $"widthRange {widthRange} must have x < y (y is exclusive)";
            if (!Ordered(heightRange)) return $"heightRange {heightRange} must have x < y (y is exclusive)";
            if (roadRows < 0 || sidewalkDepth < 2 || perimeterThickness < 1 || carSpotWidth < 2)
                return "roadRows ≥ 0, sidewalkDepth ≥ 2, perimeterThickness ≥ 1 and carSpotWidth ≥ 2 are required";
            if (lotMinSize < 1 || lotMaxSize < lotMinSize) return $"need 1 ≤ lotMinSize ≤ lotMaxSize (got {lotMinSize}, {lotMaxSize})";
            if (widthRange.x < minWidth) return $"blocks must be at least {minWidth} wide for the perimeter and one lot";
            if (heightRange.x < frameRows + lotMinSize) return $"blocks must be at least {frameRows + lotMinSize} tall for the street frame and one lot";
            if (!Ordered(alleyWidthRange) || alleyWidthRange.x < 2) return "alleys must be at least 2 wide (alleyWidthRange.x ≥ 2, x < y)";
            if (!Ordered(yardDepthRange) || yardDepthRange.x < 3) return "yards need the fence row plus 2 walkable rows (yardDepthRange.x ≥ 3, x < y)";
            if (roomMinInner < 2 || roomMaxInner < roomMinInner) return "need 2 ≤ roomMinInner ≤ roomMaxInner";
            if (tallBuildingMinSide < 1 || tallBuildingMaxSide < tallBuildingMinSide) return "need 1 ≤ tallBuildingMinSide ≤ tallBuildingMaxSide";
            if (tallMinSide < tallBuildingMinSide + 2 * alleyWidthRange.x)
                return $"tallMinSide must be at least {tallBuildingMinSide + 2 * alleyWidthRange.x}: room for a building plus an alley on both sides";
            if (parkingMinSide > lotMinSize) return "parkingMinSide must not exceed lotMinSize: parking is the fallback for lots nothing else fits";
            if (houseWeight < 0f || tallWeight < 0f || parkingWeight < 0f || parkWeight < 0f) return "lot type weights must not be negative";
            if (minHouses < 0 || maxAttempts < 1) return "need minHouses ≥ 0 and maxAttempts ≥ 1";
            return null;

            static bool Ordered(Vector2Int range) => range.x < range.y;
        }

        public float WeightOf(LotType t)
        {
            switch (t)
            {
                case LotType.House: return houseWeight;
                case LotType.TallBuilding: return tallWeight;
                case LotType.Parking: return parkingWeight;
                case LotType.Park: return parkWeight;
                default: return 0f;
            }
        }
    }
}
