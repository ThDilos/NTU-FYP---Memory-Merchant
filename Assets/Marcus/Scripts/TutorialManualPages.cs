using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManualPages : MonoBehaviour
{
    [SerializeField] private TutorialMemoryState state;
    [SerializeField] private TMP_Text[] echoPages;
    [SerializeField] private TMP_Text[] slotTexts;
    [SerializeField] private Button[] fragmentButtons;
    [SerializeField] private TMP_Text[] fragmentTexts;
    [SerializeField] private TMP_Text sentenceText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button reconstructButton;
    [Header("Editable tutorial sentence board")]
    [SerializeField] private string[] wordLabels = { "Mother", "recorder", "Study" };
    [SerializeField] private string firstConnector = "used the";
    [SerializeField] private string secondConnector = "in the";
    [Header("Reconstruction artwork")]
    [SerializeField] private Sprite reconstructionBoardArtwork;
    private string feedback = "";
    private Button removeEchoButton;
    private TMP_Text[] echoSlotLabels;
    private Image[] echoSlotCards;
    private LevelInventory echoInventory;
    private Image[] blankFrames;
    private TMP_Text[] poolGhostLabels;
    private TMP_Text fragmentProgress;
    private TMP_Text boardProgress;
    private TMP_Text reconstructLabel;
    private static readonly Color BoardInk = new Color(0.25f, 0.15f, 0.08f);
    private static readonly Color MutedInk = new Color(0.47f, 0.32f, 0.19f);
    private static readonly Color FrameInk = new Color(0.36f, 0.21f, 0.10f);
    private static readonly Color RestoredInk = new Color(0.22f, 0.36f, 0.21f);
    private static readonly Color FragmentPaper = new Color(1f, 0.95f, 0.78f);
    private static readonly Vector2 FragmentSize = new Vector2(164, 76);
    public RectTransform DragRoot { get; private set; }
    private readonly Vector2[] slotPositions = { new Vector2(-505, 80), new Vector2(-155, 80), new Vector2(-315, -20) };
    private readonly Vector2[] poolPositions = { new Vector2(-495, -221), new Vector2(-320, -221), new Vector2(-145, -221) };
    private void Awake()
    {
        BuildBoard();
        BuildEchoSlots();
        if (echoPages.Length > 0 && echoPages[0] != null && resetButton != null)
        {
            removeEchoButton = Instantiate(resetButton, echoPages[0].transform.parent);
            removeEchoButton.name = "Remove_Key_Echo";
            Bounds((RectTransform)removeEchoButton.transform, new Vector2(-330, -230), new Vector2(280, 54));
            removeEchoButton.onClick = new Button.ButtonClickedEvent();
            removeEchoButton.onClick.AddListener(() => state.RemoveKeyEcho());
            removeEchoButton.GetComponentInChildren<TMP_Text>().text = "Remove Echo";
            removeEchoButton.navigation = new Navigation { mode = Navigation.Mode.None };
        }
    }
    private void OnEnable()
    {
        if (state == null) return;
        state.Changed += RefreshBoard;
        BindEchoInventory();
        RefreshBoard();
    }
    private void Start() { BindEchoInventory(); RefreshBoard(); }
    private void BindEchoInventory()
    {
        if (echoInventory == LevelInventory.Instance) return;
        if (echoInventory != null) echoInventory.EchoCapacityChanged -= RefreshBoard;
        echoInventory = LevelInventory.Instance;
        if (echoInventory != null) echoInventory.EchoCapacityChanged += RefreshBoard;
    }
    private void OnDisable()
    {
        if (state != null) state.Changed -= RefreshBoard;
        if (echoInventory != null) echoInventory.EchoCapacityChanged -= RefreshBoard;
        echoInventory = null;
    }
    private static void Bounds(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
    private Image Area(string name, Vector2 position, Vector2 size, Color color, bool raycast, RectTransform parent = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent != null ? parent : DragRoot, false);
        Bounds((RectTransform)go.transform, position, size);
        Image image = go.GetComponent<Image>();
        image.color = color; image.raycastTarget = raycast;
        return image;
    }
    private void BuildEchoSlots()
    {
        if (echoPages.Length < 2 || echoPages[0] == null || echoPages[1] == null) return;
        RectTransform root = echoPages[0].transform.parent as RectTransform;
        if (root == null) return;
        echoSlotLabels = new TMP_Text[3]; echoSlotCards = new Image[3];
        Vector2[] positions = { new Vector2(-330, 0), new Vector2(330, 135), new Vector2(330, -135) };
        for (int i = 0; i < 3; i++)
        {
            Vector2 size = new Vector2(500, i == 0 ? 520 : 240);
            echoSlotCards[i] = Area("Echo_Slot_" + (i + 1), positions[i], size, new Color(0.75f, 0.52f, 0.34f, 0.35f), false, root);
            echoSlotCards[i].transform.SetAsFirstSibling();
            TMP_Text label = i < 2 ? echoPages[i] : Instantiate(echoPages[1], root);
            label.gameObject.name = "Echo_Slot_" + (i + 1) + "_Text";
            Bounds(label.rectTransform, positions[i] + (i == 0 ? Vector2.up * 45f : Vector2.zero), new Vector2(440, i == 0 ? 360 : 190));
            label.fontSize = 24; label.enableAutoSizing = false;
            label.alignment = TextAlignmentOptions.TopLeft; label.raycastTarget = false;
            echoSlotLabels[i] = label;
        }
    }
    private void RefreshEchoSlots()
    {
        if (echoSlotLabels == null) return;
        int capacity = LevelInventory.Instance != null ? LevelInventory.Instance.EchoSlotCount : 1;
        bool hasKeyEcho = state.EchoStored(0);
        echoSlotLabels[0].text = hasKeyEcho
            ? "<b>Slot 1: " + state.EchoName(0) + "</b>\n\nCaptured / stored\n\n" + state.EchoDescription(0)
            : "<b>Slot 1</b>\n\nEmpty";
        if (removeEchoButton != null) removeEchoButton.gameObject.SetActive(hasKeyEcho);
        for (int i = 1; i < 3; i++)
        {
            bool unlocked = i < capacity;
            echoSlotLabels[i].text = "<b>Slot " + (i + 1) + "</b>\n\n" +
                (unlocked ? "Empty\nUnlocked Echo slot." : "Locked\nUpgrade the Memory Machine to unlock.");
            echoSlotCards[i].color = unlocked ? new Color(0.75f, 0.52f, 0.34f, 0.35f) : new Color(0.38f, 0.29f, 0.20f, 0.35f);
        }
    }
    private TMP_Text BoardLabel(string name, string text, Vector2 position, Vector2 size, float fontSize,
        TextAlignmentOptions alignment = TextAlignmentOptions.Midline, bool bold = false, RectTransform parent = null)
    {
        TMP_Text label = Instantiate(slotTexts[0], parent != null ? parent : DragRoot);
        label.gameObject.name = name;
        label.gameObject.SetActive(true);
        Bounds(label.rectTransform, position, size);
        StyleBoardText(label, fontSize, alignment, bold);
        label.text = text;
        return label;
    }
    private static void StyleBoardText(TMP_Text label, float fontSize, TextAlignmentOptions alignment, bool bold = false)
    {
        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.alignment = alignment;
        label.color = BoardInk;
        label.margin = Vector4.zero;
        label.characterSpacing = label.wordSpacing = label.lineSpacing = label.paragraphSpacing = 0;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
    }
    private Image Inset(string name, RectTransform parent, float inset, Color color)
    {
        Image image = Area(name, Vector2.zero, Vector2.zero, color, false, parent);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
        return image;
    }
    private void StyleBoardButton(Button button, string text, Vector2 position, Vector2 size, bool primary)
    {
        Bounds((RectTransform)button.transform, position, size);
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.transition = Selectable.Transition.ColorTint;
        Image edge = button.GetComponent<Image>();
        edge.sprite = null;
        edge.type = Image.Type.Simple;
        edge.preserveAspect = false;
        edge.alphaHitTestMinimumThreshold = 0f;
        edge.raycastTarget = true;
        edge.raycastPadding = Vector4.zero;
        edge.color = FrameInk;
        Image fill = Inset("Button_Face", (RectTransform)button.transform, 4,
            primary ? RestoredInk : new Color(0.85f, 0.65f, 0.41f));
        fill.transform.SetAsFirstSibling();
        button.targetGraphic = fill;
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.highlightedColor = new Color(1.12f, 1.10f, 1.04f);
        colors.pressedColor = new Color(0.78f, 0.76f, 0.66f);
        colors.disabledColor = new Color(0.64f, 0.61f, 0.53f, 0.65f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label == null) return;
        Bounds(label.rectTransform, Vector2.zero, size - new Vector2(16, 10));
        StyleBoardText(label, primary ? 26 : 24, TextAlignmentOptions.Midline, true);
        label.color = primary ? new Color(1f, 0.92f, 0.75f) : BoardInk;
        label.text = text;
        label.transform.SetAsLastSibling();
    }
    private void StyleFragmentButton(Button button, int index)
    {
        StyleBoardButton(button, wordLabels[index], poolPositions[index], FragmentSize, false);
        Image edge = button.GetComponent<Image>();
        edge.color = new Color(0.88f, 0.64f, 0.27f);
        // Expand the full card, including its lower edge, without overlapping neighbouring cards.
        edge.raycastPadding = new Vector4(-4, -8, -4, -8);
        button.targetGraphic.color = Color.white;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.13f, 0.36f, 0.32f);
        colors.highlightedColor = new Color(0.24f, 0.55f, 0.46f);
        colors.pressedColor = new Color(0.10f, 0.28f, 0.25f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(0.37f, 0.42f, 0.34f);
        button.colors = colors;
        Bounds(fragmentTexts[index].rectTransform, new Vector2(9, 0), new Vector2(136, 64));
        StyleBoardText(fragmentTexts[index], 25, TextAlignmentOptions.Midline, true);
        fragmentTexts[index].color = FragmentPaper;
        // A visible grip reinforces the drag affordance without intercepting pointer events.
        for (int row = -1; row <= 1; row++)
            for (int column = 0; column < 2; column++)
                Area("Drag_Grip", new Vector2(-67 + column * 7, row * 8), new Vector2(3, 3),
                    FragmentPaper, false, (RectTransform)button.transform);
        foreach (Graphic graphic in button.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = graphic == edge;
    }
    private void BuildBoard()
    {
        DragRoot = sentenceText != null ? sentenceText.transform.parent as RectTransform : null;
        if (DragRoot == null) return;
        Image artwork = Area("Reconstruction_Board_Artwork", Vector2.zero, new Vector2(1280, 640),
            reconstructionBoardArtwork != null ? Color.white : new Color(0.92f, 0.76f, 0.53f), false);
        artwork.sprite = reconstructionBoardArtwork;
        artwork.transform.SetAsFirstSibling();
        if (reconstructionBoardArtwork == null)
        {
            Area("Page_Divider", Vector2.zero, new Vector2(4, 530), new Color(0.67f, 0.45f, 0.25f, 0.45f), false);
            Area("Fragment_Tray", new Vector2(-330, -210), new Vector2(550, 150), new Color(0.78f, 0.55f, 0.32f, 0.25f), false);
        }
        BoardLabel("Reconstruction_Heading", "Piece the memory together", new Vector2(-330, 270), new Vector2(540, 40), 32,
            TextAlignmentOptions.MidlineLeft, true);
        BoardLabel("Memory_Preview_Heading", "Your memory", new Vector2(330, 270), new Vector2(480, 40), 32,
            TextAlignmentOptions.MidlineLeft, true);
        Transform instructions = DragRoot.Find("Reconstruction_Instructions");
        if (instructions != null)
        {
            TMP_Text label = instructions.GetComponent<TMP_Text>();
            Bounds((RectTransform)instructions, new Vector2(-330, 182), new Vector2(540, 40));
            StyleBoardText(label, 23, TextAlignmentOptions.MidlineLeft);
            label.color = MutedInk;
            label.text = "Drag the green fragments into the blanks.";
        }
        BoardLabel("Sentence_Connector_1", firstConnector, new Vector2(-330, 80), new Vector2(145, 64), 27);
        BoardLabel("Sentence_Connector_2", secondConnector, new Vector2(-505, -20), new Vector2(145, 64), 27);
        BoardLabel("Sentence_Punctuation", ".", new Vector2(-205, -20), new Vector2(26, 64), 27);
        BoardLabel("Fragment_Tray_Heading", "COLLECTED FRAGMENTS", new Vector2(-410, -161), new Vector2(340, 28), 21,
            TextAlignmentOptions.MidlineLeft, true);
        fragmentProgress = BoardLabel("Fragment_Count", "", new Vector2(-154, -161), new Vector2(168, 28), 20,
            TextAlignmentOptions.MidlineRight);
        fragmentProgress.color = MutedInk;
        blankFrames = new Image[slotTexts.Length];
        poolGhostLabels = new TMP_Text[fragmentButtons.Length];
        for (int i = 0; i < slotTexts.Length; i++)
        {
            Image blank = Area("Blank_" + (i + 1), slotPositions[i], new Vector2(178, 84), FrameInk, true);
            blankFrames[i] = blank;
            Inset("Blank_Paper", blank.rectTransform, 3, new Color(0.77f, 0.57f, 0.36f));
            blank.gameObject.AddComponent<ReconstructionDropSlot>().Setup(this, i);
            slotTexts[i].transform.SetParent(blank.transform, false);
            Bounds(slotTexts[i].rectTransform, Vector2.zero, new Vector2(150, 48));
            StyleBoardText(slotTexts[i], 20, TextAlignmentOptions.Midline);
            slotTexts[i].color = new Color(0.42f, 0.27f, 0.14f, 0.65f);
            slotTexts[i].text = (i + 1).ToString();

            Area("Fragment_Tray_Place_" + i, poolPositions[i], FragmentSize,
                new Color(0.48f, 0.30f, 0.16f, 0.18f), false);
            poolGhostLabels[i] = BoardLabel("Fragment_Tray_Status_" + i, "Missing", poolPositions[i], new Vector2(150, 48), 20);
            poolGhostLabels[i].color = MutedInk;
            fragmentButtons[i].onClick = new Button.ButtonClickedEvent();
            StyleFragmentButton(fragmentButtons[i], i);
            fragmentButtons[i].gameObject.AddComponent<ReconstructionWordDrag>().Setup(this, i);
        }
        Bounds(sentenceText.rectTransform, new Vector2(330, 118), new Vector2(480, 140));
        StyleBoardText(sentenceText, 30, TextAlignmentOptions.MidlineLeft);
        Bounds(statusText.rectTransform, new Vector2(330, -25), new Vector2(480, 105));
        StyleBoardText(statusText, 24, TextAlignmentOptions.MidlineLeft);
        statusText.color = MutedInk;
        boardProgress = BoardLabel("Arrangement_Progress", "", new Vector2(330, -160), new Vector2(480, 30), 22,
            TextAlignmentOptions.MidlineLeft, true);
        StyleBoardButton(resetButton, "Reset", new Vector2(175, -220), new Vector2(170, 60), false);
        StyleBoardButton(reconstructButton, "Reconstruct", new Vector2(430, -220), new Vector2(300, 60), true);
        reconstructLabel = reconstructButton.GetComponentInChildren<TMP_Text>();
    }
    public bool CanDrag(int fragment) => state != null && DragRoot != null && DragRoot.gameObject.activeInHierarchy &&
        !state.Reconstruction.Complete && state.HasFragment(fragment);
    public int SlotFor(int fragment) => state != null ? state.Reconstruction.SlotOf(fragment) : -1;
    public void DropWord(int fragment, int slot)
    {
        if (CanDrag(fragment) && state.Reconstruction.PlaceAt(fragment, slot, state.HasFragment)) feedback = "";
        RefreshBoard();
    }
    public void ResetSentence()
    {
        if (state == null) return;
        state.Reconstruction.ResetArrangement(); feedback = ""; RefreshBoard();
    }
    public void Reconstruct()
    {
        if (state == null || state.Reconstruction.Complete) return;
        feedback = state.Reconstruct() ? "Memory Restored" : "The memory does not fit together.";
        RefreshBoard();
    }
    private string WordInSlot(SentenceReconstruction model, int slot)
    {
        int fragment = model.Slots[slot];
        return fragment >= 0 && fragment < wordLabels.Length ? wordLabels[fragment] : "____";
    }
    public void RefreshBoard()
    {
        if (state == null || state.Reconstruction == null || DragRoot == null) return;
        if (removeEchoButton != null) removeEchoButton.interactable = state.EchoStored(0);
        RefreshEchoSlots();
        SentenceReconstruction model = state.Reconstruction;
        if (!model.HasAnyPlaced && !model.Complete) feedback = "";
        int placed = 0;
        for (int i = 0; i < model.Slots.Count; i++)
        {
            bool occupied = model.Slots[i] >= 0;
            if (occupied) placed++;
            slotTexts[i].text = occupied ? "" : (i + 1).ToString();
            blankFrames[i].color = occupied ? RestoredInk : FrameInk;
        }
        int collected = 0;
        Transform heldFragment = null;
        for (int i = 0; i < fragmentButtons.Length; i++)
        {
            bool hasFragment = state.HasFragment(i);
            if (hasFragment) collected++;
            fragmentButtons[i].gameObject.SetActive(hasFragment);
            fragmentTexts[i].text = wordLabels[i];
            fragmentButtons[i].interactable = !model.Complete;
            int assigned = model.SlotOf(i);
            poolGhostLabels[i].text = !hasFragment ? "Missing" : assigned >= 0 ? "Placed" : "";
            ReconstructionWordDrag drag = fragmentButtons[i].GetComponent<ReconstructionWordDrag>();
            if (drag != null && drag.IsDragging)
                heldFragment = drag.transform;
            else
                ((RectTransform)fragmentButtons[i].transform).anchoredPosition = assigned >= 0 ? slotPositions[assigned] : poolPositions[i];
            fragmentButtons[i].transform.SetAsLastSibling();
        }
        if (heldFragment != null) heldFragment.SetAsLastSibling();
        fragmentProgress.text = collected + " / " + fragmentButtons.Length + " found";
        boardProgress.text = model.Complete ? "MEMORY RESTORED" : placed + " / " + model.Slots.Count + " words placed";
        boardProgress.color = model.Complete ? RestoredInk : MutedInk;
        sentenceText.text = WordInSlot(model, 0) + " " + firstConnector + " " + WordInSlot(model, 1) + "\n" +
            secondConnector + " " + WordInSlot(model, 2) + ".";
        statusText.text = model.Complete ? "The Study has been restored.\nClose the Manual and enter the Study." :
            feedback.Length > 0 ? "That order does not fit.\nTry moving the words." :
            model.AllSlotsFilled ? "All pieces are in place.\nReconstruct to restore the memory." :
            collected < fragmentButtons.Length ? "Find the missing fragments, then drag\neach word into a blank." :
            "Drag a word into each blank.\nSwap words to change their order.";
        statusText.color = model.Complete ? RestoredInk : feedback.Length > 0 ? new Color(0.55f, 0.23f, 0.13f) : MutedInk;
        if (reconstructLabel != null) reconstructLabel.text = model.Complete ? "Restored" : "Reconstruct";
        resetButton.interactable = !model.Complete;
        reconstructButton.interactable = !model.Complete && model.AllSlotsFilled;
    }
}
