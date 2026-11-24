using UnityEngine;
using UnityEngine.SceneManagement;

// Short, scene-owned sprites. Collectibles can hide themselves immediately after collection.
public sealed class MemoryCollectionEffects : MonoBehaviour
{
    private static Sprite sparkSprite;
    private SpriteRenderer[] sparks;
    private bool inward;
    private float elapsed;
    private Color tint;
    private const float Duration = 0.42f;

    public static void EchoCaptured(Transform source) => Spawn(source, true, new Color(1f, 0.77f, 0.29f));
    public static void FragmentCollected(Transform source) => Spawn(source, false, new Color(1f, 0.52f, 0.69f));

    private static void Spawn(Transform source, bool inward, Color tint)
    {
        Collider2D area = source.GetComponent<Collider2D>();
        var root = new GameObject(inward ? "Echo_Capture_Sparks" : "Fragment_Pickup_Glint");
        SceneManager.MoveGameObjectToScene(root, source.gameObject.scene);
        root.transform.position = area != null && area.enabled && area.gameObject.activeInHierarchy
            ? area.bounds.center : source.position;
        var effect = root.AddComponent<MemoryCollectionEffects>();
        effect.inward = inward;
        effect.tint = tint;
        effect.sparks = new SpriteRenderer[inward ? 7 : 5];
        for (int i = 0; i < effect.sparks.Length; i++)
        {
            var item = new GameObject("Spark", typeof(SpriteRenderer));
            item.transform.SetParent(root.transform, false);
            var graphic = item.GetComponent<SpriteRenderer>();
            graphic.sprite = GetSpark();
            graphic.sortingOrder = 22;
            effect.sparks[i] = graphic;
        }
        effect.Draw(0f);
    }

    private static Sprite GetSpark()
    {
        if (sparkSprite != null) return sparkSprite;
        var texture = new Texture2D(5, 5, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "Memory Spark Pixels"
        };
        var pixels = new Color32[25];
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                if (x == 2 || y == 2)
                    pixels[y * 5 + x] = new Color32(255, 255, 255, (byte)(x == 2 && y == 2 ? 255 : 200));
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        sparkSprite = Sprite.Create(texture, new Rect(0, 0, 5, 5), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect);
        sparkSprite.name = "Memory Spark";
        return sparkSprite;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= Duration) { Destroy(gameObject); return; }
        Draw(elapsed / Duration);
    }

    private void Draw(float progress)
    {
        float eased = 1f - (1f - progress) * (1f - progress);
        for (int i = 0; i < sparks.Length; i++)
        {
            float angle = (i / (float)sparks.Length) * Mathf.PI * 2f + 0.3f;
            float radius = inward ? Mathf.Lerp(0.30f, 0.025f, progress) : Mathf.Lerp(0.025f, 0.22f, eased);
            radius *= (i % 2 == 0 ? 1f : 0.75f);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            if (!inward) offset.y += 0.10f * eased;
            // Match the 100 px/unit art grid so sparks have clean stepped motion.
            sparks[i].transform.localPosition = new Vector3(Mathf.Round(offset.x * 100f), Mathf.Round(offset.y * 100f), 0f) / 100f;
            sparks[i].transform.localScale = Vector3.one * (i % 2 == 0 ? 1f : 0.6f);
            Color color = i % 2 == 0 ? tint : Color.Lerp(tint, Color.white, 0.55f);
            color.a = (1f - progress) * (inward ? 0.95f : 0.9f);
            sparks[i].color = color;
        }
    }
}
