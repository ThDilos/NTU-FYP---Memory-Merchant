using UnityEngine;
using Yarn.Unity;
using System.Collections.Generic;
using System.Collections;
using System;

public class ProgressDIalogueManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private DialogueRunner dialogueRunner;
    private ShopUIController shopUI;
    private string queuedNode;
    private bool playingReturnDialogue;
    private bool awaitingUpgrade;

    private readonly Dictionary<string, string> returnDialogueNodes =
        new Dictionary<string, string>()
        {
            { "Tutorial", "AfterTutorial" },
            { "MemoryLevel2", "AfterMemoryLevel2" },
            { "MemoryLevel3", "AfterMemoryLevel3" }
        };

    private IEnumerator Start()
    {
        // Let the shop and all Yarn presenters finish their Start setup first.
        yield return null;
        shopUI = FindAnyObjectByType<ShopUIController>();
        awaitingUpgrade = TutorialRewardBridge.NeedsFirstEchoUpgrade;
        string returnedLevel = SceneReturnState.ReturnedFromLevel;
        // Clear the stored value so it doesn't trigger again.
        SceneReturnState.ReturnedFromLevel = null;
        if (!string.IsNullOrEmpty(returnedLevel) && returnDialogueNodes.TryGetValue(returnedLevel, out string nodeName))
        {
            queuedNode = returnedLevel == "Tutorial" && !awaitingUpgrade
                ? "Tutorial_Upgrade_Complete" : nodeName;
        }
        else if (awaitingUpgrade)
        {
            // A Continue/load before the purchase resumes this lesson without
            // replaying the full return conversation or granting more rewards.
            queuedNode = "Tutorial_Upgrade_Reminder";
        }
        else if (!string.IsNullOrEmpty(returnedLevel))
        {
            Debug.LogWarning(
                $"No return dialogue configured for level: {returnedLevel}");
        }
    }

    private void Update()
    {
        if (dialogueRunner == null || playingReturnDialogue || dialogueRunner.IsDialogueRunning ||
            !dialogueRunner.DialogueTask.IsCompleted() || (shopUI != null && shopUI.IsShopInputBlocked)) return;
        if (awaitingUpgrade && TutorialRewardBridge.OwnedQuantity(ShopProduct.EchoSlotII) > 0)
        {
            awaitingUpgrade = false;
            queuedNode = "Tutorial_Upgrade_Complete";
        }
        if (string.IsNullOrEmpty(queuedNode)) return;
        string node = queuedNode;
        queuedNode = null;
        RunReturnDialogue(node);
    }

    private async void RunReturnDialogue(string node)
    {
        if (dialogueRunner.YarnProject == null || Array.IndexOf(dialogueRunner.YarnProject.NodeNames, node) < 0)
        {
            Debug.LogWarning("Missing shop tutorial dialogue: " + node, this);
            return;
        }
        playingReturnDialogue = true;
        try
        {
            await dialogueRunner.StartDialogue(node);
            await dialogueRunner.DialogueTask;
        }
        catch (Exception error) { Debug.LogException(error, this); }
        finally
        {
            playingReturnDialogue = false;
            CameraShift.ReturnCamera();
        }
    }
}
