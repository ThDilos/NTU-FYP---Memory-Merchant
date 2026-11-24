using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Rebuilds the authored prologue data without changing scene or story assets.
// Existing .asset/.anim files are updated in place to retain their GUIDs.
public static class PrologueCutsceneSetup
{
    private const string Source = "Deliverables/Prologue-Layered-A-2026-11-18";
    private const string OriginalCharacterSource = "Deliverables/Prologue-Original-Characters-2026-11-19/Sprites";
    private const string OriginalPrefix = "OriginalCharacters/";
    private const string EdgeSource = "Deliverables/Prologue-Edge-Fix-2026-11-19/Textures";
    private const string EdgePrefix = "StageEdges/";
    private const string TextureRoot = "Assets/Marcus/Cutscenes/Prologue/Textures";
    private const string AnimationRoot = "Assets/Marcus/Cutscenes/Prologue/Animations";
    private const string LibraryPath = "Assets/Marcus/Cutscenes/Resources/Prologue/PrologueCutscenes.asset";

    private static readonly string[] SourceTextures =
    {
        "Backgrounds/courtyard-320x180.png",
        "Backgrounds/shop-exterior-320x180.png",
        "Backgrounds/shop-interior-320x180.png",
        "Backgrounds/key-closeup-paving-320x180.png",
        EdgePrefix + "courtyard-foreground-overscan-324x180.png",
        OriginalPrefix + "jade-original.png",
        OriginalPrefix + "jade-blink-original.png",
        OriginalPrefix + "jade-receive-original.png",
        OriginalPrefix + "aunt-original.png",
        OriginalPrefix + "aunt-blink-original.png",
        OriginalPrefix + "aunt-offer-original.png",
        EdgePrefix + "aunt-hand-right-170x180.png",
        EdgePrefix + "jade-hand-left-170x180.png",
        "Props/shop-key-32x32.png",
        "Props/memory-machine-320x180.png",
    };

    private static readonly Dictionary<string, Vector2Int> OriginalSizes = new Dictionary<string, Vector2Int>
    {
        { "jade-original.png", new Vector2Int(704, 1056) },
        { "jade-blink-original.png", new Vector2Int(704, 1056) },
        { "jade-receive-original.png", new Vector2Int(704, 1056) },
        { "aunt-original.png", new Vector2Int(672, 1008) },
        { "aunt-blink-original.png", new Vector2Int(672, 1008) },
        { "aunt-offer-original.png", new Vector2Int(672, 1008) },
    };

