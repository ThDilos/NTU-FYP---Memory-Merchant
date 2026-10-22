using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialMemoryEcho : MemoryEcho
{
    [SerializeField] private TutorialMemoryState state;
    [SerializeField] private TutorialUIController tutorialUI;
    [SerializeField] private int echoIndex;
    [Header("Key Echo visual")]
    [Tooltip("Sprite shown when revealing the key Echo; fades out after capture.")]
    [SerializeField] private UnityEngine.Sprite previewSprite;
    [Tooltip("Height of Mum's key-taking pose in world units, including sprite padding.")]
    [SerializeField, Min(0.01f)] private float previewHeight = 0.6f;
    [SerializeField] private float previewDuration = 0.3f;
    [SerializeField] private float captureHoldDuration = 0.4f;
    private readonly KeyEchoSequence sequence = new KeyEchoSequence();
    private EchoReplayVisual preview;
    private Coroutine animationRoutine;
    private Interactor player;
    private float heldTime;
    private bool waitForRelease;
    public bool CanCapture => sequence.Current == KeyEchoSequence.Phase.ReadyToCapture && !IsStored;
    public UnityEngine.Sprite ReplayImage => previewSprite != null ? previewSprite : FallbackSprite;
    public override string InteractionHint => IsStored ? "Echo stored" : "Hold E";
    public override Vector2 InteractionMarkerPosition => (Vector2)GetComponent<Collider2D>().bounds.center + Vector2.up * 0.22f + interactionMarkerOffset;
    private bool Blocked => Time.timeScale <= 0f || (tutorialUI != null && (tutorialUI.ExternalInputBlocked ||
        ((tutorialUI.manualPanel != null && tutorialUI.manualPanel.activeSelf) ||
         (tutorialUI.settingsPanel != null && tutorialUI.settingsPanel.activeSelf))));
    public override bool IsInteractable => !Blocked && sequence.Current != KeyEchoSequence.Phase.Playing;
    private void Start() => player = FindAnyObjectByType<Interactor>();
    protected new void Update()
    {
        base.Update();
        if (sequence.Current == KeyEchoSequence.Phase.Captured && !IsStored && animationRoutine == null) sequence.Reset();
        bool held = Keyboard.current != null && Keyboard.current.eKey.isPressed;
        if (!held) { waitForRelease = false; heldTime = 0f; }
        Collider2D area = GetComponent<Collider2D>();
        bool near = player != null && Vector2.Distance(player.transform.position, area.ClosestPoint(player.transform.position)) <= 0.2f;
        if (!CanCapture || Blocked || !near || waitForRelease) { heldTime = 0f; return; }
        if (held)
        {
            heldTime += Time.deltaTime;
            if (heldTime >= Mathf.Max(0.1f, captureHoldDuration))
            { waitForRelease = true; heldTime = 0f; state.CaptureEcho(echoIndex); }
        }
    }
    public override void Interact()
    {
        if (!IsInteractable || state == null || IsStored || CanCapture) return;
        if (sequence.Current == KeyEchoSequence.Phase.Captured) sequence.Reset();
        if (sequence.Begin()) animationRoutine = StartCoroutine(PlayPreview());
    }
    private IEnumerator PlayPreview()
    {
        if (preview == null) preview = CreateReplayVisual("Key_Echo_Preview", ReplayImage, previewHeight);
        preview.RevealAt(GetComponent<Collider2D>().bounds.center + Vector3.down * 0.2f);

        yield return preview.FadeTo(0.85f, previewDuration, () => Blocked);
        sequence.FinishPreview(); waitForRelease = true; animationRoutine = null;
    }
    public override void OnCollect()
    {
        base.OnCollect(); sequence.Capture();
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(HidePreview());
    }
    private IEnumerator HidePreview()
    {
        if (preview != null)
        {
            yield return preview.FadeTo(0f, previewDuration, () => Blocked);
            preview.ResetVisual();
        }
        animationRoutine = null;
    }
    public void ResetPreview()
    {
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = null; sequence.Reset(); heldTime = 0f; waitForRelease = true;
        if (preview != null) preview.ResetVisual();
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        if (animationRoutine != null || sequence.Current == KeyEchoSequence.Phase.Playing) ResetPreview();
        else if (preview != null) preview.ResetVisual();
    }
}
