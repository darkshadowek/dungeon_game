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

    [Header("Parametry sieci")]
    [SerializeField] int networkNodesCount = 25; // Iloœæ wêz³ów w sieci
    [SerializeField] int tunnelLength = 8;
    [SerializeField] int minTunnelWidth = 3;
    [SerializeField] int maxTunnelWidth = 5;
    [SerializeField] float spacing = 1.5f;
    [SerializeField] int minNodeDistance = 18; // Minimalna odleg³oœæ miêdzy wêz³ami sieci
    [SerializeField] int mapSize = 60; // Rozmiar mapy (od -mapSize do +mapSize)

    [Header("Parametry odstaj¹cych pokoi")]
    [SerializeField] int roomBranchChance = 80; // Szansa na pokój odstaj¹cy od wêz³a
    [SerializeField] int roomBranchLength = 8; // D³ugoœæ ga³êzi do pokoju
    [SerializeField] int minRoomBranchWidth = 2; // Minimalna szerokoœæ korytarza do pokoju
    [SerializeField] int maxRoomBranchWidth = 4; // Maksymalna szerokoœæ korytarza do pokoju
    [SerializeField] int minRoomSize = 5;
    [SerializeField] int maxRoomSize = 10;
    [SerializeField] int maxRoomsPerNode = 4; // Maksymalna iloœæ pokoi na wêze³

    [Header("Parametry pokoju bosa")]
    [SerializeField] int bossRoomSize = 20; // Rozmiar pokoju bosa
    [SerializeField] int bossRoomCorridorLength = 15; // D³ugoœæ korytarza do pokoju bosa
    [SerializeField] int bossRoomCorridorWidth = 2; // Szerokoœæ korytarza do pokoju bosa
    [SerializeField] int minDistanceFromCenter = 35; // Minimalna odleg³oœæ pokoju bosa od œrodka
    [SerializeField] int bossRoomTunnelBuffer = 2; // Minimalna odleg³oœæ tuneli od pokoju bosa

    [Header("Parametry po³¹czeñ sieci")]
    [SerializeField] int extraConnectionsCount = 12; // Dodatkowe po³¹czenia w sieci
    [SerializeField] int connectionChance = 75; // Szansa na dodatkowe po³¹czenie

    private HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> roomPositions = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> bossRoomPositions = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> bossRoomCorridorPositions = new HashSet<Vector2Int>();
    private List<Vector2Int> networkNodes = new List<Vector2Int>(); // Wêz³y sieci
    private List<Vector2Int> roomCenters = new List<Vector2Int>();
    private Dictionary<Vector2Int, List<Vector2Int>> nodeConnections = new Dictionary<Vector2Int, List<Vector2Int>>();
    private Vector2Int bossRoomCenter;
    private Vector2Int bossRoomEntrance;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GenerateCaveNetwork();
        }
    }

    void GenerateCaveNetwork()
    {
        StartCoroutine(GenerateDungeonOverTime());
    }

    IEnumerator GenerateDungeonOverTime()
    {
        Debug.Log("Rozpoczynam generowanie dungeonu...");

        // 1. NAJPIERW generuj pokój bosa
        Debug.Log("Generujê pokój bosa...");
        GenerateBossRoom();
        yield return new WaitForSeconds(0.5f);

        // 2. Generuj wêz³y sieci (z uwzglêdnieniem pokoju bosa)
        Debug.Log("Generujê wêz³y sieci...");
        GenerateNetworkNodes();
        yield return new WaitForSeconds(1f);

        // 3. Po³¹cz wêz³y w sieæ
        Debug.Log("£¹czê wêz³y w sieæ...");
        ConnectNetworkNodes();
        yield return new WaitForSeconds(1f);

        // 4. Stwórz dodatkowe po³¹czenia dla lepszej sieci
        Debug.Log("Tworzê dodatkowe po³¹czenia...");
        CreateExtraConnections();
        yield return new WaitForSeconds(1f);

        // 5. Generuj tunele miêdzy wêz³ami (z uwzglêdnieniem pokoju bosa)
        Debug.Log("Generujê tunele miêdzy wêz³ami...");
        GenerateNetworkTunnels();
        yield return new WaitForSeconds(1f);

        // 6. Dodaj odstaj¹ce pokoje
        Debug.Log("Dodajê odstaj¹ce pokoje...");
        GenerateProtrudingRooms();
        yield return new WaitForSeconds(0.5f);

        // 7. Instantiate objects
        Debug.Log("Tworzê pod³ogi...");
        InstantiateFloors();
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Generujê œciany...");
        GenerateWalls();

        Debug.Log("Dodajê drzwi do pokoju bosa...");
        GenerateBossRoomDoors();

        Debug.Log("Generowanie dungeonu zakoñczone!");
    }

    void GenerateBossRoom()
    {
        // ZnajdŸ pozycjê dla pokoju bosa daleko od œrodka
        bossRoomCenter = FindBossRoomPosition();

        // Stwórz pokój bosa
        CreateBossRoom(bossRoomCenter);

        Debug.Log($"Pokój bosa utworzony na pozycji: {bossRoomCenter}");
    }

    Vector2Int FindBossRoomPosition()
    {
        Vector2Int candidate;
        int attempts = 0;

        do
        {
            // Generuj pozycjê z dala od œrodka
            float angle = Random.Range(0f, 2f * Mathf.PI);
            float distance = Random.Range(minDistanceFromCenter, mapSize - bossRoomSize / 2);

            candidate = new Vector2Int(
                Mathf.RoundToInt(distance * Mathf.Cos(angle)),
                Mathf.RoundToInt(distance * Mathf.Sin(angle))
            );
            attempts++;
        }
        while (!IsValidBossRoomPosition(candidate) && attempts < 100);

        return candidate;
    }

    bool IsValidBossRoomPosition(Vector2Int pos)
    {
        // Na pocz¹tku nie ma jeszcze innych struktur, wiêc tylko sprawdzamy granice mapy
        int halfSize = bossRoomSize / 2;
        int buffer = bossRoomTunnelBuffer + 3; // Dodatkowy bufor

        // SprawdŸ czy pokój zmieœci siê w granicach mapy
        if (pos.x - halfSize - buffer < -mapSize || pos.x + halfSize + buffer > mapSize ||
            pos.y - halfSize - buffer < -mapSize || pos.y + halfSize + buffer > mapSize)
        {
            return false;
        }

        return true;
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
    }

    void GenerateNetworkNodes()
    {
        // Pierwszy wêze³ w centrum (jeœli nie koliduje z pokojem bosa)
        Vector2Int centerNode = Vector2Int.zero;
        if (IsValidNodePosition(centerNode))
        {
            networkNodes.Add(centerNode);
            nodeConnections[centerNode] = new List<Vector2Int>();
        }

        // Generuj pozosta³e wêz³y
        while (networkNodes.Count < networkNodesCount)
        {
            Vector2Int newNode = FindValidNodePosition();
            if (newNode != Vector2Int.zero || !networkNodes.Contains(Vector2Int.zero))
            {
                networkNodes.Add(newNode);
                nodeConnections[newNode] = new List<Vector2Int>();
            }
        }
    }

    Vector2Int FindValidNodePosition()
    {
        Vector2Int candidate;
        int attempts = 0;

        do
        {
            candidate = new Vector2Int(
                Random.Range(-mapSize, mapSize + 1),
                Random.Range(-mapSize, mapSize + 1)
            );
            attempts++;
        }
        while (!IsValidNodePosition(candidate) && attempts < 1000);

        return candidate;
    }

    bool IsValidNodePosition(Vector2Int pos)
    {
        // SprawdŸ odleg³oœæ od innych wêz³ów
        foreach (Vector2Int existingNode in networkNodes)
        {
            if (Vector2Int.Distance(pos, existingNode) < minNodeDistance)
                return false;
        }

        // SprawdŸ odleg³oœæ od pokoju bosa (z buforem)
        if (IsTooCloseToBossRoom(pos))
            return false;

        return true;
    }

    bool IsTooCloseToBossRoom(Vector2Int pos)
    {
        if (bossRoomPositions.Count == 0) return false;

        int halfSize = bossRoomSize / 2;
        int buffer = bossRoomTunnelBuffer + minNodeDistance; // Wiêkszy bufor dla wêz³ów

        // SprawdŸ czy wêze³ jest za blisko pokoju bosa
        float distanceToBossCenter = Vector2Int.Distance(pos, bossRoomCenter);
        return distanceToBossCenter < (halfSize + buffer);
    }

    void ConnectNetworkNodes()
    {
        // Po³¹cz ka¿dy wêze³ z najbli¿szymi wêz³ami (minimum 2-4 po³¹czenia)
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

        // Po³¹cz pokój bosa z najbli¿szym wêz³em
        ConnectBossRoomToNetwork();
    }

    void ConnectBossRoomToNetwork()
    {
        if (networkNodes.Count == 0) return;

        // ZnajdŸ najbli¿szy wêze³ do pokoju bosa
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

        // Stwórz korytarz do pokoju bosa
        CreateBossRoomCorridor(nearestNode, bossRoomCenter);
    }

    void CreateBossRoomCorridor(Vector2Int from, Vector2Int to)
    {
        Vector2Int current = from;
        Vector2Int direction = GetDirectionTo(from, to);

        // Stwórz korytarz do pokoju bosa
        while (Vector2Int.Distance(current, to) > bossRoomSize / 2 + 2)
        {
            current += direction;

            // Dodaj segment korytarza o szerokoœci okreœlonej w parametrach
            for (int w = -bossRoomCorridorWidth / 2; w <= bossRoomCorridorWidth / 2; w++)
            {
                Vector2Int offset = (direction.x != 0)
                    ? new Vector2Int(0, w)
                    : new Vector2Int(w, 0);

                Vector2Int corridorPos = current + offset;
                floorPositions.Add(corridorPos);
                bossRoomCorridorPositions.Add(corridorPos);
            }
        }

        // Zapisz pozycjê wejœcia do pokoju bosa
        bossRoomEntrance = current;
    }

    Vector2Int GetDirectionTo(Vector2Int from, Vector2Int to)
    {
        Vector2Int diff = to - from;

        // Wybierz g³ówny kierunek
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            return new Vector2Int(diff.x > 0 ? 1 : -1, 0);
        }
        else
        {
            return new Vector2Int(0, diff.y > 0 ? 1 : -1);
        }
    }

    List<Vector2Int> FindNearestNodes(Vector2Int fromNode, int count)
    {
        List<Vector2Int> candidates = new List<Vector2Int>(networkNodes);
        candidates.Remove(fromNode);

        candidates.Sort((a, b) =>
            Vector2Int.Distance(fromNode, a).CompareTo(Vector2Int.Distance(fromNode, b))
        );

        return candidates.GetRange(0, Mathf.Min(count, candidates.Count));
    }

    void CreateExtraConnections()
    {
        for (int i = 0; i < extraConnectionsCount; i++)
        {
            Vector2Int nodeA = networkNodes[Random.Range(0, networkNodes.Count)];
            Vector2Int nodeB = networkNodes[Random.Range(0, networkNodes.Count)];

            if (nodeA != nodeB && !nodeConnections[nodeA].Contains(nodeB) &&
                Random.Range(0, 100) < connectionChance)
            {
                nodeConnections[nodeA].Add(nodeB);
                nodeConnections[nodeB].Add(nodeA);
            }
        }
    }

    void GenerateNetworkTunnels()
    {
        HashSet<string> processedConnections = new HashSet<string>();

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
        // Sortuj pozycje ¿eby unikn¹æ duplikatów (a-b i b-a to to samo)
        Vector2Int first = (a.x < b.x || (a.x == b.x && a.y < b.y)) ? a : b;
        Vector2Int second = (first == a) ? b : a;
        return $"{first.x},{first.y}-{second.x},{second.y}";
    }

    void CreateTunnel(Vector2Int from, Vector2Int to)
    {
        Vector2Int current = from;
        int width = Random.Range(minTunnelWidth, maxTunnelWidth + 1);

        // IdŸ w poziomie
        while (current.x != to.x)
        {
            current.x += (current.x < to.x) ? 1 : -1;
            AddTunnelSegment(current, width, true);
        }

        // IdŸ w pionie
        while (current.y != to.y)
        {
            current.y += (current.y < to.y) ? 1 : -1;
            AddTunnelSegment(current, width, false);
        }
    }

    void AddTunnelSegment(Vector2Int center, int width, bool horizontal)
    {
        // SprawdŸ kolizje z pokojem bosa przed dodaniem segmentu tunelu
        if (IsTooCloseToBossRoom(center, width, horizontal))
            return;

        for (int w = -width / 2; w <= width / 2; w++)
        {
            Vector2Int offset = horizontal
                ? new Vector2Int(0, w)
                : new Vector2Int(w, 0);

            floorPositions.Add(center + offset);
        }
    }

    bool IsTooCloseToBossRoom(Vector2Int center, int width, bool horizontal)
    {
        // SprawdŸ czy segment tunelu jest za blisko pokoju bosa
        if (bossRoomPositions.Count == 0) return false;

        for (int w = -width / 2; w <= width / 2; w++)
        {
            Vector2Int offset = horizontal
                ? new Vector2Int(0, w)
                : new Vector2Int(w, 0);

            Vector2Int checkPos = center + offset;

            // SprawdŸ czy pozycja jest za blisko pokoju bosa
            foreach (Vector2Int bossPos in bossRoomPositions)
            {
                if (Vector2Int.Distance(checkPos, bossPos) < bossRoomTunnelBuffer)
                    return true;
            }
        }

        return false;
    }

    void GenerateProtrudingRooms()
    {
        foreach (Vector2Int node in networkNodes)
        {
            int roomsForThisNode = Random.Range(1, maxRoomsPerNode + 1);

            for (int i = 0; i < roomsForThisNode; i++)
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
        // SprawdŸ czy wêze³ nie jest za blisko pokoju bosa
        if (IsTooCloseToBossRoom(nodePos))
            return;

        // Wybierz losowy kierunek dla ga³êzi
        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        Vector2Int direction = directions[Random.Range(0, directions.Length)];

        // Stwórz ga³¹Ÿ do pokoju z gwarantowan¹ szerokoœci¹ minimum 2
        Vector2Int branchPos = nodePos;
        int branchWidth = Random.Range(minRoomBranchWidth, maxRoomBranchWidth + 1);

        // Upewnij siê, ¿e szerokoœæ jest co najmniej 2
        if (branchWidth < 2) branchWidth = 2;

        for (int i = 0; i < roomBranchLength; i++)
        {
            branchPos += direction;

            // SprawdŸ kolizje z pokojem bosa
            if (IsTooCloseToBossRoom(branchPos))
                return;

            // Dodaj segment ga³êzi z odpowiedni¹ szerokoœci¹
            if (direction.x != 0 && direction.y == 0) // Poziomo
            {
                for (int w = -branchWidth / 2; w <= branchWidth / 2; w++)
                {
                    Vector2Int pos = branchPos + new Vector2Int(0, w);
                    if (!IsTooCloseToBossRoom(pos))
                        floorPositions.Add(pos);
                }
            }
            else if (direction.x == 0 && direction.y != 0) // Pionowo
            {
                for (int w = -branchWidth / 2; w <= branchWidth / 2; w++)
                {
                    Vector2Int pos = branchPos + new Vector2Int(w, 0);
                    if (!IsTooCloseToBossRoom(pos))
                        floorPositions.Add(pos);
                }
            }
            else // Przek¹tnie - twórz kwadratowy tunel
            {
                int halfWidth = branchWidth / 2;
                for (int x = -halfWidth; x <= halfWidth; x++)
                {
                    for (int y = -halfWidth; y <= halfWidth; y++)
                    {
                        Vector2Int pos = branchPos + new Vector2Int(x, y);
                        if (!IsTooCloseToBossRoom(pos))
                            floorPositions.Add(pos);
                    }
                }
            }
        }

        // Stwórz pokój na koñcu ga³êzi
        Vector2Int roomCenter = branchPos + direction * 2;
        if (!IsTooCloseToBossRoom(roomCenter))
        {
            CreateRoom(roomCenter);
        }
    }

    void CreateRoom(Vector2Int center)
    {
        int roomWidth = Random.Range(minRoomSize, maxRoomSize + 1);
        int roomHeight = Random.Range(minRoomSize, maxRoomSize + 1);

        roomCenters.Add(center);

        for (int x = -roomWidth / 2; x <= roomWidth / 2; x++)
        {
            for (int y = -roomHeight / 2; y <= roomHeight / 2; y++)
            {
                Vector2Int roomPos = center + new Vector2Int(x, y);

                // SprawdŸ kolizje z pokojem bosa przed dodaniem pozycji pokoju
                if (!IsTooCloseToBossRoom(roomPos))
                {
                    floorPositions.Add(roomPos);
                    roomPositions.Add(roomPos);
                }
            }
        }
    }

    void InstantiateFloors()
    {
        // Generuj korytarze (zwyk³e pod³ogi)
        foreach (Vector2Int pos in floorPositions)
        {
            if (!roomPositions.Contains(pos) && !bossRoomPositions.Contains(pos))
            {
                Vector3 worldPos = new Vector3(pos.x * spacing, pos.y * spacing, 0);
                GameObject floor = Instantiate(floorPrefab, worldPos, Quaternion.identity);
                floor.GetComponent<NetworkObject>().Spawn(true);
                floor.transform.SetParent(transform);
            }
        }

        // Generuj pokoje (specjalne pod³ogi)
        foreach (Vector2Int pos in roomPositions)
        {
            Vector3 worldPos = new Vector3(pos.x * spacing, pos.y * spacing, 0);
            GameObject roomFloor = Instantiate(roomFloorPrefab, worldPos, Quaternion.identity);
            roomFloor.GetComponent<NetworkObject>().Spawn(true);
            roomFloor.transform.SetParent(transform);
        }

        // Generuj pokój bosa (specjalne pod³ogi)
        foreach (Vector2Int pos in bossRoomPositions)
        {
            Vector3 worldPos = new Vector3(pos.x * spacing, pos.y * spacing, 0);
            GameObject bossFloor = Instantiate(bossRoomFloorPrefab, worldPos, Quaternion.identity);
            bossFloor.GetComponent<NetworkObject>().Spawn(true);
            bossFloor.transform.SetParent(transform);
        }
    }

    void GenerateWalls()
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right,
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        HashSet<Vector2Int> wallsPlaced = new HashSet<Vector2Int>();

        foreach (Vector2Int pos in floorPositions)
        {
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = pos + dir;
                if (!floorPositions.Contains(neighbor) && !wallsPlaced.Contains(neighbor))
                {
                    // Postaw zwyk³¹ œcianê
                    Vector3 wallPos = new Vector3(neighbor.x * spacing, neighbor.y * spacing, 0);
                    GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.identity);
                    wall.GetComponent<NetworkObject>().Spawn(true);
                    wall.transform.SetParent(transform);
                    wallsPlaced.Add(neighbor);
                }
            }
        }
    }

    void GenerateBossRoomDoors()
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

        HashSet<Vector2Int> doorPositions = new HashSet<Vector2Int>();

        // SprawdŸ ka¿d¹ pozycjê pokoju bosa
        foreach (Vector2Int bossPos in bossRoomPositions)
        {
            // SprawdŸ wszystkie 4 kierunki (nie przek¹tne)
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = bossPos + dir;

                // SprawdŸ czy s¹siad to zwyk³a pod³oga (nie pokój bosa, nie pokój zwyk³y)
                if (floorPositions.Contains(neighbor) &&
                    !bossRoomPositions.Contains(neighbor) &&
                    !roomPositions.Contains(neighbor) &&
                    !doorPositions.Contains(neighbor))
                {
                    // Postaw drzwi na granicy
                    Vector3 doorPos = new Vector3(neighbor.x * spacing, neighbor.y * spacing, 0);
                    GameObject door = Instantiate(doorPrefab, doorPos, Quaternion.identity);
                    door.GetComponent<NetworkObject>().Spawn(true);
                    door.transform.SetParent(transform);
                     
                    doorPositions.Add(neighbor);
                    Debug.Log($"Drzwi do pokoju bosa na pozycji: {neighbor}");
                }
            }
        }
    }

    // Metoda pomocnicza do debugowania - mo¿esz j¹ wywo³aæ ¿eby zobaczyæ pozycjê pokoju bosa
    public Vector2Int GetBossRoomCenter()
    {
        return bossRoomCenter;
    }

    public Vector2Int GetBossRoomEntrance()
    {
        return bossRoomEntrance;
    }
}