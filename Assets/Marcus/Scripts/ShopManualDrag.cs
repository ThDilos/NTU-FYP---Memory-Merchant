using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Pointer events come from the scene's existing InputSystemUIInputModule.
public class ShopManualDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ShopUIController shopUI;
    [SerializeField] private RectTransform dragSurface;
    [SerializeField] private RectTransform machineDropZone;
    [SerializeField] private Image dropHighlight;

    private RectTransform rect;
    private Vector2 originalPosition;
    private Vector2 grabOffset;
    private bool dragging;
    private int pointerId;

    private void Awake() => rect = (RectTransform)transform;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (dragging || eventData.button != PointerEventData.InputButton.Left ||
            shopUI == null || !shopUI.CanEnterTutorialMemoryWorld || dragSurface == null || machineDropZone == null)
            return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(dragSurface,
            eventData.position, eventData.pressEventCamera, out Vector2 point)) return;
        originalPosition = rect.anchoredPosition;
        grabOffset = originalPosition - point;
        pointerId = eventData.pointerId;
        dragging = true;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || eventData.pointerId != pointerId) return;
        if (shopUI == null || shopUI.IsShopInputBlocked) { CancelDrag(); return; }
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragSurface,
            eventData.position, eventData.pressEventCamera, out Vector2 point))
            rect.anchoredPosition = point + grabOffset;
        Highlight(IsOverMachine(eventData));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging || eventData.pointerId != pointerId) return;
        bool validDrop = IsOverMachine(eventData) && shopUI != null && shopUI.CanEnterTutorialMemoryWorld;
        CancelDrag();
        if (validDrop) shopUI.TryEnterMemoryWorld();
    }

    private bool IsOverMachine(PointerEventData eventData) =>
        machineDropZone != null && RectTransformUtility.RectangleContainsScreenPoint(
            machineDropZone, eventData.position, eventData.pressEventCamera);

    private void Highlight(bool active)
    {
        if (dropHighlight != null)
            dropHighlight.color = active ? new Color(1f, 0.75f, 0.3f, 0.22f) : Color.clear;
    }

    private void CancelDrag()
    {
        if (dragging && rect != null) rect.anchoredPosition = originalPosition;
        dragging = false;
        Highlight(false);
    }

    private void Update()
    {
        if (dragging && (shopUI == null || !shopUI.CanEnterTutorialMemoryWorld ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)))
            CancelDrag();
    }

    private void OnDisable() => CancelDrag();
    private void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
}
