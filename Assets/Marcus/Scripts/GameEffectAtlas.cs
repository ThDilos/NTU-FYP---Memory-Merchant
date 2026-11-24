using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Share the imported pixel frames across world sprites and screen-space effects.
internal sealed class GameEffectAtlas
{
    private static readonly Dictionary<string, GameEffectAtlas> Loaded = new Dictionary<string, GameEffectAtlas>();
    private readonly Texture2D texture;
    private readonly int frameSize;
    private readonly Sprite[] frames;

    private GameEffectAtlas(Texture2D texture)
    {
        this.texture = texture;
        frameSize = texture.height;
        frames = new Sprite[texture.width / frameSize];
    }

    internal static GameEffectAtlas Load(string assetName)
    {
        if (Loaded.TryGetValue(assetName, out var atlas) && atlas.texture != null) return atlas;
        Texture2D source = Resources.Load<Texture2D>("ShopEffects/" + assetName);
        if (source == null || source.width % source.height != 0) return null;
        atlas = new GameEffectAtlas(source);
        Loaded[assetName] = atlas;
        return atlas;
    }

    internal void Sample(RawImage image, float progress)
    {
        float width = 1f / frames.Length;
        Rect uv = new Rect(Frame(progress) * width, 0f, width, 1f);
        if (image.texture != texture) image.texture = texture;
        if (image.uvRect != uv) image.uvRect = uv;
    }

    internal Sprite GetSprite(float progress)
    {
        int index = Frame(progress);
        if (frames[index] == null)
        {
            frames[index] = Sprite.Create(texture, new Rect(index * frameSize, 0, frameSize, frameSize),
                new Vector2(.5f, .5f), frameSize, 0, SpriteMeshType.FullRect);
            frames[index].name = texture.name + " " + index;
        }
        return frames[index];
    }

    private int Frame(float progress) => Mathf.Min(frames.Length - 1, (int)(Mathf.Clamp01(progress) * frames.Length));
}
