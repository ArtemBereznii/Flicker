using UnityEngine;

namespace Flicker.Generation.Tests
{
    public static class TestSettings
    {
        public static BlockGenSettings Default() => ScriptableObject.CreateInstance<BlockGenSettings>();

        // A layout with the street frame laid, ready for lot splitting.
        public static BlockLayout Framed(int seed, int width = 60, int height = 40, BlockGenSettings s = null)
        {
            var layout = new BlockLayout(width, height, seed, 0);
            BlockFrame.Lay(layout, new Rng(seed), s ?? Default());
            return layout;
        }
    }
}
