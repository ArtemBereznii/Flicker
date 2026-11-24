using UnityEngine;

namespace Flicker.Generation
{
    public enum PropKind { Car, Tree, Bench, TrashCan }

    public readonly struct PropPlacement
    {
        public readonly PropKind Kind;
        public readonly RectInt Footprint;

        public PropPlacement(PropKind kind, RectInt footprint)
        {
            Kind = kind;
            Footprint = footprint;
        }
    }

    public static class PropKinds
    {
        // Footprint in the prop's authored orientation. A transposed footprint means the prop is rotated 90°.
        public static Vector2Int Size(PropKind k)
        {
            switch (k)
            {
                case PropKind.Car: return new Vector2Int(2, 4);
                case PropKind.Bench: return new Vector2Int(2, 1);
                default: return new Vector2Int(1, 1);
            }
        }

        // Cars and trees block light and bullets (Wall layer); benches and trash cans only block movement.
        public static bool BlocksLight(PropKind k) => k == PropKind.Car || k == PropKind.Tree;
    }
}
