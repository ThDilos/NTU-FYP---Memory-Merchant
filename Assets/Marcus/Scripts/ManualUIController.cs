using System.Collections.Generic;
using UnityEngine;
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

    [Header("Gameplay input (only the Player map is suspended)")]
    [SerializeField] private InputActionAsset[] gameplayInputAssets;

    private readonly GameplayInputPause pause = new GameplayInputPause();

    private void OnEnable()
    {
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
        if (isActiveAndEnabled) SelectControls();
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
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] != null)
            {
                tabs[i].gameObject.SetActive(IsAvailable(i));
                tabs[i].navigation = new Navigation { mode = Navigation.Mode.None };
                tabs[i].transition = Selectable.Transition.None;
                if (tabs[i].image != null)
                    tabs[i].image.sprite = i == selected ? activeTabSprite : inactiveTabSprite;
            }
            if (contents[i] != null)
                contents[i].SetActive(IsAvailable(i) && i == selected);
        }
        LayoutTabs();
    }

    private void LayoutTabs()
    {
        const float width = 250f;
        const float bottom = -56f;
        int count = context == ManualContext.Shop ? 2 : 3;
        int visibleIndex = 0;
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null || !IsAvailable(i)) continue;
            RectTransform rect = tabs[i].transform as RectTransform;
            if (rect == null) continue;

            // Pack the artwork edge to edge and align both sprite heights to the book.
            Sprite sprite = tabs[i].image != null ? tabs[i].image.sprite : null;
            float height = sprite != null && sprite.rect.width > 0f
                ? width * sprite.rect.height / sprite.rect.width : 112f;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2((visibleIndex++ - (count - 1) / 2f) * width, bottom);
        }
    }

    private void OnRectTransformDimensionsChange() => FitBook();

    private void FitBook()
    {
        RectTransform panel = transform as RectTransform;
        if (bookLayout == null || panel == null)
            return;
        float scale = Mathf.Min((panel.rect.width - 48f) / 1400f,
            (panel.rect.height - 48f) / 900f);
        bookLayout.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    private void SuspendGameplay() => pause.Capture(gameplayInputAssets);
    private void OnDisable() => pause.Restore();
}
