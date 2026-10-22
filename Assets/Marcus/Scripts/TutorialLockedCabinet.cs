using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialLockedCabinet : MemoryEcho
{
    [SerializeField] private TutorialMemoryState state;
    [SerializeField] private TutorialVisualStateController visuals;
    [SerializeField] private TutorialUIController tutorialUI;
    [SerializeField] private float useRange = 0.25f;
    [Header("Editable cabinet Echo replay")]
    [SerializeField] private TutorialMemoryEcho keyEcho;
    [Tooltip("Unlock pose shown during replay, before the cabinet opens.")]
    [SerializeField] private UnityEngine.Sprite replayPlaceholderSprite;
    [Tooltip("Height of Mum's key-using pose in world units, including sprite padding.")]
    [SerializeField, Min(0.01f)] private float replayHeight = 0.6f;
    [SerializeField] private float replayDuration = 1f;
    [SerializeField] private UnityEngine.Events.UnityEvent replayCompleted = new UnityEngine.Events.UnityEvent();
    private readonly CabinetEchoReplay replay = new CabinetEchoReplay();
    private EchoReplayVisual replayImage;
    private Coroutine replayRoutine;
    [SerializeField] private float useHoldDuration = 0.4f;
    private float holdElapsed;
    private bool holdTriggered;
    private Interactor player;
    private Collider2D interactionArea;
    private void Start()
    {
        player = FindAnyObjectByType<Interactor>();
        interactionArea = GetComponent<Collider2D>();
    }
    private bool IsOpen => visuals != null && visuals.openCabinetWithRecorder != null &&
        visuals.openCabinetWithRecorder.activeSelf;
    private bool Blocked => Time.timeScale <= 0f || (tutorialUI != null && (tutorialUI.ExternalInputBlocked ||
        ((tutorialUI.manualPanel != null && tutorialUI.manualPanel.activeSelf) ||
         (tutorialUI.settingsPanel != null && tutorialUI.settingsPanel.activeSelf))));
    public override bool IsInteractable => !IsOpen && !Blocked && !replay.Playing;
    public override string InteractionHint => "Hold 1";
    protected new void Update()
    {
        base.Update();
        interactionPrompt = state != null && state.EchoStored(0)
            ? "Use Key Echo: press 1" : "Locked - capture the Key Echo first";
        if (Keyboard.current == null || Blocked || !IsInteractable)
        { holdElapsed = 0f; holdTriggered = false; return; }
        bool held = Keyboard.current.digit1Key.isPressed || Keyboard.current.numpad1Key.isPressed;
        bool near = player != null && interactionArea != null && Vector2.Distance(player.transform.position,
            interactionArea.ClosestPoint(player.transform.position)) <= useRange;
        if (!held || !near || state == null)
        { holdElapsed = 0f; holdTriggered = false; return; }
        if (!holdTriggered)
        {
            holdElapsed += Time.deltaTime;
            if (holdElapsed >= Mathf.Max(0.1f, useHoldDuration)) { holdTriggered = true; TryUseKeyEcho(); }
        }
    }
    public void TryUseKeyEcho()
    {
        bool near = player != null && interactionArea != null &&
            Vector2.Distance(player.transform.position,
                interactionArea.ClosestPoint(player.transform.position)) <= useRange;
        if (state == null || visuals == null || !near || Blocked || IsOpen || replay.Playing) return;
        if (!state.EchoStored(0))
        {
            ExplainMissingEcho();
            return;
        }
        if (!replay.Begin(true, near, Blocked, IsOpen)) return;
        replayRoutine = StartCoroutine(PlayCabinetReplay());
    }

    private void ExplainMissingEcho()
    {
        state.ShowAlert("No Echo stored. Capture the Cabinet Key Echo first.");
        state.TryQueueGuidance("Tutorial_Cabinet_Locked");
    }

    private IEnumerator PlayCabinetReplay()
    {
        if (replayImage == null)
            replayImage = CreateReplayVisual("Cabinet_Key_Echo_Replay", replayPlaceholderSprite, replayHeight);
        Vector3 origin = interactionArea != null ? interactionArea.bounds.center : transform.position;
        origin += Vector3.down * 0.1f;
        yield return PlayReplayVisual(replayImage, origin, replayDuration, () => Blocked);
        while (Blocked) yield return null;
        replayRoutine = null;
        if (!state.EchoStored(0) || !replay.Finish()) { replay.Reset(); yield break; }
        visuals.OpenCabinet();
        state.DiscoverEcho(1);
        replayCompleted.Invoke();
    }

    public void ResetReplay()
    {
        if (replayRoutine != null) StopCoroutine(replayRoutine);
        replayRoutine = null;
        replay.Reset();
        holdElapsed = 0f; holdTriggered = false;
        if (replayImage != null) replayImage.ResetVisual();
    }
    protected override void OnDisable() { base.OnDisable(); ResetReplay(); }
    // E asks Mem for guidance; only the dedicated 1 input replays the stored Echo.
    public override void Interact()
    {
        if (!IsInteractable || state == null) return;
        if (!state.EchoStored(0)) ExplainMissingEcho();
        else state.TryQueueGuidance("Tutorial_Key_Echo_Captured");
    }
}
