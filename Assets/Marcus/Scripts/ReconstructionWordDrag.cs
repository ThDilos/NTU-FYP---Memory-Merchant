using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class ReconstructionWordDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public int FragmentIndex { get; private set; }
    private TutorialManualPages board;
    private RectTransform rect;
    private CanvasGroup group;
    private readonly UIPointerDragPosition dragPosition = new UIPointerDragPosition();
    private bool dragging;
    private int pointer;
    public bool IsDragging => dragging;
    public void Setup(TutorialManualPages owner, int index)
    {
        board = owner; FragmentIndex = index;
        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }
    public void OnBeginDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        if (dragging && data.pointerId == pointer) CancelDrag();
        if (dragging || board == null || !board.CanDrag(FragmentIndex)) return;
        if (!dragPosition.Begin(rect, data)) return;
        dragging = true; pointer = data.pointerId;
        AudioManager.Instance?.PlayInteraction();
        group.blocksRaycasts = false;
        transform.SetAsLastSibling();
        dragPosition.Move(data);
    }
    public void OnDrag(PointerEventData data)
    {
        if (!dragging || pointer != data.pointerId) return;
        dragPosition.Move(data);
    }
    public void OnEndDrag(PointerEventData data)
    {
        if (!dragging || pointer != data.pointerId) return;
        CancelDrag();
    }
    public void OnDrop(PointerEventData data)
    {
        ReconstructionWordDrag word = data.pointerDrag != null ? data.pointerDrag.GetComponent<ReconstructionWordDrag>() : null;
        int slot = board != null ? board.SlotFor(FragmentIndex) : -1;
        if (slot >= 0 && word != null && word.board == board) board.DropWord(word.FragmentIndex, slot);
    }
    private void CancelDrag()
    {
        if (!dragging) return;
        dragging = false;
        dragPosition.Clear();
        if (group != null) group.blocksRaycasts = true;
        if (board != null) board.RefreshBoard();
    }
    private void Update()
    {
        if (dragging && (board == null || !board.CanDrag(FragmentIndex) ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))) CancelDrag();
    }
    private void LateUpdate() { if (dragging) dragPosition.Refresh(); }
    private void OnDisable() => CancelDrag();
    private void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
}
