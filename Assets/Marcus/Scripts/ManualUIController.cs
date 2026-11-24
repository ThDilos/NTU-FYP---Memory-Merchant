using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The existing panel controllers own opening/closing and shortcuts.
// This component owns the book's tabs, layout, and temporary gameplay suspension.
public class ManualUIController : MonoBehaviour
{
    public enum ManualContext { Shop, MemoryWorld }
    [SerializeField] private ManualContext context;
    [Header("Tabs and spreads: Controls, Case Notes, Echoes, Reconstruction")]
    [SerializeField] private Button[] tabs;
    [SerializeField] private GameObject[] contents;
    [SerializeField] private Sprite inactiveTabSprite;
    [SerializeField] private Sprite activeTabSprite;

    [Header("Book Layout")]
    [SerializeField] private RectTransform bookLayout;
    [SerializeField] private Image bookBackground;
    [SerializeField] private Sprite reconstructionSpread;

    [Header("Gameplay input (only the Player map is suspended)")]
    [SerializeField] private InputActionAsset[] gameplayInputAssets;

    private readonly GameplayInputPause pause = new GameplayInputPause();
    private static readonly Vector2 BookSize = new Vector2(1400, 900);
    private static readonly Color Ink = new Color(0.23f, 0.14f, 0.08f);
    private static readonly Color Walnut = new Color(0.19f, 0.10f, 0.055f);
    private static readonly Color Cream = new Color(1f, 0.94f, 0.80f);
    private bool presentationReady;
    private bool fittingBook;
    private Sprite plainSpread;

    private void Awake()
    {
        if (bookBackground != null) plainSpread = bookBackground.sprite;
    }

    private void OnEnable()
    {
        BuildPresentation();
        SelectTab(0);
        FitBook();
        SuspendGameplay();
    }

    public void SelectControls() => SelectTab(0);
    public void SelectCaseNotes() => SelectTab(1);
    public void SelectEchoes() => SelectTab(2);
    public void SelectReconstruction() => SelectTab(3);

    public void SetContext(ManualContext value)
    {
        context = value;
        if (!isActiveAndEnabled) return;
        SelectControls();
        FitBook();
    }

    private bool IsAvailable(int index) => index == 0 ||
        (context == ManualContext.Shop ? index == 1 : index == 2 || index == 3);

