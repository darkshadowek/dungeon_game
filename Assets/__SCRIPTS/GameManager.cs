using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager GameManagerInsance;
    private int floorLevel = 0;
    private int Stage = 1;
    private DungeonGenerator dungeonGenerator;
    [SerializeField] private Bosses boss;
    public StageStructure[] stages;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        GameManagerInsance = this;
        Application.targetFrameRate = 90;       
    }
    private void Start()
    {
        dungeonGenerator = GameObject.FindFirstObjectByType<DungeonGenerator>();
    }
    public void GeneratedungeonCall()
    {
        StartCoroutine(dungeonGenerator.GenerateDungeonOverTime());
    }
    public void CreateDungeon()
    {
        if (floorLevel % 10 == 0 && floorLevel % 100 != 0)
        {
            //"boss"
        }
        else if (floorLevel % 10 == 0)
        {
            dungeonGenerator.generateBossRoom = true;
            Stage++;
        }
        else
        {
            if (Stage >= 0 && Stage < stages.Length)
            {
                StageChange(Stage);
            }
        }
    }

    private void StageChange(int value)
    {
        var stage = stages[value];

        dungeonGenerator.getNetworkParametrs(
            stage.networkNodesCount,
            stage.minTunnelWidth,
            stage.maxTunnelWidth,
            stage.minNodeDistance,
            stage.mapSize
        );

        dungeonGenerator.getRoomsParametrs(
            stage.roomBranchChance,
            stage.roomBranchLength,
            stage.minRoomBranchWidth,
            stage.maxRoomBranchWidth,
            stage.minRoomSize,
            stage.maxRoomSize,
            stage.maxRoomsPerNode
        );

        dungeonGenerator.getBossRoomParametrs(
            stage.bossRoomSize,
            stage.bossRoomCorridorWidth,
            stage.minDistanceFromCenter,
            stage.bossRoomTunnelBuffer
        );

        dungeonGenerator.getConnectParametrs(
            stage.extraConnectionsCount,
            stage.connectionChance
        );
    }

}
