using UnityEngine;
using Yarn.Unity;

public class DialogueManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private bool playOnSceneStart = false;
    [SerializeField] private string openingNodeName = "TutorialIntroduction";

    private void Start()
    {
        if (playOnSceneStart)
        {
            StartDialogue(openingNodeName);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    [SerializeField] private DialogueRunner dialogueRunner;

    public void StartDialogue(string nodeName)
    {
        if (dialogueRunner == null)
        {
            Debug.LogError("Dialogue Runner is not assigned!");
            return;
        }

        if (dialogueRunner.IsDialogueRunning)
        {
            Debug.LogWarning("A dialogue is already running.");
            return;
        }

        dialogueRunner.StartDialogue(nodeName);
    }
}
