using UnityEngine;

public class RoomSpawner : MonoBehaviour
{
    [SerializeField] GameObject chest;
    [SerializeField] GameObject[] monsters;
    public Vector2 roomSizeArea;
    public float spawnerLevel;
    [SerializeField] float chestDropRate;
}
