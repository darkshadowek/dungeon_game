using System.Collections;
using UnityEngine;

public class HandController : MonoBehaviour
{
    [Header("Hand Settings")]
    [SerializeField] float handDistance = 1.5f;
    public float radius = 5f;
    public float damage = 1;
    [SerializeField] GameObject player;
    [SerializeField] Transform handTransform;

    [Header("Joystick Max Position Detection")]
    [SerializeField][Range(0.8f, 1f)] float maxPositionThreshold = 0.95f; // Próg dla wykrywania maksymalnej pozycji
    public bool isAtMaxPosition = false;

    private RectTransform joystickBall;
    private RectTransform joystickCircle;
    private float joystickRadius;
    private bool canAttack = true;
    private PlayerBody playerBody;
    private Animator animator;

    void Start()
    {
        FindJoystick();
        handTransform.rotation *= Quaternion.Euler(0, 0, 90);
        playerBody = player.GetComponent<PlayerBody>();
        animator = GetComponent<Animator>();
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

    void Update()
    {
        if (player == null || handTransform == null) return;
        if (joystickBall == null || joystickRadius <= 0f)
        {
            FindJoystick();
            if (joystickBall == null) return;
        }

        Vector2 input = joystickBall.anchoredPosition / joystickRadius;
        input = Vector2.ClampMagnitude(input, 1f);

        // Sprawdzanie czy joystick jest w maksymalnej pozycji
        float inputMagnitude = input.magnitude;
        isAtMaxPosition = inputMagnitude >= maxPositionThreshold;

        if (input.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            Vector2 handOffset = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ) * handDistance;
            Vector3 newHandPosition = player.transform.position + new Vector3(handOffset.x, handOffset.y, 0f);
            handTransform.position = newHandPosition;
            handTransform.rotation = Quaternion.Euler(0, 0, angle);

            if (canAttack && isAtMaxPosition)
            {
                StartCoroutine(HandAttacking(playerBody.attackSpeed));
            }
        }
        else
        {
            // Gdy joystick nie jest u¿ywany, ustaw na false
            isAtMaxPosition = false;
        }
    }

    IEnumerator HandAttacking(float rate)
    {
        canAttack = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(handTransform.position, radius);
        foreach (Collider2D col in hits)
        {
            animator.Play("HandDemage");
            if (col.CompareTag("Enemy"))
            {
                IAttackable enemy = col.GetComponent<IAttackable>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
            }
        }
        yield return new WaitForSeconds(rate);
        canAttack = true;
    }
}
