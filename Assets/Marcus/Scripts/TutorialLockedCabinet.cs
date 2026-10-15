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
    [SerializeField] private UnityEngine.Sprite replayPlaceholderSprite;
    [SerializeField] private float replayDuration = 1f;
    private readonly CabinetEchoReplay replay = new CabinetEchoReplay();
    private GameObject replayImage;
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
            state.ShowAlert("No Echo stored. Capture the Cabinet Key Echo first.");
            return;
        }
        if (!replay.Begin(true, near, Blocked, IsOpen)) return;
        replayRoutine = StartCoroutine(PlayCabinetReplay());
    }

    private IEnumerator PlayCabinetReplay()
    {
        if (replayImage == null)
        {
            replayImage = EchoPlaceholderVisual.Create(transform, "Cabinet_Key_Echo_Replay",
                replayPlaceholderSprite != null ? replayPlaceholderSprite : EchoPlaceholderVisual.Square);
        }
        Vector3 origin = interactionArea != null
            ? transform.InverseTransformPoint(interactionArea.bounds.center) : Vector3.zero;
        origin += Vector3.up * 0.18f;
        replayImage.transform.localPosition = origin;
        replayImage.SetActive(true);
        yield return EchoPlaceholderVisual.Fade(replayImage, 0f, 0.85f, 0.3f, () => Blocked);
        float elapsed = 0f;
        while (elapsed < Mathf.Max(0.1f, replayDuration))
        {
            if (!Blocked)
            {
                elapsed += Time.deltaTime;
            }
            yield return null;
        }
        yield return EchoPlaceholderVisual.Fade(replayImage, 0.85f, 0f, 0.3f, () => Blocked);
        while (Blocked) yield return null;
        replayRoutine = null;
        replayImage.SetActive(false);
        if (!state.EchoStored(0) || !replay.Finish()) { replay.Reset(); yield break; }
        visuals.OpenCabinet();
        state.DiscoverEcho(1);
    }

    public void ResetReplay()
    {
        if (replayRoutine != null) StopCoroutine(replayRoutine);
        replayRoutine = null;
        replay.Reset();
        holdElapsed = 0f; holdTriggered = false;
        if (replayImage != null) replayImage.SetActive(false);
    }
    private void OnDisable() => ResetReplay();
    // Interact shows the instruction; only the dedicated 1 input replays the stored Echo.
    public override void Interact() { }
}
