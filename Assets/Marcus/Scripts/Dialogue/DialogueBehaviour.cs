
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class DialogueBehaviour : MonoBehaviour
{
    internal const float DialogueTextSize = 32f;
    internal static readonly Color DialogueTextColor = new Color(.98f, .95f, .88f);
    private const float PanelGutter = .1f;
    private const float PanelBottom = 18f;

    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private LinePresenter linePresenter;
    [SerializeField] private LineAdvancer lineAdvancer;
    [SerializeField] private OptionsPresenter optionsPresenter;

    [Header("Buttons")]
    [SerializeField] private Button speedButton;
    [SerializeField] private Button autoButton;

    [Header("Typewriter Speed")]
    [SerializeField] private int normalSpeed = 60;

    [Header("Button Color")]
    [SerializeField] private Color normalColor = new Color(.22f, .14f, .08f);
    [SerializeField] private Color selectedColor = new Color(.14f, .30f, .23f);

    private bool isAutoAdvance;
    private bool isFastSpeed;
    private AdjustableDialogueTypewriter typewriter;
    private float autoElapsed;
    private int lineVersion = -1;
    private bool advanceRequested;
    private DialogueChoiceFocus choices;
    private Button continueButton;
    private Button historyButton;
    private DialogueHistory history;
    private TMP_Text continueLabel;
    private TMP_Text controlHint;
    private Image advanceSurface;
    private Yarn.Unity.LineAdvancerInput.KeyCodes originalKeyboardInput;
    private bool originalKeyboardEnabled;
    private int lineStartedFrame = -1;
    private int lastRequestFrame = -1;

    public DialogueRunner Runner => dialogueRunner;
    public Button AutoControl => autoButton;
    public Button SpeedControl => speedButton;
    public Button HistoryControl => historyButton;

    public void Configure(DialogueRunner runner)
    {
        dialogueRunner = runner;
        if (runner == null) return;
        if (linePresenter == null) linePresenter = runner.GetComponentInChildren<LinePresenter>(true);
        if (lineAdvancer == null) lineAdvancer = runner.GetComponentInChildren<LineAdvancer>(true);
        if (optionsPresenter == null) optionsPresenter = runner.GetComponentInChildren<OptionsPresenter>(true);
    }

    private void Start()
    {
        Configure(dialogueRunner != null ? dialogueRunner : GetComponentInParent<DialogueRunner>());
        if (dialogueRunner == null || linePresenter == null || lineAdvancer == null)
        {
            Debug.LogError("Please assign DialogueRunner, LinePresenter and LineAdvancer");
            return;
        }
        StylePresenters();

        isFastSpeed = false;
        isAutoAdvance = linePresenter.autoAdvance;
        // Own the timer so Auto can also be switched on/off on a fully shown line.
        linePresenter.autoAdvance = false;
        typewriter = linePresenter.Typewriter as AdjustableDialogueTypewriter;
        if (typewriter == null && linePresenter.Typewriter is LetterTypewriter original)
        {
            typewriter = new AdjustableDialogueTypewriter
            {
                TextElement = original.TextElement,
                ActionMarkupHandlers = original.ActionMarkupHandlers
            };
            linePresenter.Typewriter = typewriter;
        }
        // Our Continue control reveals first, then advances. Yarn's original
        // handler directly advances and adds another listener on every line.
        linePresenter.Typewriter?.ActionMarkupHandlers.RemoveAll(handler => handler is LinePresenterButtonHandler);
        foreach (LinePresenterButtonHandler handler in linePresenter.GetComponentsInChildren<LinePresenterButtonHandler>(true))
            handler.enabled = false;
        lineAdvancer.multiAdvanceIsCancel = false;
        originalKeyboardInput = lineAdvancer.GetComponent<Yarn.Unity.LineAdvancerInput.KeyCodes>();
        if (originalKeyboardInput != null)
        {
            originalKeyboardEnabled = originalKeyboardInput.enabled;
            originalKeyboardInput.enabled = false;
        }
        if (optionsPresenter != null)
        {
            choices = optionsPresenter.GetComponent<DialogueChoiceFocus>();
            if (choices == null) choices = optionsPresenter.gameObject.AddComponent<DialogueChoiceFocus>();
            choices.Configure(optionsPresenter, linePresenter.lineText?.font);
        }
        history = dialogueRunner.GetComponent<DialogueHistory>();
        if (history == null) history = dialogueRunner.gameObject.AddComponent<DialogueHistory>();
        history.Configure(dialogueRunner, linePresenter);
        SetUpAdvanceControls();
        if (autoButton == null) autoButton = CreateButton("Dialogue_Auto");
        if (speedButton == null) speedButton = CreateButton("Dialogue_Speed");
        historyButton = CreateButton("Dialogue_History");
        StyleButton(autoButton, "Auto", -112);
        StyleButton(speedButton, "2x", 0);
        StyleButton(historyButton, "History [H]", -224, 154f);
        // Set initial values.
        SetTypingSpeed(normalSpeed);

        UpdateButtonColors();
        SetButtonsVisible(dialogueRunner.IsDialogueRunning);

        dialogueRunner.onDialogueStart.AddListener(
            OnDialogueStarted
        );

        dialogueRunner.onDialogueComplete.AddListener(
            OnDialogueEnded
        );
        if (speedButton != null) speedButton.onClick.AddListener(ToggleSpeed);
        if (autoButton != null) autoButton.onClick.AddListener(ToggleAutoAdvance);
        historyButton.onClick.AddListener(ToggleHistory);
    }

    private void ToggleAutoAdvance()
    {
        if (history != null && history.BlocksAdvanceThisFrame) return;
        isAutoAdvance = !isAutoAdvance;
        autoElapsed = 0f;

        UpdateButtonColors();
    }

    private void ToggleSpeed()
    {
        if (history != null && history.BlocksAdvanceThisFrame) return;
        isFastSpeed = !isFastSpeed;

        int newSpeed = isFastSpeed ? normalSpeed * 2 : normalSpeed;

        SetTypingSpeed(newSpeed);

        UpdateButtonColors();
    }

    private void Update()
    {
        if (dialogueRunner == null || linePresenter == null) return;
        if (history != null && history.BlocksAdvanceThisFrame)
        {
            autoElapsed = 0f;
            if (advanceSurface != null) advanceSurface.raycastTarget = false;
            if (continueButton != null) continueButton.interactable = false;
            return;
        }
        choices?.Refresh();
        ObserveLine();
        bool choosing = choices != null && choices.IsPresentingOptions;
        bool visible = dialogueRunner.IsDialogueRunning &&
            (linePresenter.canvasGroup == null || linePresenter.canvasGroup.alpha > .01f) && !choosing;
        SetButtonsVisible(visible);
        bool usable = visible && Time.timeScale > 0f && !advanceRequested;
        if (advanceSurface != null) advanceSurface.raycastTarget = usable;
        if (continueButton != null) continueButton.interactable = usable;
        if (!advanceRequested)
        {
            if (continueLabel != null) continueLabel.text = FullyVisible ? "Continue  >" : "Reveal Text";
            if (controlHint != null) controlHint.text = FullyVisible
                ? "Click text / Space / Enter to continue"
                : "Click text / Space / Enter to reveal";
        }

        if (Time.timeScale > 0f && dialogueRunner.IsDialogueRunning)
        {
            // A HUD control selected before dialogue must not also receive the
            // same Enter key used here to continue a line.
            if (usable && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                EventSystem.current.SetSelectedGameObject(null);
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                if (choosing)
                {
                    // The UI module may have submitted this key already. Yarn's
                    // completion source makes a repeated submit harmless.
                    choices.SubmitCurrentOption();
                    lastRequestFrame = Time.frameCount;
                    autoElapsed = 0f;
                }
                else AdvanceDialogue();
            }
        }
        if (!usable || choosing || !isAutoAdvance || !FullyVisible)
        { autoElapsed = 0f; return; }
        if (advanceRequested || lastRequestFrame == Time.frameCount || lineStartedFrame == Time.frameCount) return;
        autoElapsed += Time.deltaTime;
        if (autoElapsed < Mathf.Max(.75f, linePresenter.autoAdvanceDelay)) return;
        advanceRequested = true;
        lineAdvancer.RequestNextLine();
    }

    private bool FullyVisible => typewriter != null && typewriter.IsFullyVisible;

    private void ObserveLine()
    {
        if (typewriter == null || lineVersion == typewriter.LineVersion) return;
        lineVersion = typewriter.LineVersion;
        lineStartedFrame = Time.frameCount;
        autoElapsed = 0f;
        advanceRequested = false;
    }

    public bool AdvanceDialogue()
    {
        if (history != null && history.BlocksAdvanceThisFrame) return false;
        if (!isActiveAndEnabled || Time.timeScale <= 0f || dialogueRunner == null ||
            !dialogueRunner.IsDialogueRunning || linePresenter == null || lineAdvancer == null) return false;
        choices?.Refresh();
        if (choices != null && choices.IsPresentingOptions) return false;
        if (linePresenter.canvasGroup != null && linePresenter.canvasGroup.alpha <= .01f) return false;
        ObserveLine();
        if (lineStartedFrame == Time.frameCount || lastRequestFrame == Time.frameCount || advanceRequested) return false;
        lastRequestFrame = Time.frameCount;
        autoElapsed = 0f;
        if (FullyVisible)
        {
            advanceRequested = true;
            lineAdvancer.RequestNextLine();
        }
        else lineAdvancer.RequestLineHurryUp();
        return true;
    }

    private void ContinueClicked() => AdvanceDialogue();
    private void ToggleHistory() => history?.Toggle();

    private void StylePresenters()
    {
        StylePanel(linePresenter.transform);
        TMP_FontAsset font = linePresenter.lineText?.font;
        StyleText(linePresenter.lineText, font, DialogueTextSize, TextAlignmentOptions.MidlineLeft);
        StyleText(linePresenter.characterNameText, font, 26f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

        if (optionsPresenter != null)
        {
            StylePanel(optionsPresenter.transform);
            TMP_Text prompt = optionsPresenter.transform.Find("Last Line")?.GetComponent<TMP_Text>();
            StyleText(prompt, font, DialogueTextSize, TextAlignmentOptions.MidlineLeft);
        }

        RectTransform portrait = dialogueRunner.GetComponentInChildren<CharacterImage>(true)?.characterImage?.rectTransform;
        if (portrait == null) return;
        portrait.SetParent(linePresenter.transform.parent, false);
        portrait.anchorMin = Vector2.zero;
        portrait.anchorMax = new Vector2(PanelGutter, 0f);
        portrait.pivot = new Vector2(.5f, 0f);
        portrait.offsetMin = new Vector2(8f, PanelBottom);
        portrait.offsetMax = new Vector2(-8f, 150f);
        portrait.localScale = Vector3.one;
        portrait.localRotation = Quaternion.identity;
    }

    private static void StylePanel(Transform panel)
    {
        if (panel is not RectTransform rect) return;
        rect.anchorMin = new Vector2(PanelGutter, 0f);
        rect.anchorMax = new Vector2(1f - PanelGutter, 0f);
        rect.pivot = new Vector2(.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, PanelBottom);
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;

        if (panel.TryGetComponent(out VerticalLayoutGroup layout))
        {
            layout.padding = new RectOffset(24, 24, 12, 12);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var minimumSize = panel.GetComponent<LayoutElement>();
            if (minimumSize == null) minimumSize = panel.gameObject.AddComponent<LayoutElement>();
            minimumSize.minHeight = 174f;
        }

        Image background = panel.Find("Background")?.GetComponent<Image>();
        if (background != null) background.color = new Color(.10f, .07f, .05f, .93f);
    }

    internal static void StyleText(TMP_Text label, TMP_FontAsset font, float size, TextAlignmentOptions alignment,
        FontStyles style = FontStyles.Normal)
    {
        if (label == null) return;
        if (font != null && label.font != font) label.font = font;
        label.fontSize = size;
        label.fontStyle = style;
        label.enableAutoSizing = false;
        label.alignment = alignment;
        label.color = DialogueTextColor;
        label.overrideColorTags = true;
        label.raycastTarget = false;
        label.margin = Vector4.zero;
        label.textWrappingMode = TextWrappingModes.Normal;
    }

    private void SetUpAdvanceControls()
    {
        advanceSurface = linePresenter.GetComponent<Image>();
        if (advanceSurface == null)
        {
            advanceSurface = linePresenter.gameObject.AddComponent<Image>();
            advanceSurface.color = Color.clear;
        }
        advanceSurface.raycastTarget = false;
        DialogueAdvanceSurface click = linePresenter.GetComponent<DialogueAdvanceSurface>();
        if (click == null) click = linePresenter.gameObject.AddComponent<DialogueAdvanceSurface>();
        click.Configure(this);
        foreach (Graphic graphic in linePresenter.GetComponentsInChildren<Graphic>(true))
            if (graphic != advanceSurface && graphic.GetComponentInParent<Selectable>() == null)
                graphic.raycastTarget = false;

        Transform oldButton = linePresenter.transform.Find("Button Container/Continue Button");
        continueButton = oldButton != null ? oldButton.GetComponent<Button>() : null;
        if (continueButton == null) continueButton = CreateButton("Dialogue_Continue");
        RectTransform footer = continueButton.transform.parent as RectTransform;
        if (footer == null || footer == linePresenter.transform)
        {
            GameObject row = new GameObject("Dialogue_Controls", typeof(RectTransform));
            footer = row.GetComponent<RectTransform>();
            footer.SetParent(linePresenter.transform, false);
            continueButton.transform.SetParent(footer, false);
        }
        LayoutElement footerLayout = footer.GetComponent<LayoutElement>();
        if (footerLayout == null) footerLayout = footer.gameObject.AddComponent<LayoutElement>();
        footerLayout.ignoreLayout = false;
        footerLayout.minHeight = footerLayout.preferredHeight = 58f;
        footer.SetAsLastSibling();
        footer.anchorMin = Vector2.zero;
        footer.anchorMax = new Vector2(1f, 0f);
        footer.sizeDelta = new Vector2(0f, 58f);

        RectTransform rect = (RectTransform)continueButton.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, .5f);
        rect.pivot = new Vector2(1f, .5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(210f, 54f);
        rect.localScale = Vector3.one;
        continueButton.enabled = true;
        continueButton.navigation = new Navigation { mode = Navigation.Mode.None };
        Image face = continueButton.GetComponent<Image>();
        if (face != null) { face.sprite = null; face.color = Color.white; face.raycastTarget = true; }
        continueButton.targetGraphic = face;
        Outline outline = continueButton.GetComponent<Outline>();
        if (outline == null) outline = continueButton.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(.68f, .47f, .23f);
        outline.effectDistance = new Vector2(2f, -2f);
        SetButtonColor(continueButton, false);
        for (int i = 0; i < continueButton.onClick.GetPersistentEventCount(); i++)
            continueButton.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
        continueButton.onClick.AddListener(ContinueClicked);
        continueLabel = continueButton.GetComponentInChildren<TMP_Text>(true);
        if (continueLabel == null) continueLabel = CreateLabel("Label", rect);
        StyleText(continueLabel, linePresenter.lineText?.font, 24f, TextAlignmentOptions.Center);
        continueLabel.rectTransform.anchorMin = Vector2.zero;
        continueLabel.rectTransform.anchorMax = Vector2.one;
        continueLabel.rectTransform.offsetMin = new Vector2(8f, 4f);
        continueLabel.rectTransform.offsetMax = new Vector2(-8f, -4f);
        controlHint = CreateLabel("Dialogue_Control_Hint", footer);
        StyleText(controlHint, linePresenter.lineText?.font, 22f, TextAlignmentOptions.MidlineLeft);
        controlHint.rectTransform.anchorMin = Vector2.zero;
        controlHint.rectTransform.anchorMax = Vector2.one;
        controlHint.rectTransform.offsetMin = Vector2.zero;
        controlHint.rectTransform.offsetMax = new Vector2(-230f, 0f);
    }

    private Button CreateButton(string name)
    {
        GameObject control = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        control.layer = linePresenter.gameObject.layer;
        control.transform.SetParent(linePresenter.transform, false);
        CreateLabel("Label", (RectTransform)control.transform);
        return control.GetComponent<Button>();
    }

    private TMP_Text CreateLabel(string name, RectTransform parent)
    {
        GameObject label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.layer = parent.gameObject.layer;
        label.transform.SetParent(parent, false);
        return label.GetComponent<TMP_Text>();
    }

    private void StyleButton(Button button, string text, float x, float width = 102f)
    {
        if (button == null) return;
        var rect = (RectTransform)button.transform;
        rect.SetParent(linePresenter.transform, false);
        // The Yarn panel has a VerticalLayoutGroup. These controls are anchored
        // above the panel, so exclude them from its dialogue/continue layout.
        var layout = button.GetComponent<LayoutElement>();
        if (layout == null) layout = button.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        rect.SetAsLastSibling();
        rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(x - 8, 10);
        rect.sizeDelta = new Vector2(width, 48);
        rect.localScale = Vector3.one;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.transition = Selectable.Transition.ColorTint;
        Image face = button.GetComponent<Image>();
        if (face != null)
        {
            face.sprite = null; face.color = Color.white; face.raycastTarget = true;
            face.raycastPadding = Vector4.zero;
            button.targetGraphic = face;
        }
        var edge = button.GetComponent<Outline>();
        if (edge == null) edge = button.gameObject.AddComponent<Outline>();
        edge.effectColor = new Color(.68f, .47f, .23f);
        edge.effectDistance = new Vector2(2, -2);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = text;
            StyleText(label, linePresenter.lineText?.font, 24f, TextAlignmentOptions.Center, FontStyles.Bold);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(6, 4); label.rectTransform.offsetMax = new Vector2(-6, -4);
        }
    }

    private void SetTypingSpeed(int speed)
    {
        linePresenter.lettersPerSecond = speed;
        // Yarn creates its typewriter in Awake; update that instance as well.
        if (linePresenter.Typewriter is LetterTypewriter typewriter)
            typewriter.CharactersPerSecond = speed;
    }

    private void UpdateButtonColors()
    {
        SetButtonColor(speedButton, isFastSpeed);
        SetButtonColor(autoButton, isAutoAdvance);
        SetButtonColor(historyButton, false);
    }

    private void SetButtonColor(Button button, bool selected)
    {
        if (button == null)
            return;

        Color face = selected ? selectedColor : normalColor;
        ColorBlock colors = button.colors;
        colors.normalColor = colors.selectedColor = face;
        colors.highlightedColor = Color.Lerp(face, new Color(1, .92f, .73f), .18f);
        colors.pressedColor = Color.Lerp(face, Color.black, .2f);
        colors.fadeDuration = .08f;
        button.colors = colors;
    }

    private void OnDialogueStarted()
    {
        autoElapsed = 0f;
        advanceRequested = false;
        SetButtonsVisible(true);
    }

    private void OnDialogueEnded()
    {
        autoElapsed = 0f;
        SetButtonsVisible(false);
    }

    private void SetButtonsVisible(bool visible)
    {
        if (speedButton != null)
            speedButton.gameObject.SetActive(visible);

        if (autoButton != null)
            autoButton.gameObject.SetActive(visible);
        if (continueButton != null) continueButton.gameObject.SetActive(visible);
        if (controlHint != null) controlHint.gameObject.SetActive(visible);
        if (historyButton != null) historyButton.gameObject.SetActive(visible);
    }

    private void OnDestroy()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueStart.RemoveListener(OnDialogueStarted);
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueEnded);
        }
        if (originalKeyboardInput != null) originalKeyboardInput.enabled = originalKeyboardEnabled;
        if (continueButton != null) continueButton.onClick.RemoveListener(ContinueClicked);
        if (historyButton != null) historyButton.onClick.RemoveListener(ToggleHistory);
        if (speedButton != null)
            speedButton.onClick.RemoveListener(ToggleSpeed);

        if (autoButton != null)
            autoButton.onClick.RemoveListener(ToggleAutoAdvance);
    }
}
