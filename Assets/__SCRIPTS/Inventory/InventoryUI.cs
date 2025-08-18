using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;

public class InventoryUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    public UnityEngine.UI.Image icon;
    public Item item;
    public TextMeshProUGUI quantityText;
    public int quantity = 1;
    public int maxStack = 99;
    private Transform originalParent;
    public Vector3 originalPosition;
    private CanvasGroup canvasGroup;
    private GameObject handPrefab;
    private GameObject dragClone;
    private RectTransform dragCloneTransform;
    private GameObject inventoryPanel;
    
    private Item draggedItem;
    private Sword swordScript;
    private int draggedQuantity;

    [SerializeField] private bool slotForWeapon;
    public HandController playerhand;
    
    void Awake()
    {
        icon.color = new Color(1, 1, 1, 0);
    }
    
    private void Start()
    {
        inventoryPanel = GameObject.FindGameObjectWithTag("InventoryPanel");
        handPrefab = GameObject.FindGameObjectWithTag("PlayerHand");
        playerhand = handPrefab.GetComponent<HandController>();
    }
    
    public void SetItem(Item newItem, int count = 1)
    {
        item = newItem;
        quantity = Mathf.Min(count, maxStack);
        icon.sprite = item.icon;
        icon.color = Color.white; // Fully visible
        UpdateQuantity();
    }
    
    public void AddItem(int count)
    {
        quantity = Mathf.Min(quantity + count, maxStack);
        UpdateQuantity();
    }
    
    void UpdateQuantity()
    {
        quantityText.text = quantity > 1 ? quantity.ToString() : "";
        if (quantity == 0)
        {
            item = null;
            icon.sprite = null;
            icon.color = new Color(255,255,255,0);
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;
        
        originalParent = transform.parent;
        originalPosition = transform.position;
        
        // Przechowaj dane przeci¹ganego przedmiotu
        draggedItem = item;
        draggedQuantity = quantity;
        
        // Create clone
        dragClone = Instantiate(gameObject, transform.root);
        dragClone.transform.position = transform.position;
        dragCloneTransform = dragClone.GetComponent<RectTransform>();
        dragCloneTransform.sizeDelta = new Vector2(100, 100);
        
        // Set item data to clone
        InventoryUI cloneUI = dragClone.GetComponent<InventoryUI>();
        cloneUI.SetItem(draggedItem, draggedQuantity);
        
        // Clear original slot
        ClearSlot();
        
        // Setup clone for dragging
        CanvasGroup cloneCanvasGroup = dragClone.GetComponent<CanvasGroup>();
        if (cloneCanvasGroup == null)
            cloneCanvasGroup = dragClone.AddComponent<CanvasGroup>();
        cloneCanvasGroup.blocksRaycasts = false;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (dragClone == null) return;
        dragClone.transform.position = eventData.position;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragClone == null) return;
        
        // SprawdŸ czy upuszczono na jakikolwiek slot
        GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;
        InventoryUI targetSlot = null;
        
        // Spróbuj znaleŸæ InventoryUI w obiekcie lub jego rodzicach
        Transform current = hitObject?.transform;
        while (current != null && targetSlot == null)
        {
            targetSlot = current.GetComponent<InventoryUI>();
            current = current.parent;
        }
        
        bool droppedOnPanel = false;
        if (inventoryPanel != null)
        {
            droppedOnPanel = RectTransformUtility.RectangleContainsScreenPoint(
                inventoryPanel.GetComponent<RectTransform>(),
                eventData.position,
                eventData.enterEventCamera);
        }

        if (targetSlot != null && targetSlot != this)
        {
            Item tempItem = targetSlot.item;
            int tempQuantity = targetSlot.quantity;
            if (targetSlot.slotForWeapon)
            {
                if (draggedItem == null || draggedItem.itemType != ItemType.Sword)
                {
                    Debug.Log("Ten slot akceptuje tylko miecze!");
                    if (draggedItem != null)
                    {
                        SetItem(draggedItem, draggedQuantity);
                    }
                    Destroy(dragClone);
                    dragClone = null;
                    return;
                }

                // SprawdŸ czy playerhand i worldPrefab istniej¹
                if (playerhand != null && draggedItem.worldPrefab != null)
                {
                    Debug.Log("Ustawianie broni: " + draggedItem.worldPrefab.name);
                    
                    playerhand.SetWeapon(draggedItem.worldPrefab);                  
                }
                else
                {
                    Debug.LogError("Brak playerhand lub worldPrefab!");
                    // Zwróæ przedmiot do slotu jeœli nie mo¿na za³o¿yæ broni
                    if (draggedItem != null)
                    {
                        SetItem(draggedItem, draggedQuantity);
                    }
                    Destroy(dragClone);
                    dragClone = null;
                    return;
                }
            }

            if (draggedItem != null)
            {
                targetSlot.SetItem(draggedItem, draggedQuantity);
            }
            else
            {
                targetSlot.ClearSlot();
            }

            if (tempItem != null)
            {
                SetItem(tempItem, tempQuantity);
            }
            else
            {
                ClearSlot();
            }
        }

        else if (droppedOnPanel)
        {
            // Upuszczono na panel ale nie na slot - wróæ na oryginalne miejsce
            if (draggedItem != null)
            {
                SetItem(draggedItem, draggedQuantity);
                if (slotForWeapon)
                {
                    playerhand.SetWeapon(draggedItem.worldPrefab);
                }
            }
        }
        else
        {
            if (draggedItem != null && draggedItem.worldPrefab != null && handPrefab != null)
            {
                Instantiate(draggedItem.worldPrefab, handPrefab.transform.position, Quaternion.identity);
                if (draggedItem.itemType == ItemType.Sword)
                {
                    swordScript = draggedItem.worldPrefab.gameObject.GetComponent<Sword>();
                    swordScript.holdingWeapon = false;
                    swordScript.changeLayer();
                }
            }
            else if (draggedItem != null)
            {
                SetItem(draggedItem, draggedQuantity);
                Debug.Log("Brak worldPrefab lub handPrefab - przedmiot wróci³ do slotu");
            }
        }

        draggedItem = null;
        draggedQuantity = 0;
        
        // Zniszcz klona
        Destroy(dragClone);
        dragClone = null;
    }
    
    public void OnDrop(PointerEventData eventData)
    {
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (item != null && item.itemType == ItemType.Potion && PlayerBody.PlayerInstance.health != PlayerBody.PlayerInstance.maxHealth)
        {
            item.Use();
            quantity--;
            UpdateQuantity();
        }
    }
    
    public void ClearSlot()
    {
        item = null;
        quantity = 0;
        icon.sprite = null;
        icon.color = new Color(1, 1, 1, 0);
        if (slotForWeapon)
        {
            Destroy(playerhand.currentWeapon);
        }
        UpdateQuantity();
    }
}