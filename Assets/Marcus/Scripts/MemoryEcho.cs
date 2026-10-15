using System.ComponentModel;
using UnityEngine;
using System;
using System.Collections;

// Click Echo once, show animation
// Animation waits for player to press again to collect
// After collect, add into LevelInventory.
public class MemoryEcho : Collectible
{
    [Header("Memory Echo Settings")]
    [Description("A series of animation packed into a Prefab, to be played whenever the echo is used.")]
    [SerializeField] private GameObject echoReplay;
    private EchoReplayVisual shortcutVisual;
    private Coroutine shortcutRoutine;
    private static Sprite fallbackSprite;
    protected static Sprite FallbackSprite => fallbackSprite != null ? fallbackSprite :
        fallbackSprite = UnityEngine.Sprite.Create(Texture2D.whiteTexture,
            new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width / 0.18f);
    protected EchoReplayVisual CreateReplayVisual(string visualName, Sprite image, float height = 0.38f) =>
        EchoReplayVisual.Create(transform, echoReplay, image != null ? image : FallbackSprite, visualName, height);

    protected IEnumerator PlayReplayVisual(EchoReplayVisual visual, Vector3 position,
        float holdSeconds, Func<bool> blocked)
    {
        visual.RevealAt(position);
        yield return visual.FadeTo(0.85f, 0.3f, blocked);
        yield return visual.Hold(holdSeconds, blocked);
        yield return visual.FadeTo(0f, 0.3f, blocked);
        visual.ResetVisual();
    }
    public bool IsDiscovered { get; private set; }
    public bool IsStored => LevelInventory.Instance != null && LevelInventory.Instance.memoryEchoes.Contains(this);
    public void ResetDiscovery() => IsDiscovered = false;
    public void MarkDiscovered() => IsDiscovered = true;
    public override void OnCollect()
    {
        IsDiscovered = true;
    }

    // Generic slot replay is presentation only; target interactions validate world actions.
    public virtual void TriggerEchoFunction()
    {
        if (!IsStored || shortcutRoutine != null || echoReplay == null || Time.timeScale <= 0f ||
            (GameController.Instance != null && GameController.Instance.isPaused)) return;
        if (shortcutVisual == null) shortcutVisual = CreateReplayVisual("Echo_Replay", sprite);
        shortcutRoutine = StartCoroutine(PlayShortcut());
    }
    private IEnumerator PlayShortcut()
    {
        yield return PlayReplayVisual(shortcutVisual, transform.position, 1f,
            () => Time.timeScale <= 0f || (GameController.Instance != null && GameController.Instance.isPaused));
        shortcutRoutine = null;
    }
    protected virtual void OnDisable()
    {
        if (shortcutRoutine != null) StopCoroutine(shortcutRoutine);
        shortcutRoutine = null;
        if (shortcutVisual != null) shortcutVisual.ResetVisual();
    }
}
