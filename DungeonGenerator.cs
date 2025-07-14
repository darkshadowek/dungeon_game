using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class DungeonGenerator : NetworkBehaviour
{
    [Header("Konfiguracja prefabów")]
    [SerializeField] private DungeonPrefabs dungeonPrefabs;

    [Header("Parametry wydajnoœci")]
    [SerializeField] float blockBuildDelay = 0.01f;
    [SerializeField] int blocksPerFrame = 50;

    [Header("Parametry sieci")]
    [SerializeField] int networkNodesCount = 25;
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
    [SerializeField] int bossRoomCorridorWidth = 2;
    [SerializeField] int minDistanceFromCenter = 35;
    [SerializeField] int bossRoomTunnelBuffer = 2;

    [Header("Po³¹czenia")]
    [SerializeField] int extraConnectionsCount = 12;
    [SerializeField] int connectionChance = 75;

    [Header("Spawnery")]
    [SerializeField] bool spawnInBossRoom = true;
    [SerializeField] float spawnerHeight = 0.5f;
    [SerializeField] bool useRandomSpawnerVariants = true;

    [Header("Ró¿norodnoœæ prefabów")]
    [SerializeField] bool useAlternativeFloors = true;
    [SerializeField] bool useAlternativeWalls = true;
    [SerializeField][Range(0f, 1f)] float alternativePrefabChance = 0.3f;

    // G³ówne struktury danych
    private HashSet<Vector2Int> floorPositions;
    private HashSet<Vector2Int> roomPositions;
    private HashSet<Vector2Int> bossRoomPositions;
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
    private List<Vector2Int> bossDoorsToInstantiate;

    private HashSet<string> processedConnections;

    [Header("UI Controls")]
    [SerializeField] private UnityEngine.UI.Button generateDungeonButton;

    private bool isDungeonGenerated = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ValidatePrefabConfiguration();
            InitializeDataStructures();
            SetupUI();
        }
    }

    void ValidatePrefabConfiguration()
    {
        if (dungeonPrefabs == null)
        {
            Debug.LogError("DungeonPrefabs nie zosta³ przypisany! Generator nie bêdzie dzia³aæ poprawnie.");
            return;
        }

        if (dungeonPrefabs.floorPrefab == null)
        {
            Debug.LogError("Podstawowy floor prefab nie zosta³ przypisany w DungeonPrefabs!");
        }

        if (dungeonPrefabs.wallPrefab == null)
        {
            Debug.LogError("Podstawowy wall prefab nie zosta³ przypisany w DungeonPrefabs!");
        }

        if (dungeonPrefabs.spawnerPrefab == null)
        {
            Debug.LogWarning("Spawner prefab nie zosta³ przypisany w DungeonPrefabs!");
        }
    }

    void SetupUI()
    {
        if (generateDungeonButton != null)
        {
            generateDungeonButton.onClick.AddListener(OnGenerateDungeonButtonPressed);
            generateDungeonButton.interactable = true;
        }
        else
        {
            Debug.LogWarning("Generate Dungeon Button nie zosta³ przypisany w inspectorze!");
        }
    }

    void OnGenerateDungeonButtonPressed()
    {
        if (IsServer && !isDungeonGenerated)
        {
            StartCoroutine(GenerateDungeonOverTime());
            if (generateDungeonButton != null)
            {
                generateDungeonButton.interactable = false;
            }
        }
    }

    // Publiczna metoda do generowania dungeonu (opcjonalnie z kodu)
    public void GenerateDungeon()
    {
        if (IsServer && !isDungeonGenerated)
        {
            StartCoroutine(GenerateDungeonOverTime());
        }
    }

    void InitializeDataStructures()
    {
        int estimatedSize = networkNodesCount * 100;

        floorPositions = new HashSet<Vector2Int>();
        roomPositions = new HashSet<Vector2Int>();
        bossRoomPositions = new HashSet<Vector2Int>();
        networkNodes = new List<Vector2Int>();
        roomCenters = new List<Vector2Int>();
        roomSizes = new Dictionary<Vector2Int, Vector2Int>();
        nodeConnections = new Dictionary<Vector2Int, List<Vector2Int>>();

        floorsToInstantiate = new List<Vector2Int>();
        roomFloorsToInstantiate = new List<Vector2Int>();
        bossFloorsToInstantiate = new List<Vector2Int>();
        wallsToInstantiate = new List<Vector2Int>();
        doorsToInstantiate = new List<Vector2Int>();
        spawnersToInstantiate = new List<Vector2Int>();
        bossDoorsToInstantiate = new List<Vector2Int>();

        processedConnections = new HashSet<string>();
    }

    IEnumerator GenerateDungeonOverTime()
    {
        if (isDungeonGenerated)
        {
            Debug.Log("Dungeon ju¿ zosta³ wygenerowany!");
            yield break;
        }

        if (dungeonPrefabs == null)
        {
            Debug.LogError("Nie mo¿na wygenerowaæ dungeonu - brak konfiguracji prefabów!");
            yield break;
        }

        isDungeonGenerated = true;
        Debug.Log("Rozpoczynam generowanie dungeonu...");
        yield return new WaitForSeconds(1f);
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
        this.gameObject.SetActive(false);
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
        blockBuildDelay = 0.2f;
        blocksPerFrame = 5;
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
        blockBuildDelay = 0.01f;
        networkNodes.Add(Vector2Int.zero);
        nodeConnections[Vector2Int.zero] = new List<Vector2Int>();

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
                nodeConnections[newNode] = new List<Vector2Int>();
            }
            attempts++;
        }
    }

    bool IsValidNodePosition(Vector2Int pos)
    {
        if (IsTooCloseToBossRoom(pos)) return false;
        foreach (Vector2Int node in networkNodes)
        {
            if (Vector2Int.Distance(pos, node) < minNodeDistance)
                return false;
        }
        return true;
    }

    bool IsTooCloseToBossRoom(Vector2Int pos)
    {
        return Vector2Int.Distance(pos, bossRoomCenter) < (bossRoomSize / 2 + bossRoomTunnelBuffer + minNodeDistance);
    }

    void ConnectNetworkNodes()
    {
        foreach (Vector2Int node in networkNodes)
        {
            List<Vector2Int> nearestNodes = FindNearestNodes(node, 4);
            foreach (Vector2Int nearestNode in nearestNodes)
            {
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
        Vector2Int nearestNode = networkNodes[0];
        float minDistance = Vector2Int.Distance(bossRoomCenter, nearestNode);

        foreach (Vector2Int node in networkNodes)
        {
            float distance = Vector2Int.Distance(bossRoomCenter, node);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestNode = node;
            }
        }
        CreateBossRoomCorridor(nearestNode, bossRoomCenter);
    }

    void CreateBossRoomCorridor(Vector2Int from, Vector2Int to)
    {
        Vector2Int current = from;
        int targetDistance = bossRoomSize / 2 + 2;
        int maxIterations = mapSize * 2; // Safety limit to prevent infinite loops
        int iterations = 0;

        // First, move horizontally
        while (current.x != to.x && iterations < maxIterations)
        {
            current.x += (current.x < to.x) ? 1 : -1;

            for (int w = -bossRoomCorridorWidth / 2; w <= bossRoomCorridorWidth / 2; w++)
            {
                Vector2Int pos = current + new Vector2Int(0, w);
                floorPositions.Add(pos);
            }
            iterations++;
        }

        // Then, move vertically
        while (current.y != to.y && iterations < maxIterations)
        {
            current.y += (current.y < to.y) ? 1 : -1;

            for (int w = -bossRoomCorridorWidth / 2; w <= bossRoomCorridorWidth / 2; w++)
            {
                Vector2Int pos = current + new Vector2Int(w, 0);
                floorPositions.Add(pos);
            }
            iterations++;
        }

        // Now move towards the boss room until we're at the target distance
        while (Vector2Int.Distance(current, to) > targetDistance && iterations < maxIterations)
        {
            Vector2Int direction = GetDirectionTo(current, to);
            current += direction;

            for (int w = -bossRoomCorridorWidth / 2; w <= bossRoomCorridorWidth / 2; w++)
            {
                Vector2Int offset = (direction.x != 0) ? new Vector2Int(0, w) : new Vector2Int(w, 0);
                floorPositions.Add(current + offset);
            }
            iterations++;
        }

        if (iterations >= maxIterations)
        {
            Debug.LogWarning("CreateBossRoomCorridor hit maximum iterations limit!");
        }

        bossRoomEntrance = current;
    }

    Vector2Int GetDirectionTo(Vector2Int from, Vector2Int to)
    {
        Vector2Int diff = to - from;

        // If we're already at the target, return zero vector
        if (diff == Vector2Int.zero)
            return Vector2Int.zero;

        // Return the direction for the axis with the larger distance
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
            return new Vector2Int(diff.x > 0 ? 1 : -1, 0);
        else
            return new Vector2Int(0, diff.y > 0 ? 1 : -1);
    }

    List<Vector2Int> FindNearestNodes(Vector2Int fromNode, int count)
    {
        List<Vector2Int> tempList = new List<Vector2Int>(networkNodes);
        tempList.Remove(fromNode);
        tempList.Sort((a, b) => Vector2Int.Distance(fromNode, a).CompareTo(Vector2Int.Distance(fromNode, b)));
        return tempList.GetRange(0, Mathf.Min(count, tempList.Count));
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
        foreach (Vector2Int node in networkNodes)
        {
            foreach (Vector2Int connectedNode in nodeConnections[node])
            {
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
        if (IsTooCloseToBossRoom(center)) return;

        for (int w = -width / 2; w <= width / 2; w++)
        {
            Vector2Int offset = horizontal ? new Vector2Int(0, w) : new Vector2Int(w, 0);
            floorPositions.Add(center + offset);
        }
    }

    void GenerateProtrudingRooms()
    {
        foreach (Vector2Int node in networkNodes)
        {
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
            foreach (Vector2Int direction in AllDirections)
            {
                Vector2Int neighbor = pos + direction;
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
            foreach (Vector2Int direction in CardinalDirections)
            {
                Vector2Int neighbor = bossPos + direction;
                if (floorPositions.Contains(neighbor) && !bossRoomPositions.Contains(neighbor) &&
                    !roomPositions.Contains(neighbor) && !doorPositions.Contains(neighbor))
                {
                    // SprawdŸ czy to g³ówne wejœcie do pokoju bosa
                    if (Vector2Int.Distance(neighbor, bossRoomEntrance) < 3)
                    {
                        bossDoorsToInstantiate.Add(neighbor);
                    }
                    else
                    {
                        doorsToInstantiate.Add(neighbor);
                    }
                    doorPositions.Add(neighbor);
                }
            }
        }
    }

    void PrepareSpawners()
    {
        if (dungeonPrefabs == null || dungeonPrefabs.spawnerPrefab == null) return;

        foreach (Vector2Int roomCenter in roomCenters)
        {
            spawnersToInstantiate.Add(roomCenter);
        }

        if (spawnInBossRoom)
        {
            spawnersToInstantiate.Add(bossRoomCenter);
        }

        Debug.Log($"Przygotowano {spawnersToInstantiate.Count} spawnerów");
    }

    IEnumerator BuildAllObjects()
    {
        yield return StartCoroutine(BuildObjectsFromList(bossFloorsToInstantiate, GetBossRoomFloorPrefab()));
        yield return StartCoroutine(BuildObjectsFromList(roomFloorsToInstantiate, GetRoomFloorPrefab()));
        yield return StartCoroutine(BuildObjectsFromList(floorsToInstantiate, GetFloorPrefab()));
        yield return StartCoroutine(BuildObjectsFromList(wallsToInstantiate, GetWallPrefab()));
        yield return StartCoroutine(BuildObjectsFromList(doorsToInstantiate, GetDoorPrefab()));
        yield return StartCoroutine(BuildObjectsFromList(bossDoorsToInstantiate, GetBossRoomDoorPrefab()));
        yield return StartCoroutine(BuildSpawners());
    }

    // Metody pomocnicze do pobierania prefabów z ScriptableObject
    GameObject GetFloorPrefab()
    {
        if (dungeonPrefabs == null) return null;

        if (useAlternativeFloors && Random.Range(0f, 1f) < alternativePrefabChance)
        {
            return dungeonPrefabs.GetRandomFloorPrefab();
        }
        return dungeonPrefabs.floorPrefab;
    }

    GameObject GetRoomFloorPrefab()
    {
        if (dungeonPrefabs == null) return null;
        return dungeonPrefabs.roomFloorPrefab != null ? dungeonPrefabs.roomFloorPrefab : dungeonPrefabs.floorPrefab;
    }

    GameObject GetBossRoomFloorPrefab()
    {
        if (dungeonPrefabs == null) return null;
        return dungeonPrefabs.bossRoomFloorPrefab != null ? dungeonPrefabs.bossRoomFloorPrefab : dungeonPrefabs.floorPrefab;
    }

    GameObject GetWallPrefab()
    {
        if (dungeonPrefabs == null) return null;

        if (useAlternativeWalls && Random.Range(0f, 1f) < alternativePrefabChance)
        {
            return dungeonPrefabs.GetRandomWallPrefab();
        }
        return dungeonPrefabs.wallPrefab;
    }

    GameObject GetDoorPrefab()
    {
        if (dungeonPrefabs == null) return null;
        return dungeonPrefabs.doorPrefab;
    }

    GameObject GetBossRoomDoorPrefab()
    {
        if (dungeonPrefabs == null) return null;
        return dungeonPrefabs.bossRoomDoorPrefab != null ? dungeonPrefabs.bossRoomDoorPrefab : dungeonPrefabs.doorPrefab;
    }

    GameObject GetSpawnerPrefab()
    {
        if (dungeonPrefabs == null) return null;

        if (useRandomSpawnerVariants)
        {
            return dungeonPrefabs.GetRandomSpawnerPrefab();
        }
        return dungeonPrefabs.spawnerPrefab;
    }

    IEnumerator BuildObjectsFromList(List<Vector2Int> positions, GameObject prefab)
    {
        if (prefab == null || positions == null || positions.Count == 0)
            yield break;

        int index = 0;
        while (index < positions.Count)
        {
            int endIndex = Mathf.Min(index + blocksPerFrame, positions.Count);
            for (int i = index; i < endIndex; i++)
            {
                Vector2Int pos = positions[i];
                Vector3 worldPos = new Vector3(pos.x * spacing, pos.y * spacing, 0);

                // Dla œcian i prefabów alternatywnych, pobierz prefab dla ka¿dego obiektu osobno
                GameObject currentPrefab = prefab;
                if (positions == wallsToInstantiate)
                {
                    currentPrefab = GetWallPrefab();
                }
                else if (positions == floorsToInstantiate)
                {
                    currentPrefab = GetFloorPrefab();
                }

                GameObject obj = Instantiate(currentPrefab, worldPos, Quaternion.identity, transform);

                NetworkObject networkObj = obj.GetComponent<NetworkObject>();
                if (networkObj != null)
                {
                    networkObj.Spawn(true);
                }
            }
            index = endIndex;
            yield return new WaitForSeconds(blockBuildDelay);
        }
    }

    IEnumerator BuildSpawners()
    {
        if (dungeonPrefabs == null || dungeonPrefabs.spawnerPrefab == null || spawnersToInstantiate.Count == 0)
            yield break;

        Dictionary<Vector2Int, GameObject> spawnersAtPosition = new Dictionary<Vector2Int, GameObject>();
        int index = 0;

        while (index < spawnersToInstantiate.Count)
        {
            int endIndex = Mathf.Min(index + blocksPerFrame, spawnersToInstantiate.Count);
            for (int i = index; i < endIndex; i++)
            {
                Vector2Int pos = spawnersToInstantiate[i];

                if (spawnersAtPosition.ContainsKey(pos))
                {
                    GameObject existingSpawner = spawnersAtPosition[pos];
                    RoomSpawner existingRoomSpawner = existingSpawner.GetComponent<RoomSpawner>();
                    if (existingRoomSpawner != null)
                    {
                        existingRoomSpawner.spawnerLevel++;
                    }
                    continue;
                }

                Vector3 worldPos = new Vector3(pos.x * spacing, pos.y * spacing, spawnerHeight);
                GameObject spawnerPrefab = GetSpawnerPrefab();
                GameObject spawner = Instantiate(spawnerPrefab, worldPos, Quaternion.identity, transform);

                RoomSpawner roomSpawner = spawner.GetComponent<RoomSpawner>();
                if (roomSpawner != null && roomSizes.ContainsKey(pos))
                {
                    roomSpawner.roomSizeArea = roomSizes[pos];
                }

                NetworkObject networkObj = spawner.GetComponent<NetworkObject>();
                if (networkObj != null)
                {
                    networkObj.Spawn(true);
                }

                spawnersAtPosition[pos] = spawner;
            }
            index = endIndex;
            yield return new WaitForSeconds(blockBuildDelay);
        }
    }

    void OnDestroy()
    {
        if (generateDungeonButton != null)
        {
            generateDungeonButton.onClick.RemoveListener(OnGenerateDungeonButtonPressed);
        }
    }

    // Publiczne metody dostêpu
    public Vector2Int GetBossRoomCenter() => bossRoomCenter;
    public Vector2Int GetBossRoomEntrance() => bossRoomEntrance;
    public List<Vector2Int> GetRoomCenters() => new List<Vector2Int>(roomCenters);
    public List<Vector2Int> GetSpawnerPositions() => new List<Vector2Int>(spawnersToInstantiate);
    public Dictionary<Vector2Int, Vector2Int> GetRoomSizes() => new Dictionary<Vector2Int, Vector2Int>(roomSizes);
    public DungeonPrefabs GetDungeonPrefabs() => dungeonPrefabs;

    // Metoda do zmiany konfiguracji prefabów w runtime
    public void SetDungeonPrefabs(DungeonPrefabs newPrefabs)
    {
        dungeonPrefabs = newPrefabs;
        ValidatePrefabConfiguration();
    }
}