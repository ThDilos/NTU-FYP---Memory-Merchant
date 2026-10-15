using UnityEngine;
using UnityEngine.EventSystems;

public class ReconstructionWordDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public int FragmentIndex { get; private set; }
    private TutorialManualPages board;
    private RectTransform rect;
    private CanvasGroup group;
    private Vector2 offset;
    private bool dragging;
    private int pointer;
    public void Setup(TutorialManualPages owner, int index)
    {
        board = owner; FragmentIndex = index;
        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }
    public void OnBeginDrag(PointerEventData data)
    {
        if (dragging || data.button != PointerEventData.InputButton.Left || board == null || !board.CanDrag(FragmentIndex)) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(board.DragRoot, data.position, data.pressEventCamera, out Vector2 point)) return;
        dragging = true; pointer = data.pointerId;
        offset = rect.anchoredPosition - point;
        group.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }
    public void OnDrag(PointerEventData data)
    {
        if (!dragging || pointer != data.pointerId) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(board.DragRoot, data.position, data.pressEventCamera, out Vector2 point))
            rect.anchoredPosition = point + offset;
    }
    public void OnEndDrag(PointerEventData data)
    {
        if (!dragging || pointer != data.pointerId) return;
        dragging = false; group.blocksRaycasts = true;
        board.RefreshBoard();
    }
    public void OnDrop(PointerEventData data)
    {
        ReconstructionWordDrag word = data.pointerDrag != null ? data.pointerDrag.GetComponent<ReconstructionWordDrag>() : null;
        int slot = board != null ? board.SlotFor(FragmentIndex) : -1;
        if (slot >= 0 && word != null && word.board == board) board.DropWord(word.FragmentIndex, slot);
    }
    private void OnDisable()
    {
        if (!dragging) return;
        dragging = false;
        if (group != null) group.blocksRaycasts = true;
        if (board != null) board.RefreshBoard();
    }
}
