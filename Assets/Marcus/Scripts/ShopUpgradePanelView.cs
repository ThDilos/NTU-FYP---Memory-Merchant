using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation for the existing upgrade entrance. Purchase rules can be added independently.
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
    private RectTransform window;
    private TMP_Text balance;
    private Vector2 previousSize = new Vector2(-1, -1);
    private static readonly Color Ink = new Color(0.24f, 0.13f, 0.07f);
    private static readonly Color MutedInk = new Color(0.42f, 0.29f, 0.16f);

    private void Awake()
    {
        Image backdrop = GetComponent<Image>();
        if (backdrop != null) { backdrop.sprite = null; backdrop.color = new Color(0.04f, 0.025f, 0.015f, 0.80f); }
        if (placeholderText != null) placeholderText.gameObject.SetActive(false);
        Image board = Image("Memory_Machine_Workshop", transform, Vector2.zero, new Vector2(1440, 720), Color.white);
        window = board.rectTransform; board.sprite = panelArtwork; board.raycastTarget = false;
        BuildMachineArtwork();
        if (title == null || closeButton == null) return;
        title.transform.SetParent(window, false);
        Place(title.rectTransform, new Vector2(0, 315), new Vector2(1220, 64));
        Style(title, "Memory Machine", 46, Ink, true);
        Label("Workshop_Subtitle", "Make room for more memories.", new Vector2(-100, 268), new Vector2(760, 34), 23, MutedInk);
        balance = Label("Memory_Points", "", new Vector2(510, 268), new Vector2(270, 34), 23, Ink, true);
        Label("Upgrade_List_Title", "ECHO CAPACITY", new Vector2(248, 214), new Vector2(740, 26), 18, MutedInk, true);
        Card("Echo_Slot_2", 102, "02", "Second Echo Slot", "Carry another Echo and keep more\nof the past close at hand.");
        Card("Echo_Slot_3", -96, "03", "Third Echo Slot", "Complete your collection with\nroom for a third captured Echo.");
        Image capacity = Image("Current_Capacity", window, new Vector2(-435, -296), new Vector2(440, 54), new Color(0.20f, 0.28f, 0.16f));
        capacity.raycastTarget = false;
        Label("Capacity_Label", "Echo capacity   1 / 3", new Vector2(-435, -296), new Vector2(420, 48), 25, new Color(1f, 0.91f, 0.72f), true);
        Label("Upgrade_Footer", "More machine upgrades\nare coming soon.", new Vector2(20, -297), new Vector2(430, 58), 21, MutedInk);
        closeButton.transform.SetParent(window, false);
        Place((RectTransform)closeButton.transform, new Vector2(510, -297), new Vector2(235, 58));
        closeButton.navigation = new Navigation { mode = Navigation.Mode.None };
        TMP_Text closeLabel = closeButton.GetComponentInChildren<TMP_Text>();
        if (closeLabel != null)
        {
            Place(closeLabel.rectTransform, Vector2.zero, new Vector2(215, 52));
            Style(closeLabel, "Back to Shop", 26, Ink, true);
        }
    }

    private void OnEnable()
    {
        previousSize = new Vector2(-1, -1);
        if (balance != null) balance.text = "Memory Points   " + TutorialRewardBridge.MemoryPoints;
        Fit();
    }
    private void LateUpdate() => Fit();
    private void BuildMachineArtwork()
    {
        if (machineArtwork == null) return;
        Texture2D texture = machineArtwork;
        Rect uv = Rect.MinMaxRect(Mathf.Clamp01(machineUvRect.xMin), Mathf.Clamp01(machineUvRect.yMin),
            Mathf.Clamp01(machineUvRect.xMax), Mathf.Clamp01(machineUvRect.yMax));
        if (uv.width <= 0 || uv.height <= 0) return;

        GameObject go = new GameObject("Original_Memory_Machine", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.layer = 5;
        go.transform.SetParent(window, false);
        RawImage image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.uvRect = uv;
        image.color = Color.white;
        image.raycastTarget = false;
        texture.filterMode = FilterMode.Point;
        // Crop with texture coordinates so the exact game pixels are reused without resampling a new bitmap.
        float aspect = (uv.width * texture.width) / (uv.height * texture.height);
        // The 41px-tall source is enlarged exactly 8x and rests on the pixel shelf.
        Place(image.rectTransform, new Vector2(-435, 30), new Vector2(328f * aspect, 328f));
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

    private void Card(string name, float y, string number, string heading, string description)
    {
        Image border = Image(name, window, new Vector2(248, y), new Vector2(744, 166), new Color(0.60f, 0.39f, 0.19f));
        border.raycastTarget = false;
        Image face = Image(name + "_Parchment", border.transform, Vector2.zero, new Vector2(736, 158), new Color(0.94f, 0.79f, 0.56f));
        face.raycastTarget = false;
        Image numberPlate = Image(name + "_Number_Plate", window, new Vector2(-62, y), new Vector2(90, 110), new Color(0.78f, 0.61f, 0.38f));
        numberPlate.raycastTarget = false;
        Label(name + "_Number", number, new Vector2(-62, y + 8), new Vector2(90, 60), 42, Ink, true);
        Label(name + "_Slot_Label", "SLOT", new Vector2(-62, y - 31), new Vector2(84, 30), 16, MutedInk, true);
        Label(name + "_Title", heading, new Vector2(210, y + 45), new Vector2(410, 42), 30, Ink, true, TextAlignmentOptions.Left);
        Label(name + "_Description", description, new Vector2(230, y - 9), new Vector2(450, 64), 22, MutedInk, false, TextAlignmentOptions.Left);
        Label(name + "_Locked", "LOCKED", new Vector2(552, y + 45), new Vector2(120, 32), 18, MutedInk, true);
        Image unavailable = Image(name + "_Unavailable", window, new Vector2(503, y - 52), new Vector2(208, 38), new Color(0.77f, 0.65f, 0.47f));
        unavailable.raycastTarget = false;
        Label(name + "_Unavailable_Label", "Coming soon", new Vector2(503, y - 52), new Vector2(200, 34), 20, Ink);
    }

    private TMP_Text Label(string name, string value, Vector2 position, Vector2 size, float fontSize,
        Color color, bool bold = false, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        TMP_Text text = Instantiate(title, window);
        text.name = name; Place(text.rectTransform, position, size);
        Style(text, value, fontSize, color, bold); text.alignment = alignment;
        return text;
    }
    private static void Style(TMP_Text text, string value, float fontSize, Color color, bool bold)
    {
        text.text = value; text.fontSize = fontSize; text.enableAutoSizing = false;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.color = color; text.alignment = TextAlignmentOptions.Center; text.margin = Vector4.zero;
        text.overflowMode = TextOverflowModes.Truncate; text.raycastTarget = false;
    }
    private static Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5; go.transform.SetParent(parent, false);
        UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>(); image.color = color;
        Place(image.rectTransform, position, size); return image;
    }
    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one; rect.anchoredPosition = position; rect.sizeDelta = size;
    }
}
