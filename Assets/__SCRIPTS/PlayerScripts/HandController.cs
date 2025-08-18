using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HandController : MonoBehaviour
{
    [Header("Hand Settings")]
    [SerializeField] float handDistance = 1.5f;
    [SerializeField] GameObject player;
    [SerializeField] GameObject handTransform;

    [Header("Weapon")]
    public GameObject currentWeapon; // Upewnij siê ¿e to jest publiczne

    [Header("Pickup Settings")]
    [SerializeField] Button pickupButton;
    [SerializeField] float pickupRadius = 2f;

    [Header("Joystick Max Position Detection")]
    [SerializeField][Range(0.8f, 1f)] float maxPositionThreshold = 0.95f;
    public bool isAtMaxPosition = false;
    private RectTransform joystickBall;
    private RectTransform joystickCircle;
    private float joystickRadius;
    private bool canAttack = true;
    private PlayerBody playerBody;
    private Animator animator;

    private float swordDemage = 0;

    public Button showInventory;
    
    private Transform chestInventory;
    public ChestInventory chestInventoryUI;
    public Chest chest;
    void Start()
    {
        FindJoystick();
        playerBody = player.GetComponent<PlayerBody>();
        animator = GetComponent<Animator>();
        chestInventory = GameObject.FindGameObjectWithTag("ChestGameObject").transform;
        chestInventoryUI = chestInventory.GetComponent<ChestInventory>();
        if (pickupButton != null)
        {
            pickupButton.onClick.AddListener(PickupItems);
        }     
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
            handTransform.transform.position = newHandPosition;
            handTransform.transform.rotation = Quaternion.Euler(0, 0, angle);

            if (canAttack && isAtMaxPosition)
            {
                StartCoroutine(HandAttacking(playerBody.attackSpeed));
            }
        }
        else
        {
            isAtMaxPosition = false;
        }
    }

    public void PickupItems()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius);
        foreach (Collider2D col in hits)
        {
            if (col.CompareTag("Player") || col.CompareTag("Enemy") || col.CompareTag("NoPickup"))
                continue;
            if(col.CompareTag("Chest"))
            {
                chest = col.GetComponent<Chest>();
                ChestInventoryopen();
                chestInventoryUI.SynchronizeItemsWithChest(chest);
                showInventory.onClick.Invoke();
                
                break;
            }
            if (col != null && col.CompareTag("Item") && col.gameObject.layer != LayerMask.NameToLayer("IgnorePickup"))
            {
                ObjectItem objectItem = col.GetComponent<ObjectItem>();
                if (objectItem != null)
                {
                    objectItem.PickUp();
                    return;
                }
            }
        }
    }

    IEnumerator HandAttacking(float rate)
    {
        canAttack = false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(handTransform.transform.position, PlayerBody.PlayerInstance.radius);
        foreach (Collider2D col in hits)
        {
            if (currentWeapon)
            {
                animator.Play("HandDemage");
            }
            else
            {
                animator.Play("HandDemage");
            }
            if (col.CompareTag("Enemy"))
            {
                IAttackable enemy = col.GetComponent<IAttackable>();
                if (enemy != null)
                {
                    enemy.TakeDamage(PlayerBody.PlayerInstance.damage + swordDemage);
                }
            }
        }
        yield return new WaitForSeconds(rate);
        canAttack = true;
    }

    // Metoda do ustawiania broni
    public void SetWeapon(GameObject weaponPrefab)
    {
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
        }

        // Stwórz now¹ broñ
        if (weaponPrefab != null)
        {
            currentWeapon = Instantiate(weaponPrefab, handTransform.transform);
            Sword swordScript = currentWeapon.GetComponent<Sword>();
            swordScript.holdingWeapon = true;
            swordScript.changeLayer();
            swordDemage = swordScript.swordData.damage;
            currentWeapon.transform.localPosition = Vector3.zero;
            currentWeapon.transform.localRotation = Quaternion.identity;
            currentWeapon.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (handTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(handTransform.transform.position, PlayerBody.PlayerInstance.radius);
        }

        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(handTransform.transform.position, pickupRadius);
        }
    }
    private void ChestInventoryopen()
    {
        foreach (Transform child in chestInventory)
        {
            child.gameObject.SetActive(true);
        }
    }
}
