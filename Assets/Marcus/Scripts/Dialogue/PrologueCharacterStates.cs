using System;
using UnityEngine;

// Optional reusable poses for the full-body prologue actors. The small
// Memory World walking sprites use their separate locomotion controller.
[CreateAssetMenu(menuName = "The Memory Shop/Prologue Character States")]
public sealed class PrologueCharacterStates : ScriptableObject
{
    public PrologueCharacterState[] states = Array.Empty<PrologueCharacterState>();
    public PrologueCharacterGesture[] gestures = Array.Empty<PrologueCharacterGesture>();

    public PrologueCharacterState Find(string character, string state)
    {
        if (string.Equals(state, "think", StringComparison.OrdinalIgnoreCase)) state = "thinking";
        return Array.Find(states, item => item != null &&
            string.Equals(item.character, character, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.id, state, StringComparison.OrdinalIgnoreCase));
    }

    public PrologueCharacterGesture FindGesture(string id) => Array.Find(gestures,
        item => item != null && string.Equals(item.id, id, StringComparison.OrdinalIgnoreCase));
}

[Serializable]
public sealed class PrologueCharacterState
{
    public string character;
    public string id;
    public Texture2D[] frames = Array.Empty<Texture2D>();
    [Min(.1f)] public float framesPerSecond = 6f;
    public bool loop = true;
    public bool returnToIdle;

    public float Duration => (frames != null ? frames.Length : 0) / Mathf.Max(.1f, framesPerSecond);

    public Texture2D FrameAt(float elapsed)
    {
        if (frames == null || frames.Length == 0) return null;
        int index = Mathf.FloorToInt(Mathf.Max(0f, elapsed) * Mathf.Max(.1f, framesPerSecond));
        index = loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1);
        return frames[index];
    }
}

[Serializable]
public sealed class PrologueCharacterGesture
{
    public string id;
    [Tooltip("Optional pose shown during the gesture, then the previous pose is restored.")]
    public string pose;
    [Min(.1f)] public float framesPerSecond = 8f;
    public Vector2[] offsets = Array.Empty<Vector2>();
    [Tooltip("An editable native Unity clip using the same relative offsets. Apply to a zero-position visual child.")]
    public AnimationClip clip;

    public float Duration => Mathf.Max(1, offsets != null ? offsets.Length : 0) / Mathf.Max(.1f, framesPerSecond);

    public Vector2 OffsetAt(float elapsed)
    {
        if (offsets == null || offsets.Length == 0) return Vector2.zero;
        int index = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, elapsed) * Mathf.Max(.1f, framesPerSecond)), 0, offsets.Length - 1);
        Vector2 offset = offsets[index];
        return new Vector2(Mathf.Round(offset.x), Mathf.Round(offset.y));
    }
}
