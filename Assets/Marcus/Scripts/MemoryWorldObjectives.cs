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
        else if (!state.HasFragment(1))
            objectiveText.text = "Collect the recorder fragment\ninside the open cabinet.\nHold E to collect it.";
        else if (!state.HasFragment(2))
            objectiveText.text = "Find the fragment near the Study.\nHold E to collect it.";
        else if (!state.HasFragment(0))
            objectiveText.text = "Collect the Mother fragment\nat the family photo.\nHold E to collect it.";
        else objectiveText.text = "Open the Manual (R).\nDrag the words into the blanks\nand reconstruct the memory.";
    }
}
