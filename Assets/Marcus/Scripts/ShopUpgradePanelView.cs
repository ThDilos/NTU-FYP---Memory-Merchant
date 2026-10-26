using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Purchases use the catalogue currency and the shared saved shop economy.
public sealed class ShopUpgradePanelView : MonoBehaviour
{
    [SerializeField] private Sprite panelArtwork;
    [Header("Original in-game machine artwork")]
    [SerializeField] private Texture2D machineArtwork;
    [Tooltip("Normalized texture region containing the original machine, measured from the lower-left corner.")]
    [SerializeField] private Rect machineUvRect = new Rect(0.65f, 0.45f, 0.103125f, 0.22777778f);
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text placeholderText;
    [SerializeField] private Button closeButton;
    [Tooltip("Echo Slot II, Echo Slot III, Dual Echo Replay, Memory Stabiliser, Fragment Hint, Reconstruction Hint, Echo Hint.")]
    [SerializeField] private Sprite[] catalogueIcons;

    private static readonly string[] Names =
    {
        "Echo Slot II", "Echo Slot III", "Dual Echo Replay", "Memory Stabiliser",
        "Fragment Hint", "Reconstruction Hint", "Echo Hint"
    };
    private static readonly string[] Summaries =
    {
        "Carry 2 Echoes instead of 1.",
        "Carry 3 Echoes. Requires Echo Slot II.",
        "Use two Echoes together in linked puzzles.",
        "Remove 1 incorrect Reconstruction choice.",
        "Locate 1 undiscovered fragment.",
        "Place 1 fragment in the correct sentence slot.",
        "Identify the Echo needed for an obstacle."
    };
    private static readonly string[] Details =
    {
        "Expand your Echo capacity from 1 to 2.\nKeep another remembered action close.",
        "Expand your Echo capacity from 2 to 3.\nRequires Echo Slot II.",
        "Unlock puzzles that need two Echoes\nto be used or replayed together.",
        "Remove 1 incorrect fragment from\nthe Reconstruction choices.",
        "Find the location of 1 fragment\nyou have yet to discover.",
        "Place 1 fragment in its correct\nposition in the memory sentence.",
        "Discover which Echo is relevant\nto the obstacle in front of you."
    };
    private static readonly Color Ink = new Color(0.24f, 0.13f, 0.07f);
    private static readonly Color MutedInk = new Color(0.42f, 0.29f, 0.16f);
    private static readonly Color Jade = new Color(0.16f, 0.29f, 0.23f);
    private static readonly Color Cream = new Color(1f, 0.92f, 0.73f);
    private static readonly Color Brass = new Color(0.61f, 0.40f, 0.19f);
    // Shared guides keep labels aligned with the artwork's shelf and catalogue columns.
    private const float PreviewX = -420f;
    private const float CatalogueX = 248f;
    private const float FooterY = -284f;
    private RectTransform window;
    private TMP_Text balance, balanceLabel, selectedName, selectedType, selectedDescription, categoryBadge, categoryNote;
    private Image selectedIcon;
    private readonly Image[] rowBorders = new Image[7];
    private readonly Image[] rowFaces = new Image[7];
    private readonly TMP_Text[] rowMarkers = new TMP_Text[7];
    private readonly TMP_Text[] rowPrices = new TMP_Text[7];
    private Button purchaseButton;
    private TMP_Text purchaseLabel, purchaseStatus;
    private int selectedIndex;
    private readonly Image[] tabFaces = new Image[2];
    private readonly TMP_Text[] tabLabels = new TMP_Text[2];
    private Vector2 previousSize = new Vector2(-1, -1);
    private int category;

