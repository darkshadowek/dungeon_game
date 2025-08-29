using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    private GameObject boss;
    public Bosses bosses;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            Instantiate(bosses.bossesList[0], transform.position + new Vector3(0, 0, -1), Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
