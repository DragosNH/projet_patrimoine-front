using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class MapDragHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    // The container that holds the tiles 
    public Transform tileContainer;

    // Control variables
    public float dragSensitivity = 1.0f;
    public float inertiaDeceleration = 5.0f;
    public bool useInertia = true;

    // Internal state
    private Vector2 lastDragPosition;
    private Vector2 targetContainerPos;
    private Vector2 dragVelocity;

    // Event for drag finish (pass the snapped offset)
    public event Action<Vector2> OnDragFinished;

    public void OnPointerDown(PointerEventData eventData)
    {
        lastDragPosition = eventData.position;
        StopAllCoroutines();
        dragVelocity = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentPos = eventData.position;
        Vector2 delta = (currentPos - lastDragPosition) * dragSensitivity;
        lastDragPosition = currentPos;
        targetContainerPos += delta;

        // Update container immediately for visual feedback.
        if (tileContainer != null)
            tileContainer.localPosition = targetContainerPos;

        // Record drag velocity.
        dragVelocity = delta / Time.deltaTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (useInertia)
            StartCoroutine(ApplyInertia());
        else
            FinishDrag();
    }

    IEnumerator ApplyInertia()
    {
        while (dragVelocity.magnitude > 50f)
        {
            targetContainerPos += dragVelocity * Time.deltaTime;
            if (tileContainer != null)
                tileContainer.localPosition = targetContainerPos;

            // Gradually reduce velocity.
            dragVelocity = Vector2.Lerp(dragVelocity, Vector2.zero, inertiaDeceleration * Time.deltaTime);
            yield return null;
        }
        FinishDrag();
    }

    void FinishDrag()
    {
        // Snap the final offset to multiples of 256.
        float tileSize = 256f;
        float snappedX = Mathf.Round(targetContainerPos.x / tileSize) * tileSize;
        float snappedY = Mathf.Round(targetContainerPos.y / tileSize) * tileSize;
        Vector2 snappedOffset = new Vector2(snappedX, snappedY);

        // Fire event so OSMTileLoader can update the grid.
        OnDragFinished?.Invoke(snappedOffset);

        // Reset the container position.
        targetContainerPos = Vector2.zero;
        if (tileContainer != null)
            tileContainer.localPosition = targetContainerPos;
    }
}
