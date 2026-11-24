using UnityEngine;

namespace Flicker.Generation
{
    public enum Zone { Sidewalk, Alley, Yard, House, Parking, Park }

    // A walkable cell later systems (canisters, zombies, POIs) can spawn on.
    public readonly struct SpawnHint
    {
        public readonly Vector2Int Cell;
        public readonly Zone Zone;
        // Walking distance in cells from the car spot center.
        public readonly int Distance;

        public SpawnHint(Vector2Int cell, Zone zone, int distance)
        {
            Cell = cell;
            Zone = zone;
            Distance = distance;
        }
    }
}
