using UnityEngine;

namespace Flicker.Generation
{
    public enum LotType { Unassigned, House, TallBuilding, Parking, Park }

    // One leaf of the block split. Open sides face the sidewalk or an alley.
    public sealed class Lot
    {
        public RectInt Rect;
        public Sides Open;
        public LotType Type;

        public Lot(RectInt rect, Sides open)
        {
            Rect = rect;
            Open = open;
            Type = LotType.Unassigned;
        }
    }
}
