using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class DungeonGenerator : NetworkBehaviour
{
    [Header("Prefaby")]
    [SerializeField] GameObject floorPrefab;
    [SerializeField] GameObject roomFloorPrefab;
    [SerializeField] GameObject wallPrefab;
    [SerializeField] GameObject bossRoomFloorPrefab;
    [SerializeField] GameObject doorPrefab;
    [SerializeField] GameObject spawnerPrefab;

    [Header("Parametry wydajnoœci")]
    [SerializeField] float blockBuildDelay = 0.01f;
    [SerializeField] int blocksPerFrame = 50;
    [SerializeField] bool useObjectPooling = true;
    [SerializeField] int poolSize = 1000;

    [Header("Parametry sieci")]
    [SerializeField] int networkNodesCount = 25;
    [SerializeField] int tunnelLength = 8;
    [SerializeField] int minTunnelWidth = 3;
    [SerializeField] int maxTunnelWidth = 5;
    [SerializeField] float spacing = 1.5f;
    [SerializeField] int minNodeDistance = 18;
    [SerializeField] int mapSize = 60;

    [Header("Parametry pokoi")]
    [SerializeField] int roomBranchChance = 80;
    [SerializeField] int roomBranchLength = 8;
    [SerializeField] int minRoomBranchWidth = 2;
    [SerializeField] int maxRoomBranchWidth = 4;
    [SerializeField] int minRoomSize = 5;
    [SerializeField] int maxRoomSize = 10;
    [SerializeField] int maxRoomsPerNode = 4;

    [Header("Pokój bosa")]
    [SerializeField] int bossRoomSize = 20;
    [SerializeField] int bossRoomCorridorLength = 15;
    [SerializeField] int bossRoomCorridorWidth = 2;
    [SerializeField] int minDistanceFromCenter = 35;
    [SerializeField] int bossRoomTunnelBuffer = 2;

    [Header("Po³¹czenia")]
    [SerializeField] int extraConnectionsCount = 12;
    [SerializeField] int connectionChance = 75;

    [Header("Spawnery")]
    [SerializeField] bool spawnInBossRoom = true;
    [SerializeField] float spawnerHeight = 0.5f;

    // G³ówne struktury danych
    private HashSet<Vector2Int> floorPositions;
    private HashSet<Vector2Int> roomPositions;
    private HashSet<Vector2Int> bossRoomPositions;
    private HashSet<Vector2Int> bossRoomCorridorPositions;
    private List<Vector2Int> networkNodes;
    private List<Vector2Int> roomCenters;
    private Dictionary<Vector2Int, Vector2Int> roomSizes;
    private Dictionary<Vector2Int, List<Vector2Int>> nodeConnections;
    private Vector2Int bossRoomCenter;
    private Vector2Int bossRoomEntrance;

    // Kierunki
    private static readonly Vector2Int[] CardinalDirections = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };
    private static readonly Vector2Int[] AllDirections = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
    };

    // Listy do budowania
    private List<Vector2Int> floorsToInstantiate;
    private List<Vector2Int> roomFloorsToInstantiate;
    private List<Vector2Int> bossFloorsToInstantiate;
    private List<Vector2Int> wallsToInstantiate;
    private List<Vector2Int> doorsToInstantiate;
    private List<Vector2Int> spawnersToInstantiate;

    // Object pooling
    private Dictionary<GameObject, Queue<GameObject>> objectPools;
    private HashSet<string> processedConnections;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            InitializeDataStructures();
            StartCoroutine(GenerateDungeonOverTime());
        }
    }

    void InitializeDataStructures()
    {
        int estimatedSize = networkNodesCount * 100;

        floorPositions = new HashSet<Vector2Int>(estimatedSize);
        roomPositions = new HashSet<Vector2Int>(estimatedSize / 4);
        bossRoomPositions = new HashSet<Vector2Int>(bossRoomSize * bossRoomSize);
        bossRoomCorridorPositions = new HashSet<Vector2Int>(bossRoomCorridorLength * bossRoomCorridorWidth);
        networkNodes = new List<Vector2Int>(networkNodesCount);
        roomCenters = new List<Vector2Int>(networkNodesCount * maxRoomsPerNode);
        roomSizes = new Dictionary<Vector2Int, Vector2Int>();
        nodeConnections = new Dictionary<Vector2Int, List<Vector2Int>>(networkNodesCount);

        floorsToInstantiate = new List<Vector2Int>(estimatedSize);
        roomFloorsToInstantiate = new List<Vector2Int>(estimatedSize / 4);
        bossFloorsToInstantiate = new List<Vector2Int>(bossRoomSize * bossRoomSize);
        wallsToInstantiate = new List<Vector2Int>(estimatedSize);
        doorsToInstantiate = new List<Vector2Int>(100);
        spawnersToInstantiate = new List<Vector2Int>();

        processedConnections = new HashSet<string>(networkNodesCount * 4);

        if (useObjectPooling)
        {
            InitializeObjectPools();
        }
    }

    void InitializeObjectPools()
    {
        objectPools = new Dictionary<GameObject, Queue<GameObject>>();
        GameObject[] prefabs = { floorPrefab, roomFloorPrefab, wallPrefab, bossRoomFloorPrefab, doorPrefab, spawnerPrefab };

        foreach (GameObject prefab in prefabs)
        {
            if (prefab != null)
            {
                Queue<GameObject> pool = new Queue<GameObject>();
                for (int i = 0; i < poolSize / prefabs.Length; i++)
                {
                    GameObject obj = Instantiate(prefab);
                    obj.SetActive(false);
                    pool.Enqueue(obj);
                }
                objectPools[prefab] = pool;
            }
        }
    }

    GameObject GetPooledObject(GameObject prefab)
    {
        if (!useObjectPooling || !objectPools.ContainsKey(prefab) || objectPools[prefab].Count == 0)
        {
            return Instantiate(prefab);
        }

        GameObject obj = objectPools[prefab].Dequeue();
        obj.SetActive(true);
        return obj;
    }

    IEnumerator GenerateDungeonOverTime()
    {
        Debug.Log("Rozpoczynam generowanie dungeonu...");

        GenerateBossRoom();
        GenerateNetworkNodes();
        ConnectNetworkNodes();
        CreateExtraConnections();
        GenerateNetworkTunnels();
        GenerateProtrudingRooms();
        PrepareObjectsForBuilding();
        PrepareSpawners();

        yield return StartCoroutine(BuildAllObjects());
        Debug.Log("Generowanie dungeonu zakoñczone!");
    }

    void GenerateBossRoom()
    {
        bossRoomCenter = FindBossRoomPosition();
        CreateBossRoom(bossRoomCenter);
    }

    Vector2Int FindBossRoomPosition()
    {
        for (int attempts = 0; attempts < 100; attempts++)
        {
            float angle = Random.Range(0f, 2f * Mathf.PI);
            float distance = Random.Range(minDistanceFromCenter, mapSize - bossRoomSize / 2);
            Vector2Int candidate = new Vector2Int(
                Mathf.RoundToInt(distance * Mathf.Cos(angle)),
                Mathf.RoundToInt(distance * Mathf.Sin(angle))
            );
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

    void CreateBossRoom(Vector2Int center)
    {
        int halfSize = bossRoomSize / 2;
        for (int x = -halfSize; x <= halfSize; x++)
        {
            for (int y = -halfSize; y <= halfSize; y++)
            {
                Vector2Int bossPos = center + new Vector2Int(x, y);
                bossRoomPositions.Add(bossPos);
                floorPositions.Add(bossPos);
            }
        }
        roomSizes[center] = new Vector2Int(bossRoomSize, bossRoomSize);
    }

    void GenerateNetworkNodes()
    {
        Vector2Int centerNode = Vector2Int.zero;
        if (IsValidNodePosition(centerNode))
        {
            networkNodes.Add(centerNode);
            nodeConnections[centerNode] = new List<Vector2Int>(8);
        }

        int attempts = 0;
        while (networkNodes.Count < networkNodesCount && attempts < networkNodesCount * 50)
        {
            Vector2Int newNode = new Vector2Int(
                Random.Range(-mapSize, mapSize + 1),
                Random.Range(-mapSize, mapSize + 1)
            );
            if (IsValidNodePosition(newNode))
            {
                networkNodes.Add(newNode);
                nodeConnections[newNode] = new List<Vector2Int>(8);
            }
            attempts++;
        }
    }

    bool IsValidNodePosition(Vector2Int pos)
    {
        if (IsTooCloseToBossRoom(pos)) return false;
        for (int i = 0; i < networkNodes.Count; i++)
        {
            if (Vector2Int.Distance(pos, networkNodes[i]) < minNodeDistance)
                return false;
        }
        return true;
    }

    bool IsTooCloseToBossRoom(Vector2Int pos)
    {
        if (bossRoomPositions.Count == 0) return false;
        int halfSize = bossRoomSize / 2;
        int buffer = bossRoomTunnelBuffer + minNodeDistance;
        return Vector2Int.Distance(pos, bossRoomCenter) < (halfSize + buffer);
    }

    void ConnectNetworkNodes()
    {
        for (int i = 0; i < networkNodes.Count; i++)
        {
            Vector2Int node = networkNodes[i];
            List<Vector2Int> nearestNodes = FindNearestNodes(node, 4);
            for (int j = 0; j < nearestNodes.Count; j++)
            {
                Vector2Int nearestNode = nearestNodes[j];
                if (!nodeConnections[node].Contains(nearestNode))
                {
                    nodeConnections[node].Add(nearestNode);
                    nodeConnections[nearestNode].Add(node);
                }
            }
        }
        ConnectBossRoomToNetwork();
    }

    void ConnectBossRoomToNetwork()
    {
        if (networkNodes.Count == 0) return;

        Vector2Int nearestNode = networkNodes[0];
        float minDistance = Vector2Int.Distance(bossRoomCenter, nearestNode);
        for (int i = 1; i < networkNodes.Count; i++)
        {
            float distance = Vector2Int.Distance(bossRoomCenter, networkNodes[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestNode = networkNodes[i];
            }
        }
        CreateBossRoomCorridor(nearestNode, bossRoomCenter);
    }

    void CreateBossRoomCorridor(Vector2Int from, Vector2Int to)
    {
        Vector2Int current = from;
        Vector2Int direction = GetDirectionTo(from, to);
        int targetDistance = bossRoomSize / 2 + 2;

        while (Vector2Int.Distance(current, to) > targetDistance)
        {
            current += direction;
            for (int w = -bossRoomCorridorWidth / 2; w <= bossRoomCorridorWidth / 2; w++)
            {
                Vector2Int offset = (direction.x != 0) ? new Vector2Int(0, w) : new Vector2Int(w, 0);
                Vector2Int corridorPos = current + offset;
                floorPositions.Add(corridorPos);
                bossRoomCorridorPositions.Add(corridorPos);
            }
        }
        bossRoomEntrance = current;
    }

    Vector2Int GetDirectionTo(Vector2Int from, Vector2Int to)
    {
        Vector2Int diff = to - from;
        return Mathf.Abs(diff.x) > Mathf.Abs(diff.y)
            ? new Vector2Int(diff.x > 0 ? 1 : -1, 0)
            : new Vector2Int(0, diff.y > 0 ? 1 : -1);
    }

    List<Vector2Int> FindNearestNodes(Vector2Int fromNode, int count)
    {
        List<Vector2Int> tempList = new List<Vector2Int>(networkNodes);
        tempList.Remove(fromNode);
        tempList.Sort((a, b) => Vector2Int.Distance(fromNode, a).CompareTo(Vector2Int.Distance(fromNode, b)));
        int returnCount = Mathf.Min(count, tempList.Count);
        return tempList.GetRange(0, returnCount);
    }

    void CreateExtraConnections()
    {
        for (int i = 0; i < extraConnectionsCount; i++)
        {
            Vector2Int nodeA = networkNodes[Random.Range(0, networkNodes.Count)];
            Vector2Int nodeB = networkNodes[Random.Range(0, networkNodes.Count)];
            if (nodeA != nodeB && !nodeConnections[nodeA].Contains(nodeB) && Random.Range(0, 100) < connectionChance)
            {
                nodeConnections[nodeA].Add(nodeB);
                nodeConnections[nodeB].Add(nodeA);
            }
        }
    }

    void GenerateNetworkTunnels()
    {
        processedConnections.Clear();
        for (int i = 0; i < networkNodes.Count; i++)
        {
            Vector2Int node = networkNodes[i];
            List<Vector2Int> connections = nodeConnections[node];
            for (int j = 0; j < connections.Count; j++)
            {
                Vector2Int connectedNode = connections[j];
                string connectionKey = GetConnectionKey(node, connectedNode);
                if (!processedConnections.Contains(connectionKey))
                {
                    CreateTunnel(node, connectedNode);
                    processedConnections.Add(connectionKey);
                }
            }
        }
    }

    string GetConnectionKey(Vector2Int a, Vector2Int b)
    {
        Vector2Int first = (a.x < b.x || (a.x == b.x && a.y < b.y)) ? a : b;
        Vector2Int second = (first == a) ? b : a;
        return $"{first.x},{first.y}-{second.x},{second.y}";
    }

    void CreateTunnel(Vector2Int from, Vector2Int to)
    {
        Vector2Int current = from;
        int width = Random.Range(minTunnelWidth, maxTunnelWidth + 1);

        while (current.x != to.x)
        {
            current.x += (current.x < to.x) ? 1 : -1;
            AddTunnelSegment(current, width, true);
        }

        while (current.y != to.y)
        {
            current.y += (current.y < to.y) ? 1 : -1;
            AddTunnelSegment(current, width, false);
        }
    }

    void AddTunnelSegment(Vector2Int center, int width, bool horizontal)
    {
        if (IsTooCloseToBossRoom(center, width, horizontal)) return;
        for (int w = -width / 2; w <= width / 2; w++)
        {
            Vector2Int offset = horizontal ? new Vector2Int(0, w) : new Vector2Int(w, 0);
            floorPositions.Add(center + offset);
        }
    }

    bool IsTooCloseToBossRoom(Vector2Int center, int width, bool horizontal)
    {
        if (bossRoomPositions.Count == 0) return false;
        for (int w = -width / 2; w <= width / 2; w++)
        {
            Vector2Int offset = horizontal ? new Vector2Int(0, w) : new Vector2Int(w, 0);
            Vector2Int checkPos = center + offset;
            if (Vector2Int.Distance(checkPos, bossRoomCenter) < bossRoomSize / 2 + bossRoomTunnelBuffer)
                return true;
        }
        return false;
    }

    void GenerateProtrudingRooms()
    {
        for (int i = 0; i < networkNodes.Count; i++)
        {
            Vector2Int node = networkNodes[i];
            int roomsForThisNode = Random.Range(1, maxRoomsPerNode + 1);
            for (int j = 0; j < roomsForThisNode; j++)
            {
                if (Random.Range(0, 100) < roomBranchChance)
                {
                    CreateProtrudingRoom(node);
                }
            }
        }
    }

    void CreateProtrudingRoom(Vector2Int nodePos)
    {
        if (IsTooCloseToBossRoom(nodePos)) return;

        Vector2Int direction = AllDirections[Random.Range(0, AllDirections.Length)];
        Vector2Int branchPos = nodePos;
        int branchWidth = Mathf.Max(Random.Range(minRoomBranchWidth, maxRoomBranchWidth + 1), 2);

        for (int i = 0; i < roomBranchLength; i++)
        {
            branchPos += direction;
            if (IsTooCloseToBossRoom(branchPos)) return;
            AddRoomBranch(branchPos, direction, branchWidth);
        }

        Vector2Int roomCenter = branchPos + direction * 2;
        if (!IsTooCloseToBossRoom(roomCenter))
        {
            CreateRoom(roomCenter);
        }
    }

    void AddRoomBranch(Vector2Int branchPos, Vector2Int direction, int branchWidth)
    {
        if (direction.x != 0 && direction.y == 0)
        {
            for (int w = -branchWidth / 2; w <= branchWidth / 2; w++)
            {
                Vector2Int pos = branchPos + new Vector2Int(0, w);
                if (!IsTooCloseToBossRoom(pos)) floorPositions.Add(pos);
            }
        }
        else if (direction.x == 0 && direction.y != 0)
        {
            for (int w = -branchWidth / 2; w <= branchWidth / 2; w++)
            {
                Vector2Int pos = branchPos + new Vector2Int(w, 0);
                if (!IsTooCloseToBossRoom(pos)) floorPositions.Add(pos);
            }
        }
        else
        {
            int halfWidth = branchWidth / 2;
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                for (int y = -halfWidth; y <= halfWidth; y++)
                {
                    Vector2Int pos = branchPos + new Vector2Int(x, y);
                    if (!IsTooCloseToBossRoom(pos)) floorPositions.Add(pos);
                }
            }
        }
    }

    void CreateRoom(Vector2Int center)
    {
        int roomWidth = Random.Range(minRoomSize, maxRoomSize + 1);
        int roomHeight = Random.Range(minRoomSize, maxRoomSize + 1);

        roomCenters.Add(center);
        roomSizes[center] = new Vector2Int(roomWidth, roomHeight);

        for (int x = -roomWidth / 2; x <= roomWidth / 2; x++)
        {
            for (int y = -roomHeight / 2; y <= roomHeight / 2; y++)
            {
                Vector2Int roomPos = center + new Vector2Int(x, y);
                if (!IsTooCloseToBossRoom(roomPos))
                {
                    floorPositions.Add(roomPos);
                    roomPositions.Add(roomPos);
                }
            }
        }
    }

    void PrepareObjectsForBuilding()
    {
        foreach (Vector2Int pos in floorPositions)
        {
            if (bossRoomPositions.Contains(pos))
                bossFloorsToInstantiate.Add(pos);
            else if (roomPositions.Contains(pos))
                roomFloorsToInstantiate.Add(pos);
            else
                floorsToInstantiate.Add(pos);
        }
        PrepareWalls();
        PrepareDoors();
    }

    void PrepareWalls()
    {
        HashSet<Vector2Int> wallsPlaced = new HashSet<Vector2Int>();
        foreach (Vector2Int pos in floorPositions)
        {
            for (int i = 0; i < AllDirections.Length; i++)
            {
                Vector2Int neighbor = pos + AllDirections[i];
                if (!floorPositions.Contains(neighbor) && !wallsPlaced.Contains(neighbor))
                {
                    wallsToInstantiate.Add(neighbor);
                    wallsPlaced.Add(neighbor);
                }
            }
        }
    }

    void PrepareDoors()
    {
        HashSet<Vector2Int> doorPositions = new HashSet<Vector2Int>();
        foreach (Vector2Int bossPos in bossRoomPositions)
        {
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int neighbor = bossPos + CardinalDirections[i];
                if (floorPositions.Contains(neighbor) && !bossRoomPositions.Contains(neighbor) &&
                    !roomPositions.Contains(neighbor) && !doorPositions.Contains(neighbor))
                {
                    doorsToInstantiate.Add(neighbor);
                    doorPositions.Add(neighbor);
                }
            }
        }
    }

    void PrepareSpawners()
    {
        if (spawnerPrefab == null)
        {
            Debug.LogWarning("Spawner prefab nie zosta³ przypisany!");
            return;
        }

        // Spawnery tylko w centrach pokoi
        foreach (Vector2Int roomCenter in roomCenters)
        {
            spawnersToInstantiate.Add(roomCenter);
        }

        // Opcjonalnie w pokoju bosa
        if (spawnInBossRoom)
        {
            spawnersToInstantiate.Add(bossRoomCenter);
        }

        Debug.Log($"Przygotowano {spawnersToInstantiate.Count} spawnerów");
    }

    IEnumerator BuildAllObjects()
    {
        yield return StartCoroutine(BuildObjectsFromList(bossFloorsToInstantiate, bossRoomFloorPrefab, "boss floors"));
        yield return StartCoroutine(BuildObjectsFromList(roomFloorsToInstantiate, roomFloorPrefab, "room floors"));
        yield return StartCoroutine(BuildObjectsFromList(floorsToInstantiate, floorPrefab, "corridor floors"));
        yield return StartCoroutine(BuildObjectsFromList(wallsToInstantiate, wallPrefab, "walls"));
        yield return StartCoroutine(BuildObjectsFromList(doorsToInstantiate, doorPrefab, "doors"));
        yield return StartCoroutine(BuildSpawners());
    }

    IEnumerator BuildObjectsFromList(List<Vector2Int> positions, GameObject prefab, string objectType)
    {
        Debug.Log($"Building {positions.Count} {objectType}...");
        int index = 0;
        while (index < positions.Count)
        {
            int endIndex = Mathf.Min(index + blocksPerFrame, positions.Count);
            for (int i = index; i < endIndex; i++)
            {
                Vector2Int pos = positions[i];
                Vector3 worldPos = new Vector3(pos.x * spacing, pos.y * spacing, 0);
                GameObject obj = GetPooledObject(prefab);
                obj.transform.position = worldPos;
                obj.transform.rotation = Quaternion.identity;
                obj.GetComponent<NetworkObject>().Spawn(true);
                obj.transform.SetParent(transform);
            }
            index = endIndex;
            yield return new WaitForSeconds(blockBuildDelay);
        }
    }

    IEnumerator BuildSpawners()
    {
        if (spawnerPrefab == null || spawnersToInstantiate.Count == 0) yield break;

        Debug.Log($"Building {spawnersToInstantiate.Count} spawners...");

        // S³ownik do œledzenia spawnerów na ka¿dej pozycji
        Dictionary<Vector2Int, GameObject> spawnersAtPosition = new Dictionary<Vector2Int, GameObject>();

        int index = 0;
        while (index < spawnersToInstantiate.Count)
        {
            int endIndex = Mathf.Min(index + blocksPerFrame, spawnersToInstantiate.Count);
            for (int i = index; i < endIndex; i++)
            {
                Vector2Int pos = spawnersToInstantiate[i];

                // SprawdŸ czy na tej pozycji ju¿ jest spawner
                if (spawnersAtPosition.ContainsKey(pos))
                {
                    // ZnajdŸ istniej¹cy spawner i zwiêksz mu poziom
                    GameObject existingSpawner = spawnersAtPosition[pos];
                    RoomSpawner existingRoomSpawner = existingSpawner.GetComponent<RoomSpawner>();

                    if (existingRoomSpawner != null)
                    {
                        existingRoomSpawner.spawnerLevel++;
                        Debug.Log($"Spawner na pozycji {pos} zosta³ poziomowany!");
                    }

                    continue; // PrzejdŸ do nastêpnego spawnera
                }

                // Stwórz nowy spawner
                Vector3 worldPos = new Vector3(pos.x * spacing, pos.y * spacing, spawnerHeight);
                GameObject spawner = GetPooledObject(spawnerPrefab);
                spawner.transform.position = worldPos;
                spawner.transform.rotation = Quaternion.identity;

                // Przeka¿ wymiary pokoju do RoomSpawner
                RoomSpawner roomSpawner = spawner.GetComponent<RoomSpawner>();
                if (roomSpawner != null && roomSizes.ContainsKey(pos))
                {
                    Vector2Int roomSize = roomSizes[pos];
                    roomSpawner.roomSizeArea = roomSize;
                }

                NetworkObject networkObj = spawner.GetComponent<NetworkObject>();
                if (networkObj != null)
                {
                    networkObj.Spawn(true);
                }
                spawner.transform.SetParent(transform);

                // Zapisz spawner w s³owniku
                spawnersAtPosition[pos] = spawner;
            }
            index = endIndex;
            yield return new WaitForSeconds(blockBuildDelay);
        }

    }

    // Publiczne metody dostêpu
    public Vector2Int GetBossRoomCenter() => bossRoomCenter;
    public Vector2Int GetBossRoomEntrance() => bossRoomEntrance;
    public List<Vector2Int> GetRoomCenters() => new List<Vector2Int>(roomCenters);
    public List<Vector2Int> GetSpawnerPositions() => new List<Vector2Int>(spawnersToInstantiate);
    public Dictionary<Vector2Int, Vector2Int> GetRoomSizes() => new Dictionary<Vector2Int, Vector2Int>(roomSizes);
}