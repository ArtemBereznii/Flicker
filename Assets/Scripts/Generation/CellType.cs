namespace Flicker.Generation
{
    // What one 1x1 tile of the block is.
    public enum CellType
    {
        Empty,      // not generated yet; never present in a finished block
        Road,       // visual only, sealed off by the Barrier row
        Barrier,    // curb barricade between sidewalk and road
        Sidewalk,
        Alley,
        Yard,
        HouseFloor,
        Parking,
        Park,
        Wall,       // house walls
        Fence,
        Building,   // tall buildings and the block perimeter
    }

    public static class CellTypes
    {
        public static bool IsWalkable(this CellType t) =>
            t == CellType.Sidewalk || t == CellType.Alley || t == CellType.Yard ||
            t == CellType.HouseFloor || t == CellType.Parking || t == CellType.Park;

        // Blocking cells become Wall-layer blocks: they stop movement, light and bullets.
        public static bool IsBlocking(this CellType t) =>
            t == CellType.Barrier || t == CellType.Wall || t == CellType.Fence || t == CellType.Building;

        public static Zone? ZoneOf(this CellType t)
        {
            switch (t)
            {
                case CellType.Sidewalk: return Zone.Sidewalk;
                case CellType.Alley: return Zone.Alley;
                case CellType.Yard: return Zone.Yard;
                case CellType.HouseFloor: return Zone.House;
                case CellType.Parking: return Zone.Parking;
                case CellType.Park: return Zone.Park;
                default: return null;
            }
        }
    }
}
