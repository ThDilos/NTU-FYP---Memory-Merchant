using System;
using System.Collections;
using UnityEngine;

// A reusable presentation only. The interaction owns permission to change the world.
// Prefabs may contain SpriteRenderers and either Animator or legacy Animation clips.
public sealed class EchoReplayVisual : MonoBehaviour
{
    private SpriteRenderer[] images;
    private Color[] colors;
    private Animator[] animators;
    private float[] animatorSpeeds;
    private Animation[] animations;
    private float alpha;

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

    public void RevealAt(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        gameObject.SetActive(true);
        SetAlpha(0f);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].speed = animatorSpeeds[i];
            animators[i].Rebind();
        }
        foreach (Animation animation in animations) { animation.Rewind(); animation.Play(); }
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
                SetAlpha(Mathf.Lerp(start, target, elapsed / Mathf.Max(0.01f, duration)));
            }
            yield return null;
        }
        SetAlpha(target);
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
        PauseAnimations(false);
        SetAlpha(0f);
        gameObject.SetActive(false);
    }
}
