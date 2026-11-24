using UnityEngine;
using UnityEngine.EventSystems;

// A textbox click reaches this handler; child buttons consume their own clicks.
public sealed class DialogueAdvanceSurface : MonoBehaviour, IPointerClickHandler
{
    private DialogueBehaviour controls;

    public void Configure(DialogueBehaviour owner) => controls = owner;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && controls != null)
            controls.AdvanceDialogue();
    }
}
