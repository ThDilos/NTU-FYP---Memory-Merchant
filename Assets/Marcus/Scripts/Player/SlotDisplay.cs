using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Adapted from Chen Ou's SlotDisplay: empty/locked slots clear both icon and action.
public sealed class SlotDisplay : MonoBehaviour
{
    private Image frame;
    private Image surface;
    private Image icon;
    private TMP_Text shortcut;
    private TMP_Text title;
    private TMP_Text hint;
    private Button button;

    public static SlotDisplay Create(Transform parent, int index, Action useEcho)
    {
        Image frame = Panel("Echo_Slot_" + (index + 1), parent, new Color(0.68f, 0.43f, 0.18f));
        RectTransform rect = frame.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-54f - (2 - index) * 196f, -92f);
        rect.sizeDelta = new Vector2(184f, 170f);
        SlotDisplay slot = frame.gameObject.AddComponent<SlotDisplay>();
        slot.frame = frame;
        slot.surface = Panel("Surface", frame.transform, new Color(0.11f, 0.10f, 0.07f));
        slot.surface.rectTransform.anchorMin = Vector2.zero;
        slot.surface.rectTransform.anchorMax = Vector2.one;
        slot.surface.rectTransform.offsetMin = Vector2.one * 4f;
        slot.surface.rectTransform.offsetMax = Vector2.one * -4f;
        slot.surface.raycastTarget = false;
        slot.button = frame.gameObject.AddComponent<Button>();
        slot.button.targetGraphic = slot.surface;
        slot.button.navigation = new Navigation { mode = Navigation.Mode.None };
        slot.button.onClick.AddListener(() => useEcho());
        slot.shortcut = Label("Shortcut", frame.transform, new Vector2(-65, -20), new Vector2(40, 32), 24);
        slot.shortcut.fontStyle = FontStyles.Bold;
        slot.icon = Panel("Icon", frame.transform, Color.white);
        Place(slot.icon.rectTransform, new Vector2(0, -58), new Vector2(66, 66));
        slot.icon.preserveAspect = true;
        slot.icon.raycastTarget = false;
        slot.title = Label("Name", frame.transform, new Vector2(0, -108), new Vector2(164, 48), 23);
        slot.title.fontStyle = FontStyles.Bold;
        slot.hint = Label("Hint", frame.transform, new Vector2(0, -146), new Vector2(168, 30), 17);
        slot.SetDisplay(index, false, null, null, null, false);
        return slot;
    }

    public void SetDisplay(int index, bool unlocked, Sprite sprite, string displayName, string instruction, bool usable)
    {
        bool occupied = unlocked && !string.IsNullOrEmpty(displayName);
        shortcut.text = (index + 1).ToString();
        icon.sprite = occupied ? sprite : null;
        icon.enabled = occupied && sprite != null;
        title.text = !unlocked ? "Locked" : occupied ? displayName : "Empty";
        hint.text = !unlocked ? "Upgrade machine" : occupied ? instruction : "Capture an Echo";
        frame.color = unlocked ? new Color(0.68f, 0.43f, 0.18f) : new Color(0.32f, 0.28f, 0.22f);
        title.color = shortcut.color = unlocked ? new Color(1f, 0.90f, 0.68f) : new Color(0.62f, 0.57f, 0.47f);
        button.interactable = occupied && usable;
    }

    private static Image Panel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>(); image.color = color; return image;
    }
    private static TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TextMeshProUGUI>();
        Place(text.rectTransform, position, size);
        text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.83f, 0.75f, 0.58f); text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Truncate;
        return text;
    }
    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
}