    private void Awake()
    {
        if (window != null) return;
        Image backdrop = GetComponent<Image>();
        if (backdrop != null)
        {
            backdrop.sprite = null;
            backdrop.color = new Color(0.04f, 0.025f, 0.015f, 0.80f);
        }
        if (placeholderText != null) placeholderText.gameObject.SetActive(false);
        if (title == null || closeButton == null) return;
        Image board = Image("Memory_Machine_Workshop", transform, Vector2.zero, new Vector2(1440, 720), Color.white);
        window = board.rectTransform;
        board.sprite = panelArtwork;
        BuildMachineArtwork();
        title.transform.SetParent(window, false);
        Place(title.rectTransform, new Vector2(-175, 299), new Vector2(750, 50));
        Style(title, "Memory Workshop", 40, Ink, true);
        title.alignment = TextAlignmentOptions.Left;
        Label("Workshop_Subtitle", "Tools for the memories still to come.", new Vector2(-175, 260), new Vector2(750, 32), 22, MutedInk, false, TextAlignmentOptions.Left);
        Image("Balance_Plate", window, new Vector2(484, 287), new Vector2(328, 58), Jade);
        balanceLabel = Label("Memory_Points_Label", "", new Vector2(430, 287), new Vector2(190, 36), 24, Cream, false, TextAlignmentOptions.Left);
        balance = Label("Memory_Points", "", new Vector2(584, 287), new Vector2(96, 36), 26, Cream, true, TextAlignmentOptions.Right);

        BuildTab(0, "Permanent Upgrades", 58);
        BuildTab(1, "Single-use Items", 438);
        selectedName = Label("Selected_Name", "", new Vector2(PreviewX, 187), new Vector2(440, 48), 32, Ink, true);
        selectedType = Label("Selected_Type", "", new Vector2(PreviewX, 145), new Vector2(440, 26), 18, MutedInk, true);
        selectedIcon = Image("Selected_Icon", window, new Vector2(PreviewX, -27), new Vector2(222, 222), Color.white);
        selectedIcon.preserveAspect = true;
        selectedDescription = Label("Selected_Description", "", new Vector2(PreviewX, 99), new Vector2(440, 62), 22, Ink);
        for (int i = 0; i < Names.Length; i++) BuildCard(i);
        categoryNote = Label("Category_Note", "", new Vector2(CatalogueX, -182), new Vector2(710, 36), 22, MutedInk);

        Image("Category_Badge", window, new Vector2(PreviewX, FooterY), new Vector2(440, 56), Jade);
        categoryBadge = Label("Category_Badge_Label", "", new Vector2(PreviewX, FooterY), new Vector2(420, 40), 24, Cream, true);
        Image purchaseFace = Image("Purchase_Button", window, new Vector2(85, -272), new Vector2(350, 50), Jade);
        purchaseButton = MakeButton(purchaseFace, purchaseFace);
        purchaseButton.onClick.AddListener(PurchaseSelected);
        purchaseLabel = Label("Purchase_Label", "", Vector2.zero, new Vector2(334, 40), 25, Cream, true, TextAlignmentOptions.Center, purchaseFace.transform);
        purchaseStatus = Label("Purchase_Status", "", new Vector2(85, -314), new Vector2(430, 24), 18, MutedInk);
        closeButton.transform.SetParent(window, false);
        Place((RectTransform)closeButton.transform, new Vector2(500, FooterY), new Vector2(240, 56));
        closeButton.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        TMP_Text closeLabel = closeButton.GetComponentInChildren<TMP_Text>();
        if (closeLabel != null)
        {
            Place(closeLabel.rectTransform, Vector2.zero, new Vector2(220, 40));
            Style(closeLabel, "Back to Shop", 26, Ink, true);
        }
        ShowCategory(0);
    }

    private void OnEnable()
    {
        TutorialRewardBridge.Changed += RefreshCatalogue;
        previousSize = new Vector2(-1, -1);
        RefreshCatalogue();
        Fit();
    }
    private void OnDisable() => TutorialRewardBridge.Changed -= RefreshCatalogue;

    private void RefreshCatalogue()
    {
        if (balance == null || purchaseButton == null) return;
        balanceLabel.text = category == 0 ? "Memory Points" : "Coins";
        balance.text = (category == 0 ? TutorialRewardBridge.MemoryPoints : TutorialRewardBridge.Coins).ToString();
        for (int i = 0; i < rowPrices.Length; i++)
        {
            ShopProduct product = (ShopProduct)i;
            rowPrices[i].text = ShopCatalogue.IsUpgrade(product) && TutorialRewardBridge.OwnedQuantity(product) > 0
                ? "Owned" : ShopCatalogue.PriceText(product);
        }
        SelectItem(selectedIndex);
    }
    private void PurchaseSelected()
    {
        TutorialRewardBridge.TryPurchase((ShopProduct)selectedIndex, out string message);
        RefreshCatalogue();
        purchaseStatus.text = message;
    }

    private void LateUpdate() => Fit();

    private void BuildTab(int index, string heading, float x)
    {
        Image border = Image("Category_Tab_" + index, window, new Vector2(x, 190), new Vector2(364, 50), Brass);
        Image face = Image("Face", border.transform, Vector2.zero, new Vector2(356, 42), Cream);
        tabFaces[index] = face;
        TMP_Text label = Label("Tab_Label", heading, Vector2.zero, new Vector2(348, 38), 23, Ink, true, TextAlignmentOptions.Center, border.transform);
        tabLabels[index] = label;
        MakeButton(border, face).onClick.AddListener(() => ShowCategory(index));
    }

    private void BuildCard(int index)
    {
        int row = index < 3 ? index : index - 3;
        float y = 112 - row * 96;
        Image border = Image("Catalogue_" + index, window, new Vector2(CatalogueX, y), new Vector2(744, 84), Brass);
        rowBorders[index] = border;
        Image face = Image("Face", border.transform, Vector2.zero, new Vector2(736, 76), Cream);
        rowFaces[index] = face;
        Image icon = Image("Item_Icon", border.transform, new Vector2(-316, 0), new Vector2(70, 70), Color.white);
        icon.sprite = Icon(index);
        icon.preserveAspect = true;
        icon.enabled = icon.sprite != null;
        Label("Item_Name", Names[index], new Vector2(26, 17), new Vector2(548, 34), 27, Ink, true, TextAlignmentOptions.Left, border.transform);
        Label("Item_Summary", Summaries[index], new Vector2(26, -17), new Vector2(548, 30), 22, MutedInk, false, TextAlignmentOptions.Left, border.transform);
        rowPrices[index] = Label("Price", "", new Vector2(303, 17), new Vector2(116, 32), 20, Ink, true, TextAlignmentOptions.Right, border.transform);
        rowMarkers[index] = Label("Selection_Marker", ">", new Vector2(340, -17), new Vector2(30, 30), 25, Jade, true, TextAlignmentOptions.Center, border.transform);
        MakeButton(border, face).onClick.AddListener(() => SelectItem(index));
    }

