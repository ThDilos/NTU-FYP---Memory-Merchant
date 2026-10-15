using TMPro;
using UnityEngine;

public class MemoryWorldObjectives : MonoBehaviour
{
    [SerializeField] private TutorialMemoryState state;
    [SerializeField] private TMP_Text objectiveText;
    private void OnEnable() { if (state != null) state.Changed += Refresh; Refresh(); }
    private void OnDisable() { if (state != null) state.Changed -= Refresh; }
    private void Start() => Refresh();
    private void Refresh()
    {
        if (state == null || objectiveText == null || state.Reconstruction == null) return;
        if (state.StudyRestored) objectiveText.text = "Enter the Study";
        else if (!state.EchoStored(0) && !state.CabinetUnlocked) objectiveText.text = "Hold E at the cabinet key.\nRelease, then hold E again\nto capture its Echo.";
        else if (!state.CabinetUnlocked) objectiveText.text = "Go to the locked cabinet.\nHold 1 to use the Key Echo.";
        else if (!state.HasFragment(0) || !state.HasFragment(1) || !state.HasFragment(2))
            objectiveText.text = "Collect the memory fragments:\nMother, recorder and Study.";
        else objectiveText.text = "Open the Manual (R).\nDrag the words into the blanks\nand reconstruct the memory.";
    }
}
