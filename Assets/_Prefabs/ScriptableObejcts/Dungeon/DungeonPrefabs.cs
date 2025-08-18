using UnityEngine;

[CreateAssetMenu(fileName = "New Dungeon Prefabs", menuName = "Dungeon System/Prefabs Configuration", order = 1)]

public class DungeonPrefabs : ScriptableObject
{
    [Header("Podstawowe prefaby")]
    public GameObject floorPrefab;
    public GameObject roomFloorPrefab;
    public GameObject wallPrefab;
    public GameObject bossRoomFloorPrefab;
    public GameObject doorPrefab;
    public GameObject spawnerPrefab;

    [Header("Dodatkowe prefaby")]
    public GameObject[] alternativeFloorPrefabs;
    public GameObject[] alternativeWallPrefabs;
    public GameObject[] specialRoomPrefabs;

    [Header("Drzwi specjalne")]
    public GameObject bossRoomDoorPrefab;
    public GameObject lockedDoorPrefab;
    public GameObject trapDoorPrefab;

    [Header("Spawner variants")]
    public GameObject[] spawnerVariants;

    // Metody pomocnicze
    public GameObject GetRandomFloorPrefab()
    {
        if (alternativeFloorPrefabs != null && alternativeFloorPrefabs.Length > 0)
        {
            return alternativeFloorPrefabs[Random.Range(0, alternativeFloorPrefabs.Length)];
        }
        return floorPrefab;
    }

    public GameObject GetRandomWallPrefab()
    {
        if (alternativeWallPrefabs != null && alternativeWallPrefabs.Length > 0)
        {
            return alternativeWallPrefabs[Random.Range(0, alternativeWallPrefabs.Length)];
        }
        return wallPrefab;
    }

    public GameObject GetRandomSpawnerPrefab()
    {
        if (spawnerVariants != null && spawnerVariants.Length > 0)
        {
            return spawnerVariants[Random.Range(0, spawnerVariants.Length)];
        }
        return spawnerPrefab;
    }
}