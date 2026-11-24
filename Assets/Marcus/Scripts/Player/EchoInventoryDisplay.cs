using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Adapted from Chen Ou's generated slot inventory, using Marcus's collection and replay gates.
public sealed class EchoInventoryDisplay : MonoBehaviour
{
    [SerializeField] private LevelInventory inventory;
    [SerializeField] private TutorialMemoryState state;
    [SerializeField] private TutorialUIController tutorialUI;
    [SerializeField] private TutorialLockedCabinet cabinet;
    private Canvas hud;
    private readonly SlotDisplay[] slots = new SlotDisplay[3];
    private readonly HashSet<MemoryEcho> shownEchoes = new HashSet<MemoryEcho>();
    private readonly List<MemoryEcho> pendingPulses = new List<MemoryEcho>();
    private int pulseReadyFrame;
    private bool Blocked => Time.timeScale <= 0f ||
        (GameController.Instance != null && GameController.Instance.isPaused) ||
        (tutorialUI != null && (tutorialUI.ExternalInputBlocked ||
        (tutorialUI.manualPanel != null && tutorialUI.manualPanel.activeSelf) ||
        (tutorialUI.settingsPanel != null && tutorialUI.settingsPanel.activeSelf)));

    private void Awake()
    {
        GameObject root = new GameObject("Echo_Inventory_HUD", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        hud = root.GetComponent<Canvas>(); hud.renderMode = RenderMode.ScreenSpaceOverlay; hud.sortingOrder = 10;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        GameObject heading = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        heading.transform.SetParent(root.transform, false);
        TMP_Text label = heading.GetComponent<TMP_Text>(); label.text = "Echoes";
        label.fontSize = 28; label.fontStyle = FontStyles.Bold; label.raycastTarget = false;
        label.color = new Color(1f, 0.90f, 0.68f); label.alignment = TextAlignmentOptions.Right;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-54, -42); rect.sizeDelta = new Vector2(576, 40);
        for (int i = 0; i < slots.Length; i++)
        {
            int slot = i;
            slots[i] = SlotDisplay.Create(root.transform, i, () => UseEcho(slot));
        }
        hud.gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        if (inventory != null)
        {
            RememberInventory();
            inventory.EchoInventoryChanged += OnInventoryChanged;
            inventory.EchoCapacityChanged += Refresh;
        }
        if (state != null) state.Changed += Refresh;
        Refresh();
    }
    private void Start() => Refresh();
    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.EchoInventoryChanged -= OnInventoryChanged;
            inventory.EchoCapacityChanged -= Refresh;
        }
        if (state != null) state.Changed -= Refresh;
        pendingPulses.Clear();
        shownEchoes.Clear();
        if (hud != null) hud.gameObject.SetActive(false);
    }
    private void LateUpdate()
    {
        if (hud == null) return;
        bool visible = inventory != null && !Blocked;
        if (hud.gameObject.activeSelf != visible) hud.gameObject.SetActive(visible);
        if (!visible || pendingPulses.Count == 0 || Time.frameCount < pulseReadyFrame) return;
        for (int i = 0; i < slots.Length; i++)
            if (pendingPulses.Contains(inventory.GetEchoInSlot(i))) slots[i].PulseCapture();
        pendingPulses.Clear();
    }
    private void RememberInventory()
    {
        shownEchoes.Clear();
        for (int i = 0; i < slots.Length; i++)
        {
            MemoryEcho echo = inventory.GetEchoInSlot(i);
            if (echo != null) shownEchoes.Add(echo);
        }
    }
    private void OnInventoryChanged()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            MemoryEcho echo = inventory.GetEchoInSlot(i);
            if (echo == null || shownEchoes.Contains(echo) || pendingPulses.Contains(echo)) continue;
            pendingPulses.Add(echo);
            // Tutorial guidance starts on the following frame and can hide the HUD.
            pulseReadyFrame = Time.frameCount + 2;
        }
        RememberInventory();
        pendingPulses.RemoveAll(echo => !shownEchoes.Contains(echo));
        Refresh();
    }
    private void Refresh()
    {
        if (inventory == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            MemoryEcho echo = inventory.GetEchoInSlot(i);
            bool keyEcho = echo is TutorialMemoryEcho;
            bool cabinetOpen = keyEcho && state != null && state.CabinetUnlocked;
            string name = echo != null ? echo.DisplayName : null;
            if (echo != null && string.IsNullOrWhiteSpace(name)) name = echo.name;
            slots[i].SetDisplay(i, i < inventory.EchoSlotCount, echo != null ? echo.Sprite : null, name,
                cabinetOpen ? "Replayed / stored" : keyEcho ? "Hold 1 at cabinet" : "Use Echo",
                echo != null && !cabinetOpen);
        }
    }
    private void UseEcho(int slotIndex)
    {
        if (Blocked || inventory == null) return;
        MemoryEcho echo = inventory.GetEchoInSlot(slotIndex);
        if (echo == null) return;
        // Do not call the key Echo's generic replay: cabinet range/state checks remain authoritative.
        if (echo is TutorialMemoryEcho)
        {
            if (cabinet != null) cabinet.TryUseKeyEcho();
        }
        else echo.TriggerEchoFunction();
    }
}