    private void SelectTab(int selected)
    {
        if (tabs == null || contents == null || tabs.Length != 4 || contents.Length != 4)
        {
            Debug.LogError("Assign the four context-sensitive Manual tabs and spreads.", this);
            return;
        }
        if (!IsAvailable(selected)) selected = 0;
        ClearHiddenSelection(selected);
        // Both spreads use one Image, so transparent frame pixels cannot expose a second cover.
        if (bookBackground != null)
            bookBackground.sprite = selected == 3 && reconstructionSpread != null ? reconstructionSpread : plainSpread;
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] != null)
            {
                tabs[i].gameObject.SetActive(IsAvailable(i));
                tabs[i].navigation = new Navigation { mode = Navigation.Mode.None };
                StyleTab(i, i == selected);
            }
            if (contents[i] != null)
                contents[i].SetActive(IsAvailable(i) && i == selected);
        }
        LayoutTabs();
    }

    private void LayoutTabs()
    {
        const float width = 250f;
        int count = context == ManualContext.Shop ? 2 : 3;
        int visibleIndex = 0;
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null || !IsAvailable(i)) continue;
            RectTransform rect = tabs[i].transform as RectTransform;
            if (rect == null) continue;

            Sprite sprite = tabs[i].GetComponent<Image>()?.sprite;
            float height = sprite != null && sprite.rect.width > 0
                ? width * sprite.rect.height / sprite.rect.width : 90f;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(width, height);
            // The paper sits into the top edge of the book; the active page rises higher.
            rect.anchoredPosition = new Vector2((visibleIndex++ - (count - 1) / 2f) * width, -56f);
        }
    }

    private void BuildPresentation()
    {
        if (presentationReady || bookLayout == null || tabs == null || tabs.Length != 4) return;
        TMP_Text template = null;
        foreach (Button tab in tabs)
            if (tab != null && (template = tab.GetComponentInChildren<TMP_Text>(true)) != null) break;
        if (template == null) return;

        Image overlay = GetComponent<Image>();
        if (overlay != null)
        {
            overlay.sprite = null;
            overlay.color = new Color(0.055f, 0.04f, 0.025f, 0.28f);
            overlay.raycastTarget = true;
        }

        TMP_Text closeHint = Label(template, "Manual_Close_Hint", "R / Esc to close", new Vector2(0, -410),
            new Vector2(300, 28), 20, Cream);
        closeHint.outlineColor = Walnut;
        closeHint.outlineWidth = 0.18f;

        Transform close = bookLayout.Find("Close_Button");
        if (close != null && close.TryGetComponent(out Button closeButton))
        {
            Place((RectTransform)close, new Vector2(0, -368), new Vector2(280, 62));
            closeButton.navigation = new Navigation { mode = Navigation.Mode.None };
            closeButton.transition = Selectable.Transition.ColorTint;
            Image closeImage = closeButton.GetComponent<Image>();
            if (closeImage != null)
            {
                closeImage.color = Color.white;
                closeImage.raycastTarget = true;
                closeImage.raycastPadding = new Vector4(-6, -6, -6, -6);
                closeButton.targetGraphic = closeImage;
            }
            SetButtonColours(closeButton);
            TMP_Text closeText = closeButton.GetComponentInChildren<TMP_Text>(true);
            if (closeText != null)
            {
                Place(closeText.rectTransform, Vector2.zero, new Vector2(256, 48));
                FormatLabel(closeText, 25, Ink, true);
                closeText.text = "Close Manual";
                closeText.transform.SetAsLastSibling();
            }
        }

        StyleShopPages();
        presentationReady = true;
    }

    private void StyleShopPages()
    {
        if (context != ManualContext.Shop || contents == null || contents.Length != 4) return;
        string[][] text =
        {
            new[]
            {
                "<b><size=32>General Controls</size></b>\n\n<b>R</b>   Open or close the Manual\n\n<b>Q</b>   Open Settings\n\n<b>Esc</b>   Close a panel\n\nUse the mouse to select tabs and shop objects.",
                "<b><size=32>Inside the Shop</size></b>\n\nDrag the counter manual onto the Memory Machine to enter its Memory World.\n\nAfter the tutorial, visit Upgrades to improve your Echo capacity.\n\nRepair photographs in the optional puzzle to earn Coins."
            },
            new[]
            {
                "<b><size=32>Case Notes</size></b>\n\nNo customer case is active.\n\nThis is where you can keep track of a customer's story and Memory Anchor.",
                "<b><size=32>Discovered Information</size></b>\n\nNo case notes have been recorded yet.\n\nLook closely at meaningful objects and the memories connected to them."
            }
        };
        for (int page = 0; page < 2; page++)
        {
            if (contents[page] == null) continue;
            TMP_Text[] labels = contents[page].GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length && i < 2; i++)
            {
                Place(labels[i].rectTransform, new Vector2(i == 0 ? -330 : 330, 0), new Vector2(528, 520));
                FormatLabel(labels[i], 27, Ink, false, TextAlignmentOptions.TopLeft);
                labels[i].text = text[page][i];
                labels[i].lineSpacing = 3f;
                labels[i].paragraphSpacing = 4f;
            }
        }
    }

    private void StyleTab(int index, bool selected)
    {
        Button button = tabs[index];
        Image edge = button.GetComponent<Image>();
        if (edge != null)
        {
            edge.sprite = selected ? activeTabSprite : inactiveTabSprite;
            edge.color = Color.white;
            edge.type = Image.Type.Simple;
            edge.preserveAspect = false;
            edge.raycastTarget = true;
            edge.raycastPadding = new Vector4(-4, -4, -4, -4);
            button.targetGraphic = edge;
        }
        foreach (Graphic graphic in button.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = graphic == edge;
        button.transition = Selectable.Transition.ColorTint;
        SetButtonColours(button);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;
        Place(label.rectTransform, new Vector2(0, 4), new Vector2(226, 44));
        FormatLabel(label, 25, Ink, selected);
        label.transform.SetAsLastSibling();
    }

    private static void SetButtonColours(Button button)
    {
        ColorBlock colours = ColorBlock.defaultColorBlock;
        colours.highlightedColor = new Color(1.12f, 1.10f, 1.04f);
        colours.pressedColor = new Color(0.82f, 0.82f, 0.75f);
        colours.selectedColor = Color.white;
        colours.fadeDuration = 0.08f;
        button.colors = colours;
    }

    private TMP_Text Label(TMP_Text template, string name, string text, Vector2 position, Vector2 size,
        float fontSize, Color colour, bool bold = false)
    {
        TMP_Text label = Instantiate(template, bookLayout);
        label.name = name;
        Place(label.rectTransform, position, size);
        FormatLabel(label, fontSize, colour, bold);
        label.text = text;
        return label;
    }

    private static void FormatLabel(TMP_Text label, float fontSize, Color colour, bool bold,
        TextAlignmentOptions alignment = TextAlignmentOptions.Midline)
    {
        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.alignment = alignment;
        label.color = colour;
        label.margin = Vector4.zero;
        label.characterSpacing = label.wordSpacing = label.lineSpacing = label.paragraphSpacing = 0f;
        label.overflowMode = TextOverflowModes.Truncate;
        label.raycastTarget = false;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.sizeDelta = size;
    }

    private void ClearHiddenSelection(int selected)
    {
        EventSystem events = EventSystem.current;
        GameObject current = events != null ? events.currentSelectedGameObject : null;
        if (current == null) return;
        for (int i = 0; i < contents.Length; i++)
            if (i != selected && contents[i] != null && current.transform.IsChildOf(contents[i].transform))
            {
                events.SetSelectedGameObject(null);
                return;
            }
        for (int i = 0; i < tabs.Length; i++)
            if (!IsAvailable(i) && tabs[i] != null && current.transform.IsChildOf(tabs[i].transform))
            {
                events.SetSelectedGameObject(null);
                return;
            }
    }

    private void OnRectTransformDimensionsChange() => FitBook();

    private void FitBook()
    {
        RectTransform panel = transform as RectTransform;
        if (fittingBook || bookLayout == null || panel == null || panel.rect.width <= 0f || panel.rect.height <= 0f) return;
        fittingBook = true;
        Place(bookLayout, Vector2.zero, BookSize);
        bool memoryWorld = context == ManualContext.MemoryWorld;
        // The book's bounds include its tabs and close controls. The spread leaves room to see the world.
        float scale = Mathf.Min(memoryWorld ? 1.2f : 1.1f,
            panel.rect.width * (memoryWorld ? 0.92f : 0.86f) / BookSize.x,
            panel.rect.height * (memoryWorld ? 0.98f : 0.86f) / BookSize.y);
        bookLayout.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        fittingBook = false;
    }

    private void SuspendGameplay() => pause.Capture(gameplayInputAssets);
    private void OnDisable()
    {
        pause.Restore();
        EventSystem events = EventSystem.current;
        if (events != null && events.currentSelectedGameObject != null &&
            events.currentSelectedGameObject.transform.IsChildOf(transform))
            events.SetSelectedGameObject(null);
    }
}
