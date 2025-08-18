using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class DungeonGenerator : MonoBehaviour
{
    [Header("Konfiguracja prefabów")]
    [SerializeField] private DungeonPrefabs dungeonPrefabs;

    [Header("Parametry wydajnoœci")]
    [SerializeField] private float blockBuildDelay = 0.01f;
    [SerializeField] private int blocksPerFrame = 50;

    [Header("Parametry sieci")]
    [SerializeField] private int networkNodesCount = 25;
    [SerializeField] private int minTunnelWidth = 3;
    [SerializeField] private int maxTunnelWidth = 5;
    [SerializeField] private float spacing = 1.5f;
    [SerializeField] private int minNodeDistance = 18;
    [SerializeField] private int mapSize = 60;

    [Header("Parametry pokoi")]
    [SerializeField] private int roomBranchChance = 80;
    [SerializeField] private int roomBranchLength = 8;
    [SerializeField] private int minRoomBranchWidth = 2;
    [SerializeField] private int maxRoomBranchWidth = 4;
    [SerializeField] private int minRoomSize = 5;
    [SerializeField] private int maxRoomSize = 10;
    [SerializeField] private int maxRoomsPerNode = 4;

    [Header("Pokój bosa")]
    [SerializeField] public bool generateBossRoom = false;
    [SerializeField] private int bossRoomSize = 20;
    [SerializeField] private int bossRoomCorridorWidth = 2;
    [SerializeField] private int minDistanceFromCenter = 35;
    [SerializeField] private int bossRoomTunnelBuffer = 2;

    [Header("Po³¹czenia")]
    [SerializeField] private int extraConnectionsCount = 12;
    [SerializeField] private int connectionChance = 75;

    [Header("Spawnery")]
    [SerializeField] private bool spawnInBossRoom = true;
    [SerializeField] private float spawnerHeight = 0.5f;
    [SerializeField] private bool useRandomSpawnerVariants = true;

    [Header("Ró¿norodnoœæ prefabów")]
    [SerializeField] private bool useAlternativeFloors = true;
    [SerializeField] private bool useAlternativeWalls = true;
    [SerializeField, Range(0f, 1f)] private float alternativePrefabChance = 0.3f;

    private HashSet<Vector2Int> floorPositions = new();
    private HashSet<Vector2Int> roomPositions = new();
    private HashSet<Vector2Int> bossRoomPositions = new();
    private List<Vector2Int> networkNodes = new();
    private List<Vector2Int> roomCenters = new();
    private Dictionary<Vector2Int, Vector2Int> roomSizes = new();
    private Dictionary<Vector2Int, List<Vector2Int>> nodeConnections = new();

    private Vector2Int bossRoomCenter, bossRoomEntrance;
    [SerializeField] private bool isDungeonGenerated = false;

    private enum PrefabType { Floor, RoomFloor, BossFloor, Wall, Door, BossDoor, Spawner }
    private struct BuildItem { public Vector2Int pos; public PrefabType type; }
    private List<BuildItem> buildQueue = new();

    private static readonly Vector2Int[] CardinalDirections = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };
    private static readonly Vector2Int[] AllDirections = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    public IEnumerator GenerateDungeonOverTime()
    {
        if (isDungeonGenerated) yield break;
        Debug.Log("GenerateDungeonOverTime called!");
        isDungeonGenerated = true;
        EnsureFloorAtOrigin();

        if (generateBossRoom)
            GenerateBossRoom();

        GenerateNetworkNodes();
        ConnectNetworkNodes();
        CreateExtraConnections();
        GenerateNetworkTunnels();
        GenerateProtrudingRooms();
        PrepareObjectsForBuilding();
        yield return StartCoroutine(BuildAllObjects());
        Debug.Log("Dungeon Done!!");
    }

    void EnsureFloorAtOrigin() => floorPositions.Add(Vector2Int.zero);

    void GenerateBossRoom()
    {
        bossRoomCenter = FindBossRoomPosition();
        int halfSize = bossRoomSize / 2;
        for (int x = -halfSize; x <= halfSize; x++)
            for (int y = -halfSize; y <= halfSize; y++)
                bossRoomPositions.Add(bossRoomCenter + new Vector2Int(x, y));
        floorPositions.UnionWith(bossRoomPositions);
        roomSizes[bossRoomCenter] = new Vector2Int(bossRoomSize, bossRoomSize);
    }

    Vector2Int FindBossRoomPosition()
    {
        for (int attempts = 0; attempts < 100; attempts++)
        {
            float angle = Random.Range(0f, 2f * Mathf.PI);
            float distance = Random.Range(minDistanceFromCenter, mapSize - bossRoomSize / 2);
            var candidate = new Vector2Int(Mathf.RoundToInt(distance * Mathf.Cos(angle)), Mathf.RoundToInt(distance * Mathf.Sin(angle)));
            if (IsValidBossRoomPosition(candidate)) return candidate;
        }
        return new Vector2Int(minDistanceFromCenter, 0);
    }

    bool IsValidBossRoomPosition(Vector2Int pos)
    {
        int halfSize = bossRoomSize / 2;
        int buffer = bossRoomTunnelBuffer + 3;
        return pos.x - halfSize - buffer >= -mapSize && pos.x + halfSize + buffer <= mapSize &&
               pos.y - halfSize - buffer >= -mapSize && pos.y + halfSize + buffer <= mapSize;
    }

    void GenerateNetworkNodes()
    {
        networkNodes.Add(Vector2Int.zero);
        nodeConnections[Vector2Int.zero] = new List<Vector2Int>();
        int attempts = 0;
        while (networkNodes.Count < networkNodesCount && attempts < networkNodesCount * 50)
        {
            Vector2Int newNode = new(Random.Range(-mapSize, mapSize + 1), Random.Range(-mapSize, mapSize + 1));
            if (IsValidNodePosition(newNode))
            {
                networkNodes.Add(newNode);
                nodeConnections[newNode] = new List<Vector2Int>();
            }
            attempts++;
        }
    }

    bool IsValidNodePosition(Vector2Int pos)
    {
        if (generateBossRoom && Vector2Int.Distance(pos, bossRoomCenter) < (bossRoomSize / 2 + bossRoomTunnelBuffer + minNodeDistance))
            return false;
        foreach (Vector2Int node in networkNodes)
            if (Vector2Int.Distance(pos, node) < minNodeDistance) return false;
        return true;
    }

    void ConnectNetworkNodes()
    {
        foreach (Vector2Int node in networkNodes)
            foreach (Vector2Int nearestNode in FindNearestNodes(node, 4))
                if (!nodeConnections[node].Contains(nearestNode))
                {
                    nodeConnections[node].Add(nearestNode);
                    nodeConnections[nearestNode].Add(node);
                }

        if (generateBossRoom)
            ConnectBossRoomToNetwork();
    }

    void ConnectBossRoomToNetwork()
    {
        Vector2Int nearestNode = networkNodes[0];
        float minDist = Vector2Int.Distance(bossRoomCenter, nearestNode);
        foreach (var node in networkNodes)
        {
            float d = Vector2Int.Distance(bossRoomCenter, node);
            if (d < minDist) { minDist = d; nearestNode = node; }
        }
        CreateBossRoomCorridor(nearestNode, bossRoomCenter);
    }

    void CreateBossRoomCorridor(Vector2Int from, Vector2Int to)
    {
        Vector2Int current = from;
        int targetDistance = bossRoomSize / 2 + 2;
        int iterations = 0, maxIterations = mapSize * 2;
        while (current.x != to.x && iterations++ < maxIterations) { current.x += current.x < to.x ? 1 : -1; AddCorridorTiles(current, true); }
        while (current.y != to.y && iterations++ < maxIterations) { current.y += current.y < to.y ? 1 : -1; AddCorridorTiles(current, false); }
        while (Vector2Int.Distance(current, to) > targetDistance && iterations++ < maxIterations)
        {
            Vector2Int dir = Mathf.Abs(to.x - current.x) > Mathf.Abs(to.y - current.y) ?
                new Vector2Int(to.x > current.x ? 1 : -1, 0) : new Vector2Int(0, to.y > current.y ? 1 : -1);
            current += dir;
            AddCorridorTiles(current, dir.x != 0);
        }
        bossRoomEntrance = current;
    }

    void AddCorridorTiles(Vector2Int pos, bool horizontal)
    {
        for (int w = -bossRoomCorridorWidth / 2; w <= bossRoomCorridorWidth / 2; w++)
            floorPositions.Add(pos + (horizontal ? new Vector2Int(0, w) : new Vector2Int(w, 0)));
    }

    List<Vector2Int> FindNearestNodes(Vector2Int fromNode, int count)
    {
        var list = new List<Vector2Int>(networkNodes);
        list.Remove(fromNode);
        list.Sort((a, b) => Vector2Int.Distance(fromNode, a).CompareTo(Vector2Int.Distance(fromNode, b)));
        return list.GetRange(0, Mathf.Min(count, list.Count));
    }

    void CreateExtraConnections()
    {
        for (int i = 0; i < extraConnectionsCount; i++)
        {
            var a = networkNodes[Random.Range(0, networkNodes.Count)];
            var b = networkNodes[Random.Range(0, networkNodes.Count)];
            if (a != b && !nodeConnections[a].Contains(b) && Random.Range(0, 100) < connectionChance)
            {
                nodeConnections[a].Add(b);
                nodeConnections[b].Add(a);
            }
        }
    }

    void GenerateNetworkTunnels()
    {
        HashSet<string> processed = new();
        foreach (var node in networkNodes)
            foreach (var connected in nodeConnections[node])
            {
                string key = node.x <= connected.x ? $"{node}-{connected}" : $"{connected}-{node}";
                if (!processed.Contains(key))
                {
                    CreateTunnel(node, connected);
                    processed.Add(key);
                }
            }
    }

    void CreateTunnel(Vector2Int from, Vector2Int to)
    {
        Vector2Int current = from;
        int width = Random.Range(minTunnelWidth, maxTunnelWidth + 1);
        while (current.x != to.x) { current.x += current.x < to.x ? 1 : -1; AddTunnelSegment(current, width, true); }
        while (current.y != to.y) { current.y += current.y < to.y ? 1 : -1; AddTunnelSegment(current, width, false); }
    }

    void AddTunnelSegment(Vector2Int center, int width, bool horizontal)
    {
        for (int w = -width / 2; w <= width / 2; w++)
            floorPositions.Add(center + (horizontal ? new Vector2Int(0, w) : new Vector2Int(w, 0)));
    }

    void GenerateProtrudingRooms()
    {
        foreach (var node in networkNodes)
            for (int j = 0; j < Random.Range(1, maxRoomsPerNode + 1); j++)
                if (Random.Range(0, 100) < roomBranchChance) CreateProtrudingRoom(node);
    }

    void CreateProtrudingRoom(Vector2Int nodePos)
    {
        Vector2Int dir = AllDirections[Random.Range(0, AllDirections.Length)];
        Vector2Int pos = nodePos;
        int branchWidth = Mathf.Max(Random.Range(minRoomBranchWidth, maxRoomBranchWidth + 1), 2);
        for (int i = 0; i < roomBranchLength; i++) { pos += dir; AddRoomBranch(pos, dir, branchWidth); }
        CreateRoom(pos + dir * 2);
    }

    void AddRoomBranch(Vector2Int pos, Vector2Int dir, int width)
    {
        if (dir.x != 0 && dir.y == 0)
            for (int w = -width / 2; w <= width / 2; w++) floorPositions.Add(pos + new Vector2Int(0, w));
        else if (dir.x == 0 && dir.y != 0)
            for (int w = -width / 2; w <= width / 2; w++) floorPositions.Add(pos + new Vector2Int(w, 0));
        else
            for (int x = -width / 2; x <= width / 2; x++)
                for (int y = -width / 2; y <= width / 2; y++)
                    floorPositions.Add(pos + new Vector2Int(x, y));
    }

    void CreateRoom(Vector2Int center)
    {
        int w = Random.Range(minRoomSize, maxRoomSize + 1);
        int h = Random.Range(minRoomSize, maxRoomSize + 1);
        roomCenters.Add(center);
        roomSizes[center] = new Vector2Int(w, h);
        for (int x = -w / 2; x <= w / 2; x++)
            for (int y = -h / 2; y <= h / 2; y++)
            {
                var p = center + new Vector2Int(x, y);
                floorPositions.Add(p);
                roomPositions.Add(p);
            }
    }

    void PrepareObjectsForBuilding()
    {
        foreach (var pos in floorPositions)
            buildQueue.Add(new BuildItem
            {
                pos = pos,
                type = generateBossRoom && bossRoomPositions.Contains(pos) ? PrefabType.BossFloor :
                       roomPositions.Contains(pos) ? PrefabType.RoomFloor : PrefabType.Floor
            });

        HashSet<Vector2Int> wallPos = new();
        foreach (var pos in floorPositions)
            foreach (var dir in AllDirections)
            {
                var neigh = pos + dir;
                if (!floorPositions.Contains(neigh) && wallPos.Add(neigh))
                    buildQueue.Add(new BuildItem { pos = neigh, type = PrefabType.Wall });
            }

        if (generateBossRoom)
        {
            foreach (var pos in bossRoomPositions)
                foreach (var dir in CardinalDirections)
                {
                    var neigh = pos + dir;
                    if (floorPositions.Contains(neigh) && !bossRoomPositions.Contains(neigh))
                        buildQueue.Add(new BuildItem { pos = neigh, type = Vector2Int.Distance(neigh, bossRoomEntrance) < 3 ? PrefabType.BossDoor : PrefabType.Door });
                }

            if (spawnInBossRoom)
                buildQueue.Add(new BuildItem { pos = bossRoomCenter, type = PrefabType.Spawner });
        }

        foreach (var center in roomCenters)
            buildQueue.Add(new BuildItem { pos = center, type = PrefabType.Spawner });
    }

    IEnumerator BuildAllObjects()
    {
        int index = 0;
        while (index < buildQueue.Count)
        {
            int end = Mathf.Min(index + blocksPerFrame, buildQueue.Count);
            for (int i = index; i < end; i++)
            {
                var item = buildQueue[i];
                var prefab = GetPrefab(item.type);
                if (prefab != null)
                {
                    Vector3 worldPos = new(item.pos.x * spacing, item.pos.y * spacing, item.type == PrefabType.Spawner ? spawnerHeight : 0);
                    var obj = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                    if (item.type == PrefabType.Spawner && obj.TryGetComponent(out RoomSpawner rs) && roomSizes.ContainsKey(item.pos))
                        rs.roomSizeArea = roomSizes[item.pos];
                }
            }
            index = end;
            yield return new WaitForSeconds(blockBuildDelay);
        }
    }

    GameObject GetPrefab(PrefabType type) => type switch
    {
        PrefabType.Floor => (useAlternativeFloors && Random.value < alternativePrefabChance) ? dungeonPrefabs.GetRandomFloorPrefab() : dungeonPrefabs.floorPrefab,
        PrefabType.RoomFloor => dungeonPrefabs.roomFloorPrefab ?? dungeonPrefabs.floorPrefab,
        PrefabType.BossFloor => dungeonPrefabs.bossRoomFloorPrefab ?? dungeonPrefabs.floorPrefab,
        PrefabType.Wall => (useAlternativeWalls && Random.value < alternativePrefabChance) ? dungeonPrefabs.GetRandomWallPrefab() : dungeonPrefabs.wallPrefab,
        PrefabType.Door => dungeonPrefabs.doorPrefab,
        PrefabType.BossDoor => dungeonPrefabs.bossRoomDoorPrefab ?? dungeonPrefabs.doorPrefab,
        PrefabType.Spawner => useRandomSpawnerVariants ? dungeonPrefabs.GetRandomSpawnerPrefab() : dungeonPrefabs.spawnerPrefab,
        _ => null
    };
    public void getNetworkParametrs(int networkNodesCountVar, int minTunnelWidthVar, int maxTunnelWidthintVar, int minNodeDistanceVar, int mapSizeVar)
    {
        networkNodesCount = networkNodesCountVar;
        minTunnelWidth = minTunnelWidthVar;
        maxTunnelWidth = maxTunnelWidthintVar;
        minNodeDistance = minNodeDistanceVar;
        mapSize = mapSizeVar;
    }
    public void getRoomsParametrs(int roomBranchChanceVar, int roomBranchLengthVar, int minRoomBranchWidthVar, int maxRoomBranchWidthVar, int minRoomSizeVar, int maxRoomSizeVar, int maxRoomsPerNodeVar)
    {
        roomBranchChance = roomBranchChanceVar;
        roomBranchLength = roomBranchLengthVar;
        minRoomBranchWidth = minRoomBranchWidthVar;
        maxRoomBranchWidth = maxRoomBranchWidthVar;
        minRoomSize = minRoomSizeVar;
        maxRoomSize = maxRoomSizeVar;
        maxRoomsPerNode = maxRoomsPerNodeVar;
    }
    public void getBossRoomParametrs(int bossRoomSizeVar, int bossRoomCorridorWidthVar, int minDistanceFromCenterVar, int bossRoomTunnelBufferVar)
    {
        bossRoomSize = bossRoomSizeVar;
        bossRoomCorridorWidth = bossRoomCorridorWidthVar;
        minDistanceFromCenter = minDistanceFromCenterVar;
        bossRoomTunnelBuffer = bossRoomTunnelBufferVar;
    }
    public void getConnectParametrs(int extraConnectionsCountVar, int connectionChanceVar)
    {
        extraConnectionsCount = extraConnectionsCountVar;
        connectionChance = connectionChanceVar;
    }

    public void DestroyDungeon()
    {
        isDungeonGenerated = false;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        MonsterScript[] enemies = GameObject.FindObjectsByType<MonsterScript>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }

        Chest[] spawners = GameObject.FindObjectsByType<Chest>(FindObjectsSortMode.None);
        foreach (var spawner in spawners)
        {
            Destroy(spawner.gameObject);
        }
    }

}
