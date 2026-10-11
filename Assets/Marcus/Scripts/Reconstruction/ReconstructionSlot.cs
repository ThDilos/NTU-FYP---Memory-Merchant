using UnityEngine;
using UnityEngine.EventSystems;

public class ReconstructionSlot : MonoBehaviour, IDropHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string slotID;

    public string CorrectFragmentID
    {
        get; private set;
    }
    public string AssignedFragmentID
    {
        get; private set;
    }
    public bool IsFilled => !string.IsNullOrEmpty(AssignedFragmentID);

    public void Setup(ReconstructionSlotData data)
    {
        slotID = data.slotID;
        CorrectFragmentID = data.correctFragementID;

        AssignedFragmentID = "";
    }

    public void OnDrop(PointerEventData eventData)
    {

        if (IsFilled) return;

        DraggableFragment fragment =
            eventData.pointerDrag?.GetComponent<DraggableFragment>();

        if (fragment == null) return;

        AssignedFragmentId = fragment.FragmentId;

        // Place the image inside the slot.
        fragment.transform.SetParent(transform, false);

        RectTransform rect =
            fragment.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        fragment.SetPlacedInSlot(this);
    }

    public bool IsCorrect()
    {
        return IsFilled && AssignedFragmentID == CorrectFragmentID;
    }

    public void ClearSlot()
    {
        AssignedFragmentId = "";
    }
}
