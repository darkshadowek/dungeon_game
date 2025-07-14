using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class RoomSpawner : NetworkBehaviour
{
    private GameObject dungeonGen;
    private int enemyStartCount;
    [SerializeField] GameObject chest;
    [SerializeField] GameObject[] monsters;
    [SerializeField] GameObject[] items;
    public Vector2 roomSizeArea;
    public int spawnerLevel;
    [SerializeField] Vector3 offset;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            enemyStartCount = Random.Range(2, 6);
            dungeonGen = GameObject.FindGameObjectWithTag("DungeonGenerator");
            ChestSpawn();
            StartCoroutine(SpawnEnemyInRoom(1f, 0));
        }
    }

    private void ChestSpawn()
    {
        if (IsServer)
        {
            GameObject chestInstance = Instantiate(chest, transform.position + offset, Quaternion.identity);
            NetworkObject chestNetworkObject = chestInstance.GetComponent<NetworkObject>();
            if (chestNetworkObject != null)
            {
                chestNetworkObject.Spawn();
            }
        }
    }

    IEnumerator SpawnEnemyInRoom(float time, int monsterType)
    {
        if (!IsServer) yield break;

        for (int i = 0; i < enemyStartCount * spawnerLevel; i++)
        {
            float randomX = Random.Range(-roomSizeArea.x + 3, roomSizeArea.x -3);
            float randomY = Random.Range(-roomSizeArea.y + 3, roomSizeArea.y -3);
            Vector3 spawnPos = transform.position + new Vector3(randomX, randomY, 0) + offset;

            GameObject monsterInstance = Instantiate(monsters[monsterType], spawnPos, Quaternion.identity);
            NetworkObject monsterNetworkObject = monsterInstance.GetComponent<NetworkObject>();
            if (monsterNetworkObject != null)
            {
                monsterNetworkObject.Spawn();
            }

            yield return new WaitForSeconds(time);
        }
    }
}