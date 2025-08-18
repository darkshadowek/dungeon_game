using UnityEngine;

public class Teleport : MonoBehaviour
{
    private DungeonGenerator dunGenerator;

    private void Start()
    {
        dunGenerator = GameObject.FindFirstObjectByType<DungeonGenerator>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StartCoroutine(BuildDung());
        }
    }

    private System.Collections.IEnumerator BuildDung()
    {
        Debug.Log("Teleport triggered!");

        dunGenerator.DestroyDungeon();
        yield return null;
        yield return new WaitForSeconds(2);
        GameManager.GameManagerInsance.GeneratedungeonCall();

        Destroy(gameObject);
    }
}
