using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialMemoryState : MonoBehaviour
{
    [SerializeField] private TutorialVisualStateController visuals;
    [SerializeField] private MemoryFragment[] fragments;
    [SerializeField] private MemoryEcho[] echoes;
    [SerializeField] private string[] fragmentLabels = { "Mother", "used the recorder", "in the Study" };
    [SerializeField] private string[] fragmentAlertNames = { "Mother", "recorder", "Study" };
    [SerializeField] private int[] targetOrder = { 0, 1, 2 };
    [SerializeField] private string[] echoNames = { "Cabinet Key Echo", "Cabinet unlock action" };
    [TextArea] [SerializeField] private string[] echoDescriptions = {
        "Hold E at the cabinet key to reveal its Echo. Release E, then hold E again to capture it. Hold 1 near the locked cabinet to replay it. Removed Echoes can be captured again at the key.",
        "A reconstructed past action: the cabinet is unlocked." };
    public event Action Changed;
    [SerializeField] private float collectionAlertDuration = 2f;
    private readonly Queue<string> collectionAlerts = new Queue<string>();
    private Coroutine alertRoutine;
    private TMP_Text alertText;
    private GameObject alertPanel;
    private TutorialMemoryCompletionController tutorialDialogue;
    public SentenceReconstruction Reconstruction { get; private set; }
    public bool StudyRestored => Reconstruction != null && Reconstruction.Complete && visuals != null && visuals.StudyRestored;
    public bool CabinetUnlocked => visuals != null && visuals.openCabinetWithRecorder != null && visuals.openCabinetWithRecorder.activeSelf;
    private void Awake() => Reconstruction = new SentenceReconstruction(targetOrder);
    public string FragmentLabel(int i) => fragmentLabels[i];
    public string EchoName(int i) => echoNames[i];
    public string EchoDescription(int i) => echoDescriptions[i];
    public bool HasFragment(int i) => i >= 0 && i < fragments.Length &&
        fragments[i] != null && LevelInventory.Instance != null &&
        LevelInventory.Instance.memoryFragments.Contains(fragments[i]);
    public bool EchoDiscovered(int i) => echoes[i] != null && echoes[i].IsDiscovered;
    public bool EchoStored(int i) => echoes[i] != null && echoes[i].IsStored;
    public string EchoStatus(int i) => echoes[i] is TutorialLockedCabinet
        ? "Replayed / cabinet unlocked" : EchoStored(i) ? "Captured / stored" : "Discovered / not stored";
    public void CollectFragment(int i)
    {
        if (!HasFragment(i) && fragments[i] != null && LevelInventory.Instance != null)
        {
            LevelInventory.Instance.Collect(fragments[i]);
            AudioManager.Instance?.PlayCollection();
            ShowAlert("Fragment collected: " + fragmentAlertNames[i]);
        }
        Changed?.Invoke();
    }
    public void CaptureEcho(int i)
    {
        if (!(echoes[i] is TutorialMemoryEcho key) || !key.CanCapture) return;
        if (echoes[i] != null && !echoes[i].IsStored && LevelInventory.Instance != null)
        {
            LevelInventory.Instance.Collect(echoes[i]);
            AudioManager.Instance?.PlayCollection();
            ShowAlert("Echo collected: " + EchoName(i));
            TryQueueGuidance("Tutorial_Key_Echo_Captured");
        }
        Changed?.Invoke();
    }
    public void DiscoverEcho(int i)
    {
        if (echoes[i] != null) echoes[i].MarkDiscovered();
        Changed?.Invoke();
        if (i == 1 && CabinetUnlocked) TryQueueGuidance("Tutorial_Cabinet_Opened");
    }
    public bool TryQueueGuidance(string node)
    {
        if (tutorialDialogue == null)
            tutorialDialogue = FindAnyObjectByType<TutorialMemoryCompletionController>();
        return tutorialDialogue != null && tutorialDialogue.TryQueueGuidance(node);
    }
    public void QueueFragmentGuidance(int index, string collectionNode, DialogueManager fallback)
    {
        // The recorder can be found after the Study fragment; never point at a collected object.
        string node = index == 1 && HasFragment(2) ? "Recorder_Study_Collected" : collectionNode;
        if (!string.IsNullOrWhiteSpace(node) && !TryQueueGuidance(node) && fallback != null)
            fallback.StartDialogue(node);

        if (HasFragment(0) && HasFragment(1) && HasFragment(2))
            TryQueueGuidance("Tutorial_Reconstruction_Ready");
        else if (HasFragment(1) && HasFragment(2))
            TryQueueGuidance("Tutorial_Find_Mother_Fragment");
    }
    public void NotifyChanged() => Changed?.Invoke();
    public void RemoveKeyEcho()
    {
        if (!EchoStored(0) || LevelInventory.Instance == null) return;
        LevelInventory.Instance.RemoveEcho(echoes[0]);
        if (echoes[0] is TutorialMemoryEcho key) key.ResetPreview();
        Changed?.Invoke();
    }
    public void ShowAlert(string message)
    {
        collectionAlerts.Enqueue(message);
        if (alertRoutine == null) alertRoutine = StartCoroutine(DisplayCollectionAlerts());
    }
    private IEnumerator DisplayCollectionAlerts()
    {
        if (alertPanel == null)
        {
            GameObject canvasObject = new GameObject("Collection_Alerts", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            alertPanel = new GameObject("Collected_Alert", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            alertPanel.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = (RectTransform)alertPanel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f); panelRect.anchoredPosition = new Vector2(0f, -70f);
            panelRect.sizeDelta = new Vector2(760, 80);
            Image background = alertPanel.GetComponent<Image>(); background.color = new Color(0.13f, 0.10f, 0.06f, 0.95f); background.raycastTarget = false;
            GameObject label = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(alertPanel.transform, false);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16, 8); labelRect.offsetMax = new Vector2(-16, -8);
            alertText = label.GetComponent<TextMeshProUGUI>();
            alertText.fontSize = 30; alertText.alignment = TextAlignmentOptions.Center;
            alertText.color = new Color(1f, 0.91f, 0.72f); alertText.raycastTarget = false;
        }
        while (collectionAlerts.Count > 0)
        {
            alertText.text = collectionAlerts.Dequeue(); alertPanel.SetActive(true);
            yield return new WaitForSeconds(Mathf.Max(0.1f, collectionAlertDuration));
            alertPanel.SetActive(false);
        }
        alertRoutine = null;
    }
    public bool Reconstruct()
    {
        if (!Reconstruction.TryComplete()) return false;
        if (visuals != null) visuals.RestoreStudy();
        Changed?.Invoke();
        return true;
    }
    public void ResetProgress()
    {
        if (tutorialDialogue != null) tutorialDialogue.ResetGuidance();
        collectionAlerts.Clear();
        if (alertRoutine != null) StopCoroutine(alertRoutine);
        alertRoutine = null;
        if (alertPanel != null) alertPanel.SetActive(false);
        LevelInventory inventory = LevelInventory.Instance;
        foreach (MemoryFragment fragment in fragments)
            if (inventory != null) inventory.memoryFragments.Remove(fragment);
        foreach (MemoryEcho echo in echoes)
        {
            if (inventory != null) inventory.RemoveEcho(echo);
            if (echo != null) echo.ResetDiscovery();
            if (echo is TutorialMemoryEcho key) key.ResetPreview();
            if (echo is TutorialLockedCabinet cabinet) cabinet.ResetReplay();
        }
        Reconstruction = new SentenceReconstruction(targetOrder);
        Changed?.Invoke();
    }
}
