using System.Collections;
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
    private static readonly Color QuietBorder = new Color(0.72f, 0.56f, 0.36f);
    private static readonly Color SelectedBorder = new Color(0.52f, 0.34f, 0.18f);
    private static readonly Color Paper = new Color(0.98f, 0.89f, 0.74f);
    private static readonly Color SelectedPaper = new Color(1f, 0.90f, 0.69f);
    // Shared guides keep labels aligned with the artwork's shelf and catalogue columns.
    private const float PreviewX = -420f;
    private const float CatalogueX = 248f;
    private const float FooterY = -274f;
    private static readonly Vector2 PreviewIconPosition = new Vector2(PreviewX, -27f);
    private static readonly Vector2 SpendCoinPosition = new Vector2(535f, 287f);
    private RectTransform window;
    private TMP_Text balance, balanceLabel, selectedName, selectedType, selectedDescription, inventoryCount, categoryNote;
    private Image selectedIcon;
    private Image balanceHighlight;
    private Image itemPurchaseGleam, purchaseCoinSpend;
    private GameEffectAtlas itemPurchaseAtlas, coinSpendAtlas;
    private Coroutine purchaseEffect;
    private readonly Image[] rowBorders = new Image[7];
    private readonly Image[] rowFaces = new Image[7];
    private readonly TMP_Text[] rowMarkers = new TMP_Text[7];
    private readonly TMP_Text[] rowPrices = new TMP_Text[7];
    private Button purchaseButton;
    private Image purchaseFace;
    private TMP_Text purchaseLabel, purchaseStatus;
    private int selectedIndex;
    private readonly Image[] tabFaces = new Image[2];
    private readonly Image[] tabUnderlines = new Image[2];
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
        Place(title.rectTransform, new Vector2(-175, 287), new Vector2(750, 50));
        Style(title, "Memory Workshop", 40, Ink, true);
        title.alignment = TextAlignmentOptions.Left;
        Image("Balance_Plate", window, new Vector2(484, 287), new Vector2(328, 58), new Color(0.91f, 0.80f, 0.62f, 0.62f));
        balanceHighlight = Image("Balance_Spent_Highlight", window, new Vector2(484, 287), new Vector2(328, 58), Color.clear);
        balanceLabel = Label("Memory_Points_Label", "", new Vector2(430, 287), new Vector2(190, 36), 24, MutedInk, false, TextAlignmentOptions.Left);
        balance = Label("Memory_Points", "", new Vector2(584, 287), new Vector2(96, 36), 26, Ink, true, TextAlignmentOptions.Right);

        BuildTab(0, "Permanent Upgrades", 58);
        BuildTab(1, "Single-use Items", 438);
        selectedName = Label("Selected_Name", "", new Vector2(PreviewX, 187), new Vector2(440, 48), 32, Ink, true);
        selectedType = Label("Selected_Type", "", new Vector2(PreviewX, 145), new Vector2(440, 26), 18, MutedInk, true);
        selectedIcon = Image("Selected_Icon", window, PreviewIconPosition, new Vector2(222, 222), Color.white);
        selectedIcon.preserveAspect = true;
        selectedDescription = Label("Selected_Description", "", new Vector2(PreviewX, 99), new Vector2(440, 62), 22, Ink);
        BuildPurchaseArtwork();
        for (int i = 0; i < Names.Length; i++) BuildCard(i);
        categoryNote = Label("Category_Note", "Buy Echo Slot II to continue.", new Vector2(CatalogueX, -182), new Vector2(710, 36), 22, MutedInk);

        inventoryCount = Label("Inventory_Count", "", new Vector2(PreviewX, FooterY), new Vector2(420, 36), 22, MutedInk);
        purchaseFace = Image("Purchase_Button", window, new Vector2(85, FooterY), new Vector2(350, 54), Jade);
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
        if (window != null && TutorialRewardBridge.NeedsFirstEchoUpgrade) ShowCategory(0);
        RefreshCatalogue();
        Fit();
    }
    private void OnDisable()
    {
        TutorialRewardBridge.Changed -= RefreshCatalogue;
        ClearPurchaseEffect();
    }

    private void RefreshCatalogue()
    {
        if (balance == null || purchaseButton == null) return;
        balanceLabel.text = category == 0 ? "Memory Points" : "Coins";
        balance.text = (category == 0 ? TutorialRewardBridge.MemoryPoints : TutorialRewardBridge.Coins).ToString();
        categoryNote.gameObject.SetActive(category == 0 && TutorialRewardBridge.NeedsFirstEchoUpgrade);
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
        ShopProduct product = (ShopProduct)selectedIndex;
        bool purchased = TutorialRewardBridge.TryPurchase(product, out string message);
        RefreshCatalogue();
        purchaseStatus.text = message;
        if (purchased && isActiveAndEnabled)
        {
            ClearPurchaseEffect();
            purchaseEffect = StartCoroutine(PlayPurchaseEffect(product));
        }
    }

    private IEnumerator PlayPurchaseEffect(ShopProduct product)
    {
        bool upgrade = ShopCatalogue.IsUpgrade(product);
        Color tint = upgrade ? new Color(1f, .80f, .66f) : new Color(1f, .74f, .36f);
        bool revealEchoSlot = product == ShopProduct.EchoSlotII || product == ShopProduct.EchoSlotIII;
        float duration = upgrade ? .46f : .56f;
        if (!upgrade)
        {
            if (itemPurchaseGleam != null) itemPurchaseGleam.gameObject.SetActive(true);
            if (purchaseCoinSpend != null) purchaseCoinSpend.gameObject.SetActive(true);
        }
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = elapsed / duration;
            float pulse = Mathf.Sin(progress * Mathf.PI);
            tint.a = .36f * pulse;
            balanceHighlight.color = tint;
            if (!upgrade) DrawItemPurchase(progress, pulse);
            else if (revealEchoSlot && selectedIndex == (int)product && selectedIcon.enabled)
            {
                float scale = progress < .58f ? Mathf.Lerp(.83f, 1.04f, progress / .58f)
                    : Mathf.Lerp(1.04f, 1f, (progress - .58f) / .42f);
                selectedIcon.rectTransform.localScale = new Vector3(scale, scale, 1f);
                selectedIcon.color = new Color(1f, 1f, 1f, Mathf.Lerp(.46f, 1f, progress / .58f));
            }
            yield return null;
        }
        purchaseEffect = null;
        ClearPurchaseEffect();
    }

    private void BuildPurchaseArtwork()
    {
        itemPurchaseAtlas = GameEffectAtlas.Load("EchoReplayOutline");
        if (itemPurchaseAtlas != null)
        {
            itemPurchaseGleam = Image("Item_Purchase_Gleam", window, PreviewIconPosition + Vector2.down * 8f,
                new Vector2(244f, 244f), Color.white);
            itemPurchaseGleam.gameObject.SetActive(false);
        }
        coinSpendAtlas = GameEffectAtlas.Load("PhotoRewardCoin");
        if (coinSpendAtlas != null)
        {
            purchaseCoinSpend = Image("Purchase_Coin_Spend", window, SpendCoinPosition, new Vector2(68f, 68f), Color.white);
            purchaseCoinSpend.gameObject.SetActive(false);
        }
    }

    private void DrawItemPurchase(float progress, float pulse)
    {
        if (selectedIcon.enabled)
        {
            float bump = Mathf.Sin(progress * Mathf.PI * 1.3f) * (1f - progress);
            float scale = 1f + .15f * bump;
            selectedIcon.rectTransform.localScale = new Vector3(scale, scale, 1f);
            selectedIcon.rectTransform.anchoredPosition = PreviewIconPosition + Vector2.up * (14f * pulse);
        }
        inventoryCount.rectTransform.localScale = new Vector3(1f + .08f * pulse, 1f + .08f * pulse, 1f);
        inventoryCount.color = Color.Lerp(MutedInk, Jade, pulse);
        if (itemPurchaseGleam != null) itemPurchaseGleam.sprite = itemPurchaseAtlas.GetSprite(progress);
        if (purchaseCoinSpend != null)
        {
            float slide = Mathf.SmoothStep(0f, 48f, progress);
            purchaseCoinSpend.rectTransform.anchoredPosition = SpendCoinPosition + new Vector2(-slide, 9f * pulse);
            purchaseCoinSpend.sprite = coinSpendAtlas.GetSprite(progress);
        }
    }

    private void ClearPurchaseEffect()
    {
        if (purchaseEffect != null) StopCoroutine(purchaseEffect);
        purchaseEffect = null;
        if (balanceHighlight != null) balanceHighlight.color = Color.clear;
        if (selectedIcon != null)
        {
            selectedIcon.rectTransform.localScale = Vector3.one;
            selectedIcon.rectTransform.anchoredPosition = PreviewIconPosition;
            selectedIcon.color = Color.white;
        }
        if (inventoryCount != null)
        {
            inventoryCount.rectTransform.localScale = Vector3.one;
            inventoryCount.color = MutedInk;
        }
        if (itemPurchaseGleam != null) itemPurchaseGleam.gameObject.SetActive(false);
        if (purchaseCoinSpend != null) purchaseCoinSpend.gameObject.SetActive(false);
    }

    private void LateUpdate() => Fit();

    private void BuildTab(int index, string heading, float x)
    {
        Image border = Image("Category_Tab_" + index, window, new Vector2(x, 190), new Vector2(364, 50), QuietBorder);
        Image face = Image("Face", border.transform, Vector2.zero, new Vector2(356, 42), Paper);
        tabFaces[index] = face;
        tabUnderlines[index] = Image("Selected_Underline", border.transform, new Vector2(0, -21), new Vector2(356, 5), SelectedBorder);
        TMP_Text label = Label("Tab_Label", heading, Vector2.zero, new Vector2(348, 38), 23, Ink, true, TextAlignmentOptions.Center, border.transform);
        tabLabels[index] = label;
        MakeButton(border, face).onClick.AddListener(() => ShowCategory(index));
    }

    private void BuildCard(int index)
    {
        int row = index < 3 ? index : index - 3;
        float y = 112 - row * 96;
        Image border = Image("Catalogue_" + index, window, new Vector2(CatalogueX, y), new Vector2(744, 84), QuietBorder);
        rowBorders[index] = border;
        Image face = Image("Face", border.transform, Vector2.zero, new Vector2(736, 76), Paper);
        rowFaces[index] = face;
        Image icon = Image("Item_Icon", border.transform, new Vector2(-316, 0), new Vector2(70, 70), Color.white);
        icon.sprite = Icon(index);
        icon.preserveAspect = true;
        icon.enabled = icon.sprite != null;
        Label("Item_Name", Names[index], new Vector2(26, 17), new Vector2(548, 34), 27, Ink, true, TextAlignmentOptions.Left, border.transform);
        Label("Item_Summary", Summaries[index], new Vector2(26, -17), new Vector2(548, 30), 22, MutedInk, false, TextAlignmentOptions.Left, border.transform);
        rowPrices[index] = Label("Price", "", new Vector2(303, 17), new Vector2(116, 32), 20, Ink, true, TextAlignmentOptions.Right, border.transform);
        rowMarkers[index] = Label("Selection_Marker", ">", new Vector2(340, -17), new Vector2(30, 30), 25, SelectedBorder, true, TextAlignmentOptions.Center, border.transform);
        MakeButton(border, face).onClick.AddListener(() => SelectItem(index));
    }

    private static Button MakeButton(Image border, Image face)
    {
        border.raycastTarget = true;
        Button button = border.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.97f, 0.91f);
        colors.pressedColor = new Color(0.91f, 0.83f, 0.72f);
        colors.selectedColor = new Color(1f, 0.94f, 0.85f);
        colors.disabledColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    private void ShowCategory(int index)
    {
        if (category != index) ClearPurchaseEffect();
        category = index;
        for (int i = 0; i < Names.Length; i++)
            rowBorders[i].gameObject.SetActive((i < 3) == (index == 0));
        for (int i = 0; i < tabFaces.Length; i++)
        {
            bool active = i == index;
            tabFaces[i].color = active ? SelectedPaper : Paper;
            tabUnderlines[i].gameObject.SetActive(active);
            tabLabels[i].fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
        }
        SelectItem(index == 0 ? 0 : 3);
        RefreshCatalogue();
    }

    private void SelectItem(int index)
    {
        if ((index < 3) != (category == 0)) return;
        if (selectedIndex != index && selectedIcon != null)
        {
            if (category == 1) ClearPurchaseEffect();
            else
            {
                selectedIcon.rectTransform.localScale = Vector3.one;
                selectedIcon.color = Color.white;
            }
        }
        selectedIndex = index;
        ShopProduct product = (ShopProduct)index;
        int owned = TutorialRewardBridge.OwnedQuantity(product);
        selectedName.text = Names[index];
        selectedType.text = index == 2 ? "UPGRADE / FUTURE PUZZLES" :
            index < 3 ? "MACHINE UPGRADE" : "SINGLE USE / FUTURE CASES";
        selectedDescription.text = Details[index];
        selectedIcon.sprite = Icon(index);
        selectedIcon.enabled = selectedIcon.sprite != null;
        bool upgrade = ShopCatalogue.IsUpgrade(product);
        bool alreadyOwned = upgrade && owned > 0;
        inventoryCount.gameObject.SetActive(!upgrade);
        inventoryCount.text = "In inventory: " + owned;
        bool canBuy = TutorialRewardBridge.CanPurchase(product, out string reason);
        purchaseButton.interactable = canBuy;
        purchaseFace.color = canBuy ? Jade : Paper;
        purchaseLabel.color = canBuy ? Cream : MutedInk;
        purchaseLabel.text = canBuy ? "Buy for " + ShopCatalogue.PriceText(product) : alreadyOwned ? "Owned" : "Unavailable";
        purchaseStatus.text = canBuy || alreadyOwned ? "" : reason;
        for (int i = 0; i < Names.Length; i++)
        {
            rowBorders[i].color = i == index ? SelectedBorder : QuietBorder;
            rowFaces[i].color = i == index ? SelectedPaper : Paper;
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
