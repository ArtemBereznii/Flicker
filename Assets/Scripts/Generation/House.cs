using System.Collections.Generic;
using UnityEngine;

namespace Flicker.Generation
{
    // An enterable house: rooms, 2-wide door gaps, a front door and a fenced yard with a gate.
    public sealed class House
    {
        public Lot Lot;
        public Sides Front;
        public RectInt Footprint;
        public RectInt Yard;
        public List<RectInt> Rooms = new List<RectInt>();
        public List<RectInt> Doors = new List<RectInt>();
        public RectInt FrontDoor;
        public RectInt Gate;
    }
}
