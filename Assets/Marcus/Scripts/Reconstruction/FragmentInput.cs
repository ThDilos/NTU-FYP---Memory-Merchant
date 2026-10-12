using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FragmentInput : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string fragmentID;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Transform originalParent;
    private Vector2 originalPosition;
    private ReconstructionSlot currentSlot;

    public string FragmentID => fragmentID;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();
    }
    public void Setup(string id, Sprite sprite)
    {
        fragmentID = id;

        Image image = GetComponentInChildren<Image>();
        if (image != null)
            image.sprite = sprite;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

        canvasGroup.blocksRaycasts = false;

        transform.SetParent(canvas.transform, true);
        currentSlot = null;
    }
    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition +=
            eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // If no slot accepted it, restore its position.
        if (currentSlot == null)
        {
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    public void SetPlacedInSlot(ReconstructionSlot slot)
    {
        currentSlot = slot;
    }
}
