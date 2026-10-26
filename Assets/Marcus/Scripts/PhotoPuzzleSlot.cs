using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PhotoPuzzleSlot : MonoBehaviour, IDropHandler
{
    public MinigamePlaceholderController Owner;
    public int Index;
    public void OnDrop(PointerEventData data)
    {
        var piece = data.pointerDrag != null ? data.pointerDrag.GetComponent<PhotoPuzzlePiece>() : null;
        if (piece != null && piece.Owner == Owner) Owner.TryPlacePiece(piece.Index, Index);
    }
}
