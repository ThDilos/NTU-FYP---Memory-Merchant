using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class ReconstructionWordDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int FragmentIndex { get; private set; }
    private TutorialManualPages board;
    private RectTransform rect;
    private RectTransform artwork;
    private Coroutine settle;
    private CanvasGroup group;
    private Outline outline;
    private readonly UIPointerDragPosition dragPosition = new UIPointerDragPosition();
    private bool dragging;
    private bool hovered;
    private bool previousBlocksRaycasts;
    private float previousAlpha;
    private int pointer;
    public bool IsDragging => dragging;
    public bool BelongsTo(TutorialManualPages owner) => board != null && board == owner;
    public void Setup(TutorialManualPages owner, int index)
    {
        board = owner; FragmentIndex = index;
        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        outline = GetComponent<Outline>();
        if (outline == null) outline = gameObject.AddComponent<Outline>();
        outline.useGraphicAlpha = true;
        outline.effectDistance = new Vector2(2f, -2f);
        Image hitTarget = GetComponent<Image>();
        if (hitTarget != null)
        {
            hitTarget.alphaHitTestMinimumThreshold = 0f;
            hitTarget.raycastTarget = true;
        }
        // Animate the artwork inside the card. The original Image and RectTransform
        // still own hit testing, slot layout, and cursor alignment.
        artwork = (RectTransform)new GameObject("Fragment_Artwork", typeof(RectTransform)).transform;
        artwork.gameObject.layer = gameObject.layer;
        artwork.SetParent(rect, false);
        artwork.anchorMin = Vector2.zero;
        artwork.anchorMax = Vector2.one;
        artwork.offsetMin = artwork.offsetMax = Vector2.zero;
        while (rect.childCount > 1) rect.GetChild(0).SetParent(artwork, false);
        RefreshOutline();
    }
    public void OnBeginDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        if (dragging && data.pointerId == pointer) CancelDrag();
        if (dragging || board == null || !board.CanDrag(FragmentIndex)) return;
        if (!dragPosition.Begin(rect, data)) return;
        CancelSettle();
        dragging = true; pointer = data.pointerId;
        data.eligibleForClick = false;
        previousBlocksRaycasts = group.blocksRaycasts;
        previousAlpha = group.alpha;
        AudioManager.Instance?.PlayInteraction();
        group.blocksRaycasts = false;
        group.alpha = previousAlpha * 0.94f;
        transform.SetAsLastSibling();
        board.SetDragFeedback(FragmentIndex, true);
        dragPosition.Move(data);
        RefreshOutline();
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
        if (data.button != PointerEventData.InputButton.Left) return;
        ReconstructionWordDrag word = data.pointerDrag != null ? data.pointerDrag.GetComponent<ReconstructionWordDrag>() : null;
        int slot = board != null ? board.SlotFor(FragmentIndex) : -1;
        if (slot >= 0 && word != null && word.IsDragging && word.BelongsTo(board) && board.CanDrag(word.FragmentIndex))
            board.DropWord(word.FragmentIndex, slot);
    }
    public void OnPointerEnter(PointerEventData data)
    {
        hovered = true;
        RefreshOutline();
    }
    public void OnPointerExit(PointerEventData data)
    {
        hovered = false;
        RefreshOutline();
    }
    private void CancelDrag()
    {
        if (!dragging) return;
        dragging = false;
        dragPosition.Clear();
        if (group != null)
        {
            group.blocksRaycasts = previousBlocksRaycasts;
            group.alpha = previousAlpha;
        }
        if (board != null)
        {
            board.ClearSlotHighlights();
            board.SetDragFeedback(FragmentIndex, false);
            board.RefreshBoard();
        }
        RefreshOutline();
    }
    private void Update()
    {
        if (dragging && (board == null || !board.CanDrag(FragmentIndex) ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))) CancelDrag();
    }
    private void LateUpdate()
    {
        if (dragging) dragPosition.Refresh();
        RefreshOutline();
    }
    private void RefreshOutline()
    {
        if (outline == null) return;
        bool available = board != null && board.CanDrag(FragmentIndex);
        bool selected = available && board.ActiveFragmentIndex == FragmentIndex;
        outline.enabled = available && (hovered || selected || dragging);
        outline.effectColor = selected || dragging
            ? new Color(1f, 0.82f, 0.43f)
            : new Color(0.87f, 0.65f, 0.33f);
        outline.effectDistance = dragging ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
    }
    public void PlaySettle()
    {
        CancelSettle();
        if (artwork != null && isActiveAndEnabled) settle = StartCoroutine(AnimateSettle());
    }

    private IEnumerator AnimateSettle()
    {
        // Unity sends EndDrag after Drop. Let the board put the card into its slot first.
        yield return null;
        const float duration = 0.22f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = elapsed / duration;
            float spring = Mathf.Cos(progress * Mathf.PI * 1.75f) * (1f - progress);
            artwork.localScale = new Vector3(1f + spring * 0.042f, 1f - spring * 0.09f, 1f);
            yield return null;
        }
        artwork.localScale = Vector3.one;
        settle = null;
    }

    private void CancelSettle()
    {
        if (settle != null) StopCoroutine(settle);
        settle = null;
        if (artwork != null) artwork.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        hovered = false;
        CancelSettle();
        CancelDrag();
        if (outline != null) outline.enabled = false;
    }
    private void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
}
