using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ScrollToInputField : MonoBehaviour, ISelectHandler
{
    public ScrollRect scrollRect;
    private TMP_InputField selectedInput;

    public void OnSelect(BaseEventData eventData)
    {
        selectedInput = GetComponent<TMP_InputField>();
    }

    void Update()
    {
        if (TouchScreenKeyboard.visible && selectedInput != null)
        {
            RectTransform inputRect = selectedInput.GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            inputRect.GetWorldCorners(corners);
            float inputBottomY = corners[0].y;

            float keyboardTopY = TouchScreenKeyboard.area.y;

            if (inputBottomY < keyboardTopY)
            {
                scrollRect.verticalNormalizedPosition = Mathf.Lerp(scrollRect.verticalNormalizedPosition, 1f, Time.deltaTime * 5f);
            }
        }
    }
}