    [MenuItem("The Memory Shop/Cutscenes/Rebuild Prologue Assets")]
    public static void Build()
    {
        foreach (string relative in SourceTextures)
            if (!File.Exists(SourceFile(relative)))
                throw new FileNotFoundException("Missing prepared prologue texture", SourceFile(relative));

        Directory.CreateDirectory(ProjectFile(TextureRoot));
        Directory.CreateDirectory(ProjectFile(AnimationRoot));
        Directory.CreateDirectory(Path.GetDirectoryName(ProjectFile(LibraryPath)));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        foreach (string relative in SourceTextures)
            ImportTexture(relative);
        PrologueCutsceneWalkingSetup.PrepareTextures();

        AnimationClip photo = Clip("Photo", delegate(AnimationClip clip)
        {
            Curve(clip, "ShotRoot/Flash", typeof(RawImage), "m_Color.a",
                0f, 0f, .04f, .8f, .18f, 0f, .6f, 0f);
        });
        AnimationClip handover = HandoverClip();
        AnimationClip approach = Clip("ApproachShop", delegate(AnimationClip clip)
        {
            Position(clip, "Jade", "x", 0f, 98f, 3f, 188f);
        });
        AnimationClip unlock = Clip("UnlockShop", delegate(AnimationClip clip)
        {
            // A deliberate integer-scale close cut keeps the source pixels intact.
            CameraCut(clip, .12f, 1f, 0f, 0f, 5f, -1040f, 390f, 1.6f);
            Curve(clip, "ShotRoot/Key", typeof(RawImage), "m_Color.a", 0f, 0f, .12f, 1f, 1.6f, 1f);
            // Whole quarter-turn frames preserve the pixel grid. Compensate the
            // top-left pivot by16px so the 16x16 icon turns around its centre.
            StepCurve(clip, "ShotRoot/Key", typeof(Transform), "localEulerAnglesRaw.z",
                0f, 0f, .55f, 90f, 1.25f, 0f, 1.6f, 0f);
            StepCurve(clip, "ShotRoot/Key", typeof(RectTransform), "m_AnchoredPosition.y",
                0f, -89f, .55f, -105f, 1.25f, -89f, 1.6f, -89f);
        });
        AnimationClip reveal = Clip("RevealMachine", delegate(AnimationClip clip)
        {
            CameraCut(clip, .45f, 1f, 0f, 0f, 2f, -288f, 72f, 1.2f);
        });
        AnimationClip returnView = Clip("ReturnShopView", delegate(AnimationClip clip)
        {
            CameraCut(clip, .3f, 2f, -288f, 72f, 1f, 0f, 0f, .8f);
        });

        PrologueCutsceneLibrary library = AssetDatabase.LoadAssetAtPath<PrologueCutsceneLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<PrologueCutsceneLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }
        library.shots = new[]
        {
            Courtyard("courtyard", new Rect(0, 0, 320, 180), photo),
            Courtyard("conversation", new Rect(80, 68, 160, 90), photo),
            new PrologueShot
            {
                id = "handover", background = Texture("key-closeup-paving-320x180"),
                layers = new[]
                {
                    Solid("Shade", new Color(68f/255f, 43f/255f, 24f/255f, 52f/255f)),
                    Layer("AuntHand", "aunt-hand-right-170x180", 160, 0, 170, 180),
                    Layer("JadeHand", "jade-hand-left-170x180", -10, 0, 170, 180),
                    Layer("Key", "shop-key-32x32", 185, 106, 32, 32),
                },
                actions = new[] { Action("handover", handover, interaction: true) }
            },
            new PrologueShot
            {
                id = "exterior", background = Texture("shop-exterior-320x180"),
                layers = new[]
                {
                    PrologueCutsceneWalkingSetup.CreateExteriorJade(),
                    Layer("Key", "shop-key-32x32", 226, 89, 16, 16, new Color(1f, 1f, 1f, 0f)),
                },
                actions = new[]
                {
                    Action("approach", approach, "Jade"),
                    Action("unlock", unlock, interaction: true),
                }
            },
            new PrologueShot
            {
                id = "interior", background = Texture("shop-interior-320x180"),
                layers = new[]
                {
                    PrologueCutsceneWalkingSetup.CreateShopJade(),
                    Layer("Machine", "memory-machine-320x180", 0, 0, 320, 180),
                },
                actions = new[]
                {
                    Action("reveal", reveal),
                    Action("return", returnView),
                }
            },
        };
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        PrologueBackgroundPolishSetup.Apply();
        Debug.Log("Prepared five layered prologue shots and six editable animation clips at " + LibraryPath);
    }

    // Update only the three edge-touching layers and the handover's position
    // curves. Keep the handover on the same sides as the preceding conversation.
    [MenuItem("The Memory Shop/Cutscenes/Fix Stage Edge Gaps")]
    public static void ApplyEdgeOverscan()
    {
        PrologueCutsceneLibrary library = AssetDatabase.LoadAssetAtPath<PrologueCutsceneLibrary>(LibraryPath);
        if (library == null || library.shots == null)
            throw new InvalidOperationException("Build the prologue library before fixing its stage edges.");
        foreach (string relative in SourceTextures.Where(path => path.StartsWith(EdgePrefix, StringComparison.Ordinal)))
        {
            if (!File.Exists(SourceFile(relative)))
                throw new FileNotFoundException("Missing prepared edge-extruded prologue texture", SourceFile(relative));
            ImportTexture(relative);
        }
        AnimationClip handover = HandoverClip();
        foreach (PrologueShot shot in library.shots)
        {
            if (shot == null || shot.layers == null) continue;
            foreach (PrologueLayer layer in shot.layers)
            {
                if (layer == null) continue;
                if ((shot.id == "courtyard" || shot.id == "conversation") && layer.name == "Foliage")
                {
                    layer.texture = Texture("courtyard-foreground-overscan-324x180");
                    layer.position = new Vector2(-2, 0);
                    layer.size = new Vector2(324, 180);
                }
                else if (shot.id == "handover" && layer.name == "AuntHand")
                {
                    layer.texture = Texture("aunt-hand-right-170x180");
                    layer.position = new Vector2(160, 0);
                    layer.size = new Vector2(170, 180);
                }
                else if (shot.id == "handover" && layer.name == "JadeHand")
                {
                    layer.texture = Texture("jade-hand-left-170x180");
                    layer.position = new Vector2(-10, 0);
                    layer.size = new Vector2(170, 180);
                }
                else if (shot.id == "handover" && layer.name == "Key")
                {
                    layer.position = new Vector2(185, 106);
                }
            }
            if (shot.id == "handover" && shot.actions != null)
                foreach (PrologueAction action in shot.actions)
                    if (action != null && action.id == "handover") action.clip = handover;
        }
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Debug.Log("Applied foreground overscan and the right-to-left key handover.");
    }

    private static AnimationClip HandoverClip()
    {
        return Clip("KeyHandover", delegate(AnimationClip clip)
        {
            Position(clip, "Key", "x", 0f, 185f, .5f, 176f, 1.8f, 101f, 2.4f, 101f);
            Position(clip, "Key", "y", 0f, -106f, .6f, -94f, 1.7f, -94f, 2.1f, -106f, 2.4f, -106f);
            // The cuffs extend past the frame as both hands reach inward.
            Position(clip, "AuntHand", "x", 0f, 160f, .6f, 152f, 1.8f, 152f, 2.4f, 160f);
            Position(clip, "JadeHand", "x", 0f, -10f, .6f, -2f, 1.8f, -2f, 2.4f, -10f);
        });
    }

    // Refresh standing characters without rebuilding the shot layouts or clips.
    [MenuItem("The Memory Shop/Cutscenes/Use Original Character Resolution")]
    public static void UseOriginalCharacters()
    {
        PrologueCutsceneLibrary library = AssetDatabase.LoadAssetAtPath<PrologueCutsceneLibrary>(LibraryPath);
        if (library == null || library.shots == null)
            throw new InvalidOperationException("Build the prologue library before upgrading its character textures.");
        foreach (string name in OriginalSizes.Keys)
            if (!File.Exists(SourceFile(OriginalPrefix + name)))
                throw new FileNotFoundException("Missing original-resolution character texture", SourceFile(OriginalPrefix + name));

        Directory.CreateDirectory(ProjectFile(TextureRoot));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string name in OriginalSizes.Keys) ImportTexture(OriginalPrefix + name);

        int updated = 0;
        foreach (PrologueShot shot in library.shots)
        {
            if (shot == null || shot.layers == null) continue;
            foreach (PrologueLayer layer in shot.layers)
            {
                if (layer == null) continue;
                if ((shot.id == "courtyard" || shot.id == "conversation") && layer.name == "Jade")
                {
                    layer.texture = Texture("jade-original");
                    layer.blink = Texture("jade-blink-original");
                    layer.speakingPose = Texture("jade-receive-original");
                    updated++;
                }
                else if ((shot.id == "courtyard" || shot.id == "conversation") && layer.name == "Aunt")
                {
                    layer.texture = Texture("aunt-original");
                    layer.blink = Texture("aunt-blink-original");
                    layer.speakingPose = Texture("aunt-offer-original");
                    updated++;
                }
            }
        }
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Debug.Log("Upgraded " + updated + " prologue character layers to original-resolution textures; " +
                  "standing shot layouts, camera framing and animation clips were preserved.");
    }

    private static string SourceFile(string relative)
    {
        if (relative.StartsWith(EdgePrefix, StringComparison.Ordinal))
            return ProjectFile(EdgeSource + "/" + Path.GetFileName(relative));
        return ProjectFile(relative.StartsWith(OriginalPrefix, StringComparison.Ordinal)
            ? OriginalCharacterSource + "/" + Path.GetFileName(relative)
            : Source + "/" + relative);
    }

    private static void ImportTexture(string relative)
    {
        string input = SourceFile(relative);
        string name = Path.GetFileName(relative);
        string output = TextureRoot + "/" + name;
        string outputFile = ProjectFile(output);
        if (!File.Exists(outputFile) || !File.ReadAllBytes(input).SequenceEqual(File.ReadAllBytes(outputFile)))
            File.Copy(input, outputFile, true);
        AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ConfigureTexture(output);
        if (relative.StartsWith(OriginalPrefix, StringComparison.Ordinal))
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(output);
            Vector2Int expected = OriginalSizes[name];
            if (texture == null || texture.width != expected.x || texture.height != expected.y)
                throw new InvalidOperationException("Original character texture was resized on import: " + output +
                    ". Expected " + expected.x + "x" + expected.y + ".");
        }
    }

    private static string ProjectFile(string relative)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
    }

    private static void ConfigureTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Texture importer missing: " + path);
        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.anisoLevel = 0;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = false;
        importer.maxTextureSize = 2048;
        importer.isReadable = false;
        importer.SaveAndReimport();
    }

    private static Texture2D Texture(string name)
    {
        string path = TextureRoot + "/" + name + ".png";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null) throw new InvalidOperationException("Imported prologue texture missing: " + path);
        return texture;
    }

    private static PrologueLayer Layer(string name, string texture, float x, float y, float width, float height,
        Color? color = null)
    {
        return new PrologueLayer
        {
            name = name, texture = Texture(texture), position = new Vector2(x, y),
            size = new Vector2(width, height), color = color ?? Color.white,
        };
    }

    private static PrologueLayer Solid(string name, Color color)
    {
        return new PrologueLayer { name = name, size = new Vector2(320, 180), color = color };
    }

    private static PrologueShot Courtyard(string id, Rect crop, AnimationClip photo)
    {
        PrologueLayer jade = Layer("Jade", "jade-original", 98, 58, 64, 96);
        jade.blink = Texture("jade-blink-original");
        jade.speakingPose = Texture("jade-receive-original");
        PrologueLayer aunt = Layer("Aunt", "aunt-original", 146, 58, 64, 96);
        aunt.blink = Texture("aunt-blink-original");
        aunt.speakingPose = Texture("aunt-offer-original");
        PrologueLayer foliage = Layer("Foliage", "courtyard-foreground-overscan-324x180", -2, 0, 324, 180);
        foliage.sway = true;
        return new PrologueShot
        {
            id = id, cameraRect = crop, background = Texture("courtyard-320x180"),
            layers = new[] { jade, aunt, foliage, Solid("Flash", new Color(1f, 1f, 1f, 0f)) },
            actions = new[] { Action("photo", photo, interaction: true) }
        };
    }

    private static PrologueAction Action(string id, AnimationClip clip, string walkingLayer = null, bool interaction = false)
    {
        return new PrologueAction { id = id, clip = clip, walkingLayer = walkingLayer, interactionCue = interaction };
    }

    private static AnimationClip Clip(string name, System.Action<AnimationClip> author)
    {
        string path = AnimationRoot + "/" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool create = clip == null;
        if (create) clip = new AnimationClip();
        clip.name = name;
        clip.ClearCurves();
        clip.legacy = true;
        clip.frameRate = 8f;
        clip.wrapMode = WrapMode.ClampForever;
        author(clip);
        if (create) AssetDatabase.CreateAsset(clip, path);
        else EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void Position(AnimationClip clip, string layer, string axis, params float[] points)
    {
        Curve(clip, "ShotRoot/" + layer, typeof(RectTransform), "m_AnchoredPosition." + axis, points);
    }

    private static void Curve(AnimationClip clip, string path, Type type, string property, params float[] points)
    {
        if (points.Length < 4 || points.Length % 2 != 0) throw new ArgumentException("Expected time/value pairs");
        var keys = new Keyframe[points.Length / 2];
        for (int i = 0; i < keys.Length; i++)
        {
            float time = points[i * 2], value = points[i * 2 + 1];
            float incoming = i == 0 ? 0f : (value - points[(i - 1) * 2 + 1]) / (time - points[(i - 1) * 2]);
            float outgoing = i == keys.Length - 1 ? 0f : (points[(i + 1) * 2 + 1] - value) / (points[(i + 1) * 2] - time);
            keys[i] = new Keyframe(time, value, incoming, outgoing);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, property), new AnimationCurve(keys));
    }

    private static void StepCurve(AnimationClip clip, string path, Type type, string property, params float[] points)
    {
        var curve = new AnimationCurve();
        for (int i = 0; i < points.Length; i += 2)
            curve.AddKey(new Keyframe(points[i], points[i + 1], float.PositiveInfinity, float.PositiveInfinity));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, property), curve);
    }

    private static void CameraCut(AnimationClip clip, float cutAt, float fromScale, float fromX, float fromY,
        float toScale, float toX, float toY, float end)
    {
        StepCurve(clip, "ShotRoot", typeof(Transform), "m_LocalScale.x", 0, fromScale, cutAt, toScale, end, toScale);
        StepCurve(clip, "ShotRoot", typeof(Transform), "m_LocalScale.y", 0, fromScale, cutAt, toScale, end, toScale);
        StepCurve(clip, "ShotRoot", typeof(RectTransform), "m_AnchoredPosition.x", 0, fromX, cutAt, toX, end, toX);
        StepCurve(clip, "ShotRoot", typeof(RectTransform), "m_AnchoredPosition.y", 0, fromY, cutAt, toY, end, toY);
    }
}
