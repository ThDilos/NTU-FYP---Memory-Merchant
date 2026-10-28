using System.Threading;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

// Retain Yarn's markup/lifecycle behaviour while reading the selected speed each
// frame. The built-in LetterTypewriter snapshots its speed when a line starts.
public sealed class AdjustableDialogueTypewriter : LetterTypewriter, IAsyncTypewriter
{
    public int LineVersion { get; private set; }
    public bool IsFullyVisible { get; private set; }

    public new void PrepareForContent(MarkupParseResult line)
    {
        LineVersion++;
        IsFullyVisible = false;
        base.PrepareForContent(line);
    }
    public new void ContentWillDismiss()
    {
        IsFullyVisible = false;
        base.ContentWillDismiss();
    }
    public new async YarnTask RunTypewriter(MarkupParseResult line, CancellationToken cancellationToken)
    {
        if (TextElement == null) return;
        TextElement.maxVisibleCharacters = 0;
        TextElement.text = line.Text;
        foreach (var handler in ActionMarkupHandlers) handler.OnLineDisplayBegin(line, TextElement);
        int count = TextElement.GetTextInfo(line.Text).characterCount;
        double budget = CharactersPerSecond > 0 ? 1d / CharactersPerSecond : 0;
        for (int i = 0; i < count; i++)
        {
            while (!cancellationToken.IsCancellationRequested && CharactersPerSecond > 0 && budget < 1d / CharactersPerSecond)
            {
                double before = Time.timeAsDouble;
                await YarnTask.Yield();
                budget += Time.timeAsDouble - before;
            }
            foreach (var handler in ActionMarkupHandlers)
                await handler.OnCharacterWillAppear(i, line, cancellationToken).SuppressCancellationThrow();
            TextElement.maxVisibleCharacters++;
            if (!cancellationToken.IsCancellationRequested && CharactersPerSecond > 0)
                AudioManager.Instance?.PlayDialogueCharacter(TextElement.textInfo.characterInfo[i].character);
            if (CharactersPerSecond > 0) budget -= 1d / CharactersPerSecond;
        }
        TextElement.maxVisibleCharacters = count;
        IsFullyVisible = true;
        foreach (var handler in ActionMarkupHandlers) handler.OnLineDisplayComplete();
    }
}
