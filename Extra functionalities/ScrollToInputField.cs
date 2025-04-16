using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ScrollToInputField : MonoBehaviour, ISelectHandler
{
    public ScrollRect scrollRect;
    public float offsetFromKeyboard = 200f; 

    public void OnSelect(BaseEventData eventData)
    {
        StartCoroutine(ScrollToSelected());
    }

    private System.Collections.IEnumerator ScrollToSelected()
    {
        yield return new WaitForEndOfFrame(); 
        yield return new WaitForSeconds(0.15f); 

        RectTransform inputRect = GetComponent<RectTransform>();
        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport;

        // Convert the input field's position to viewport space
        Vector3 worldPos = inputRect.position;
        Vector3 localPoint = viewport.InverseTransformPoint(worldPos);

        // Calculate how far to scroll above the keypad
        float scrollY = content.anchoredPosition.y + (localPoint.y - offsetFromKeyboard);

        // Stops scrolling before it goes too far
        scrollY = Mathf.Clamp(scrollY, 0, content.sizeDelta.y);

        // Apply
        scrollRect.content.anchoredPosition = new Vector2(content.anchoredPosition.x, scrollY);
    }
}
