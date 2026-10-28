using UnityEngine;
using Yarn.Unity;
using System.Collections.Generic;

public class ProgressDIalogueManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private DialogueRunner dialogueRunner;

    private readonly Dictionary<string, string> returnDialogueNodes =
        new Dictionary<string, string>()
        {
            { "Tutorial", "AfterTutorial" },
            { "MemoryLevel2", "AfterMemoryLevel2" },
            { "MemoryLevel3", "AfterMemoryLevel3" }
        };

    private void Start()
    {
        string returnedLevel = SceneReturnState.ReturnedFromLevel;

        if (string.IsNullOrEmpty(returnedLevel))
            return;

        // Clear the stored value so it doesn't trigger again.
        SceneReturnState.ReturnedFromLevel = null;

        if (returnDialogueNodes.TryGetValue(
            returnedLevel, out string nodeName))
        {
            dialogueRunner.StartDialogue(nodeName);
        }
        else
        {
            Debug.LogWarning(
                $"No return dialogue configured for level: {returnedLevel}");
        }
    }
}
