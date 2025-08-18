using UnityEngine;

[CreateAssetMenu(fileName = "NewStageStructure", menuName = "Structure")]
public class StageStructure : ScriptableObject
{
    [Header("Parametry sieci")]
    public int networkNodesCount = 25;
    public int minTunnelWidth = 3;
    public int maxTunnelWidth = 5;
    public int minNodeDistance = 18;
    public int mapSize = 60;

    [Header("Parametry pokoi")]
    public int roomBranchChance = 80;
    public int roomBranchLength = 8;
    public int minRoomBranchWidth = 2;
    public int maxRoomBranchWidth = 4;
    public int minRoomSize = 5;
    public int maxRoomSize = 10;
    public int maxRoomsPerNode = 4;

    [Header("Pokój bosa")]
    public int bossRoomSize = 20;
    public int bossRoomCorridorWidth = 2;
    public int minDistanceFromCenter = 35;
    public int bossRoomTunnelBuffer = 2;

    [Header("Po³¹czenia")]
    public int extraConnectionsCount = 12;
    public int connectionChance = 75;
}
