using System.Collections.Generic;
using UnityEngine;

namespace Flicker.Generation
{
    // BSP of a house's floor into rooms separated by 1-thick walls (the same idea as the lot split, one level down).
    public static class RoomSplitter
    {
        private const float OptionalSplitChance = 0.3f;

        public static List<RectInt> Split(RectInt inner, BlockLayout layout, Rng rng, BlockGenSettings s)
        {
            var rooms = new List<RectInt>();
            SplitRoom(inner, layout, rng, s, rooms);
            return rooms;
        }

        private static void SplitRoom(RectInt r, BlockLayout layout, Rng rng, BlockGenSettings s, List<RectInt> rooms)
        {
            int minSplit = 2 * s.roomMinInner + 1;
            bool canX = r.width >= minSplit, canY = r.height >= minSplit;
            bool mustSplit = r.width > s.roomMaxInner || r.height > s.roomMaxInner;

            if ((!canX && !canY) || (!mustSplit && !rng.Chance(OptionalSplitChance)))
            {
                rooms.Add(r);
                return;
            }

            bool splitX = canX && canY
                ? (r.width == r.height ? rng.Chance(0.5f) : r.width > r.height)
                : canX;
            int length = splitX ? r.width : r.height;
            int low = rng.Range(s.roomMinInner, length - s.roomMinInner); // wall sits right after the low room

            if (splitX)
            {
                layout.Fill(new RectInt(r.xMin + low, r.yMin, 1, r.height), CellType.Wall);
                SplitRoom(new RectInt(r.xMin, r.yMin, low, r.height), layout, rng, s, rooms);
                SplitRoom(new RectInt(r.xMin + low + 1, r.yMin, r.width - low - 1, r.height), layout, rng, s, rooms);
            }
            else
            {
                layout.Fill(new RectInt(r.xMin, r.yMin + low, r.width, 1), CellType.Wall);
                SplitRoom(new RectInt(r.xMin, r.yMin, r.width, low), layout, rng, s, rooms);
                SplitRoom(new RectInt(r.xMin, r.yMin + low + 1, r.width, r.height - low - 1), layout, rng, s, rooms);
            }
        }
    }
}
