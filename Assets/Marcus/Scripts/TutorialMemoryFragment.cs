using UnityEngine;

public class TutorialMemoryFragment : MemoryFragment
{
    [SerializeField] private TutorialVisualStateController visuals;
    [SerializeField] private TutorialMemoryState state;
    [SerializeField] private int fragmentIndex;

    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private string nodeName;
    public override void Interact()
    {
        if (Time.timeScale > 0f && state != null) state.CollectFragment(fragmentIndex);
    }
    public override void OnCollect()
    {
        if (visuals == null) return;
        if (fragmentIndex == 0) visuals.CollectMotherFragment();
        else if (fragmentIndex == 1) visuals.CollectRecorderFragment();
        else visuals.CollectStudyFragment();

        dialogueManager.StartDialogue(nodeName);
    }
}
