using System.Collections.Generic;
using UnityEngine;

namespace Flicker.Generation
{
    // Binary space partition of the block's inner area into lots. Some cuts become alleys.
    // 
    // Invariant: a rect's "open" sides border walkable sidewalk/alley along their whole length.
    // An alley is only carved when one of its ends touches an open side, so every alley joins the
    // network that started at the sidewalk. A cut is only made if both halves keep an open side,
    // so every lot ends up with at least one side it can be entered from.
    public static class LotSplitter
    {
        private enum Axis { X, Y } // X: vertical cut line (splits width). Y: horizontal cut line.

        public static List<Lot> Split(BlockLayout layout, Rng rng, BlockGenSettings s)
        {
            var lots = new List<Lot>();
            SplitRect(layout.InnerArea, Sides.South, 0, layout, rng, s, lots);
            return lots;
        }

        private static void SplitRect(RectInt r, Sides open, int depth, BlockLayout layout, Rng rng, BlockGenSettings s, List<Lot> lots)
        {
            if (Mathf.Max(r.width, r.height) <= s.lotMaxSize)
            {
                lots.Add(new Lot(r, open));
                return;
            }

            Axis first = Mathf.Abs(r.width - r.height) <= 2
                ? (rng.Chance(0.5f) ? Axis.X : Axis.Y)
                : (r.width > r.height ? Axis.X : Axis.Y);

            foreach (var axis in new[] { first, first == Axis.X ? Axis.Y : Axis.X })
            {
                if (TryCut(r, open, axis, depth, layout, rng, s, lots)) return;
            }

            if (TrySideAlley(r, open, depth, layout, rng, s, lots)) return;

            lots.Add(new Lot(r, open)); // no valid cut: keep it as one big lot
        }

        // Access fix for a long rect whose only entrances sit at one end (e.g. a strip along the perimeter
        // open only to the sidewalk): carve an alley along one long side, starting from an open end.
        // The rest of the rect gains that side as an entrance and can then be cut across.
        private static bool TrySideAlley(RectInt r, Sides open, int depth, BlockLayout layout, Rng rng, BlockGenSettings s, List<Lot> lots)
        {
            var candidates = new List<Sides>();
            foreach (var side in (Sides.West | Sides.East | Sides.South | Sides.North).Each())
            {
                if ((open & side) != 0) continue;
                bool vertical = side == Sides.West || side == Sides.East;
                Sides ends = vertical ? (Sides.South | Sides.North) : (Sides.West | Sides.East);
                int along = vertical ? r.height : r.width;
                int across = vertical ? r.width : r.height;
                if ((open & ends) != 0 && along > s.lotMaxSize && across - s.alleyWidthRange.x >= s.lotMinSize)
                    candidates.Add(side);
            }
            if (candidates.Count == 0) return false;

            var chosen = rng.Pick(candidates);
            bool isVertical = chosen == Sides.West || chosen == Sides.East;
            int maxWidth = (isVertical ? r.width : r.height) - s.lotMinSize;
            int width = Mathf.Min(rng.Range(s.alleyWidthRange.x, s.alleyWidthRange.y), maxWidth);

            RectInt alley, rest;
            switch (chosen)
            {
                case Sides.West:
                    alley = new RectInt(r.xMin, r.yMin, width, r.height);
                    rest = new RectInt(r.xMin + width, r.yMin, r.width - width, r.height);
                    break;
                case Sides.East:
                    alley = new RectInt(r.xMax - width, r.yMin, width, r.height);
                    rest = new RectInt(r.xMin, r.yMin, r.width - width, r.height);
                    break;
                case Sides.South:
                    alley = new RectInt(r.xMin, r.yMin, r.width, width);
                    rest = new RectInt(r.xMin, r.yMin + width, r.width, r.height - width);
                    break;
                default:
                    alley = new RectInt(r.xMin, r.yMax - width, r.width, width);
                    rest = new RectInt(r.xMin, r.yMin, r.width, r.height - width);
                    break;
            }

            layout.Fill(alley, CellType.Alley);
            SplitRect(rest, open | chosen, depth + 1, layout, rng, s, lots);
            return true;
        }

        private static bool TryCut(RectInt r, Sides open, Axis axis, int depth, BlockLayout layout, Rng rng, BlockGenSettings s, List<Lot> lots)
        {
            int length = axis == Axis.X ? r.width : r.height;
            Sides lowFacing = axis == Axis.X ? Sides.East : Sides.North;   // side of the low child facing the cut
            Sides highFacing = axis == Axis.X ? Sides.West : Sides.South;  // side of the high child facing the cut
            Sides alleyEnds = axis == Axis.X ? (Sides.South | Sides.North) : (Sides.West | Sides.East);

            Sides lowOpen = open & ~lowFacing;
            Sides highOpen = open & ~highFacing;
            bool alleyPossible = (open & alleyEnds) != 0;
            bool needAlley = lowOpen == Sides.None || highOpen == Sides.None;
            if (needAlley && !alleyPossible) return false;

            int alleyWidth = rng.Range(s.alleyWidthRange.x, s.alleyWidthRange.y);
            bool wantAlley = rng.Chance(s.alleyChanceTop * Mathf.Pow(s.alleyChanceFalloff, depth));
            bool alley = alleyPossible && (wantAlley || needAlley);

            if (alley && length - alleyWidth < 2 * s.lotMinSize)
            {
                if (needAlley) return false;
                alley = false; // too tight for an alley; a plain lot boundary still works
            }
            int gap = alley ? alleyWidth : 0;

            // Without an alley here, a half that is still longer than a lot along the cut line can only
            // be opened up later by a side alley, so it must stay wide enough to fit one.
            int crossLength = axis == Axis.X ? r.height : r.width;
            int minChild = !alley && crossLength > s.lotMaxSize ? s.lotMinSize + s.alleyWidthRange.x : s.lotMinSize;
            if (length - gap < 2 * minChild) return false; // the other axis or a side alley handles it

            int lowLength = rng.Range(minChild, length - gap - minChild + 1);

            RectInt low, high, alleyRect;
            if (axis == Axis.X)
            {
                low = new RectInt(r.xMin, r.yMin, lowLength, r.height);
                alleyRect = new RectInt(r.xMin + lowLength, r.yMin, gap, r.height);
                high = new RectInt(alleyRect.xMax, r.yMin, r.xMax - alleyRect.xMax, r.height);
            }
            else
            {
                low = new RectInt(r.xMin, r.yMin, r.width, lowLength);
                alleyRect = new RectInt(r.xMin, r.yMin + lowLength, r.width, gap);
                high = new RectInt(r.xMin, alleyRect.yMax, r.width, r.yMax - alleyRect.yMax);
            }

            if (alley)
            {
                layout.Fill(alleyRect, CellType.Alley);
                lowOpen |= lowFacing;
                highOpen |= highFacing;
            }

            SplitRect(low, lowOpen, depth + 1, layout, rng, s, lots);
            SplitRect(high, highOpen, depth + 1, layout, rng, s, lots);
            return true;
        }
    }
}
