using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Yarn.Unity;

// A read-only session log. It never returns an option or advances Yarn.
[DisallowMultipleComponent]
[DefaultExecutionOrder(-150)]
public sealed class DialogueHistory : DialoguePresenterBase
{
    private struct Entry
    {
        public string Speaker;
        public string Text;
    }

    private const int Capacity = 200;
    private static readonly List<Entry> entries = new List<Entry>();
    private static readonly Regex tags = new Regex("<[^>]*>");
    private static readonly Color Cream = new Color(1f, .92f, .73f);
    private static readonly Color Gold = new Color(.68f, .47f, .23f);
    private readonly GameplayInputPause pause = new GameplayInputPause();
    private readonly Dictionary<TutorialUIController, bool> uiLocks = new Dictionary<TutorialUIController, bool>();
    private readonly List<ShopUIController> suspendedShopControls = new List<ShopUIController>();
    private readonly List<TMP_Text> rows = new List<TMP_Text>();
    private DialogueRunner runner;
    private LinePresenter linePresenter;
    private Entry pending;
    private bool hasPending;
    private GameObject overlay;
    private RectTransform window;
    private RectTransform content;
    private RectTransform viewport;
    private ScrollRect scroll;
    private TMP_Text emptyLabel;
    private Button closeButton;
    private GameObject previousSelection;
    private int closedFrame = -1;

    public bool IsOpen { get; private set; }
    public bool BlocksAdvanceThisFrame => IsOpen || closedFrame == Time.frameCount;
    public int EntryCount => entries.Count;
    public static bool AnyOpen { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        ClearSession();
        AnyOpen = false;
    }

    public static void ClearSession() => entries.Clear();

    public void Configure(DialogueRunner dialogueRunner, LinePresenter presenter)
    {
        if (runner != null && runner != dialogueRunner) Unregister();
        runner = dialogueRunner;
        linePresenter = presenter;
        if (runner == null) return;
        var presenters = new List<DialoguePresenterBase>(runner.DialoguePresenters);
        if (!presenters.Contains(this))
        {
            presenters.Add(this);
            runner.DialoguePresenters = presenters;
        }
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        hasPending = false;
        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        CommitShownLine();
        pending = new Entry
        {
            Speaker = Plain(line.CharacterName),
            Text = Plain(line.TextWithoutCharacterName.Text)
        };
        hasPending = !string.IsNullOrWhiteSpace(pending.Text);
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        CommitShownLine();
        hasPending = false;
        return YarnTask.CompletedTask;
    }

    private static string Plain(string value) => string.IsNullOrEmpty(value) ? "" : tags.Replace(value, "").Trim();

    private void CommitShownLine()
    {
        if (!hasPending || linePresenter == null) return;
        bool shown = linePresenter.Typewriter is AdjustableDialogueTypewriter writer
            ? writer.IsFullyVisible
            : linePresenter.lineText != null && linePresenter.lineText.maxVisibleCharacters >=
                linePresenter.lineText.textInfo.characterCount;
        if (!shown || (linePresenter.canvasGroup != null && linePresenter.canvasGroup.alpha <= .01f)) return;
        entries.Add(pending);
        if (entries.Count > Capacity) entries.RemoveAt(0);
        hasPending = false;
        if (IsOpen) RefreshRows();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (IsOpen)
        {
            if (keyboard.hKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame) Close();
        }
        else if (!AnyOpen && keyboard.hKey.wasPressedThisFrame &&
            (entries.Count > 0 || (runner != null && runner.IsDialogueRunning))) Open();
    }

