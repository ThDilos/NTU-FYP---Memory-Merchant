using UnityEngine;
using UnityEngine.EventSystems;

public class ReconstructionDropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler,
    IPointerExitHandler, IPointerClickHandler
{
    private TutorialManualPages board;
    private int slot;
    private bool hovered;
    private ReconstructionWordDrag hoveredDrag;
    public void Setup(TutorialManualPages owner, int index) { board = owner; slot = index; }
    public void OnDrop(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        ReconstructionWordDrag word = data.pointerDrag != null ? data.pointerDrag.GetComponent<ReconstructionWordDrag>() : null;
        if (word != null && word.IsDragging && word.BelongsTo(board) && board.CanDrag(word.FragmentIndex))
            board.DropWord(word.FragmentIndex, slot);
        ClearHighlight();
    }
    public void OnPointerClick(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || board == null || !board.HasActiveFragment) return;
        int fragment = board.ActiveFragmentIndex;
        if (board.CanDrag(fragment)) board.DropWord(fragment, slot);
        RefreshHighlight();
    }
    public void OnPointerEnter(PointerEventData data)
    {
        hovered = true;
        hoveredDrag = data.pointerDrag != null ? data.pointerDrag.GetComponent<ReconstructionWordDrag>() : null;
        RefreshHighlight();
    }
    public void OnPointerExit(PointerEventData data)
    {
        hovered = false;
        hoveredDrag = null;
        ClearHighlight();
    }
    private void LateUpdate()
    {
        if (hovered) RefreshHighlight();
    }
    private void RefreshHighlight()
    {
        if (board == null) return;
        bool valid = hovered && (hoveredDrag != null && hoveredDrag.IsDragging
            ? hoveredDrag.BelongsTo(board) && board.CanDrag(hoveredDrag.FragmentIndex)
            : board.HasActiveFragment && board.CanDrag(board.ActiveFragmentIndex));
        board.HighlightSlot(slot, valid);
    }
    private void ClearHighlight()
    {
        if (board != null) board.HighlightSlot(slot, false);
    }
    private void OnDisable()
    {
        hovered = false;
        hoveredDrag = null;
        ClearHighlight();
    }
}
