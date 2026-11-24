using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Replace the graduation art without rebuilding characters, motion or framing.
// Background people are separate, stationary layers behind the story actors.
public static class PrologueBackgroundPolishSetup
{
    public const string LibraryPath = "Assets/Marcus/Cutscenes/Resources/Prologue/PrologueCutscenes.asset";
    public const string TextureRoot = "Assets/Marcus/Cutscenes/Prologue/Textures";
    private const string SourceRoot = "Deliverables/Cutscene-Backgrounds-2026-11-19/Textures";
    private static readonly string[] Names = {
        "courtyard-clean-320x180", "key-closeup-clean-320x180", "crowd-left", "crowd-right"
    };

    [MenuItem("The Memory Shop/Cutscenes/Apply Clean Graduation Backgrounds")]
    public static void Apply()
    {
        PrologueCutsceneLibrary library = AssetDatabase.LoadAssetAtPath<PrologueCutsceneLibrary>(LibraryPath);
        if (library == null || library.shots == null)
            throw new InvalidOperationException("Build the prologue shot library before applying its background polish.");
        foreach (string id in new[] { "courtyard", "conversation", "handover" })
        {
            PrologueShot shot = library.Find(id);
            if (shot == null || shot.layers == null)
                throw new InvalidOperationException("The prologue is missing its authored shot: " + id);
        }

        // Validate every source before changing any imported texture or shot.
        var dimensions = new Dictionary<string, Vector2Int>();
        foreach (string name in Names)
        {
            string source = SourceRoot + "/" + name + ".png";
            if (!File.Exists(source)) throw new FileNotFoundException("Missing prepared prologue background", source);
            Texture2D decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!decoded.LoadImage(File.ReadAllBytes(source)))
                    throw new InvalidOperationException("Could not read prepared PNG: " + source);
                Vector2Int size = new Vector2Int(decoded.width, decoded.height);
                if (size.x > 2048 || size.y > 2048)
                    throw new InvalidOperationException("Prepare this texture at no more than 2048 pixels per side: " + source);
                if (name.EndsWith("-320x180", StringComparison.Ordinal) && size != new Vector2Int(320, 180))
                    throw new InvalidOperationException("Prologue backgrounds must retain their authored 320x180 stage: " + source);
                dimensions.Add(name, size);
            }
            finally { UnityEngine.Object.DestroyImmediate(decoded); }
        }
        foreach (string name in Names) Import(name, dimensions[name]);

        Texture2D courtyard = Texture("courtyard-clean-320x180");
        foreach (string id in new[] { "courtyard", "conversation" })
        {
            PrologueShot shot = library.Find(id);
            shot.background = courtyard;
            // Reapplying the setup replaces these two layers instead of adding
            // duplicates. Existing actor/foreground references remain intact.
            PrologueLayer[] existing = shot.layers.Where(layer => layer == null ||
                (layer.name != "CrowdLeft" && layer.name != "CrowdRight")).ToArray();
            shot.layers = new[] {
                Crowd("CrowdLeft", "crowd-left", new Vector2(35, 58)),
                Crowd("CrowdRight", "crowd-right", new Vector2(248, 56))
            }.Concat(existing).ToArray();
        }
        library.Find("handover").background = Texture("key-closeup-clean-320x180");
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Debug.Log("PROLOGUE_BACKGROUNDS_READY: clean courtyard and matching paving, with separate background families; authored actors, framing, walking and clips preserved.");
    }

    private static PrologueLayer Crowd(string id, string texture, Vector2 position)
    {
        return new PrologueLayer {
            name = id, characterId = string.Empty, texture = Texture(texture),
            position = position, size = new Vector2(id == "CrowdLeft" ? 44 : 48, 48),
            color = Color.white, frameCount = 1, sway = false
        };
    }

    private static Texture2D Texture(string name)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + "/" + name + ".png");
        if (texture == null) throw new InvalidOperationException("Imported prologue background is missing: " + name);
        return texture;
    }

    private static void Import(string name, Vector2Int expected)
    {
        string source = SourceRoot + "/" + name + ".png";
        string destination = TextureRoot + "/" + name + ".png";
        Directory.CreateDirectory(TextureRoot);
        if (!File.Exists(destination) || !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(destination)))
            File.Copy(source, destination, true);
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(destination);
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.maxTextureSize = 2048;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = false;
        importer.SaveAndReimport();
        Texture2D image = AssetDatabase.LoadAssetAtPath<Texture2D>(destination);
        if (image == null || image.width != expected.x || image.height != expected.y)
            throw new InvalidOperationException("Prepared prologue texture was resized during import: " + destination);
    }
}