    private void LateUpdate()
    {
        CommitShownLine();
        if (IsOpen) Fit();
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (IsOpen || AnyOpen || Time.timeScale <= 0f || !isActiveAndEnabled) return;
        foreach (TutorialUIController ui in Object.FindObjectsByType<TutorialUIController>(FindObjectsInactive.Exclude))
            if ((ui.manualPanel != null && ui.manualPanel.activeSelf) ||
                (ui.settingsPanel != null && ui.settingsPanel.activeSelf)) return;
        foreach (ShopUIController shop in Object.FindObjectsByType<ShopUIController>(FindObjectsInactive.Exclude))
            if (shop.IsShopInputBlocked) return;
        CommitShownLine();
        if (overlay == null) Build();
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        uiLocks.Clear();
        foreach (TutorialUIController ui in Object.FindObjectsByType<TutorialUIController>(FindObjectsInactive.Exclude))
        {
            uiLocks.Add(ui, ui.ExternalInputBlocked);
            ui.ExternalInputBlocked = true;
        }
        // The shop owns Q separately from TutorialUIController. Suspend its
        // hotkeys so a second modal cannot capture this paused time scale.
        suspendedShopControls.Clear();
        foreach (ShopUIController shop in Object.FindObjectsByType<ShopUIController>(FindObjectsInactive.Exclude))
            if (shop.enabled) { suspendedShopControls.Add(shop); shop.enabled = false; }
        pause.Capture(null, pauseTime: true, suspendDialogueInput: true);
        IsOpen = AnyOpen = true;
        overlay.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Fit();
        RefreshRows();
        if (EventSystem.current != null) closeButton.Select();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = AnyOpen = false;
        closedFrame = Time.frameCount;
        if (overlay != null) overlay.SetActive(false);
        pause.Restore();
        foreach (var pair in uiLocks) if (pair.Key != null) pair.Key.ExternalInputBlocked = pair.Value;
        uiLocks.Clear();
        foreach (ShopUIController shop in suspendedShopControls) if (shop != null) shop.enabled = true;
        suspendedShopControls.Clear();
        EventSystem events = EventSystem.current;
        if (events != null)
        {
            Selectable selected = previousSelection != null ? previousSelection.GetComponent<Selectable>() : null;
            events.SetSelectedGameObject(selected != null && selected.isActiveAndEnabled && selected.IsInteractable()
                ? previousSelection : null);
        }
        previousSelection = null;
    }

