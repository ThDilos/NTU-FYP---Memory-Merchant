using System;
using System.Collections;
using UnityEngine;

// A reusable presentation only. The interaction owns permission to change the world.
// Prefabs may contain SpriteRenderers and either Animator or legacy Animation clips.
public sealed class EchoReplayVisual : MonoBehaviour
{
    private enum Materialization { None, GoldHalo, RisingSweep }

    private SpriteRenderer[] images;
    private Color[] colors;
    private Animator[] animators;
    private float[] animatorSpeeds;
    private Animation[] animations;
    private float alpha;
    private GameObject goldOutlineRoot;
    private SpriteRenderer[] goldSilhouette;
    private SpriteRenderer goldGleam;
    private GameEffectAtlas goldGleamAtlas;
    private Materialization materialization;
    private static readonly Vector2[] OutlineOffsets = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };

    public static EchoReplayVisual Create(Transform owner, GameObject prefab, Sprite sprite,
        string visualName, float height)
    {
        GameObject root = prefab != null ? Instantiate(prefab, owner) : new GameObject(visualName);
        root.name = visualName;
        root.transform.SetParent(owner, false);
        if (prefab == null)
        {
            SpriteRenderer image = root.AddComponent<SpriteRenderer>();
            image.sprite = sprite;
            image.sortingOrder = 20;
            if (sprite != null && sprite.bounds.size.y > 0f)
                root.transform.localScale = Vector3.one * height / sprite.bounds.size.y;
        }
        EchoReplayVisual result = root.GetComponent<EchoReplayVisual>();
        if (result == null) result = root.AddComponent<EchoReplayVisual>();
        result.Cache();
        result.ResetVisual();
        return result;
    }

    private void Cache()
    {
        images = GetComponentsInChildren<SpriteRenderer>(true);
        colors = new Color[images.Length];
        for (int i = 0; i < images.Length; i++) colors[i] = images[i].color;
        animators = GetComponentsInChildren<Animator>(true);
        animatorSpeeds = new float[animators.Length];
        for (int i = 0; i < animators.Length; i++) animatorSpeeds[i] = animators[i].speed;
        animations = GetComponentsInChildren<Animation>(true);
    }

    public void RevealAt(Vector3 worldPosition, bool goldOutline = false) =>
        ShowAt(worldPosition, goldOutline ? Materialization.GoldHalo : Materialization.None);

    internal void RevealWithRisingOutline(Vector3 worldPosition) =>
        ShowAt(worldPosition, Materialization.RisingSweep);

    private void ShowAt(Vector3 worldPosition, Materialization effect)
    {
        EndGoldOutline();
        transform.position = worldPosition;
        gameObject.SetActive(true);
        SetAlpha(0f);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].speed = animatorSpeeds[i];
            animators[i].Rebind();
        }
        foreach (Animation animation in animations) { animation.Rewind(); animation.Play(); }
        if (effect != Materialization.None && images.Length > 0 && images[0].sprite != null)
        {
            materialization = effect;
            PrepareGoldOutline(images[0]);
            goldOutlineRoot.SetActive(true);
            DrawGoldOutline(0f);
        }
    }

    public IEnumerator FadeTo(float target, float duration, Func<bool> blocked)
    {
        float start = alpha;
        float elapsed = 0f;
        while (elapsed < Mathf.Max(0.01f, duration))
        {
            bool paused = blocked != null && blocked();
            PauseAnimations(paused);
            if (!paused)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                SetAlpha(Mathf.Lerp(start, target, progress));
                if (materialization != Materialization.None) DrawGoldOutline(progress);
            }
            yield return null;
        }
        SetAlpha(target);
        EndGoldOutline();
    }

    private void PrepareGoldOutline(SpriteRenderer source)
    {
        if (goldOutlineRoot != null) return;
        goldOutlineRoot = new GameObject("Echo_Replay_Gold_Outline");
        goldOutlineRoot.layer = source.gameObject.layer;
        goldOutlineRoot.transform.SetParent(source.transform, false);
        goldSilhouette = new SpriteRenderer[OutlineOffsets.Length];
        for (int i = 0; i < goldSilhouette.Length; i++)
        {
            SpriteRenderer edge = AddGoldArt(source, "Echo_Outline_Pixel", -1);
            edge.transform.localPosition = OutlineOffsets[i] / source.sprite.pixelsPerUnit;
            goldSilhouette[i] = edge;
        }
        goldGleamAtlas = GameEffectAtlas.Load("EchoReplayOutline");
        if (goldGleamAtlas != null)
        {
            goldGleam = AddGoldArt(source, "Echo_Replay_Gold_Gleam", 1);
        }
    }

    private SpriteRenderer AddGoldArt(SpriteRenderer source, string name, int sortingOffset)
    {
        var art = new GameObject(name, typeof(SpriteRenderer));
        art.layer = source.gameObject.layer;
        art.transform.SetParent(goldOutlineRoot.transform, false);
        SpriteRenderer renderer = art.GetComponent<SpriteRenderer>();
        renderer.sharedMaterial = source.sharedMaterial;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder + sortingOffset;
        return renderer;
    }

    private void DrawGoldOutline(float progress)
    {
        SpriteRenderer source = images[0];
        float brightness = Mathf.Sin(progress * Mathf.PI);
        var gold = new Color(1f, .79f, .32f, brightness * .64f);
        foreach (SpriteRenderer edge in goldSilhouette)
        {
            edge.sprite = source.sprite;
            edge.flipX = source.flipX;
            edge.flipY = source.flipY;
            edge.color = gold;
        }
        if (goldGleam != null)
        {
            Bounds bounds = source.sprite.bounds;
            if (materialization == Materialization.RisingSweep)
            {
                // Sweep from the feet toward the raised hand while the pose stays anchored to the key.
                float rise = Mathf.SmoothStep(0f, 1f, progress);
                goldGleam.transform.localPosition = bounds.center + new Vector3(
                    bounds.size.x * Mathf.Lerp(-.16f, .26f, rise), bounds.size.y * Mathf.Lerp(-.36f, .33f, rise), 0f);
                goldGleam.transform.localScale = Vector3.one * (bounds.size.y * Mathf.Lerp(.43f, .55f, brightness));
            }
            else
            {
                goldGleam.transform.localPosition = bounds.center + Vector3.up * (bounds.size.y * .25f);
                goldGleam.transform.localScale = Vector3.one * (bounds.size.y * .37f);
            }
            goldGleam.sprite = goldGleamAtlas.GetSprite(progress);
            goldGleam.color = new Color(1f, .91f, .51f, brightness * .68f);
        }
    }

    private void EndGoldOutline()
    {
        materialization = Materialization.None;
        if (goldOutlineRoot != null) goldOutlineRoot.SetActive(false);
    }

    public IEnumerator Hold(float seconds, Func<bool> blocked)
    {
        float elapsed = 0f;
        while (elapsed < Mathf.Max(0f, seconds))
        {
            bool paused = blocked != null && blocked();
            PauseAnimations(paused);
            if (!paused) elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void SetAlpha(float value)
    {
        alpha = value;
        for (int i = 0; i < images.Length; i++)
        {
            Color color = colors[i]; color.a *= value; images[i].color = color;
        }
    }

    private void PauseAnimations(bool paused)
    {
        for (int i = 0; i < animators.Length; i++)
            animators[i].speed = paused ? 0f : animatorSpeeds[i];
        foreach (Animation animation in animations)
            foreach (AnimationState state in animation) state.speed = paused ? 0f : 1f;
    }

    public void ResetVisual()
    {
        EndGoldOutline();
        PauseAnimations(false);
        SetAlpha(0f);
        gameObject.SetActive(false);
    }
}
