using System;
using System.Collections;
using System.Collections.Generic;
using Flicker.Generation;
using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.Tilemaps;

// Turns a generated BlockLayout into the playable level: paints the ground Tilemap, spawns merged wall
// blocks and props, bakes the NavMesh at runtime and puts the player on the car spot.
// Layout cell (x, y) is centred at world (x + 0.5, y + 0.5); the Grid and this object sit at the origin.
public class BlockBuilder : MonoBehaviour
{
    [Serializable] public struct GroundTile { public CellType type; public TileBase tile; }
    [Serializable] public struct BlockPrefab { public CellType type; public GameObject prefab; }
    [Serializable] public struct PropPrefab { public PropKind kind; public GameObject prefab; }

    private const string RootName = "Generated";

    [SerializeField] private BlockGenSettings settings;
    [SerializeField] private Tilemap ground;
    [SerializeField] private GroundTile[] groundTiles;
    [Tooltip("One prefab per blocking cell type: Wall, Fence, Building, Barrier.")]
    [SerializeField] private BlockPrefab[] blockPrefabs;
    [SerializeField] private PropPrefab[] propPrefabs;
    [Tooltip("The player's car, spawned in the barrier gap under the car spot. Needs an ExitCar component.")]
    [SerializeField] private ExitCar exitCarPrefab;
    [Tooltip("Spawned on every canister cell of the layout.")]
    [SerializeField] private FuelCanister canisterPrefab;
    [Tooltip("One surface per NavMesh agent type (normal zombies, large zombies). All are rebaked per block.")]
    [SerializeField] private NavMeshSurface[] navMeshSurfaces;
    [SerializeField] private Rigidbody2D player;

    [Header("Seed")]
    [SerializeField] private bool randomSeedOnStart = true;
    [SerializeField] private int startSeed;

    [Header("Debug: zombies to test pathing (real spawning comes later)")]
    [SerializeField] private GameObject debugZombiePrefab;
    [SerializeField] private int debugZombieCount;
    [SerializeField] private int debugZombieMinDistance = 15;

    public BlockLayout Current { get; private set; }
    // The exit car spawned for the current block (null before the first build).
    public ExitCar ExitCar { get; private set; }
    // Where the player starts and steps out of the car: the car spot center.
    public Vector2 PlayerSpawn => Current != null ? (Vector2)CenterOf(Current.CarSpot) : (Vector2)transform.position;
    public event Action<BlockLayout> Generated;

    void Start()
    {
        if (randomSeedOnStart) GenerateRandom();
        else Generate(startSeed);
    }

    public void GenerateRandom() => Generate(Rng.Derive(Environment.TickCount, 1));

    // revivePlayer: false keeps the player's health (the next level of a run); true is a fresh start.
    public void Generate(int seed, bool revivePlayer = true)
    {
        if (!TryCreateLayout(seed, out var layout)) return;
        StopAllCoroutines();
        StartCoroutine(Rebuild(layout, revivePlayer));
    }

    [ContextMenu("Generate (random seed)")]
    private void GenerateInEditor()
    {
        if (!TryCreateLayout(Rng.Derive(Environment.TickCount, 1), out var layout)) return;
        ClearGenerated(immediate: true);
        BuildContent(layout);
        BakeNavMeshes();
        Current = layout;
        Debug.Log($"Generated block seed {layout.Seed} ({layout.Width}x{layout.Height}, attempt {layout.Attempt})", this);
    }

    // Removes the preview, including its baked NavMesh, so saving the scene doesn't embed a stale block.
    [ContextMenu("Clear")]
    private void ClearInEditor()
    {
        ClearGenerated(immediate: true);
        foreach (var surface in navMeshSurfaces)
        {
            surface.RemoveData();
            surface.navMeshData = null;
        }
        Current = null;
    }

    private void BakeNavMeshes()
    {
        foreach (var surface in navMeshSurfaces) surface.BuildNavMesh();
    }

    private bool TryCreateLayout(int seed, out BlockLayout layout)
    {
        try
        {
            layout = BlockGenerator.Generate(seed, settings);
            return true;
        }
        catch (BlockGenerationException e)
        {
            Debug.LogError(e.Message, this);
            layout = null;
            return false;
        }
    }

    private IEnumerator Rebuild(BlockLayout layout, bool revivePlayer)
    {
        ClearGenerated(immediate: false);
        yield return null; // let destroyed colliders and renderers go before the NavMesh bake collects sources

        BuildContent(layout);
        BakeNavMeshes();
        PlacePlayer(layout, revivePlayer);
        SpawnDebugZombies(layout);

        Current = layout;
        Generated?.Invoke(layout);
    }

