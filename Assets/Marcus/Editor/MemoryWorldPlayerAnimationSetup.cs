using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MemoryWorldPlayerAnimationSetup
{
    public const string Folder = "Assets/Marcus/Animations/MemoryWorldPlayer";
    public const string ControllerPath = Folder + "/MemoryWorldPlayer.controller";
    public const string ScenePath = "Assets/Scenes/Tutorial_MemoryWorld_Marcus.unity";
    private const float FramesPerSecond = 8f;

    [MenuItem("Tools/Memory Merchant/Rebuild Memory World Player Animations")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before rebuilding animations.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildAssets();
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        ConnectPlayer(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Debug.Log("Memory World player animations rebuilt: 4 walk loops, 4 facing idle poses, 8-state controller.");
    }

    public static void BuildAssets()
    {
        Sprite[] down = Slice("WalkDown", "Jade_Down");
        Sprite[] up = Slice("WalkUp", "Jade_Up");
        Sprite[] side = Slice("WalkSide", "Jade_Side");
        var directions = new[] { down, up, side, side };
        string[] names = { "Down", "Up", "Right", "Left" };
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.parameters = Array.Empty<AnimatorControllerParameter>();
        controller.AddParameter("Facing", AnimatorControllerParameterType.Int);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("PlaybackSpeed", AnimatorControllerParameterType.Float);
        var parameters = controller.parameters; parameters[2].defaultFloat = 1f; controller.parameters = parameters;
        var machine = controller.layers[0].stateMachine;
        foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
        foreach (var state in machine.states) machine.RemoveState(state.state);
        for (int direction = 0; direction < names.Length; direction++)
            for (int moving = 0; moving < 2; moving++)
            {
                string name = (moving == 1 ? "Walk" : "Idle") + names[direction];
                AnimationClip clip = MakeClip(name, directions[direction], direction == 3, moving == 1);
                var state = machine.AddState(name, new Vector3(moving * 270, direction * 85));
                state.motion = clip; state.writeDefaultValues = false;
                if (moving == 1) { state.speedParameter = "PlaybackSpeed"; state.speedParameterActive = true; }
                if (direction == 0 && moving == 0) machine.defaultState = state;
                var transition = machine.AddAnyStateTransition(state);
                transition.hasExitTime = false; transition.duration = 0; transition.canTransitionToSelf = false;
                transition.AddCondition(moving == 1 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "IsMoving");
                transition.AddCondition(AnimatorConditionMode.Equals, direction, "Facing");
            }
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }

    private static Sprite[] Slice(string file, string prefix)
    {
        string path = Folder + "/" + file + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing sprite sheet: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 1000; // The exported sheets are 10x the original 32px artwork.
        importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048; importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var existing = provider.GetSpriteRects();
        var rects = new SpriteRect[4];
        for (int i = 0; i < rects.Length; i++)
        {
            string name = prefix + "_" + i;
            rects[i] = new SpriteRect
            {
                name = name, rect = new Rect(i * 320, 0, 320, 320),
                alignment = SpriteAlignment.Center, pivot = new Vector2(.5f, .5f),
                spriteID = existing.FirstOrDefault(r => r.name == name)?.spriteID ?? GUID.Generate()
            };
        }
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (sprites.Length != 4) throw new InvalidOperationException("Expected four frames: " + path);
        return sprites;
    }

    private static AnimationClip MakeClip(string name, Sprite[] frames, bool mirror, bool moving)
    {
        string path = Folder + "/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.ClearCurves(); clip.frameRate = FramesPerSecond;
        int count = moving ? frames.Length : 1;
        var keys = new ObjectReferenceKeyframe[count + 1];
        for (int i = 0; i < count; i++) keys[i] = new ObjectReferenceKeyframe { time = i / FramesPerSecond, value = frames[i] };
        keys[count] = new ObjectReferenceKeyframe { time = count / FramesPerSecond, value = frames[0] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_FlipX"), AnimationCurve.Constant(0, count / FramesPerSecond, mirror ? 1 : 0));
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    public static void ConnectPlayer(Scene scene)
    {
        var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerController>(true)).Single();
        var sprite = player.GetComponentInChildren<SpriteRenderer>(true);
        var animator = sprite.GetComponent<Animator>();
        if (animator == null) animator = sprite.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        sprite.sprite = AssetDatabase.LoadAllAssetsAtPath(Folder + "/WalkDown.png").OfType<Sprite>().OrderBy(s => s.name).First();
        var driver = player.GetComponent<PlayerSpriteAnimation>();
        if (driver == null) driver = player.gameObject.AddComponent<PlayerSpriteAnimation>();
        var serialized = new SerializedObject(driver);
        serialized.FindProperty("player").objectReferenceValue = player;
        serialized.FindProperty("animator").objectReferenceValue = animator;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(sprite);
        EditorSceneManager.MarkSceneDirty(scene);
    }
}
