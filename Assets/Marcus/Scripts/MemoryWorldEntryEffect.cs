using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A single overlay carries the ripple over the scene boundary, then removes itself.
internal sealed class MemoryWorldEntryEffect : MonoBehaviour
{
    private const float CoverDuration = .27f;
    private const float RevealDuration = .30f;
    private static readonly Color ShadeColour = new Color(.105f, .072f, .052f);
    private RawImage shade;
    private RawImage ripple;
    private Texture2D fallback;
    private GameEffectAtlas atlas;
    private Vector2 origin;

    internal static void Enter(string sceneName, SpriteRenderer machine)
    {
        Vector2 centre = new Vector2(.5f, .5f);
        Camera camera = Camera.main;
        if (camera != null && ShopMachineWakeEffect.TryGetDialPosition(machine, out Vector3 dial))
        {
            Vector3 point = camera.WorldToViewportPoint(dial);
            centre = new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));
        }

        var root = new GameObject("Memory World Entry", typeof(RectTransform), typeof(Canvas));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        DontDestroyOnLoad(root);
        var effect = root.AddComponent<MemoryWorldEntryEffect>();
        effect.origin = centre;
        effect.StartCoroutine(effect.Play(sceneName));
    }

    private IEnumerator Play(string sceneName)
    {
        shade = CreateImage("Scene Shade");
        shade.texture = Texture2D.whiteTexture;
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = Vector2.one;
        shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;

        ripple = CreateImage("Amber Ripple");
        ripple.rectTransform.anchorMin = ripple.rectTransform.anchorMax = origin;
        ripple.rectTransform.anchoredPosition = Vector2.zero;
        atlas = GameEffectAtlas.Load("MemoryEntryRipple");
        if (atlas == null)
        {
            fallback = MakeRipple();
            ripple.texture = fallback;
        }

        try
        {
            float start = Time.unscaledTime;
            for (float elapsed = 0f; elapsed < CoverDuration; elapsed = Time.unscaledTime - start)
            {
                float progress = elapsed / CoverDuration;
                Draw(progress * .52f, Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
            }

            Draw(.52f, 1f);
            yield return SceneManager.LoadSceneAsync(sceneName);

            start = Time.unscaledTime;
            for (float elapsed = 0f; elapsed < RevealDuration; elapsed = Time.unscaledTime - start)
            {
                float progress = elapsed / RevealDuration;
                Draw(Mathf.Lerp(.52f, 1f, progress), 1f - Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
            }
        }
        finally { Destroy(gameObject); }
    }

    private RawImage CreateImage(string objectName)
    {
        var layer = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        layer.transform.SetParent(transform, false);
        var image = layer.GetComponent<RawImage>();
        image.raycastTarget = false;
        return image;
    }

    private void Draw(float progress, float darkness)
    {
        shade.color = new Color(ShadeColour.r, ShadeColour.g, ShadeColour.b, darkness);
        Vector2 span = new Vector2(Mathf.Max(origin.x, 1f - origin.x) * Screen.width,
            Mathf.Max(origin.y, 1f - origin.y) * Screen.height);
        float diameter = Mathf.Lerp(Screen.height * .13f, 2.35f * span.magnitude, progress);
        ripple.rectTransform.sizeDelta = Vector2.one * diameter;
        ripple.color = new Color(1f, 1f, 1f, .68f * Mathf.Sin(Mathf.PI * progress));
        atlas?.Sample(ripple, progress);
    }

    private static Texture2D MakeRipple()
    {
        const int size = 48;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Amber ripple fallback", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float radius = new Vector2(x - 23.5f, y - 23.5f).magnitude;
                if (radius > 20f && radius <= 22f) pixels[y * size + x] = new Color32(255, 189, 92, 255);
                else if (radius > 17f && radius <= 18f) pixels[y * size + x] = new Color32(246, 148, 58, 100);
            }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void OnDestroy()
    {
        if (fallback != null) Destroy(fallback);
    }
}
