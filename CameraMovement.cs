using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);
    private Transform target;

    private void Start()
    {
        FindLocalPlayer();
    }

    private void Update()
    {
        if (target == null) return;
        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }

    private void FindLocalPlayer()
    {
        target = transform.root; // gracz, do którego podpięta jest kamera
    }
}