    private static Button MakeButton(Image border, Image face)
    {
        border.raycastTarget = true;
        Button button = border.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.94f, 0.80f);
        colors.pressedColor = new Color(0.79f, 0.86f, 0.74f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    private void ShowCategory(int index)
    {
        category = index;
        for (int i = 0; i < Names.Length; i++)
            rowBorders[i].gameObject.SetActive((i < 3) == (index == 0));
        for (int i = 0; i < tabFaces.Length; i++)
        {
            tabFaces[i].color = i == index ? Jade : new Color(0.91f, 0.76f, 0.53f);
            tabLabels[i].color = i == index ? Cream : Ink;
        }
        categoryNote.gameObject.SetActive(index == 0);
        categoryNote.text = "Expand what your Echoes can do.";
        SelectItem(index == 0 ? 0 : 3);
        RefreshCatalogue();
    }

    private void SelectItem(int index)
    {
        if ((index < 3) != (category == 0)) return;
        selectedIndex = index;
        ShopProduct product = (ShopProduct)index;
        int owned = TutorialRewardBridge.OwnedQuantity(product);
        selectedName.text = Names[index];
        selectedType.text = index == 2 ? "UPGRADE / FUTURE PUZZLES" :
            index < 3 ? "MACHINE UPGRADE" : "SINGLE USE / FUTURE CASES";
        selectedDescription.text = Details[index];
        selectedIcon.sprite = Icon(index);
        selectedIcon.enabled = selectedIcon.sprite != null;
        categoryBadge.text = index < 3 ? (owned > 0 ? "Upgrade owned" : "Permanent upgrade") : "In inventory: " + owned;
        purchaseButton.interactable = TutorialRewardBridge.CanPurchase(product, out string reason);
        purchaseLabel.text = index < 3 && owned > 0 ? "Owned" : "Buy - " + ShopCatalogue.PriceText(product);
        purchaseStatus.text = purchaseButton.interactable ? "Purchases save automatically." : reason;
        for (int i = 0; i < Names.Length; i++)
        {
            rowBorders[i].color = i == index ? Jade : Brass;
            rowFaces[i].color = i == index ? new Color(0.91f, 0.85f, 0.65f) : new Color(0.96f, 0.83f, 0.62f);
            rowMarkers[i].gameObject.SetActive(i == index);
        }
    }

    private Sprite Icon(int index) => catalogueIcons != null && index < catalogueIcons.Length ? catalogueIcons[index] : null;

    private void BuildMachineArtwork()
    {
        if (machineArtwork == null) return;
        Rect uv = Rect.MinMaxRect(Mathf.Clamp01(machineUvRect.xMin), Mathf.Clamp01(machineUvRect.yMin),
            Mathf.Clamp01(machineUvRect.xMax), Mathf.Clamp01(machineUvRect.yMax));
        if (uv.width <= 0 || uv.height <= 0) return;
        GameObject go = new GameObject("Original_Memory_Machine", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.layer = 5;
        go.transform.SetParent(window, false);
        RawImage image = go.GetComponent<RawImage>();
        image.texture = machineArtwork;
        image.uvRect = uv;
        image.raycastTarget = false;
        machineArtwork.filterMode = FilterMode.Point;
        float aspect = (uv.width * machineArtwork.width) / (uv.height * machineArtwork.height);
        Place(image.rectTransform, new Vector2(-624, 287), new Vector2(66f * aspect, 66f));
    }

    private void Fit()
    {
        RectTransform overlay = transform as RectTransform;
        if (window == null || overlay == null || overlay.rect.size == previousSize) return;
        Vector2 available = overlay.rect.size;
        if (available.x <= 0 || available.y <= 0) return;
        previousSize = available;
        float scale = Mathf.Min(1f, (available.x - 80f) / 1440f, (available.y - 80f) / 720f);
        window.localScale = Vector3.one * Mathf.Max(0.1f, scale);
    }

    private TMP_Text Label(string name, string value, Vector2 position, Vector2 size, float fontSize,
        Color color, bool bold = false, TextAlignmentOptions alignment = TextAlignmentOptions.Center, Transform parent = null)
    {
        TMP_Text text = Instantiate(title, parent != null ? parent : window);
        text.name = name;
        Place(text.rectTransform, position, size);
        Style(text, value, fontSize, color, bold);
        text.alignment = alignment;
        return text;
    }

    private static void Style(TMP_Text text, string value, float fontSize, Color color, bool bold)
    {
        text.text = value;
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.margin = Vector4.zero;
        text.characterSpacing = 0;
        text.wordSpacing = 0;
        text.lineSpacing = 0;
        text.paragraphSpacing = 0;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
    }

    private static Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        Place(image.rectTransform, position, size);
        return image;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
