using NUnit.Framework;
using UnityEngine;

namespace Flicker.Generation.Tests
{
    public class FrameTests
    {
        [Test]
        public void Frame_RowsInOrder()
        {
            var l = TestSettings.Framed(1);
            Assert.AreEqual(CellType.Road, l[30, 0]);
            Assert.AreEqual(CellType.Road, l[30, 2]);
            Assert.AreEqual(CellType.Barrier, l[30, 3]);
            Assert.AreEqual(CellType.Sidewalk, l[30, 4]);
            Assert.AreEqual(CellType.Sidewalk, l[30, 6]);
            Assert.AreEqual(CellType.Building, l[0, 10]);
            Assert.AreEqual(CellType.Building, l[59, 10]);
            Assert.AreEqual(CellType.Building, l[30, 39]);
            Assert.AreEqual(CellType.Building, l[1, 5]);
        }

        [Test]
        public void Frame_InnerAreaIsEmptyAndAboveSidewalk()
        {
            var l = TestSettings.Framed(1);
            Assert.AreEqual(7, l.InnerArea.yMin);
            Assert.AreEqual(2, l.InnerArea.xMin);
            Assert.AreEqual(58, l.InnerArea.xMax);
            Assert.AreEqual(38, l.InnerArea.yMax);
            for (int x = l.InnerArea.xMin; x < l.InnerArea.xMax; x++)
                for (int y = l.InnerArea.yMin; y < l.InnerArea.yMax; y++)
                    Assert.AreEqual(CellType.Empty, l[x, y]);
        }

        [Test]
        public void Frame_NoEmptyOutsideInnerArea()
        {
            var l = TestSettings.Framed(3);
            for (int x = 0; x < l.Width; x++)
                for (int y = 0; y < l.Height; y++)
                    if (!l.InnerArea.Contains(new Vector2Int(x, y)))
                        Assert.AreNotEqual(CellType.Empty, l[x, y], $"{x},{y}");
        }

        [Test]
        public void Frame_CarSpotOnSidewalk_ForManySeeds()
        {
            var xs = new System.Collections.Generic.HashSet<int>();
            for (int seed = 0; seed < 200; seed++)
            {
                var l = TestSettings.Framed(seed);
                Assert.AreEqual(new Vector2Int(4, 3), l.CarSpot.size);
                xs.Add(l.CarSpot.x);
                for (int x = l.CarSpot.xMin; x < l.CarSpot.xMax; x++)
                    for (int y = l.CarSpot.yMin; y < l.CarSpot.yMax; y++)
                        Assert.AreEqual(CellType.Sidewalk, l[x, y], $"seed {seed}");
            }
            Assert.Greater(xs.Count, 10, "car spot position should vary between seeds");
        }
    }
}
