using UnityEngine;
using Unity.Netcode;
public class PlayerMovement : NetworkBehaviour
{
    [Header("Player")]
    [SerializeField] float playerSpeed = 5f;
    [SerializeField] GameObject player;
    [SerializeField] Transform playerTransform;

    private RectTransform joystickBall;
    private RectTransform joystickCircle;
    private float joystickRadius;
    private SpriteRenderer spriteRend;
    private Animator playerAnimator;

    // NetworkVariables zamiast RPC
    private NetworkVariable<Vector3> networkPosition = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkIsMoving = new(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkFlipX = new(writePerm: NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        spriteRend = GetComponentInChildren<SpriteRenderer>();
        playerAnimator = GetComponentInChildren<Animator>();

        if (IsOwner)
        {
            FindJoystick();
        }

        // Subskrybuj zmiany NetworkVariables
        networkPosition.OnValueChanged += OnPositionChanged;
        networkIsMoving.OnValueChanged += OnMovingChanged;
        networkFlipX.OnValueChanged += OnFlipChanged;
    }

    private void FindJoystick()
    {
        GameObject joyBall = GameObject.FindGameObjectWithTag("JoyBall");
        GameObject joyCircle = GameObject.FindGameObjectWithTag("JoyCircle");

        if (joyBall) joystickBall = joyBall.GetComponent<RectTransform>();
        if (joyCircle) joystickCircle = joyCircle.GetComponent<RectTransform>();

        if (joystickBall && joystickCircle)
            joystickRadius = joystickCircle.sizeDelta.x * 0.5f;
    }

    private void OnPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        if (!IsOwner && playerTransform != null)
        {
            // Smooth interpolation dla płynniejszego ruchu
            if (Vector3.Distance(transform.position, newPos) > 0.1f)
            {
                transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * 15f);
            }
            else
            {
                transform.position = newPos;
            }
        }
    }

    private void OnMovingChanged(bool oldValue, bool newValue)
    {
        if (!IsOwner && playerAnimator != null)
            playerAnimator.SetBool("IsMoving", newValue);
    }

    private void OnFlipChanged(bool oldValue, bool newValue)
    {
        if (!IsOwner && spriteRend != null)
            spriteRend.flipX = newValue;
    }

    void Update()
    {
        if (!IsOwner || playerTransform == null) return;

        if (joystickBall == null || joystickRadius <= 0f)
        {
            FindJoystick();
            if (joystickBall == null) return;
        }

        // Pobierz input z joysticka (znormalizowany)
        Vector2 input = joystickBall.anchoredPosition / joystickRadius;
        input = Vector2.ClampMagnitude(input, 1f);

        // Sprawdź czy joystick jest używany
        bool isMoving = input.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            // Ruch lokalny
            Vector3 move = new Vector3(input.x, input.y, 0f) * playerSpeed * Time.deltaTime;
            transform.position += move;

            // Flip sprite
            bool shouldFlip = input.x > 0.01f;
            if (spriteRend != null && spriteRend.flipX != shouldFlip)
            {
                spriteRend.flipX = shouldFlip;
                networkFlipX.Value = shouldFlip;
            }

            // Aktualizuj pozycję w sieci
            networkPosition.Value = transform.position;
        }

        // Animacja lokalna
        if (playerAnimator != null)
            playerAnimator.SetBool("IsMoving", isMoving);

        // Aktualizuj stan animacji w sieci
        networkIsMoving.Value = isMoving;
    }

    // Metody pomocnicze do ustawiania referencji
    public void SetPlayerSpeed(float speed)
    {
        playerSpeed = speed;
    }

    public void SetPlayerReference(GameObject playerObj)
    {
        player = playerObj;
    }

    public void SetPlayerTransform(Transform playerTrans)
    {
        playerTransform = playerTrans;
    }
}