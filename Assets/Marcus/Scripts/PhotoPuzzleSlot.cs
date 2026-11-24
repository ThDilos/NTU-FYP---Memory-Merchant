using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PhotoPuzzleSlot : MonoBehaviour, IDropHandler
{
    public MinigamePlaceholderController Owner;
    public int Index;
    public void OnDrop(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        var piece = data.pointerDrag != null ? data.pointerDrag.GetComponent<PhotoPuzzlePiece>() : null;
        if (piece != null && piece.Owner == Owner && piece.IsBeingDraggedBy(data.pointerId))
            piece.TryPlaceAt(Index);
    }
}
