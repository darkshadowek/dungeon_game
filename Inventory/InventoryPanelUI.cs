using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanelUI : MonoBehaviour
{
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
}
