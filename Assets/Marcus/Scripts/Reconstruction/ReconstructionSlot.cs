using UnityEngine;
using TMPro;
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
    public TMP_Text answerText;

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
        answerText.text = "";
    }

    public void OnDrop(PointerEventData eventData)
    {
        FragmentInput fragment = eventData.pointerDrag?.GetComponent<FragmentInput>();

        if (fragment != null)
        {
            return;
        }

        if (IsFilled)
        {
            return;
        }

        AssignedFragmentID = fragment.FragmentID;
        answerText.text = GetFragmentText(fragment);

        fragment.transform.SetParent(transform, false );

        RectTransform fragmentRect = fragment.GetComponent<RectTransform>();

        fragmentRect.anchoredPosition = Vector2.zero;
    }

    private string GetFragmentText(FragmentInput fragment)
    {
        return fragment.GetComponentInChildren<TMP_Text>().text;
    }

    public bool IsCorrect()
    {
        return IsFilled && AssignedFragmentID == CorrectFragmentID;
    }
}
