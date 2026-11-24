using System;
using UnityEngine;

// Authored shot layouts and native Unity clips. Coordinates use the artwork's
// top-left origin on a 320 x 180 logical stage.
[CreateAssetMenu(menuName = "The Memory Shop/Prologue Cutscenes")]
public sealed class PrologueCutsceneLibrary : ScriptableObject
{
    public PrologueShot[] shots = Array.Empty<PrologueShot>();

    public PrologueShot Find(string id) => Array.Find(shots, shot => shot.id == id);
}

[Serializable]
public sealed class PrologueShot
{
    public string id;
    public Texture2D background;
    public Rect cameraRect = new Rect(0, 0, 320, 180);
    public PrologueLayer[] layers = Array.Empty<PrologueLayer>();
    public PrologueAction[] actions = Array.Empty<PrologueAction>();
}

[Serializable]
public sealed class PrologueLayer
{
    public string name;
    [Tooltip("Optional character identifier for reusable poses. Empty uses this layer's name.")]
    public string characterId;
    public Texture2D texture;
    [Tooltip("Optional existing game sprite for a stationary layer.")]
    public Sprite sprite;
    [Tooltip("Optional full-frame resting sprite for walking layers.")]
    public Texture2D idleTexture;
    public Texture2D blink;
    public Texture2D speakingPose;
    public Vector2 position;
    public Vector2 size;
    public Color color = Color.white;
    [Min(1)] public int frameCount = 1;
    [Min(1f)] public float framesPerSecond = 8f;
    public bool sway;
}

[Serializable]
public sealed class PrologueAction
{
    public string id;
    public AnimationClip clip;
    public string walkingLayer;
    public bool interactionCue;
}