    private void ClearGenerated(bool immediate)
    {
        ground.ClearAllTiles();
        // Find by name rather than a field, so a root left over from an edit-mode preview is cleared too.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (child.name != RootName) continue;
            if (immediate) DestroyImmediate(child);
            else
            {
                child.SetActive(false);
                Destroy(child);
            }
        }
    }

    private void BuildContent(BlockLayout layout)
    {
        var root = new GameObject(RootName).transform;
        root.SetParent(transform, false);

        PaintGround(layout);

        foreach (var block in blockPrefabs)
        {
            var type = block.type;
            foreach (var r in GreedyRects.Merge(layout.Width, layout.Height, (x, y) => layout[x, y] == type))
            {
                var go = Instantiate(block.prefab, CenterOf(r), Quaternion.identity, root);
                go.transform.localScale = new Vector3(r.width, r.height, 1f);
            }
        }

        var props = new Dictionary<PropKind, GameObject>();
        foreach (var p in propPrefabs) props[p.kind] = p.prefab;
        foreach (var prop in layout.Props)
        {
            if (!props.TryGetValue(prop.Kind, out var prefab)) continue;
            bool rotated = prop.Footprint.size != PropKinds.Size(prop.Kind);
            Instantiate(prefab, CenterOf(prop.Footprint), Quaternion.Euler(0f, 0f, rotated ? 90f : 0f), root);
        }

        // The exit car is authored lying along +X (its 4-long side), so it needs no rotation.
        ExitCar = exitCarPrefab != null ? Instantiate(exitCarPrefab, CenterOf(layout.ExitCar), Quaternion.identity, root) : null;

        if (canisterPrefab != null)
            foreach (var cell in layout.Canisters)
                Instantiate(canisterPrefab, new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f), Quaternion.identity, root);
    }

    private void PaintGround(BlockLayout layout)
    {
        var tiles = new Dictionary<CellType, TileBase>();
        foreach (var t in groundTiles) tiles[t.type] = t.tile;

        var positions = new List<Vector3Int>();
        var painted = new List<TileBase>();
        for (int x = 0; x < layout.Width; x++)
            for (int y = 0; y < layout.Height; y++)
                if (!layout[x, y].IsBlocking() && tiles.TryGetValue(layout[x, y], out var tile))
                {
                    positions.Add(new Vector3Int(x, y, 0));
                    painted.Add(tile);
                }
        ground.SetTiles(positions.ToArray(), painted.ToArray());
    }

    private void PlacePlayer(BlockLayout layout, bool revive)
    {
        if (player == null) return;
        Vector2 spot = CenterOf(layout.CarSpot);
        player.position = spot;
        player.transform.position = spot;
        player.linearVelocity = Vector2.zero;
        // A restart is a fresh start, even if the zombies got you in the last block.
        if (revive && player.TryGetComponent(out PlayerHealth health)) health.Revive();
    }

    private void SpawnDebugZombies(BlockLayout layout)
    {
        if (debugZombiePrefab == null || debugZombieCount <= 0) return;

        var far = layout.SpawnHints.FindAll(h => h.Distance >= debugZombieMinDistance);
        if (far.Count == 0) return;

        var root = transform.Find(RootName);
        var rng = new Rng(layout.Seed ^ 0x5EED);
        for (int i = 0; i < debugZombieCount; i++)
            if (TryFindSpawn(debugZombiePrefab, far, rng, out var position))
                Instantiate(debugZombiePrefab, position, Quaternion.identity, root);
    }

    // A spawn hint where this zombie's own NavMesh exists. A large zombie only fits where the large-agent
    // mesh reaches; spawning it elsewhere wedges it into walls and desyncs its agent.
    private static bool TryFindSpawn(GameObject zombiePrefab, List<SpawnHint> hints, Rng rng, out Vector3 position)
    {
        var agent = zombiePrefab.GetComponent<UnityEngine.AI.NavMeshAgent>();
        var filter = new UnityEngine.AI.NavMeshQueryFilter
        {
            agentTypeID = agent != null ? agent.agentTypeID : 0,
            areaMask = UnityEngine.AI.NavMesh.AllAreas,
        };
        for (int attempt = 0; attempt < 20; attempt++)
        {
            var cell = rng.Pick(hints).Cell;
            var center = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
            if (UnityEngine.AI.NavMesh.SamplePosition(center, out var hit, 0.3f, filter))
            {
                position = new Vector3(hit.position.x, hit.position.y, 0f);
                return true;
            }
        }
        position = default;
        return false;
    }

    private static Vector3 CenterOf(RectInt r) => new Vector3(r.x + r.width * 0.5f, r.y + r.height * 0.5f, 0f);

    void OnDrawGizmosSelected()
    {
        if (Current == null) return;
        foreach (var lot in Current.Lots)
        {
            Gizmos.color = lot.Type switch
            {
                LotType.House => new Color(1f, 0.8f, 0.2f, 0.9f),
                LotType.TallBuilding => new Color(0.6f, 0.6f, 0.7f, 0.9f),
                LotType.Parking => new Color(0.3f, 0.6f, 1f, 0.9f),
                LotType.Park => new Color(0.3f, 1f, 0.4f, 0.9f),
                _ => Color.magenta,
            };
            DrawRect(lot.Rect, 0.1f);
        }
        foreach (var house in Current.Houses)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            foreach (var room in house.Rooms) DrawRect(room, 0.25f);
            Gizmos.color = Color.red;
            foreach (var door in house.Doors) DrawRect(door, 0f);
            DrawRect(house.FrontDoor, 0f);
            DrawRect(house.Gate, 0f);
        }
        Gizmos.color = Color.cyan;
        DrawRect(Current.CarSpot, 0f);
        DrawRect(Current.ExitCar, 0.1f);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        foreach (var c in Current.Canisters)
            Gizmos.DrawWireSphere(new Vector3(c.x + 0.5f, c.y + 0.5f, 0f), 0.6f);
    }

    private static void DrawRect(RectInt r, float inset) =>
        Gizmos.DrawWireCube(CenterOf(r), new Vector3(r.width - 2 * inset, r.height - 2 * inset, 0f));
}
