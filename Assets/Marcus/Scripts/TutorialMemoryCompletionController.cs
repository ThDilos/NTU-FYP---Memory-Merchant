using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yarn.Unity;

public class TutorialMemoryCompletionController : MonoBehaviour
{
    [SerializeField] private TutorialMemoryState state;
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private TutorialUIController tutorialUI;
    [SerializeField] private InputActionAsset[] gameplayInputAssets;
    [SerializeField] private string motherNode = "Tutorial_Mother_Message";
    [Header("First visit instructions (editable Yarn node)")]
    [SerializeField] private string openingNode = "Tutorial_Memory_Introduction";
    [SerializeField] private GameObject dialogueBlocker;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text memoryPointsText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text continueLabel;
    [SerializeField] private string shopScene = "Marcus_TutorialEnvironment";
    private readonly TutorialCompletionSequence sequence = new TutorialCompletionSequence();
    private readonly GameplayInputPause inputLock = new GameplayInputPause();
    private bool lockHeld;
    private bool previousUIBlock;
    private bool openingDialogue;
    private bool openingPending = true;
    private readonly Queue<string> guidanceQueue = new Queue<string>();
    private readonly HashSet<string> acceptedGuidance = new HashSet<string>(StringComparer.Ordinal);
    private bool guidanceDialogue;
    private int guidanceGeneration;
    private int guidanceReadyFrame;
    private void Awake()
    {
        Image blockerImage = dialogueBlocker.GetComponent<Image>();
        if (blockerImage != null)
        {
            blockerImage.sprite = null;
            blockerImage.color = Color.clear;
            blockerImage.raycastTarget = true;
            blockerImage.preserveAspect = false;
        }
        dialogueBlocker.SetActive(false);
        resultsPanel.SetActive(false);
        // Explicit starts choose the introduction, interaction guidance, or restored Study dialogue.
        dialogueRunner.autoStart = false;
        foreach (Canvas canvas in dialogueRunner.GetComponentsInChildren<Canvas>(true))
            canvas.sortingOrder = 150;
        continueButton.onClick = new Button.ButtonClickedEvent();
        continueButton.onClick.AddListener(ReturnToShop);
        continueButton.navigation = new Navigation { mode = Navigation.Mode.None };
        if (memoryPointsText != null) memoryPointsText.text = TutorialRewardBridge.IsArchiveReplay
            ? "Memory Points   +0 (Replay)" : "Memory Points   +3";
        if (coinsText != null) coinsText.text = "Coins   +0";
        if (continueLabel != null) continueLabel.text = TutorialRewardBridge.IsArchiveReplay ? "Back to Archives" : "Continue";
    }
    private IEnumerator Start()
    {
        // Wait until all scene Start methods have set up the player and presenters.
        yield return null;
        if (TutorialRewardBridge.MemoryWorldIntroSeen || TutorialRewardBridge.TutorialCompleted ||
            TutorialRewardBridge.IsArchiveReplay || string.IsNullOrWhiteSpace(openingNode))
        {
            openingPending = false;
            yield break;
        }
        while (DialogueBusy() || GameplayBlocked()) yield return null;
        if (!HasNode(openingNode))
        {
            Debug.LogWarning("Opening tutorial dialogue node is missing: " + openingNode, this);
            openingPending = false;
            yield break;
        }
        openingDialogue = true;
        openingPending = false;
        CaptureGameplay();
        RunOpeningMessage();
    }
    // Interaction callbacks only enqueue. Starting on a later frame lets collection/replay
    // finish before CaptureGameplay disables those interaction components.
    public bool TryQueueGuidance(string node)
    {
        if (!isActiveAndEnabled || string.IsNullOrWhiteSpace(node) || !HasNode(node)) return false;
        if (acceptedGuidance.Contains(node)) return true;
        if (node == openingNode || node == motherNode ||
            sequence.Current != TutorialCompletionSequence.Phase.WaitingForStudy) return false;
        acceptedGuidance.Add(node);
        guidanceQueue.Enqueue(node);
        guidanceReadyFrame = Time.frameCount + 1;
        return true;
    }
    private void Update()
    {
        if (guidanceQueue.Count == 0 || guidanceDialogue || openingPending || openingDialogue ||
            lockHeld || Time.frameCount < guidanceReadyFrame || DialogueBusy() || GameplayBlocked() ||
            sequence.Current != TutorialCompletionSequence.Phase.WaitingForStudy) return;
        string node = guidanceQueue.Dequeue();
        if (!HasNode(node))
        {
            Debug.LogWarning("Queued tutorial guidance node is missing: " + node, this);
            return;
        }
        guidanceDialogue = true;
        RunGuidance(node, ++guidanceGeneration);
    }
    private bool DialogueBusy() => dialogueRunner != null &&
        (dialogueRunner.IsDialogueRunning || !dialogueRunner.DialogueTask.IsCompleted());
    private async void RunGuidance(string node, int generation)
    {
        try
        {
            CaptureGameplay();
            await dialogueRunner.StartDialogue(node);
            if (generation != guidanceGeneration)
            {
                // A reset/disable can happen while presenters are preparing the node.
                if (dialogueRunner != null && dialogueRunner.IsDialogueRunning) StopGuidanceDialogue();
                CameraShift.ReturnCamera();
                return;
            }
            // StartDialogue only starts the node. Keep input locked through all lines,
            // choices, and the presenters' completion animations.
            await dialogueRunner.DialogueTask;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            if (generation == guidanceGeneration && dialogueRunner != null && dialogueRunner.IsDialogueRunning)
                StopGuidanceDialogue();
        }
        finally
        {
            if (generation == guidanceGeneration)
            {
                guidanceDialogue = false;
                guidanceReadyFrame = Time.frameCount + 1;
                CameraShift.ReturnCamera();
                ReleaseGameplay();
            }
        }
    }
    public void ResetGuidance()
    {
        acceptedGuidance.Clear();
        CancelGuidance();
    }
    private void CancelGuidance()
    {
        guidanceQueue.Clear();
        ++guidanceGeneration;
        if (!guidanceDialogue) return;
        guidanceDialogue = false;
        CameraShift.ReturnCamera();
        ReleaseGameplay();
        if (dialogueRunner != null && dialogueRunner.IsDialogueRunning) StopGuidanceDialogue();
    }
    private async void StopGuidanceDialogue()
    {
        try { await dialogueRunner.Stop(); }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }
    private bool GameplayBlocked() => Time.timeScale <= 0f ||
        (tutorialUI != null && (tutorialUI.ExternalInputBlocked ||
        (tutorialUI.manualPanel != null && tutorialUI.manualPanel.activeSelf) ||
        (tutorialUI.settingsPanel != null && tutorialUI.settingsPanel.activeSelf)));
    private bool HasNode(string node) => dialogueRunner != null && dialogueRunner.YarnProject != null &&
        dialogueRunner.YarnProject.Program != null && Array.IndexOf(dialogueRunner.YarnProject.NodeNames, node) >= 0;
    private async void RunOpeningMessage()
    {
        try { await dialogueRunner.StartDialogue(openingNode); }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            openingDialogue = false;
            ReleaseGameplay();
        }
    }
    private void OnEnable()
    {
        dialogueRunner.AddCommandHandler("memory_complete", (Action)RequestMemoryComplete);
        dialogueRunner.onDialogueComplete.AddListener(OnMotherDialogueFinished);
    }
    private void OnDisable()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.RemoveCommandHandler("memory_complete");
            dialogueRunner.onDialogueComplete.RemoveListener(OnMotherDialogueFinished);
        }
        CancelGuidance();
        ReleaseGameplay();
    }
    public bool TryBeginMotherMessage()
    {
        if (state == null || !state.StudyRestored || openingPending || openingDialogue || guidanceDialogue ||
            guidanceQueue.Count > 0 || DialogueBusy() || GameplayBlocked()) return false;
        if (!HasNode(motherNode))
        {
            Debug.LogError("Mother dialogue node is missing from the existing Yarn project: " + motherNode, this);
            return false;
        }
        if (!sequence.TryBegin(state.StudyRestored)) return false;
        CaptureGameplay();
        RunMotherMessage();
        return true;
    }
    private void CaptureGameplay()
    {
        if (lockHeld) return;
        lockHeld = true;
        if (tutorialUI != null)
        {
            previousUIBlock = tutorialUI.ExternalInputBlocked;
            tutorialUI.CloseAllPanels(); tutorialUI.ExternalInputBlocked = true;
        }
        // Yarn keeps its own input and animation time; only world input/components are suspended.
        inputLock.Capture(gameplayInputAssets, pauseTime: false, suspendDialogueInput: false);
        dialogueBlocker.SetActive(true);
    }
    private async void RunMotherMessage()
    {
        try { await dialogueRunner.StartDialogue(motherNode); }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            sequence.AbortDialogue(); ReleaseGameplay();
        }
    }
    private void RequestMemoryComplete() => sequence.RequestCompletion();
    private void OnMotherDialogueFinished()
    {
        if (guidanceDialogue) return;
        if (openingDialogue)
        {
            openingDialogue = false;
            TutorialRewardBridge.MarkMemoryWorldIntroSeen();
            ReleaseGameplay();
            return;
        }
        // The final command arms completion. Yarn's real completion event shows results after presenters finish.
        if (sequence.FinishDialogue())
        {
            TutorialRewardBridge.AwardTutorialOnce();
            dialogueBlocker.SetActive(false);
            resultsPanel.SetActive(true);
        }
        else if (sequence.Current == TutorialCompletionSequence.Phase.MotherDialogue)
        {
            Debug.LogError("Mother dialogue ended without <<memory_complete>>. No reward was awarded.", this);
            sequence.AbortDialogue(); ReleaseGameplay();
        }
    }
    public void ReturnToShop()
    {
        string destination = TutorialRewardBridge.IsArchiveReplay ? "Start Menu" : shopScene;
        if (!Application.CanStreamedLevelBeLoaded(destination))
        {
            Debug.LogError("Add the completion destination to the scene build list: " + destination, this);
            return;
        }
        if (!sequence.TryContinue()) return;
        continueButton.interactable = false;
        ReleaseGameplay();
        bool replay = TutorialRewardBridge.IsArchiveReplay;
        TutorialRewardBridge.FinishArchiveReplay();
        SceneReturnState.ReturnedFromLevel = replay ? null : "Tutorial";
        SceneManager.LoadScene(destination);
    }
    private void ReleaseGameplay()
    {
        if (!lockHeld) return;
        inputLock.Restore(); lockHeld = false;
        if (tutorialUI != null) tutorialUI.ExternalInputBlocked = previousUIBlock;
        if (dialogueBlocker != null) dialogueBlocker.SetActive(false);
    }
}
