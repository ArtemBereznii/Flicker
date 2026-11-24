using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flicker.Generation
{
    // Merges grid cells into few non-overlapping rectangles (so walls spawn as a handful of blocks).
    public static class GreedyRects
    {
        public static List<RectInt> Merge(int width, int height, Func<int, int, bool> include)
        {
            var result = new List<RectInt>();
            var covered = new bool[width, height];
            bool Free(int x, int y) => !covered[x, y] && include(x, y);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!Free(x, y)) continue;

                    int w = 1;
                    while (x + w < width && Free(x + w, y)) w++;

                    int h = 1;
                    while (y + h < height && RowFree(x, y + h, w)) h++;

                    for (int dx = 0; dx < w; dx++)
                        for (int dy = 0; dy < h; dy++)
                            covered[x + dx, y + dy] = true;
                    result.Add(new RectInt(x, y, w, h));
                }
            }
            return result;

            bool RowFree(int x0, int row, int w)
            {
                for (int dx = 0; dx < w; dx++)
                    if (!Free(x0 + dx, row)) return false;
                return true;
            }
        }
    }
}
