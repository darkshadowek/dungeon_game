using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Player")]
    public float playerSpeed = 5f;
    [SerializeField] GameObject player;
    [SerializeField] Transform playerTransform;

    private RectTransform joystickBall;
    private RectTransform joystickCircle;
    private float joystickRadius;
    private SpriteRenderer spriteRend;
    private Animator playerAnimator;

    void Start()
    {
        spriteRend = GetComponentInChildren<SpriteRenderer>();
        playerAnimator = GetComponentInChildren<Animator>();
        FindJoystick();
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

    void Update()
    {
        if (playerTransform == null) return;

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
            // Ruch
            Vector3 move = new Vector3(input.x, input.y, 0f) * playerSpeed * Time.deltaTime;
            transform.position += move;

            // Flip sprite
            bool shouldFlip = input.x > 0.01f;
            if (spriteRend != null)
            {
                spriteRend.flipX = shouldFlip;
            }
        }

        // Animacja
        if (playerAnimator != null)
            playerAnimator.SetBool("IsMoving", isMoving);
    }
}