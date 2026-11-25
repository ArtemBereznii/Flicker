using System.Collections.Generic;
using UnityEngine;

namespace Flicker.Generation
{
    // The generated block: a grid of cells plus lots, houses, props and the car spot.
    public sealed class BlockLayout
    {
        public int Width { get; }
        public int Height { get; }
        // The seed the player asked for (shown on screen).
        public int Seed { get; }
        // Which deterministic retry of that seed produced this layout.
        public int Attempt { get; }

        public RectInt InnerArea;
        public RectInt CarSpot;
        // The player's car: a 4x2 footprint in the barrier gap under the car spot (barrier row + top road row).
        public RectInt ExitCar;
        // Fuel canister cells, filled by CanisterPlacer after validation.
        public readonly List<Vector2Int> Canisters = new List<Vector2Int>();
        public readonly List<Lot> Lots = new List<Lot>();
        public readonly List<House> Houses = new List<House>();
        // Walking distance from the car spot center; -1 where not walkable. Filled by the validator.
        public int[,] DistanceFromCar;
        public readonly List<SpawnHint> SpawnHints = new List<SpawnHint>();

        private readonly CellType[,] cells;
        private readonly bool[,] propCells;
        private readonly List<PropPlacement> props = new List<PropPlacement>();

        public BlockLayout(int width, int height, int seed, int attempt)
        {
            Width = width;
            Height = height;
            Seed = seed;
            Attempt = attempt;
            cells = new CellType[width, height];
            propCells = new bool[width, height];
        }

        public CellType this[int x, int y]
        {
            get => cells[x, y];
            set => cells[x, y] = value;
        }

        public CellType this[Vector2Int c]
        {
            get => cells[c.x, c.y];
            set => cells[c.x, c.y] = value;
        }

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;

        public void Fill(RectInt r, CellType t)
        {
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    cells[x, y] = t;
        }

        public bool HasProp(Vector2Int c) => InBounds(c) && propCells[c.x, c.y];

        // In bounds, a walkable cell type, and not covered by a prop.
        public bool IsWalkable(Vector2Int c) => InBounds(c) && cells[c.x, c.y].IsWalkable() && !propCells[c.x, c.y];

        public IReadOnlyList<PropPlacement> Props => props;

        public void AddProp(PropPlacement p)
        {
            props.Add(p);
            SetPropCells(p.Footprint, true);
        }

        public void RemoveLastProp()
        {
            var p = props[props.Count - 1];
            props.RemoveAt(props.Count - 1);
            SetPropCells(p.Footprint, false);
        }

        private void SetPropCells(RectInt r, bool value)
        {
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    propCells[x, y] = value;
        }
    }
}
