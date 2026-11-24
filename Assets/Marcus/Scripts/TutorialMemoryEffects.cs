using System.Collections.Generic;
using UnityEngine;

// Short, collider-free copies of the room artwork. The real props retain their authored transforms.
internal sealed class TutorialMemoryEffects
{
    private static readonly Vector2 MapSpriteSize = new Vector2(620f, 180f);
    private readonly GameObject root;
    private readonly List<Sprite> slices = new List<Sprite>();

    private TutorialMemoryEffects(Transform owner, string name)
    {
        root = new GameObject(name);
        root.transform.SetParent(owner, true);
    }

    private static bool IsMapLayer(SpriteRenderer source) => source != null && source.sprite != null &&
        source.sprite.rect.size == MapSpriteSize && !source.flipX && !source.flipY;

    private SpriteRenderer AddSlice(SpriteRenderer source, string name, Rect region, Vector2 pivot)
    {
        // The source is a full-map sprite. Crop the visible artwork and re-use its world position.
        Rect original = source.sprite.rect;
        var rect = new Rect(original.x + region.x, original.yMax - region.yMax, region.width, region.height);
        float pixelsPerUnit = source.sprite.pixelsPerUnit;
        Sprite slice = Sprite.Create(source.sprite.texture, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
        slices.Add(slice);
        SpriteRenderer art = AddArt(source, name);
        art.sprite = slice;
        Vector2 pivotInOriginal = rect.position - original.position + slice.pivot;
        art.transform.position = source.transform.TransformPoint((pivotInOriginal - source.sprite.pivot) / pixelsPerUnit);
        art.transform.rotation = source.transform.rotation;
        art.transform.localScale = source.transform.lossyScale;
        return art;
    }

    private SpriteRenderer AddArt(SpriteRenderer source, string name)
    {
        var item = new GameObject(name, typeof(SpriteRenderer));
        item.layer = source.gameObject.layer;
        item.transform.SetParent(root.transform, false);
        SpriteRenderer art = item.GetComponent<SpriteRenderer>();
        art.sharedMaterial = source.sharedMaterial;
        art.sortingLayerID = source.sortingLayerID;
        art.sortingOrder = source.sortingOrder;
        art.color = source.color;
        return art;
    }

    private void Remove()
    {
        if (root != null) { root.SetActive(false); Object.Destroy(root); }
        foreach (Sprite slice in slices) Object.Destroy(slice);
        slices.Clear();
    }

    internal sealed class CabinetMotion
    {
        internal const float LatchDuration = .26f;
        internal const float Duration = .56f;
        private readonly TutorialMemoryEffects effect;
        private readonly SpriteRenderer originalLock, latch, door, gleam;
        private readonly GameEffectAtlas atlas;
        private readonly bool wasEnabled;
        private readonly Vector3 latchCenter, doorScale;
        private readonly Quaternion latchRotation, doorRotation;

        internal static CabinetMotion Create(Transform owner, SpriteRenderer lockArt, SpriteRenderer openArt) =>
            IsMapLayer(lockArt) && IsMapLayer(openArt) ? new CabinetMotion(owner, lockArt, openArt) : null;

        private CabinetMotion(Transform owner, SpriteRenderer lockArt, SpriteRenderer openArt)
        {
            effect = new TutorialMemoryEffects(owner, "Cabinet_Unlock_Effect");
            originalLock = lockArt;
            wasEnabled = originalLock.enabled;
            // Only the lock ring and the open door swing. The exclamation mark and recorder are separate.
            latch = effect.AddSlice(lockArt, "Cabinet_Latch", new Rect(147, 61, 8, 10), new Vector2(.5f, .5f));
            door = effect.AddSlice(openArt, "Cabinet_Door_Settle", new Rect(156, 56, 7, 26), new Vector2(0f, .5f));
            latchCenter = latch.transform.position;
            latchRotation = latch.transform.rotation;
            doorRotation = door.transform.rotation;
            doorScale = door.transform.localScale;
            atlas = GameEffectAtlas.Load("CabinetLatch");
            if (atlas != null)
            {
                gleam = effect.AddArt(lockArt, "Cabinet_Latch_Gleam");
                gleam.transform.position = latchCenter;
                gleam.transform.localScale = Vector3.one * .22f;
                gleam.sortingOrder += 2;
            }
            originalLock.enabled = false;
            Draw(0f);
        }

        internal void Draw(float time)
        {
            bool turning = time < LatchDuration;
            latch.enabled = turning;
            door.enabled = !turning;
            if (gleam != null) gleam.enabled = turning;
            if (turning)
            {
                float t = time / LatchDuration;
                // A quick key turn with two small settling motions before the door releases.
                float angle = -32f * Mathf.SmoothStep(0f, 1f, t) + 5f * Mathf.Sin(t * Mathf.PI * 3f) * t;
                latch.transform.rotation = latchRotation * Quaternion.Euler(0f, 0f, angle);
                latch.transform.position = latchCenter + Vector3.down * (.016f * t);
                if (gleam != null)
                {
                    gleam.sprite = atlas.GetSprite(t);
                    gleam.color = new Color(1f, .88f, .47f, .85f * Mathf.Sin(t * Mathf.PI));
                }
                return;
            }

            float open = Mathf.Clamp01((time - LatchDuration) / (Duration - LatchDuration));
            float swing = Mathf.SmoothStep(.12f, 1f, open);
            float settle = Mathf.Sin(open * Mathf.PI * 4f) * open * (1f - open);
            door.transform.localScale = new Vector3(doorScale.x * (swing + .45f * settle), doorScale.y, doorScale.z);
            door.transform.rotation = doorRotation * Quaternion.Euler(0f, 0f, 11f * (1f - swing) + 12f * settle);
        }

        internal void Remove()
        {
            if (originalLock != null) originalLock.enabled = wasEnabled;
            effect.Remove();
        }
    }

    internal sealed class StudyPeel
    {
        internal const float Duration = .84f;
        private const int BandCount = 9;
        private const int WispCount = 4;
        private readonly TutorialMemoryEffects effect;
        private readonly SpriteRenderer[] bands = new SpriteRenderer[BandCount];
        private readonly Vector3[] bandHomes = new Vector3[BandCount];
        private readonly Quaternion[] bandRotations = new Quaternion[BandCount];
        private readonly SpriteRenderer[] wisps = new SpriteRenderer[WispCount];
        private readonly Color sourceColor;
        private readonly GameEffectAtlas atlas;
        private readonly Vector3 entrance;

        internal static StudyPeel Create(Transform owner, SpriteRenderer fog) =>
            IsMapLayer(fog) && fog.enabled && fog.gameObject.activeInHierarchy ? new StudyPeel(owner, fog) : null;

        private StudyPeel(Transform owner, SpriteRenderer fog)
        {
            effect = new TutorialMemoryEffects(owner, "Study_Fog_Peel");
            sourceColor = fog.color;
            // This 162×147 visible region is the Study haze in the tutorial's full-map layer.
            const int left = 418, top = 14, width = 162, height = 147;
            for (int i = 0; i < BandCount; i++)
            {
                int x = left + i * width / BandCount;
                int nextX = left + (i + 1) * width / BandCount;
                SpriteRenderer band = effect.AddSlice(fog, "Study_Fog_Slice", new Rect(x, top, nextX - x, height), new Vector2(.5f, .5f));
                bands[i] = band;
                bandHomes[i] = band.transform.position;
                bandRotations[i] = band.transform.rotation;
            }
            entrance = bandHomes[0];
            atlas = GameEffectAtlas.Load("StudyFogWisp");
            if (atlas != null)
            {
                for (int i = 0; i < wisps.Length; i++)
                {
                    wisps[i] = effect.AddArt(fog, "Study_Fog_Wisp");
                    wisps[i].sortingOrder++;
                    wisps[i].transform.localScale = new Vector3(.59f - i * .07f, .44f - i * .04f, 1f);
                }
            }
            Draw(0f);
        }

        internal bool DoorwayVisible(Camera camera)
        {
            if (camera == null) return true;
            Vector3 screen = camera.WorldToViewportPoint(entrance);
            return screen.z > 0f && screen.x >= 0f && screen.x <= 1f && screen.y >= 0f && screen.y <= 1f;
        }

        internal void Draw(float time)
        {
            for (int i = 0; i < bands.Length; i++)
            {
                float t = Mathf.Clamp01((time - i * .041f) / .49f);
                float eased = 1f - (1f - t) * (1f - t);
                Vector3 shift = new Vector3(.26f, .075f + i % 3 * .045f, 0f) * eased;
                shift = new Vector3(Mathf.Round(shift.x * 100f), Mathf.Round(shift.y * 100f), 0f) / 100f;
                bands[i].transform.position = bandHomes[i] + shift;
                bands[i].transform.rotation = bandRotations[i] * Quaternion.Euler(0f, 0f, (4f + i % 3) * t);
                Color color = sourceColor;
                color.a *= (1f - t) * (1f - t);
                bands[i].color = color;
            }
            if (atlas == null) return;
            for (int i = 0; i < wisps.Length; i++)
            {
                float t = Mathf.Clamp01((time - i * .068f) / .61f);
                wisps[i].sprite = atlas.GetSprite(t);
                wisps[i].color = new Color(.91f, .89f, .79f, Mathf.Sin(t * Mathf.PI) * .65f);
                wisps[i].transform.position = bandHomes[i * 2] + new Vector3(.41f * t, (i % 2 == 0 ? -.21f : .24f) + .15f * t, 0f);
            }
        }

        internal void Remove() => effect.Remove();
    }
}
