using Unity.Netcode;
using UnityEngine;

public class HandController : NetworkBehaviour
{
    [Header("Hand Settings")]
    [SerializeField] float handDistance = 1.5f;
    [SerializeField] GameObject player;
    [SerializeField] Transform handTransform;

    private RectTransform joystickBall;
    private RectTransform joystickCircle;
    private float joystickRadius;

    private NetworkVariable<Vector3> networkHandPosition = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> networkHandAngle = new(writePerm: NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            FindJoystick();
        }

        networkHandPosition.OnValueChanged += OnHandPositionChanged;
        networkHandAngle.OnValueChanged += OnHandAngleChanged;
    }

    private void FindJoystick()
    {
        var joyBallObj = GameObject.FindGameObjectWithTag("JoyBall1");
        var joyCircleObj = GameObject.FindGameObjectWithTag("JoyCircle1");

        if (joyBallObj) joystickBall = joyBallObj.GetComponent<RectTransform>();
        if (joyCircleObj) joystickCircle = joyCircleObj.GetComponent<RectTransform>();

        if (joystickBall && joystickCircle)
            joystickRadius = joystickCircle.sizeDelta.x * 0.5f;
    }

    private void OnHandPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        if (!IsOwner && handTransform != null)
            handTransform.position = newPos;
    }

    private void OnHandAngleChanged(float oldAngle, float newAngle)
    {
        if (!IsOwner && handTransform != null)
            handTransform.rotation = Quaternion.Euler(0, 0, newAngle);
    }

    void Update()
    {
        if (!IsOwner || player == null || handTransform == null) return;

        if (joystickBall == null || joystickRadius <= 0f)
        {
            FindJoystick();
            if (joystickBall == null) return;
        }

        // Pobierz input z joysticka (znormalizowany)
        Vector2 input = joystickBall.anchoredPosition / joystickRadius;
        input = Vector2.ClampMagnitude(input, 1f);

        // SprawdŸ czy joystick jest u¿ywany (ma jakiœ input)
        if (input.sqrMagnitude > 0.01f)
        {
            // Oblicz k¹t na podstawie pozycji joysticka
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;

            // Oblicz pozycjê rêki dooko³a postaci
            Vector2 handOffset = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ) * handDistance;

            // Ustaw pozycjê rêki wzglêdem postaci
            Vector3 newHandPosition = player.transform.position + new Vector3(handOffset.x, handOffset.y, 0f);
            handTransform.position = newHandPosition;
            handTransform.rotation = Quaternion.Euler(0, 0, angle);

            // Aktualizuj network variables
            networkHandPosition.Value = newHandPosition;
            networkHandAngle.Value = angle;
        }
        // Jeœli joystick nie jest u¿ywany, rêka zostaje w ostatniej pozycji
    }

    // Metody pomocnicze do ustawiania referencji
    public void SetHandDistance(float distance)
    {
        handDistance = distance;
    }

    public void SetPlayerReference(GameObject playerObj)
    {
        player = playerObj;
    }

    public void SetHandTransform(Transform hand)
    {
        handTransform = hand;
    }
}