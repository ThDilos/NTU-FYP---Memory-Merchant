using UnityEngine;
using Unity.Profiling;

// Presentation only: the Study's blocking fog and restoration stay independent.
[DisallowMultipleComponent]
public sealed class MemoryWorldAmbientFog : MonoBehaviour
{
    [SerializeField] private SpriteRenderer mapRenderer;
    [Header("Appearance")]
    [SerializeField] private Color fogColour = new Color(0.76f, 0.80f, 0.77f, 1f);
    [SerializeField, Range(0f, 0.6f)] private float opacity = 0.48f;
    [Tooltip("How far the mist can feather inward over the outer map border, in world units.")]
    [SerializeField, Min(0.01f)] private float innerFeather = 0.35f;
    [SerializeField, Min(0.1f)] private float outerReach = 1.6f;
    [Tooltip("Point-filtered fog pixels; the room artwork keeps its original sharpness.")]
    [SerializeField, Min(0.01f)] private float pixelSize = 0.025f;
    [SerializeField, Min(0f)] private float driftSpeed = 0.025f;
    [Tooltip("Above scenery, below Echo replay and interaction markers.")]
    [SerializeField] private int sortingOrder = 8;

    private SpriteRenderer fogRenderer;
    private Texture2D fogTexture;
    private Sprite fogSprite;
    private Color32[] pixels;
    private FogAxis[] columns;
    private FogAxis[] rows;
    private float reach;
    private float elapsed;
    private float refreshTimer;
    private float nextSampleTime;
    private int generatedRows;

    private const float RefreshInterval = 0.125f;
    private const int MinGenerationFrames = 4;
    private static readonly ProfilerMarker GenerateMarker = new ProfilerMarker("MemoryWorldFog.GenerateRows");
    private static readonly ProfilerMarker UploadMarker = new ProfilerMarker("MemoryWorldFog.Upload");

