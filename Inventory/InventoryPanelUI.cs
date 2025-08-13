using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanelUI : MonoBehaviour
{
    public Transform chestInventory;
    private HandController handController;
    private void Start()
    {
        handController = GameObject.FindAnyObjectByType<HandController>();
    }
    public void HideAllImages(Transform parent)
    {
        Image img = parent.GetComponent<Image>();
        if (img != null)
        {
            img.enabled = false;
        }

        TextMeshProUGUI tmp = parent.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.enabled = false;
        }

        RawImage rawImage = parent.GetComponent<RawImage>();
        if (rawImage != null)
        {
            rawImage.enabled = false;
        }
        foreach (Transform child in parent)
        {
            HideAllImages(child);
        }
    }

    public void ShowAllImages(Transform parent)
    {
        Image img = parent.GetComponent<Image>();
        if (img != null)
        {
            img.enabled = true;
        }

        TextMeshProUGUI tmp = parent.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.enabled = true;
        }

        RawImage rawImage = parent.GetComponent<RawImage>();
        if (rawImage != null)
        {
            rawImage.enabled = true;
        }

        foreach (Transform child in parent)
        {
            ShowAllImages(child);
        }
    }
    public void ChestInventoryClose()
    {
        foreach (Transform child in chestInventory)
        {
            child.gameObject.SetActive(false);
        }
        if(handController.chest)
        {
            handController.chestInventoryUI.SynchronizeChestWithItems(handController.chest);
            handController.chest = null;
        }
    }
}
