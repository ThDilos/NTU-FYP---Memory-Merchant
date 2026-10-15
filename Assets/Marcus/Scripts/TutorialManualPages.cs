using System.Text;
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
    public RectTransform DragRoot { get; private set; }
    private readonly Vector2[] slotPositions = { new Vector2(-495, 110), new Vector2(-165, 110), new Vector2(-330, 0) };
    private readonly Vector2[] poolPositions = { new Vector2(-495, -175), new Vector2(-320, -175), new Vector2(-155, -175) };
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
    private void Connector(string name, string text, Vector2 position, Vector2 size)
    {
        TMP_Text label = Instantiate(slotTexts[0], DragRoot);
        label.gameObject.name = name;
        Bounds(label.rectTransform, position, size);
        label.text = text; label.fontSize = 24; label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
    }
    private void BuildBoard()
    {
        DragRoot = sentenceText != null ? sentenceText.transform.parent as RectTransform : null;
        if (DragRoot == null) return;
        Area("Sentence_Board", new Vector2(-330, 65), new Vector2(500, 370), new Color(0.93f, 0.75f, 0.52f, 0.8f), false).transform.SetAsFirstSibling();
        Area("Memory_Status_Board", new Vector2(330, 65), new Vector2(500, 370), new Color(0.93f, 0.75f, 0.52f, 0.8f), false).transform.SetAsFirstSibling();
        Transform instructions = DragRoot.Find("Reconstruction_Instructions");
        if (instructions != null)
        {
            TMP_Text label = instructions.GetComponent<TMP_Text>();
            Bounds((RectTransform)instructions, new Vector2(-330, 205), new Vector2(460, 65));
            label.text = "Drag the words into the blanks."; label.fontSize = 24;
        }
        Connector("Sentence_Connector_1", firstConnector, new Vector2(-330, 110), new Vector2(150, 55));
        Connector("Sentence_Connector_2", secondConnector, new Vector2(-505, 0), new Vector2(150, 55));
        Connector("Sentence_Punctuation", ".", new Vector2(-220, 0), new Vector2(30, 55));
        for (int i = 0; i < slotTexts.Length; i++)
        {
            Image blank = Area("Blank_" + (i + 1), slotPositions[i], new Vector2(i == 2 ? 180 : 160, 55), new Color(0.75f, 0.52f, 0.34f), true);
            blank.gameObject.AddComponent<ReconstructionDropSlot>().Setup(this, i);
            slotTexts[i].transform.SetParent(blank.transform, false);
            Bounds(slotTexts[i].rectTransform, Vector2.zero, new Vector2(140, 45));
            slotTexts[i].text = ""; slotTexts[i].raycastTarget = false;
            fragmentButtons[i].onClick = new Button.ButtonClickedEvent();
            fragmentButtons[i].navigation = new Navigation { mode = Navigation.Mode.None };
            fragmentButtons[i].transition = Selectable.Transition.ColorTint;
            fragmentButtons[i].image.sprite = null;
            fragmentButtons[i].image.color = new Color(0.73f, 0.47f, 0.30f);
            Bounds((RectTransform)fragmentButtons[i].transform, poolPositions[i], new Vector2(i == 2 ? 140 : 150, 48));
            Bounds(fragmentTexts[i].rectTransform, Vector2.zero, new Vector2(i == 2 ? 130 : 140, 44));
            fragmentTexts[i].fontSize = 22; fragmentTexts[i].fontStyle = FontStyles.Bold;
            fragmentTexts[i].alignment = TextAlignmentOptions.Center; fragmentTexts[i].raycastTarget = false;
            fragmentButtons[i].gameObject.AddComponent<ReconstructionWordDrag>().Setup(this, i);
        }
        Bounds(sentenceText.rectTransform, new Vector2(330, 160), new Vector2(440, 170));
        Bounds(statusText.rectTransform, new Vector2(330, -30), new Vector2(440, 150));
        Bounds((RectTransform)resetButton.transform, new Vector2(330, -165), new Vector2(220, 54));
        Bounds((RectTransform)reconstructButton.transform, new Vector2(330, -240), new Vector2(360, 60));
        reconstructButton.image.sprite = null;
        reconstructButton.image.color = new Color(0.20f, 0.32f, 0.17f);
        TMP_Text reconstructLabel = reconstructButton.GetComponentInChildren<TMP_Text>();
        if (reconstructLabel != null) reconstructLabel.color = new Color(1f, 0.91f, 0.72f);
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
    public void RefreshBoard()
    {
        if (state == null || state.Reconstruction == null || DragRoot == null) return;
        if (removeEchoButton != null) removeEchoButton.interactable = state.EchoStored(0);
        RefreshEchoSlots();
        SentenceReconstruction model = state.Reconstruction;
        if (!model.HasAnyPlaced && !model.Complete) feedback = "";
        StringBuilder sentence = new StringBuilder();
        for (int i = 0; i < model.Slots.Count; i++)
        {
            if (i > 0) sentence.Append(" ");
            sentence.Append(model.Slots[i] >= 0 ? state.FragmentLabel(model.Slots[i]) : "...");
            slotTexts[i].text = "";
        }
        for (int i = 0; i < fragmentButtons.Length; i++)
        {
            fragmentButtons[i].gameObject.SetActive(state.HasFragment(i));
            fragmentTexts[i].text = wordLabels[i];
            fragmentButtons[i].interactable = !model.Complete;
            int assigned = model.SlotOf(i);
            ((RectTransform)fragmentButtons[i].transform).anchoredPosition = assigned >= 0 ? slotPositions[assigned] : poolPositions[i];
            fragmentButtons[i].transform.SetAsLastSibling();
        }
        sentenceText.text = "<b>Your memory</b>\n\n" + sentence + ".";
        statusText.text = model.Complete ? "Memory Restored" : feedback.Length > 0 ? feedback : "Collect words in the world. Drag them into the blanks.";
        statusText.fontSize = model.Complete ? 28 : 24;
        resetButton.interactable = !model.Complete;
        reconstructButton.interactable = !model.Complete && model.AllSlotsFilled;
    }
}
