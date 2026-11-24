using System.Collections;
using UnityEngine;

// The shop machine uses a transparent 320 by 180 stage sprite. Its receiver
// occupies the top part of the artwork, ending at the top of the case.
internal static class ShopMachineWakeEffect
{
    private const int ReceiverBottom = 67;
    internal const float Duration = .44f;
    private static readonly Vector2 Dial = new Vector2(224.5f, 73.5f);
    private static readonly Vector2[] Rays =
    {
        Vector2.zero, Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1)
    };

    internal static bool TryGetDialPosition(SpriteRenderer machine, out Vector3 position)
    {
        position = Vector3.zero;
        Sprite sprite = machine != null ? machine.sprite : null;
        if (sprite == null || sprite.rect.size != new Vector2(320, 180) || machine.flipX || machine.flipY) return false;
        Vector2 local = (new Vector2(Dial.x, sprite.rect.height - Dial.y) - sprite.pivot) / sprite.pixelsPerUnit;
        position = machine.transform.TransformPoint(local);
        return true;
    }

    internal static IEnumerator Play(SpriteRenderer machine)
    {
        Sprite original = machine.sprite;
        if (original == null || original.rect.size != new Vector2(320, 180) || machine.flipX || machine.flipY)
            yield break;

        Rect source = original.rect;
        float ppu = original.pixelsPerUnit;
        float seam = source.yMax - ReceiverBottom;
        Sprite body = CreateSprite(original.texture, Rect.MinMaxRect(source.xMin, source.yMin, source.xMax, seam), ppu);
        Sprite receiver = CreateSprite(original.texture, Rect.MinMaxRect(source.xMin, seam, source.xMax, source.yMax), ppu);
        Sprite pixel = CreateSprite(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), ppu);
        GameObject effect = new GameObject("Memory Machine Wake");
        effect.transform.SetParent(machine.transform, false);
        AddArt(effect.transform, machine, body, (body.rect.center - source.position - original.pivot) / ppu);
        Vector2 receiverHome = (receiver.rect.center - source.position - original.pivot) / ppu;
        SpriteRenderer handset = AddArt(effect.transform, machine, receiver, receiverHome);
        var light = new SpriteRenderer[Rays.Length];
        for (int i = 0; i < light.Length; i++)
        {
            light[i] = AddArt(effect.transform, machine, pixel, Vector2.zero);
            light[i].sortingOrder++;
        }

        machine.enabled = false;
        try
        {
            float start = Time.unscaledTime;
            for (float time = 0f; time < Duration; time = Time.unscaledTime - start)
            {
                int tick = Mathf.FloorToInt(time / .045f);
                float offset = tick == 0 || tick == 2 ? 1f : tick == 1 ? -1f : 0f;
                handset.transform.localPosition = receiverHome + new Vector2(offset / ppu, 0f);

                float pulseTime = Mathf.Clamp01((time - .095f) / .30f);
                float brightness = Mathf.Sin(pulseTime * Mathf.PI);
                int radius = Mathf.RoundToInt(Mathf.Lerp(1f, 5f, pulseTime));
                for (int i = 0; i < light.Length; i++)
                {
                    Vector2 point = Dial + Rays[i] * (i < 5 ? radius : Mathf.Max(1, radius - 2));
                    light[i].transform.localPosition = (new Vector2(point.x, source.height - point.y) - original.pivot) / ppu;
                    light[i].color = new Color(1f, .75f, .33f, brightness * (i == 0 ? .90f : .58f));
                }
                yield return null;
            }
        }
        finally
        {
            if (machine != null) machine.enabled = true;
            if (effect != null) effect.SetActive(false);
            Object.Destroy(effect);
            Object.Destroy(body);
            Object.Destroy(receiver);
            Object.Destroy(pixel);
        }
    }

    private static Sprite CreateSprite(Texture2D texture, Rect rect, float ppu) =>
        Sprite.Create(texture, rect, new Vector2(.5f, .5f), ppu, 0, SpriteMeshType.FullRect);

    private static SpriteRenderer AddArt(Transform parent, SpriteRenderer source, Sprite sprite, Vector2 position)
    {
        var child = new GameObject("Machine Effect Art");
        child.layer = source.gameObject.layer;
        child.transform.SetParent(parent, false);
        child.transform.localPosition = position;
        SpriteRenderer visual = child.AddComponent<SpriteRenderer>();
        visual.sprite = sprite;
        visual.sharedMaterial = source.sharedMaterial;
        visual.sortingLayerID = source.sortingLayerID;
        visual.sortingOrder = source.sortingOrder;
        visual.color = source.color;
        return visual;
    }
}
