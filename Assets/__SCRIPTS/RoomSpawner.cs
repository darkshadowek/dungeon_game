using UnityEngine;
using System.Collections;

public class RoomSpawner : MonoBehaviour
{
    private GameObject dungeonGen;
    private int enemyStartCount;
    [SerializeField] GameObject chest;
    [SerializeField] GameObject[] monsters;
    [SerializeField] GameObject[] items;
    public Vector2 roomSizeArea;
    public int spawnerLevel;
    [SerializeField] Vector3 offset;

    void Start()
    {
        enemyStartCount = Random.Range(2, 6);
        dungeonGen = GameObject.FindGameObjectWithTag("DungeonGenerator");
        ChestSpawn();
        StartCoroutine(SpawnEnemyInRoom(1f, 0));
    }

    private void ChestSpawn()
    {
        GameObject chestInstance = Instantiate(chest, transform.position + offset, Quaternion.identity);
        // Skrzynia jest od razu aktywna w single player
    }

    IEnumerator SpawnEnemyInRoom(float time, int monsterType)
    {
        for (int i = 0; i < enemyStartCount * spawnerLevel; i++)
        {
            float randomX = Random.Range(-roomSizeArea.x + 3, roomSizeArea.x - 3);
            float randomY = Random.Range(-roomSizeArea.y + 3, roomSizeArea.y - 3);
            Vector3 spawnPos = transform.position + new Vector3(randomX, randomY, 0) + offset;

            GameObject monsterInstance = Instantiate(monsters[monsterType], spawnPos, Quaternion.identity);
            // Monster jest od razu aktywny w single player

            yield return new WaitForSeconds(time);
        }
    }
}