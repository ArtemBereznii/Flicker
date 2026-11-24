using System;
using System.Collections.Generic;

namespace Flicker.Generation
{
    [Flags]
    public enum Sides { None = 0, South = 1, North = 2, West = 4, East = 8 }

    public static class SidesExt
    {
        private static readonly Sides[] Order = { Sides.South, Sides.North, Sides.West, Sides.East };

        // Each set side, always in the order South, North, West, East.
        public static IEnumerable<Sides> Each(this Sides s)
        {
            foreach (var side in Order)
                if ((s & side) != 0) yield return side;
        }
    }
}
