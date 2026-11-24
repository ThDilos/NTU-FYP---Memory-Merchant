using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Story actors have their own proportions. Never modify the Memory World sheet.
public static class PrologueCutsceneWalkingSetup
{
    public const string SheetPath = "Assets/Marcus/Cutscenes/Prologue/Textures/jade-walk-cutscene-two-frame.png";
    public const string IdlePath = "Assets/Marcus/Cutscenes/Prologue/Textures/jade-cutscene-idle.png";
    private const string Source = "Deliverables/Prologue-Walking-2026-11-19/Sprites/";
    private const string Library = "Assets/Marcus/Cutscenes/Resources/Prologue/PrologueCutscenes.asset";
    private const string ShopSpriteSheet = "Assets/Marcus/Environment/FYP memory merchant-Sheet.png";
    private const long ShopSpriteId = -6728740883493928157L;

    [MenuItem("The Memory Shop/Cutscenes/Use Cutscene Walking Sprites")]
    public static void Apply()
    {
        PrologueCutsceneLibrary library = AssetDatabase.LoadAssetAtPath<PrologueCutsceneLibrary>(Library);
        if (library == null) throw new InvalidOperationException("Build the prologue shot library first.");
        PrepareTextures();
        PrologueShot exterior = library.Find("exterior");
        PrologueShot interior = library.Find("interior");
        if (exterior == null || interior == null) throw new InvalidOperationException("The prologue shop shots are missing.");
        PrologueLayer exteriorJade = CreateExteriorJade();
        PrologueLayer shopJade = CreateShopJade();
        ReplaceJade(exterior, exteriorJade);
        ReplaceJade(interior, shopJade);
        interior.actions = interior.actions.Where(a => a.id != "enter").ToArray();
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Debug.Log($"CUTSCENE_WALK_READY: two exterior frames at {exteriorJade.framesPerSecond} frames per second, with the existing shop sprite indoors.");
    }

    internal static void PrepareTextures()
    {
        Import(SheetPath, 1408, 1056);
        Import(IdlePath, 704, 1056);
    }

    internal static PrologueLayer CreateExteriorJade()
    {
        return new PrologueLayer
        {
            name = "Jade",
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath),
            idleTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(IdlePath),
            position = new Vector2(98, 58),
            size = new Vector2(64, 96),
            frameCount = 2,
            framesPerSecond = 3f
        };
    }

    internal static PrologueLayer CreateShopJade()
    {
        Sprite sprite = AssetDatabase.LoadAllAssetRepresentationsAtPath(ShopSpriteSheet).OfType<Sprite>()
            .FirstOrDefault(s => AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out string _, out long id) && id == ShopSpriteId);
        if (sprite == null) throw new InvalidOperationException("The shop's Jade sprite is missing from " + ShopSpriteSheet);
        return new PrologueLayer
        {
            name = "Jade",
            sprite = sprite,
            position = new Vector2(147, 59),
            size = sprite.rect.size
        };
    }

    private static void ReplaceJade(PrologueShot shot, PrologueLayer replacement)
    {
        int originalIndex = Array.FindIndex(shot.layers, layer => layer.name == "Jade");
        if (originalIndex < 0) throw new InvalidOperationException("The shot is missing Jade: " + shot.id);
        if (shot.id == "interior")
        {
            // The gameplay sprite sits behind the machine layer.
            shot.layers = new[] { replacement }.Concat(shot.layers.Where(layer => layer.name != "Jade")).ToArray();
        }
        else
        {
            replacement.position = shot.layers[originalIndex].position;
            shot.layers[originalIndex] = replacement;
        }
    }

    private static void Import(string destination, int width, int height)
    {
        string source = Source + Path.GetFileName(destination);
        if (!File.Exists(source)) throw new FileNotFoundException("Missing prepared cutscene sprite", source);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
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
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = false;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
        Texture2D image = AssetDatabase.LoadAssetAtPath<Texture2D>(destination);
        if (image == null || image.width != width || image.height != height)
            throw new InvalidOperationException("Cutscene walking sprite was resized on import: " + destination);
    }
}
