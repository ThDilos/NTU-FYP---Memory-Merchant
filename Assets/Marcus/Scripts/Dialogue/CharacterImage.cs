using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class CharacterImage : DialoguePresenterBase
{
    [Header("UI Reference")]
    public Image characterImage;

    [Header("Character Sprites")]
    public List<Sprite> characterImages;

    [Header("Yarn")]
    public DialogueRunner dialogueRunner;

    private LinePresenter linePresenter;
    private Sprite jadePortrait;
    private Sprite currentPortrait;
    private Sprite portraitOverride;
    private bool hasPortraitOverride;

    private void Awake()
    {
        if (dialogueRunner == null) dialogueRunner = GetComponentInParent<DialogueRunner>();
        if (dialogueRunner != null) linePresenter = dialogueRunner.GetComponentInChildren<LinePresenter>(true);
        jadePortrait = FindPortrait("JadeSmile_0") ?? FindPortrait("Jade_0");
        if (characterImage != null)
        {
            characterImage.preserveAspect = true;
            characterImage.raycastTarget = false;
        }
        ClearPortrait();
    }

    private void OnEnable()
    {
        if (dialogueRunner == null || characterImage == null) return;
        var presenters = new List<DialoguePresenterBase>(dialogueRunner.DialoguePresenters);
        if (!presenters.Contains(this))
        {
            presenters.Add(this);
            dialogueRunner.DialoguePresenters = presenters;
        }
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        ClearPortrait();
        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        // Resolve the portrait on each line so the previous speaker's artwork cannot carry over.
        currentPortrait = isActiveAndEnabled && IsJade(line.CharacterName)
            ? (hasPortraitOverride ? portraitOverride : jadePortrait) : null;
        hasPortraitOverride = false;
        portraitOverride = null;
        RefreshPortrait();
        return YarnTask.CompletedTask;
    }

    public override YarnTask<DialogueOption> RunOptionsAsync(DialogueOption[] options, LineCancellationToken token)
    {
        ClearPortrait();
        return YarnTask<DialogueOption>.FromResult(null);
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        ClearPortrait();
        return YarnTask.CompletedTask;
    }

    private static bool IsJade(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker)) return false;
        int direction = speaker.IndexOf('(');
        if (direction >= 0) speaker = speaker.Substring(0, direction);
        return string.Equals(speaker.Trim(), "Jade", StringComparison.OrdinalIgnoreCase);
    }

    [YarnCommand("portrait")]
    public void ChangePortrait(string spriteName)
    {
        if (characterImage == null || dialogueRunner == null || !dialogueRunner.IsDialogueRunning) return;
        bool hide = string.Equals(spriteName, "none", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(spriteName, "hide", StringComparison.OrdinalIgnoreCase);
        Sprite requested = hide ? null : FindPortrait(spriteName);
        if (!hide && requested == null)
        {
            Debug.LogWarning($"Portrait sprite '{spriteName}' not found in the Portrait list!", this);
            return;
        }
        // A command can select an expression or hide the portrait for the next Jade line.
        portraitOverride = requested;
        hasPortraitOverride = true;
        if (hide)
        {
            currentPortrait = null;
            RefreshPortrait();
        }
    }

    private Sprite FindPortrait(string spriteName)
    {
        if (characterImages == null) return null;
        foreach (Sprite sprite in characterImages)
            if (sprite != null && string.Equals(sprite.name, spriteName, StringComparison.OrdinalIgnoreCase)) return sprite;
        return null;
    }

    private void LateUpdate() => RefreshPortrait();

    private void RefreshPortrait()
    {
        if (characterImage == null) return;
        bool captionVisible = linePresenter == null || linePresenter.canvasGroup == null || linePresenter.canvasGroup.alpha > .01f;
        bool show = currentPortrait != null && dialogueRunner != null && dialogueRunner.IsDialogueRunning && captionVisible;
        if (show && characterImage.sprite != currentPortrait) characterImage.sprite = currentPortrait;
        if (characterImage.enabled != show) characterImage.enabled = show;
    }

    private void ClearPortrait()
    {
        currentPortrait = null;
        portraitOverride = null;
        hasPortraitOverride = false;
        RefreshPortrait();
    }

    private void OnDisable() => ClearPortrait();
}