    private void Build()
    {
        overlay = new GameObject("Dialogue_History_Overlay", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlay.layer = 5;
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30000;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        Image dimmer = Image("Dimmer", overlay.transform, new Color(.04f, .03f, .02f, .55f), true);
        Stretch(dimmer.rectTransform, Vector2.zero, Vector2.zero);
        Image border = Image("History_Window", overlay.transform, Gold);
        window = border.rectTransform;
        window.anchorMin = window.anchorMax = window.pivot = new Vector2(.5f, .5f);
        window.sizeDelta = new Vector2(1280, 820);
        Image face = Image("Paper", window, new Color(.16f, .11f, .075f, .98f));
        Stretch(face.rectTransform, new Vector2(4, 4), new Vector2(-4, -4));
        TMP_Text title = Label("Title", window, "Dialogue History", 38, true);
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0, 1);
        title.rectTransform.pivot = new Vector2(0, 1);
        title.rectTransform.anchoredPosition = new Vector2(38, -26);
        title.rectTransform.sizeDelta = new Vector2(1150, 58);
        Image divider = Image("Divider", window, Gold);
        divider.rectTransform.anchorMin = new Vector2(0, 1);
        divider.rectTransform.anchorMax = Vector2.one;
        divider.rectTransform.pivot = new Vector2(.5f, 1);
        divider.rectTransform.offsetMin = new Vector2(36, -96);
        divider.rectTransform.offsetMax = new Vector2(-36, -94);

        RectTransform scrollRoot = Rect("History_Scroll", window);
        Stretch(scrollRoot, new Vector2(36, 108), new Vector2(-36, -112));
        scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;
        Image clip = Image("Viewport", scrollRoot, Color.clear, true);
        viewport = clip.rectTransform;
        Stretch(viewport, Vector2.zero, new Vector2(-30, 0));
        clip.gameObject.AddComponent<RectMask2D>();
        content = Rect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 24f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = content;

        Image track = Image("Scroll_Track", scrollRoot, new Color(.08f, .055f, .035f), true);
        track.rectTransform.anchorMin = new Vector2(1, 0);
        track.rectTransform.anchorMax = Vector2.one;
        track.rectTransform.pivot = new Vector2(1, .5f);
        track.rectTransform.offsetMin = new Vector2(-14, 0);
        track.rectTransform.offsetMax = Vector2.zero;
        Image handle = Image("Handle", track.transform, Gold, true);
        Stretch(handle.rectTransform, Vector2.zero, Vector2.zero);
        Scrollbar bar = track.gameObject.AddComponent<Scrollbar>();
        bar.handleRect = handle.rectTransform;
        bar.targetGraphic = handle;
        bar.direction = Scrollbar.Direction.BottomToTop;
        bar.navigation = new Navigation { mode = Navigation.Mode.None };
        scroll.verticalScrollbar = bar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        emptyLabel = Label("Empty", viewport, "Dialogue you have read will appear here.", 28);
        Stretch(emptyLabel.rectTransform, new Vector2(14, 0), new Vector2(-14, 0));
        TMP_Text hint = Label("Close_Hint", window, "H / Esc to close", 22);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0, 0);
        hint.rectTransform.pivot = Vector2.zero;
        hint.rectTransform.anchoredPosition = new Vector2(38, 32);
        hint.rectTransform.sizeDelta = new Vector2(720, 58);
        hint.alignment = TextAlignmentOptions.MidlineLeft;
        Image closeFace = Image("Close_History", window, new Color(.14f, .30f, .23f), true);
        RectTransform closeRect = closeFace.rectTransform;
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1, 0);
        closeRect.pivot = new Vector2(1, 0);
        closeRect.anchoredPosition = new Vector2(-38, 32);
        closeRect.sizeDelta = new Vector2(240, 60);
        Outline outline = closeFace.gameObject.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(2, -2);
        closeButton = closeFace.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeFace;
        closeButton.navigation = new Navigation { mode = Navigation.Mode.None };
        closeButton.onClick.AddListener(Close);
        TMP_Text closeText = Label("Label", closeRect, "Close", 26, true);
        closeText.alignment = TextAlignmentOptions.Center;
        Stretch(closeText.rectTransform, new Vector2(8, 4), new Vector2(-8, -4));
        overlay.SetActive(false);
    }

    private void RefreshRows()
    {
        Canvas.ForceUpdateCanvases();
        float width = Mathf.Max(100f, viewport.rect.width - 28f);
        for (int i = 0; i < entries.Count; i++)
        {
            if (rows.Count <= i)
            {
                TMP_Text row = Label("History_Line_" + i, content, "", 28);
                row.gameObject.AddComponent<LayoutElement>();
                row.overflowMode = TextOverflowModes.Overflow;
                rows.Add(row);
            }
            TMP_Text text = rows[i];
            Entry entry = entries[i];
            text.gameObject.SetActive(true);
            text.text = string.IsNullOrWhiteSpace(entry.Speaker) ? entry.Text
                : "<b><color=#EDD5A3>" + entry.Speaker + "</color></b>\n" + entry.Text;
            float height = Mathf.Ceil(text.GetPreferredValues(text.text, width, Mathf.Infinity).y);
            LayoutElement layout = text.GetComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = Mathf.Max(36, height);
            layout.flexibleHeight = 0;
        }
        for (int i = entries.Count; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
        emptyLabel.gameObject.SetActive(entries.Count == 0);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 0f;
    }

    private void Fit()
    {
        if (window == null || overlay == null) return;
        Vector2 available = ((RectTransform)overlay.transform).rect.size;
        window.localScale = Vector3.one * Mathf.Max(.1f, Mathf.Min(1f,
            available.x * .92f / 1280f, available.y * .90f / 820f));
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject item = new GameObject(name, typeof(RectTransform));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        return item.GetComponent<RectTransform>();
    }

    private static Image Image(string name, Transform parent, Color colour, bool raycast = false)
    {
        RectTransform rect = Rect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = colour;
        image.raycastTarget = raycast;
        return image;
    }

    private TMP_Text Label(string name, Transform parent, string value, float size, bool bold = false)
    {
        RectTransform rect = Rect(name, parent);
        TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (linePresenter != null && linePresenter.lineText != null) label.font = linePresenter.lineText.font;
        label.text = value;
        label.fontSize = size;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.color = Cream;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.margin = Vector4.zero;
        label.lineSpacing = 4f;
        return label;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
    }

    private void Unregister()
    {
        if (runner == null) return;
        var presenters = new List<DialoguePresenterBase>(runner.DialoguePresenters);
        if (presenters.Remove(this)) runner.DialoguePresenters = presenters;
    }

    private void OnDisable() => Close();

    private void OnDestroy()
    {
        Close();
        Unregister();
        if (overlay != null) Destroy(overlay);
    }
}
