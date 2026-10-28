
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using TMPro;

public class DialogueBehaviour : MonoBehaviour
{
    [Header("Yarn")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private LinePresenter linePresenter;
    [SerializeField] private LineAdvancer lineAdvancer;

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

    private void Start()
    {
        if (dialogueRunner == null || linePresenter == null || lineAdvancer == null)
        {
            Debug.LogError("Please assign DialogueRunner, LinePresenter and LineAdvancer");
            return;
        }

        isFastSpeed = false;
        isAutoAdvance = linePresenter.autoAdvance;
        // Own the timer so Auto can also be switched on/off on a fully shown line.
        linePresenter.autoAdvance = false;
        if (linePresenter.Typewriter is LetterTypewriter original)
        {
            typewriter = new AdjustableDialogueTypewriter
            {
                TextElement = original.TextElement,
                ActionMarkupHandlers = original.ActionMarkupHandlers
            };
            linePresenter.Typewriter = typewriter;
        }
        StyleButton(autoButton, "Auto", -112);
        StyleButton(speedButton, "2x", 0);
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
    }

    private void ToggleAutoAdvance()
    {
        isAutoAdvance = !isAutoAdvance;
        autoElapsed = 0f;

        UpdateButtonColors();
    }

    private void ToggleSpeed()
    {
        isFastSpeed = !isFastSpeed;

        int newSpeed = isFastSpeed ? normalSpeed * 2 : normalSpeed;

        SetTypingSpeed(newSpeed);

        UpdateButtonColors();
    }

    private void Update()
    {
        if (dialogueRunner == null || linePresenter == null) return;
        bool visible = dialogueRunner.IsDialogueRunning &&
            (linePresenter.canvasGroup == null || linePresenter.canvasGroup.alpha > .01f);
        SetButtonsVisible(visible);
        if (typewriter != null && lineVersion != typewriter.LineVersion)
        {
            lineVersion = typewriter.LineVersion;
            autoElapsed = 0f;
            advanceRequested = false;
        }
        if (!visible || !isAutoAdvance || typewriter == null || !typewriter.IsFullyVisible)
        { autoElapsed = 0f; return; }
        if (advanceRequested) return;
        autoElapsed += Time.deltaTime;
        if (autoElapsed < Mathf.Max(.75f, linePresenter.autoAdvanceDelay)) return;
        advanceRequested = true;
        lineAdvancer.RequestNextLine();
    }

    private void StyleButton(Button button, string text, float x)
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
        rect.sizeDelta = new Vector2(102, 48);
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
            label.text = text; label.font = linePresenter.lineText.font;
            label.fontSize = 24; label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = false; label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1, .92f, .73f); label.raycastTarget = false;
            label.margin = Vector4.zero;
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
    }

    private void OnDestroy()
    {
        if (speedButton != null)
            speedButton.onClick.RemoveListener(ToggleSpeed);

        if (autoButton != null)
            autoButton.onClick.RemoveListener(ToggleAutoAdvance);

        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueStart.RemoveListener(
                OnDialogueStarted
            );

            dialogueRunner.onDialogueComplete.RemoveListener(
                OnDialogueEnded
            );
        }
    }
}
