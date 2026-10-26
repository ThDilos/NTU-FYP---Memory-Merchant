using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PhotoPuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public MinigamePlaceholderController Owner { get; private set; }
    public int Index { get; private set; }
    private RectTransform rect;
    private Image border;
    private CanvasGroup group;
    private Vector2 home, grabOffset;
    private bool placed, dragging;
    public void Initialize(MinigamePlaceholderController owner, int index, Image image)
    {
        Owner = owner; Index = index; border = image; rect = (RectTransform)transform;
        group = gameObject.AddComponent<CanvasGroup>();
    }
    public void ResetPiece(Vector2 position, Vector2 size)
    {
        placed = false; dragging = false; home = position;
        rect.anchoredPosition = home; rect.sizeDelta = size;
        group.blocksRaycasts = true; group.alpha = 1;
        GetComponent<Button>().interactable = true;
        SetSelected(false);
    }
    public void SetSelected(bool selected) =>
        border.color = selected ? new Color(.12f, .37f, .27f) : new Color(.62f, .41f, .20f);
    public void OnBeginDrag(PointerEventData data)
    {
        if (placed || Owner.Completed) return;
        dragging = true; Owner.SelectPiece(Index); transform.SetAsLastSibling();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(Owner.DragRoot, data.position, data.pressEventCamera, out var pointer);
        grabOffset = rect.anchoredPosition - pointer;
        group.blocksRaycasts = false;
    }
    public void OnDrag(PointerEventData data)
    {
        if (!dragging) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(Owner.DragRoot, data.position, data.pressEventCamera, out var pointer))
            rect.anchoredPosition = pointer + grabOffset;
    }
    public void OnEndDrag(PointerEventData data)
    {
        if (!dragging) return;
        dragging = false; group.blocksRaycasts = !placed;
        if (!placed) rect.anchoredPosition = home;
    }
    public void SnapTo(Vector2 position, Vector2 size)
    {
        placed = true; rect.anchoredPosition = position; rect.sizeDelta = size;
        group.blocksRaycasts = false; GetComponent<Button>().interactable = false;
    }
}
