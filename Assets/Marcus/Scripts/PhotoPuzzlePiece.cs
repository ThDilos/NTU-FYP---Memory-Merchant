using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PhotoPuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public MinigamePlaceholderController Owner { get; private set; }
    public int Index { get; private set; }
    private RectTransform rect;
    private Image border;
    private Image hintEdge;
    private Button button;
    private RawImage fogCover;
    private CanvasGroup group;
    private PhotoPuzzleLooseGroups looseGroups;
    private Vector2 home, grabOffset;
    private bool placed, dragging, fogged, selected, linked, matching;
    private int dragPointer;
    private Coroutine fogFade;

    public int LinkedPieceCount => looseGroups.CountFor(this);
    internal bool IsPlaced => placed;
    internal bool CanArrange => !placed && !fogged;
    internal Vector2 TablePosition
    {
        get => rect.anchoredPosition;
        set => rect.anchoredPosition = value;
    }
    internal Vector2 TileSize => rect.sizeDelta;

    internal void Initialize(MinigamePlaceholderController owner, int index, Image image, Button pieceButton,
        RawImage cover, PhotoPuzzleLooseGroups tableGroups)
    {
        Owner = owner; Index = index; border = image; rect = (RectTransform)transform;
        button = pieceButton;
        fogCover = cover;
        group = gameObject.AddComponent<CanvasGroup>();
        looseGroups = tableGroups;
        looseGroups.Register(this);
    }

    public void ResetPiece(Vector2 position, Vector2 size, bool startsFogged)
    {
        if (fogFade != null) StopCoroutine(fogFade);
        fogFade = null;
        placed = false; dragging = false; home = position;
        fogged = startsFogged;
        matching = false;
        rect.anchoredPosition = home; rect.sizeDelta = size;
        group.blocksRaycasts = true; group.alpha = 1;
        button.interactable = !fogged;
        fogCover.color = Color.white;
        fogCover.gameObject.SetActive(fogged);
        SetSelected(false);
    }

    public void RevealFog()
    {
        if (!fogged) return;
        fogged = false;
        button.interactable = !placed;
        if (isActiveAndEnabled) fogFade = StartCoroutine(DissipateFog());
        else fogCover.gameObject.SetActive(false);
    }

    private IEnumerator DissipateFog()
    {
        const float duration = .34f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float opacity = 1f - Mathf.SmoothStep(0f, 1f, elapsed / duration);
            fogCover.color = new Color(1f, 1f, 1f, opacity);
            yield return null;
        }
        fogCover.gameObject.SetActive(false);
        fogFade = null;
    }

    public void SetSelected(bool isSelected)
    {
        selected = isSelected;
        RefreshBorder();
    }

    internal void SetLinked(bool isLinked)
    {
        linked = isLinked;
        RefreshBorder();
    }

    internal void SetMatchPreview(bool showMatch)
    {
        matching = showMatch;
        RefreshBorder();
    }

    internal void SetHintEdge(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            if (hintEdge != null) hintEdge.gameObject.SetActive(false);
            return;
        }

        if (hintEdge == null)
        {
            var marker = new GameObject("Hint_Matching_Edge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            marker.layer = gameObject.layer;
            marker.transform.SetParent(transform, false);
            hintEdge = marker.GetComponent<Image>();
            hintEdge.color = new Color(1f, .83f, .34f);
            hintEdge.raycastTarget = false;
        }

        RectTransform edge = hintEdge.rectTransform;
        const float inset = 4f;
        const float depth = 12f;
        const float padding = 15f;
        if (direction.x != 0)
        {
            bool right = direction.x > 0;
            float xAnchor = right ? 1f : 0f;
            edge.anchorMin = new Vector2(xAnchor, 0f);
            edge.anchorMax = new Vector2(xAnchor, 1f);
            edge.offsetMin = new Vector2(right ? -depth : inset, padding);
            edge.offsetMax = new Vector2(right ? -inset : depth, -padding);
        }
        else
        {
            bool top = direction.y > 0;
            float yAnchor = top ? 1f : 0f;
            edge.anchorMin = new Vector2(0f, yAnchor);
            edge.anchorMax = new Vector2(1f, yAnchor);
            edge.offsetMin = new Vector2(padding, top ? -depth : inset);
            edge.offsetMax = new Vector2(-padding, top ? -inset : depth);
        }
        hintEdge.gameObject.SetActive(true);
    }

    private void RefreshBorder()
    {
        if (matching) border.color = new Color(1f, .86f, .38f);
        else if (selected && !fogged) border.color = new Color(.12f, .37f, .27f);
        else if (linked) border.color = new Color(.32f, .62f, .43f);
        else border.color = new Color(.62f, .41f, .20f);
    }

    internal void RememberTablePosition() => home = TablePosition;
    internal void ReturnToTablePosition() => TablePosition = home;

    internal void SetDragPresentation(bool isDragging)
    {
        group.blocksRaycasts = !isDragging && !placed;
        if (isDragging) transform.SetAsLastSibling();
    }

    public bool IsBeingDraggedBy(int pointerId) => dragging && !placed && dragPointer == pointerId;

    public bool TryPlaceAt(int slot)
    {
        if (!CanArrange || !looseGroups.CanSubmit(this)) return false;

        int[] members = looseGroups.GetLinkedIndices(this);
        return members.Length == 1
            ? Owner.TryPlacePiece(Index, slot)
            : Owner.TryPlaceGroup(members, Index, slot);
    }

    public void OnBeginDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || placed || fogged || dragging || Owner.Completed) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Owner.DragRoot, data.pressPosition,
            data.pressEventCamera, out var pointer)) return;
        if (!looseGroups.BeginDrag(this)) return;

        dragPointer = data.pointerId;
        data.eligibleForClick = false;
        dragging = true;
        Owner.SelectPiece(Index);
        grabOffset = TablePosition - pointer;
        OnDrag(data);
    }
    public void OnDrag(PointerEventData data)
    {
        if (!dragging || placed || data.pointerId != dragPointer) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(Owner.DragRoot, data.position, data.pressEventCamera, out var pointer))
            looseGroups.MoveDrag(this, pointer + grabOffset, pointer);
    }
    public void OnEndDrag(PointerEventData data)
    {
        if (!dragging || data.pointerId != dragPointer) return;
        dragging = false;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(Owner.DragRoot, data.position,
            data.pressEventCamera, out var pointer))
        {
            if (!placed) looseGroups.MoveDrag(this, pointer + grabOffset, pointer);
            looseGroups.EndDrag(this, pointer);
        }
        else looseGroups.CancelDrag(this);
    }

    private void OnDisable()
    {
        if (!dragging) return;
        dragging = false;
        looseGroups.CancelDrag(this);
    }

    public void SnapTo(Vector2 position, Vector2 size)
    {
        placed = true; rect.anchoredPosition = position; rect.sizeDelta = size;
        group.blocksRaycasts = false; button.interactable = false;
        looseGroups.Detach(this);
    }
}
