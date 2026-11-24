using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Restrained, visual-only motion for the story stage. The controller supplies
// scaled elapsed time, so History freezes these effects with the dialogue.
public sealed class PrologueAmbientEffects : IDisposable
{
    internal const float KeyLandingCue = 2.1f;
    private const float LandingGlintDuration = .32f;
    private static readonly Vector2[] DustHomes = { new Vector2(74, -46), new Vector2(184, -54) };
    private readonly List<GameObject> roots = new List<GameObject>();
    private readonly List<RawImage> pixels = new List<RawImage>();
    private readonly RawImage key;
    private readonly RawImage machine;
    private readonly bool handover;
    private RawImage[] glint;
    private RawImage landingAtlas;
    private int landingFrameCount;
    private float landingTime = float.PositiveInfinity;
    private RawImage indicator;
    private RawImage[] dust;
    private bool disposed;

    public float Clock { get; private set; }
    public float KeyGlintAlpha { get; private set; }
    public float MachinePulseAlpha { get; private set; }
    public int PixelCount => pixels.Count;

    public PrologueAmbientEffects(RectTransform stage, string shotId, Func<string, RawImage> findLayer)
    {
        if (stage == null || findLayer == null) return;
        handover = string.Equals(shotId, "handover", StringComparison.Ordinal);
        key = findLayer("Key");
        machine = findLayer("Machine");

        if (key != null)
        {
            RectTransform root = Root("KeyGlint", key.rectTransform);
            Vector2 centre = SourcePoint(key, 8, 12);
            if (handover)
            {
                Texture2D atlas = Resources.Load<Texture2D>("ShopEffects/KeyLanding");
                if (atlas != null && atlas.width % atlas.height == 0)
                {
                    landingFrameCount = atlas.width / atlas.height;
                    landingAtlas = Pixel("LandingGlint", root, centre + new Vector2(-4, 4));
                    landingAtlas.texture = atlas;
                    landingAtlas.rectTransform.sizeDelta = new Vector2(9, 9);
                }
            }
            if (landingAtlas == null)
            {
                Vector2[] offsets = { Vector2.zero, Vector2.left, Vector2.right, Vector2.up, Vector2.down };
                glint = new RawImage[offsets.Length];
                for (int i = 0; i < offsets.Length; i++)
                    glint[i] = Pixel("GlintPixel" + i, root, centre + offsets[i]);
            }
        }

        if (machine != null)
        {
            RectTransform root = Root("MachineDialGlow", machine.rectTransform);
            // This is the existing warm indicator inside the dial artwork.
            // Keep the casing, phone receiver and all authored alpha untouched.
            indicator = Pixel("Indicator", root, SourcePoint(machine, 224, 72));
        }

        if (string.Equals(shotId, "interior", StringComparison.Ordinal))
        {
            RectTransform root = Root("QuietShopDust", stage);
            dust = new[] { Pixel("Dust0", root, DustHomes[0]), Pixel("Dust1", root, DustHomes[1]) };
        }
        Sample(0f);
    }

    public void TriggerKeyLanding(float elapsed)
    {
        if (handover && float.IsPositiveInfinity(landingTime)) landingTime = elapsed;
    }

    public void Sample(float elapsed)
    {
        if (disposed) return;
        Clock = Mathf.Max(0f, elapsed);

        float landingProgress = (Clock - landingTime) / LandingGlintDuration;
        bool landing = handover && landingProgress >= 0f && landingProgress < 1f;
        if (landingAtlas != null)
        {
            KeyGlintAlpha = landing ? Opacity(key) : 0f;
            if (landing)
            {
                int frame = Mathf.Min(landingFrameCount - 1, (int)(landingProgress * landingFrameCount));
                landingAtlas.uvRect = new Rect(frame / (float)landingFrameCount, 0f, 1f / landingFrameCount, 1f);
            }
            Tint(landingAtlas, new Color(1f, 1f, 1f, KeyGlintAlpha));
        }
        else if (glint != null)
        {
            // In the handover, the key shines once after landing in Jade's palm.
            // Outside the shop, it keeps its occasional glint during the unlock.
            int frame = handover ? (landing ? 10 + (int)(landingProgress * 3f) : -1)
                : Mathf.FloorToInt(Clock * 8f) % 40;
            float strength = frame == 10 ? .38f : frame == 11 ? .88f : frame == 12 ? .52f : 0f;
            KeyGlintAlpha = strength * Opacity(key);
            for (int i = 0; i < glint.Length; i++)
            {
                float alpha = i == 0 ? KeyGlintAlpha : frame == 11 ? KeyGlintAlpha * .72f : 0f;
                Tint(glint[i], new Color(1f, .91f, .62f, alpha));
            }
        }

        if (indicator != null)
        {
            float wave = .5f + .5f * Mathf.Sin(Clock * 1.1f);
            MachinePulseAlpha = Mathf.Lerp(.04f, .22f, wave) * Opacity(machine);
            Tint(indicator, new Color(1f, .66f, .23f, MachinePulseAlpha));
        }

        if (dust != null)
        {
            for (int i = 0; i < dust.Length; i++)
            {
                float phase = Clock * .5f + i * 2.1f;
                Vector2 offset = new Vector2(Mathf.Round(Mathf.Sin(phase)), Mathf.Round(Mathf.Cos(phase * .65f) * 2f));
                dust[i].rectTransform.anchoredPosition = DustHomes[i] + offset;
                float alpha = .06f + .07f * (.5f + .5f * Mathf.Sin(phase * .8f));
                Tint(dust[i], new Color(1f, .85f, .56f, alpha));
            }
        }
    }

    private static float Opacity(RawImage image) => image != null && image.enabled && image.gameObject.activeInHierarchy
        ? Mathf.Clamp01(image.color.a * image.canvasRenderer.GetInheritedAlpha()) : 0f;

    private static Vector2 SourcePoint(RawImage image, float x, float y)
    {
        float width = image.texture != null ? image.texture.width : image.rectTransform.rect.width;
        float height = image.texture != null ? image.texture.height : image.rectTransform.rect.height;
        return new Vector2(Mathf.Round(x * image.rectTransform.rect.width / Mathf.Max(1f, width)),
            -Mathf.Round(y * image.rectTransform.rect.height / Mathf.Max(1f, height)));
    }

    private RectTransform Root(string name, RectTransform parent)
    {
        var item = new GameObject(name, typeof(RectTransform));
        item.layer = 5;
        var rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.one;
        roots.Add(item);
        return rect;
    }

    private RawImage Pixel(string name, RectTransform parent, Vector2 position)
    {
        var item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        item.layer = 5;
        var image = item.GetComponent<RawImage>();
        RectTransform rect = image.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = Vector2.one;
        image.texture = Texture2D.whiteTexture;
        image.color = Color.clear;
        image.raycastTarget = false;
        pixels.Add(image);
        return image;
    }

    private static void Tint(RawImage image, Color color)
    {
        if (image != null) image.color = color;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (GameObject root in roots)
        {
            if (root == null) continue;
            root.SetActive(false);
            UnityEngine.Object.Destroy(root);
        }
        roots.Clear();
        pixels.Clear();
        KeyGlintAlpha = MachinePulseAlpha = 0f;
    }
}
