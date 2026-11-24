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
    private string feedback = "";
    private Button removeEchoButton;
    private TMP_Text[] echoSlotLabels;
    private Image[] echoSlotCards;
    private LevelInventory echoInventory;
    private Image[] blankFrames;
    private Image[] blankPapers;
    private bool[] highlightedSlots;
    private Image[] progressMarks;
    private TMP_Text[] poolGhostLabels;
    private TMP_Text fragmentProgress;
    private TMP_Text boardProgress;
    private TMP_Text reconstructLabel;
    private int activeFragment = -1;
    private bool fragmentDragging;
    private static readonly Color BoardInk = new Color(0.25f, 0.15f, 0.08f);
    private static readonly Color MutedInk = new Color(0.47f, 0.32f, 0.19f);
    private static readonly Color FrameInk = new Color(0.36f, 0.21f, 0.10f);
    private static readonly Color RestoredInk = new Color(0.22f, 0.36f, 0.21f);
    private static readonly Color FragmentPaper = new Color(1f, 0.95f, 0.78f);
    private static readonly Color Brass = new Color(0.85f, 0.64f, 0.31f);
    // Use matching content insets on either side of the printed page divider.
    private const float PageCenter = 304f;
    private const float PageWidth = 512f;
    private const float FooterHeadingY = -162f;
    private static readonly Vector2 FragmentSize = new Vector2(164, 64);
    public RectTransform DragRoot { get; private set; }
    public int ActiveFragmentIndex => HasActiveFragment ? activeFragment : -1;
    public bool HasActiveFragment => CanDrag(activeFragment);
    private static readonly Vector2[] SlotPositions = { new Vector2(-472, 82), new Vector2(-136, 82), new Vector2(-364, -22) };
    private static readonly Vector2[] PoolPositions = { new Vector2(-478, -212), new Vector2(-304, -212), new Vector2(-130, -212) };
    private void Awake()
    {
        BuildBoard();
        BuildControls();
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
            StyleBoardButton(removeEchoButton, "Remove Echo", new Vector2(-330, -230), new Vector2(280, 56), false);
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
        activeFragment = -1;
        fragmentDragging = false;
        ClearSlotHighlights();
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
    private void BuildControls()
    {
        RectTransform controls = DragRoot != null && DragRoot.parent != null
            ? DragRoot.parent.Find("Controls_Content") as RectTransform : null;
        if (controls == null) return;
        foreach (TMP_Text oldText in controls.GetComponentsInChildren<TMP_Text>(true))
            oldText.gameObject.SetActive(false);
        BoardLabel("Controls_Heading", "Move & interact", new Vector2(-330, 236), new Vector2(486, 44), 30,
            TextAlignmentOptions.MidlineLeft, true, controls);
        string[] keys = { "WASD / Arrows", "Left Shift", "E", "1 / 2 / 3", "R", "Q", "Esc" };
        string[] actions = { "Move", "Run", "Interact / collect", "Replay an Echo slot", "Open / close Manual", "Open Settings", "Close a panel" };
        for (int i = 0; i < keys.Length; i++)
        {
            float y = 160 - i * 62;
            Image key = Area("Control_Key_" + i, new Vector2(-486, y), new Vector2(164, 46), FrameInk, false, controls);
            Inset("Key_Paper", key.rectTransform, 3, new Color(1f, 0.95f, 0.81f));
            BoardLabel("Control_Key_Label_" + i, keys[i], Vector2.zero, new Vector2(152, 40), i < 2 ? 20 : 24,
                TextAlignmentOptions.Midline, true, key.rectTransform);
            BoardLabel("Control_Action_" + i, actions[i], new Vector2(-236, y), new Vector2(310, 48), 23,
                TextAlignmentOptions.MidlineLeft, false, controls);
        }
        BoardLabel("Memory_Types_Heading", "Two kinds of memory", new Vector2(330, 236), new Vector2(486, 44), 30,
            TextAlignmentOptions.MidlineLeft, true, controls);
        TMP_Text echoTitle = BoardLabel("Echo_Explanation_Heading", "ECHOES  /  REMEMBERED ACTIONS", new Vector2(330, 166),
            new Vector2(486, 34), 22, TextAlignmentOptions.MidlineLeft, true, controls);
        echoTitle.color = new Color(0.57f, 0.34f, 0.10f);
        TMP_Text echoHelp = BoardLabel("Echo_Explanation", "Hold E to reveal an Echo.\nRelease E, then hold E to capture it.\nHold its slot number to replay the action.",
            new Vector2(330, 83), new Vector2(486, 132), 24, TextAlignmentOptions.TopLeft, false, controls);
        echoHelp.lineSpacing = 8;
        Area("Memory_Types_Divider", new Vector2(330, -6), new Vector2(486, 3), Brass, false, controls);
        TMP_Text fragmentTitle = BoardLabel("Fragment_Explanation_Heading", "FRAGMENTS  /  CLUES TO AN EVENT", new Vector2(330, -55),
            new Vector2(486, 34), 22, TextAlignmentOptions.MidlineLeft, true, controls);
        fragmentTitle.color = new Color(0.55f, 0.27f, 0.18f);
        TMP_Text fragmentHelp = BoardLabel("Fragment_Explanation", "Press E near a Fragment to collect it.\nArrange the clues in Reconstruction\nto piece the memory together.",
            new Vector2(330, -138), new Vector2(486, 132), 24, TextAlignmentOptions.TopLeft, false, controls);
        fragmentHelp.lineSpacing = 8;
        TMP_Text note = BoardLabel("Echo_Upgrade_Note", "Extra Echo slots unlock through shop upgrades.", new Vector2(330, -238),
            new Vector2(486, 42), 19, TextAlignmentOptions.MidlineLeft, false, controls);
        note.color = MutedInk;
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
            echoSlotCards[i] = Area("Echo_Slot_" + (i + 1), positions[i], size, Brass, false, root);
            Inset("Echo_Slot_Paper", echoSlotCards[i].rectTransform, 3, new Color(0.98f, 0.90f, 0.73f));
            echoSlotCards[i].transform.SetAsFirstSibling();
            TMP_Text label = i < 2 ? echoPages[i] : Instantiate(echoPages[1], root);
            label.gameObject.name = "Echo_Slot_" + (i + 1) + "_Text";
            Bounds(label.rectTransform, positions[i] + (i == 0 ? Vector2.up * 45f : Vector2.zero), new Vector2(440, i == 0 ? 360 : 190));
            StyleBoardText(label, 25, TextAlignmentOptions.TopLeft);
            label.lineSpacing = 6;
            echoSlotLabels[i] = label;
        }
    }
    private void RefreshEchoSlots()
    {
        if (echoSlotLabels == null) return;
        int capacity = LevelInventory.Instance != null ? LevelInventory.Instance.EchoSlotCount : 1;
        bool hasKeyEcho = state.EchoStored(0);
        echoSlotLabels[0].text = hasKeyEcho
            ? "<size=30><b>Slot 1</b></size>\n<size=20>STORED ACTION</size>\n\n<b>" + state.EchoName(0) + "</b>\n\n" + state.EchoDescription(0)
            : "<size=30><b>Slot 1</b></size>\n<size=20>EMPTY</size>\n\nCapture an Echo to store a remembered action here.\n\nHold its slot number near the matching obstacle to replay it.";
        if (removeEchoButton != null) removeEchoButton.gameObject.SetActive(hasKeyEcho);
        for (int i = 1; i < 3; i++)
        {
            bool unlocked = i < capacity;
            echoSlotLabels[i].text = "<size=28><b>Slot " + (i + 1) + "</b></size>\n\n" +
                (unlocked ? "Empty\nReady for another remembered action." : "Locked\nUnlock an extra Echo slot at the shop.");
            echoSlotCards[i].color = unlocked ? Brass : new Color(0.60f, 0.48f, 0.33f);
            echoSlotLabels[i].color = unlocked ? BoardInk : MutedInk;
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
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
    }
    private Image Inset(string name, RectTransform parent, float inset, Color color)
    {
        Transform existing = parent.Find(name);
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (image == null) image = Area(name, Vector2.zero, Vector2.zero, color, false, parent);
        image.color = color;
        image.raycastTarget = false;
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
        StyleBoardButton(button, wordLabels[index], PoolPositions[index], FragmentSize, false);
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
        Bounds(fragmentTexts[index].rectTransform, new Vector2(9, 0), new Vector2(130, 52));
        StyleBoardText(fragmentTexts[index], 25, TextAlignmentOptions.Midline, true);
        fragmentTexts[index].color = FragmentPaper;
        // A visible grip reinforces the drag affordance without intercepting pointer events.
        for (int row = -1; row <= 1; row++)
            for (int column = 0; column < 2; column++)
                Area("Drag_Grip", new Vector2(-69 + column * 7, row * 8), new Vector2(3, 3),
                    FragmentPaper, false, (RectTransform)button.transform);
        foreach (Graphic graphic in button.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = graphic == edge;
    }
    private void BuildBoard()
    {
        DragRoot = sentenceText != null ? sentenceText.transform.parent as RectTransform : null;
        if (DragRoot == null) return;
        Vector2 pageHeadingSize = new Vector2(PageWidth, 44);
        BoardLabel("Reconstruction_Heading", "Build the memory", new Vector2(-PageCenter, 262), pageHeadingSize, 32,
            TextAlignmentOptions.MidlineLeft, true);
        BoardLabel("Memory_Preview_Heading", "Your memory", new Vector2(PageCenter, 262), pageHeadingSize, 32,
            TextAlignmentOptions.MidlineLeft, true);
        Transform instructions = DragRoot.Find("Reconstruction_Instructions");
        if (instructions != null)
        {
            TMP_Text label = instructions.GetComponent<TMP_Text>();
            Bounds((RectTransform)instructions, new Vector2(-PageCenter, 177), new Vector2(PageWidth, 60));
            StyleBoardText(label, 23, TextAlignmentOptions.MidlineLeft);
            label.color = MutedInk;
            label.text = "Drag a Fragment, or select one\nthen click a numbered blank.";
        }
        BoardLabel("Sentence_Connector_1", firstConnector, new Vector2(-PageCenter, SlotPositions[0].y), new Vector2(148, 64), 27);
        BoardLabel("Sentence_Connector_2", secondConnector, new Vector2(-510, SlotPositions[2].y), new Vector2(100, 64), 27,
            TextAlignmentOptions.MidlineLeft);
        BoardLabel("Sentence_Punctuation", ".", new Vector2(-260, SlotPositions[2].y), new Vector2(22, 64), 27);
        BoardLabel("Fragment_Tray_Heading", "FRAGMENTS", new Vector2(-432, FooterHeadingY), new Vector2(256, 28), 23,
            TextAlignmentOptions.MidlineLeft, true);
        fragmentProgress = BoardLabel("Fragment_Count", "", new Vector2(-176, FooterHeadingY), new Vector2(256, 28), 21,
            TextAlignmentOptions.MidlineRight);
        fragmentProgress.color = MutedInk;
        blankFrames = new Image[slotTexts.Length];
        blankPapers = new Image[slotTexts.Length];
        highlightedSlots = new bool[slotTexts.Length];
        poolGhostLabels = new TMP_Text[fragmentButtons.Length];
        for (int i = 0; i < slotTexts.Length; i++)
        {
            Image blank = Area("Blank_" + (i + 1), SlotPositions[i], new Vector2(176, 76), FrameInk, true);
            blankFrames[i] = blank;
            blank.alphaHitTestMinimumThreshold = 0;
            blank.raycastPadding = new Vector4(-3, -3, -3, -3);
            blankPapers[i] = Inset("Blank_Paper", blank.rectTransform, 4, new Color(0.87f, 0.71f, 0.48f));
            blank.gameObject.AddComponent<ReconstructionDropSlot>().Setup(this, i);
            slotTexts[i].transform.SetParent(blank.transform, false);
            Bounds(slotTexts[i].rectTransform, Vector2.zero, new Vector2(150, 48));
            StyleBoardText(slotTexts[i], 25, TextAlignmentOptions.Midline, true);
            slotTexts[i].color = new Color(0.42f, 0.27f, 0.14f);
            slotTexts[i].text = (i + 1).ToString();

            Area("Fragment_Tray_Place_" + i, PoolPositions[i], FragmentSize,
                new Color(0.48f, 0.30f, 0.16f, 0.18f), false);
            poolGhostLabels[i] = BoardLabel("Fragment_Tray_Status_" + i, "Not found", PoolPositions[i], new Vector2(146, 48), 21);
            poolGhostLabels[i].color = MutedInk;
            fragmentButtons[i].onClick = new Button.ButtonClickedEvent();
            int fragment = i;
            fragmentButtons[i].onClick.AddListener(() => SelectFragment(fragment));
            StyleFragmentButton(fragmentButtons[i], i);
            fragmentButtons[i].gameObject.AddComponent<ReconstructionWordDrag>().Setup(this, i);
        }
        Bounds(sentenceText.rectTransform, new Vector2(PageCenter, 119), new Vector2(PageWidth, 122));
        StyleBoardText(sentenceText, 32, TextAlignmentOptions.MidlineLeft);
        sentenceText.lineSpacing = 8;
        Area("Preview_Divider", new Vector2(PageCenter, 27), new Vector2(PageWidth, 3), new Color(0.63f, 0.43f, 0.23f, 0.4f), false);
        Bounds(statusText.rectTransform, new Vector2(PageCenter, -49), new Vector2(PageWidth, 106));
        StyleBoardText(statusText, 24, TextAlignmentOptions.MidlineLeft);
        statusText.color = MutedInk;
        boardProgress = BoardLabel("Arrangement_Progress", "", new Vector2(PageCenter, FooterHeadingY), new Vector2(PageWidth, 28), 22,
            TextAlignmentOptions.MidlineLeft, true);
        progressMarks = new Image[slotTexts.Length];
        const float markGap = 12f;
        float markWidth = (PageWidth - (progressMarks.Length - 1) * markGap) / progressMarks.Length;
        float firstMarkX = PageCenter - PageWidth / 2f + markWidth / 2f;
        for (int i = 0; i < progressMarks.Length; i++)
            progressMarks[i] = Area("Progress_Mark_" + i, new Vector2(firstMarkX + i * (markWidth + markGap), -181),
                new Vector2(markWidth, 4), Brass, false);
        StyleBoardButton(resetButton, "Reset", new Vector2(130, -217), new Vector2(164, 54), false);
        StyleBoardButton(reconstructButton, "Reconstruct", new Vector2(396, -217), new Vector2(328, 54), true);
        reconstructLabel = reconstructButton.GetComponentInChildren<TMP_Text>();
    }
    public bool CanDrag(int fragment) => fragment >= 0 && fragment < wordLabels.Length && state != null &&
        state.Reconstruction != null && DragRoot != null && DragRoot.gameObject.activeInHierarchy &&
        !state.Reconstruction.Complete && state.HasFragment(fragment);
    public int SlotFor(int fragment) => state != null && state.Reconstruction != null && fragment >= 0
        ? state.Reconstruction.SlotOf(fragment) : -1;
    public void SelectFragment(int fragment)
    {
        if (!CanDrag(fragment) || fragmentDragging) return;
        int destination = SlotFor(fragment);
        if (HasActiveFragment && activeFragment != fragment && destination >= 0)
        {
            DropWord(activeFragment, destination);
            return;
        }
        activeFragment = activeFragment == fragment ? -1 : fragment;
        ClearSlotHighlights();
        RefreshBoard();
    }
    public void SetDragFeedback(int fragment, bool dragging)
    {
        fragmentDragging = dragging && CanDrag(fragment);
        activeFragment = fragmentDragging ? fragment : -1;
        RefreshBoard();
    }
    public void HighlightSlot(int slot, bool highlighted)
    {
        if (highlightedSlots == null || slot < 0 || slot >= highlightedSlots.Length) return;
        highlightedSlots[slot] = highlighted && HasActiveFragment;
        ApplySlotVisual(slot);
    }
    public void ClearSlotHighlights()
    {
        if (highlightedSlots == null) return;
        for (int i = 0; i < highlightedSlots.Length; i++)
        {
            highlightedSlots[i] = false;
            ApplySlotVisual(i);
        }
    }
    private void ApplySlotVisual(int slot)
    {
        if (blankFrames == null || blankPapers == null || blankFrames[slot] == null) return;
        bool occupied = state != null && state.Reconstruction != null && state.Reconstruction.Slots[slot] >= 0;
        bool highlighted = highlightedSlots[slot] && HasActiveFragment;
        blankFrames[slot].color = highlighted ? new Color(1f, 0.79f, 0.33f) : occupied ? RestoredInk : FrameInk;
        blankPapers[slot].color = highlighted ? new Color(1f, 0.92f, 0.65f) : new Color(0.87f, 0.71f, 0.48f);
    }
    public void DropWord(int fragment, int slot)
    {
        if (CanDrag(fragment) && state.Reconstruction.PlaceAt(fragment, slot, state.HasFragment))
        {
            feedback = "";
            activeFragment = -1;
            AudioManager.Instance?.PlayPlacement();
            fragmentButtons[fragment].GetComponent<ReconstructionWordDrag>()?.PlaySettle();
        }
        ClearSlotHighlights();
        RefreshBoard();
    }
    public void ResetSentence()
    {
        if (state == null) return;
        state.Reconstruction.ResetArrangement();
        activeFragment = -1;
        feedback = "";
        ClearSlotHighlights();
        RefreshBoard();
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
        return fragment >= 0 && fragment < wordLabels.Length
            ? "<color=#315443>" + wordLabels[fragment] + "</color>" : "<color=#92714C>_____</color>";
    }
    public void RefreshBoard()
    {
        if (state == null || state.Reconstruction == null || DragRoot == null) return;
        if (removeEchoButton != null) removeEchoButton.interactable = state.EchoStored(0);
        RefreshEchoSlots();
        SentenceReconstruction model = state.Reconstruction;
        if (model.Complete) { activeFragment = -1; fragmentDragging = false; }
        if (!model.HasAnyPlaced && !model.Complete) feedback = "";
        int placed = 0;
        for (int i = 0; i < model.Slots.Count; i++)
        {
            bool occupied = model.Slots[i] >= 0;
            if (occupied) placed++;
            slotTexts[i].text = occupied ? "" : (i + 1).ToString();
            ApplySlotVisual(i);
            progressMarks[i].color = occupied ? RestoredInk : new Color(0.69f, 0.52f, 0.32f, 0.4f);
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
            poolGhostLabels[i].text = !hasFragment ? "Not found" : assigned >= 0 ? "Placed" : "";
            ReconstructionWordDrag drag = fragmentButtons[i].GetComponent<ReconstructionWordDrag>();
            if (drag != null && drag.IsDragging)
                heldFragment = drag.transform;
            else
                ((RectTransform)fragmentButtons[i].transform).anchoredPosition = assigned >= 0 ? SlotPositions[assigned] : PoolPositions[i];
            fragmentButtons[i].transform.SetAsLastSibling();
        }
        if (heldFragment != null) heldFragment.SetAsLastSibling();
        fragmentProgress.text = collected + " / " + fragmentButtons.Length + " found";
        boardProgress.text = model.Complete ? "MEMORY RESTORED" : placed + " / " + model.Slots.Count + " Fragments placed";
        boardProgress.color = model.Complete ? RestoredInk : MutedInk;
        sentenceText.text = WordInSlot(model, 0) + " " + firstConnector + " " + WordInSlot(model, 1) + "\n" +
            secondConnector + " " + WordInSlot(model, 2) + ".";
        statusText.text = model.Complete ? "The Study has been restored.\nClose the Manual and enter the Study." :
            HasActiveFragment ? (fragmentDragging ? "Move " : "Selected: ") + wordLabels[activeFragment] +
                (fragmentDragging ? " into a blank." : "\nChoose a blank or a placed Fragment.") :
            feedback.Length > 0 ? "That order does not fit yet.\nMove the Fragments and try again." :
            model.AllSlotsFilled ? "Your sentence is ready.\nReconstruct to restore the memory." :
            collected < fragmentButtons.Length ? "Explore the rooms to find\nthe missing Fragments." :
            "Arrange your collected clues\nto rebuild the event.";
        statusText.color = model.Complete || HasActiveFragment ? RestoredInk :
            feedback.Length > 0 ? new Color(0.55f, 0.23f, 0.13f) : MutedInk;
        if (reconstructLabel != null) reconstructLabel.text = model.Complete ? "Restored" : "Reconstruct";
        resetButton.interactable = !model.Complete && (model.HasAnyPlaced || HasActiveFragment);
        reconstructButton.interactable = !model.Complete && model.AllSlotsFilled;
    }
}
