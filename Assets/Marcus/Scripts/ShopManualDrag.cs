using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Pointer events come from the scene's existing InputSystemUIInputModule.
[DefaultExecutionOrder(100)]
public class ShopManualDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private ShopUIController shopUI;
    [SerializeField] private RectTransform dragSurface;
    [SerializeField] private RectTransform machineDropZone;
    [SerializeField] private Image dropHighlight;

    private RectTransform rect;
    private Vector2 originalPosition;
    private readonly UIPointerDragPosition dragPosition = new UIPointerDragPosition();
    private bool dragging;
    private int pointerId;

    private void Awake() => rect = (RectTransform)transform;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        // Recover from a release outside the Game view that never delivered EndDrag.
        if (dragging && eventData.pointerId == pointerId) CancelDrag();
        if (dragging ||
            shopUI == null || !shopUI.CanEnterTutorialMemoryWorld || dragSurface == null || machineDropZone == null)
            return;
        if (!dragPosition.Begin(rect, eventData)) return;
        originalPosition = rect.anchoredPosition;
        pointerId = eventData.pointerId;
        dragging = true;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || eventData.pointerId != pointerId) return;
        if (shopUI == null || shopUI.IsShopInputBlocked) { CancelDrag(); return; }
        dragPosition.Move(eventData);
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
        dragPosition.Clear();
        Highlight(false);
    }

    private void Update()
    {
        if (dragging && (shopUI == null || !shopUI.CanEnterTutorialMemoryWorld ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)))
            CancelDrag();
    }

    private void LateUpdate() { if (dragging) dragPosition.Refresh(); }

    private void OnDisable() => CancelDrag();
    private void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
}
