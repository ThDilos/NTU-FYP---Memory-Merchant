using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yarn.Unity;

// Yarn pools its option items. Rebuild their navigation for each live choice set
// instead of allowing automatic navigation to find shop or HUD controls.
[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public sealed class DialogueChoiceFocus : MonoBehaviour
{
    private static readonly Color UnavailableText = new Color(.63f, .60f, .56f);
    private static readonly Color NormalAccent = new Color(.33f, .26f, .19f);
    private static readonly Color FocusedAccent = new Color(.45f, .72f, .58f);
    private OptionsPresenter presenter;
    private CanvasGroup canvasGroup;
    private TMP_FontAsset font;
    private readonly List<OptionItem> views = new List<OptionItem>();
    private readonly List<OptionItem> available = new List<OptionItem>();
    private readonly Dictionary<OptionItem, (TMP_Text Label, Image Background)> visuals =
        new Dictionary<OptionItem, (TMP_Text, Image)>();

    public bool IsPresentingOptions { get; private set; }
    public bool ChoicesReady { get; private set; }

    public void Configure(OptionsPresenter optionsPresenter, TMP_FontAsset dialogueFont)
    {
        presenter = optionsPresenter;
        canvasGroup = presenter != null ? presenter.GetComponent<CanvasGroup>() : null;
        font = dialogueFont;
        Refresh();
    }

    private void Update() => Refresh();
    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        IsPresentingOptions = false;
        ChoicesReady = false;
        available.Clear();
        views.Clear();
        if (presenter == null || !presenter.isActiveAndEnabled)
        {
            ClearOwnedSelection();
            return;
        }

        presenter.GetComponentsInChildren(true, views);
        bool visible = canvasGroup == null || canvasGroup.alpha > 0.01f;
        bool ready = Time.timeScale > 0f && visible && (canvasGroup == null || canvasGroup.interactable);
        foreach (OptionItem view in views)
        {
            // OnOptionSelected is assigned after Option. Avoid touching Option
            // or selecting a newly instantiated item before Yarn configures it.
            if (view == null || !view.isActiveAndEnabled || view.OnOptionSelected == null) continue;
            bool pending = !view.completionToken.IsCancellationRequested;
            if (pending || visible) IsPresentingOptions = true;
            RefreshAppearance(view);

            // An unavailable or fading item must never search outside this list.
            view.navigation = NavigationWithin(view, view);
            if (ready && pending && view.interactable && view.IsInteractable()) available.Add(view);
        }

        ChoicesReady = available.Count > 0;
        if (!ChoicesReady)
        {
            if (!IsPresentingOptions) ClearOwnedSelection();
            return;
        }

        for (int index = 0; index < available.Count; index++)
        {
            OptionItem previous = available[(index + available.Count - 1) % available.Count];
            OptionItem next = available[(index + 1) % available.Count];
            available[index].navigation = NavigationWithin(previous, next);
        }

        EventSystem events = EventSystem.current;
        if (events == null) return;
        GameObject selected = events.currentSelectedGameObject;
        foreach (OptionItem view in available)
            if (selected == view.gameObject) return;

        // Select only focuses a response; the player still submits or clicks it.
        available[0].Select();
        // Yarn applies its prefab text colors when selection changes. Restore
        // our text palette immediately for both the newly focused and old row.
        if (selected != null && selected.TryGetComponent(out OptionItem previousOption) && visuals.ContainsKey(previousOption))
            RefreshAppearance(previousOption);
        RefreshAppearance(available[0]);
    }

    private void RefreshAppearance(OptionItem view)
    {
        if (!visuals.TryGetValue(view, out var appearance))
        {
            appearance = (view.GetComponentInChildren<TMP_Text>(true),
                view.transform.Find("Option Background")?.GetComponent<Image>());
            visuals.Add(view, appearance);
            DialogueBehaviour.StyleText(appearance.Label, font, DialogueBehaviour.DialogueTextSize, TextAlignmentOptions.MidlineLeft);
            if (appearance.Label != null)
            {
                appearance.Label.margin = new Vector4(32f, 4f, 12f, 4f);
                appearance.Label.raycastTarget = true;
            }

            var layout = view.GetComponent<LayoutElement>();
            if (layout == null) layout = view.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 48f;
            view.transition = Selectable.Transition.None;

            // Yarn draws the text on the option root before its child graphics.
            // Keep the focus accent in the left gutter, clear of the text.
            if (appearance.Background != null)
            {
                RectTransform accent = appearance.Background.rectTransform;
                accent.anchorMin = Vector2.zero;
                accent.anchorMax = Vector2.up;
                accent.offsetMin = new Vector2(0f, 4f);
                accent.offsetMax = new Vector2(5f, -4f);
                appearance.Background.raycastTarget = false;
            }
        }

        Color textColor = view.interactable ? DialogueBehaviour.DialogueTextColor : UnavailableText;
        if (appearance.Label != null && appearance.Label.color != textColor) appearance.Label.color = textColor;

        bool focused = view.IsInteractable() && EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == view.gameObject;
        Color accentColor = focused ? FocusedAccent : NormalAccent;
        if (appearance.Background != null && appearance.Background.color != accentColor) appearance.Background.color = accentColor;
    }

    public bool SubmitCurrentOption()
    {
        Refresh();
        if (!ChoicesReady || EventSystem.current == null) return false;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        foreach (OptionItem view in available)
        {
            if (view.gameObject != selected) continue;
            if (view.OnOptionSelected == null) return false;
            // Yarn consumes its one-shot Awaitable as soon as submission runs.
            // Do not inspect that completion source's Task after invoking it.
            view.InvokeOptionSelected();
            return true;
        }
        return false;
    }

    private static Navigation NavigationWithin(Selectable previous, Selectable next)
    {
        return new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = previous,
            selectOnDown = next,
            selectOnLeft = previous,
            selectOnRight = next
        };
    }

    private void ClearOwnedSelection()
    {
        EventSystem events = EventSystem.current;
        GameObject selected = events != null ? events.currentSelectedGameObject : null;
        if (selected == null || presenter == null) return;
        OptionItem option = selected.GetComponent<OptionItem>();
        if (option != null && option.transform.IsChildOf(presenter.transform))
            events.SetSelectedGameObject(null);
    }

    private void OnDisable()
    {
        IsPresentingOptions = false;
        ChoicesReady = false;
        ClearOwnedSelection();
        available.Clear();
        views.Clear();
    }
}
