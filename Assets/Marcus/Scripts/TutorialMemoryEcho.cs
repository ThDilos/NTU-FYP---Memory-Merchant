using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialMemoryEcho : MemoryEcho
{
    [SerializeField] private TutorialMemoryState state;
    [SerializeField] private TutorialUIController tutorialUI;
    [SerializeField] private int echoIndex;
    [Header("Editable placeholder preview")]
    [SerializeField] private UnityEngine.Sprite previewSprite;
    [SerializeField] private float previewDuration = 0.3f;
    [SerializeField] private float captureHoldDuration = 0.4f;
    private readonly KeyEchoSequence sequence = new KeyEchoSequence();
    private GameObject preview;
    private Coroutine animationRoutine;
    private Interactor player;
    private float heldTime;
    private bool waitForRelease;
    public bool CanCapture => sequence.Current == KeyEchoSequence.Phase.ReadyToCapture && !IsStored;
    public UnityEngine.Sprite ReplayImage => previewSprite != null ? previewSprite : EchoPlaceholderVisual.Square;
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
        if (preview == null) preview = EchoPlaceholderVisual.Create(transform, "Key_Echo_Preview", ReplayImage);
        preview.transform.localPosition = transform.InverseTransformPoint(GetComponent<Collider2D>().bounds.center) + Vector3.up * 0.05f;
        preview.SetActive(true);
        yield return EchoPlaceholderVisual.Fade(preview, 0f, 0.85f, previewDuration, () => Blocked);
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
            yield return EchoPlaceholderVisual.Fade(preview, 0.85f, 0f, previewDuration, () => Blocked);
            preview.SetActive(false);
        }
        animationRoutine = null;
    }
    public void ResetPreview()
    {
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = null; sequence.Reset(); heldTime = 0f; waitForRelease = true;
        if (preview != null) preview.SetActive(false);
    }
    private void OnDisable()
    {
        if (animationRoutine != null || sequence.Current == KeyEchoSequence.Phase.Playing) ResetPreview();
        else if (preview != null) preview.SetActive(false);
    }
}

internal static class EchoPlaceholderVisual
{
    private static UnityEngine.Sprite square;
    public static UnityEngine.Sprite Square => square != null ? square : square = UnityEngine.Sprite.Create(Texture2D.whiteTexture,
        new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width / 0.18f);
    public static GameObject Create(Transform parent, string name, UnityEngine.Sprite sprite)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false);
        SpriteRenderer image = go.AddComponent<SpriteRenderer>(); image.sprite = sprite; image.sortingOrder = 20;
        image.color = new Color(0.8f, 0.95f, 1f, 0f); return go;
    }
    public static IEnumerator Fade(GameObject go, float from, float to, float duration, System.Func<bool> blocked)
    {
        SpriteRenderer image = go.GetComponent<SpriteRenderer>(); float elapsed = 0f;
        image.color = new Color(0.8f, 0.95f, 1f, from);
        while (elapsed < Mathf.Max(0.01f, duration))
        {
            if (!blocked())
            {
                elapsed += Time.deltaTime;
                image.color = new Color(0.8f, 0.95f, 1f, Mathf.Lerp(from, to, elapsed / Mathf.Max(0.01f, duration)));
            }
            yield return null;
        }
        image.color = new Color(0.8f, 0.95f, 1f, to);
    }
}
