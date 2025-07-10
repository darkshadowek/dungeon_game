using UnityEngine;
using Unity.Netcode;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);
    private Transform target;

    private void Start()
    {
        // Kamera musi być podpięta pod gracza z NetworkBehaviour
        var player = GetComponentInParent<NetworkBehaviour>();
        if (player == null || !player.IsOwner)
        {
            // Nie jesteś właścicielem tego gracza → wyłącz kamerę
            Camera cam = GetComponent<Camera>();
            if (cam) cam.enabled = false;

            AudioListener listener = GetComponent<AudioListener>();
            if (listener) listener.enabled = false;

            enabled = false; // wyłącz CameraMovement
            return;
        }

        // Tylko lokalny gracz uruchamia FindLocalPlayer
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
        // Szukamy tylko raz na starcie lokalnego właściciela
        target = transform.root; // gracz, do którego podpięta jest kamera
    }
}
