using UnityEngine;
using UnityEngine.EventSystems;

public class ReconstructionDropSlot : MonoBehaviour, IDropHandler
{
    private TutorialManualPages board;
    private int slot;
    public void Setup(TutorialManualPages owner, int index) { board = owner; slot = index; }
    public void OnDrop(PointerEventData data)
    {
        ReconstructionWordDrag word = data.pointerDrag != null ? data.pointerDrag.GetComponent<ReconstructionWordDrag>() : null;
        if (word != null && board != null) board.DropWord(word.FragmentIndex, slot);
    }
}