    private readonly struct FogAxis
    {
        public readonly float Distance;
        public readonly float Broad;
        public readonly float Detail;
        public readonly float OutsideFade;

        public FogAxis(float position, float centre, float extent, float reach, float broadFrequency)
        {
            Distance = Mathf.Abs(position - centre) - extent;
            Broad = position * broadFrequency;
            Detail = position * 4.5f;
            OutsideFade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(reach * 0.65f, reach, Distance));
        }
    }

    private void OnEnable()
    {
        if (mapRenderer == null) mapRenderer = GetComponent<SpriteRenderer>();
        if (mapRenderer == null || mapRenderer.sprite == null) return;

        Bounds mapBounds = mapRenderer.bounds;
        float cell = Mathf.Max(0.01f, pixelSize);
        reach = Mathf.Max(0.1f, outerReach);
        int width = Mathf.CeilToInt((mapBounds.size.x + reach * 2f) / cell);
        int height = Mathf.CeilToInt((mapBounds.size.y + reach * 2f) / cell);
        fogTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Memory edge mist",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        pixels = new Color32[width * height];
        Vector2 textureOrigin = (Vector2)mapBounds.center - new Vector2(width, height) * cell * 0.5f;
        columns = new FogAxis[width];
        rows = new FogAxis[height];
        for (int x = 0; x < width; x++)
            columns[x] = new FogAxis(textureOrigin.x + (x + 0.5f) * cell, mapBounds.center.x, mapBounds.extents.x, reach, 1.6f);
        for (int y = 0; y < height; y++)
            rows[y] = new FogAxis(textureOrigin.y + (y + 0.5f) * cell, mapBounds.center.y, mapBounds.extents.y, reach, 2.1f);
        fogSprite = Sprite.Create(fogTexture, new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f), 1f / cell, 0, SpriteMeshType.FullRect);
        fogSprite.name = "Memory edge mist";
        fogSprite.hideFlags = HideFlags.DontSave;

        GameObject visual = new GameObject("Memory_Edge_Fog") { hideFlags = HideFlags.DontSave };
        // Preserve world scale if the background has an authored scale of its own.
        visual.transform.SetParent(transform, true);
        visual.transform.position = mapBounds.center;
        fogRenderer = visual.AddComponent<SpriteRenderer>();
        fogRenderer.sprite = fogSprite;
        fogRenderer.sharedMaterial = mapRenderer.sharedMaterial;
        fogRenderer.sortingLayerID = mapRenderer.sortingLayerID;
        fogRenderer.sortingOrder = sortingOrder;
        elapsed = refreshTimer = 0f;
        nextSampleTime = RefreshInterval;
        generatedRows = 0;
        GenerateRows(0, height, 0f);
        UploadFog();
    }

    private void Update()
    {
        if (fogTexture == null || Time.deltaTime <= 0f) return;
        elapsed += Time.deltaTime;
        refreshTimer += Time.deltaTime;
        int height = rows.Length;
        int desiredRows = Mathf.FloorToInt(height * Mathf.Clamp01(refreshTimer / RefreshInterval));
        int rowBudget = (height + MinGenerationFrames - 1) / MinGenerationFrames;
        int endRow = Mathf.Min(desiredRows, generatedRows + rowBudget);
        if (endRow > generatedRows)
        {
            // Keep each texture on one drift phase. Spread generation across frames
            // and upload only when the complete phase is ready to display.
            GenerateRows(generatedRows, endRow, nextSampleTime);
            generatedRows = endRow;
        }

        if (generatedRows != height || refreshTimer < RefreshInterval) return;
        UploadFog();
        refreshTimer %= RefreshInterval;
        nextSampleTime = elapsed + RefreshInterval - refreshTimer;
        generatedRows = 0;
    }

    private void GenerateRows(int firstRow, int endRow, float time)
    {
        using var scope = GenerateMarker.Auto();
        float feather = Mathf.Max(0.01f, innerFeather);
        float drift = time * driftSpeed;
        Color32 colour = fogColour;
        float maximumAlpha = Mathf.Clamp01(opacity) * fogColour.a;

        int width = columns.Length;
        for (int y = firstRow; y < endRow; y++)
        {
            FogAxis row = rows[y];
            float broadY = row.Broad - drift * 0.4f + 31f;
            float detailY = row.Detail + drift * 0.3f + 11f;
            int index = y * width;
            for (int x = 0; x < width; x++)
            {
                FogAxis column = columns[x];
                FogAxis edgeSample = column.Distance > row.Distance ? column : row;
                float distance = edgeSample.Distance;
                float alpha = 0f;
                if (distance > -feather * 1.5f && distance < reach)
                {
                    float broad = Mathf.PerlinNoise(column.Broad + drift + 17f, broadY);
                    float detail = Mathf.PerlinNoise(column.Detail - drift * 0.6f + 53f, detailY);
                    float cloud = Mathf.Round((broad * 0.75f + detail * 0.25f) * 12f) / 12f;
                    float edge = Mathf.InverseLerp(-feather, feather * 0.6f,
                        distance + (cloud - 0.5f) * feather * 0.75f);
                    alpha = maximumAlpha * Mathf.SmoothStep(0f, 1f, edge)
                        * Mathf.Lerp(0.35f, 1f, cloud) * edgeSample.OutsideFade;
                }
                colour.a = (byte)Mathf.RoundToInt(alpha * 255f);
                pixels[index + x] = colour;
            }
        }
    }

    private void UploadFog()
    {
        using var scope = UploadMarker.Auto();
        fogTexture.SetPixels32(pixels);
        fogTexture.Apply(false, false);
    }

    private void OnDisable()
    {
        if (fogRenderer != null)
        {
            fogRenderer.enabled = false;
            Release(fogRenderer.gameObject);
        }
        if (fogSprite != null) Release(fogSprite);
        if (fogTexture != null) Release(fogTexture);
        fogRenderer = null;
        fogSprite = null;
        fogTexture = null;
        pixels = null;
        columns = rows = null;
    }

    private static void Release(Object resource)
    {
        if (Application.isPlaying) Destroy(resource);
        else DestroyImmediate(resource);
    }
}
