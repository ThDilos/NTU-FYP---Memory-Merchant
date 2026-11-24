using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// This optional pack adds assets beside the existing prologue. It never rebuilds
// the shot library, changes its layout, or inserts commands into the story.
public static class PrologueCharacterStateSetup
{
    public const string LibraryPath = "Assets/Marcus/Cutscenes/Resources/Prologue/CharacterStates.asset";
    public const string TextureRoot = "Assets/Marcus/Cutscenes/CharacterActions/Textures";
    private const string ExistingRoot = "Assets/Marcus/Cutscenes/Prologue/Textures";
    private const string ClipRoot = "Assets/Marcus/Cutscenes/CharacterActions/Animations";

    private static readonly string[] NewTextures = {
        "jade-thinking", "jade-surprised", "jade-inspect", "jade-talk-0", "jade-talk-1",
        "aunt-point", "aunt-reassure", "aunt-explain", "aunt-talk-0", "aunt-talk-1"
    };

    [MenuItem("The Memory Shop/Cutscenes/Build Character Action Pack")]
    public static void Build()
    {
        foreach (string name in NewTextures)
            if (!File.Exists(TextureRoot + "/" + name + ".png"))
                throw new FileNotFoundException("Missing prepared character action texture", TextureRoot + "/" + name + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));
        Directory.CreateDirectory(ClipRoot);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string name in NewTextures) ConfigureTexture(TextureRoot + "/" + name + ".png");

        var states = new List<PrologueCharacterState> {
            State("Jade", "idle", Old("jade-original")),
            State("Jade", "talk", New("jade-talk-0"), New("jade-talk-1")),
            State("Jade", "thinking", New("jade-thinking")),
            State("Jade", "surprised", New("jade-surprised")),
            State("Jade", "inspect", New("jade-inspect")),
            State("Jade", "receive", Old("jade-receive-original")),
            State("Jade", "reach", Old("jade-receive-original")),
            State("Aunt", "idle", Old("aunt-original")),
            State("Aunt", "talk", New("aunt-talk-0"), New("aunt-talk-1")),
            State("Aunt", "point", New("aunt-point")),
            State("Aunt", "reassure", New("aunt-reassure")),
            State("Aunt", "explain", New("aunt-explain")),
            State("Aunt", "offer", Old("aunt-offer-original")),
            State("Aunt", "reach", Old("aunt-offer-original")),
        };
        var gestures = new[] {
            Gesture("nod", "Nod", "", new Vector2(0,0),new Vector2(0,-1),new Vector2(0,-1),new Vector2(0,0),new Vector2(0,0),new Vector2(0,-1),new Vector2(0,0)),
            Gesture("headshake", "HeadShake", "", new Vector2(0,0),new Vector2(-1,0),new Vector2(0,0),new Vector2(1,0),new Vector2(0,0),new Vector2(-1,0),new Vector2(1,0),new Vector2(0,0)),
            Gesture("hop", "SmallHop", "", new Vector2(0,0),new Vector2(0,1),new Vector2(0,3),new Vector2(0,4),new Vector2(0,3),new Vector2(0,1),new Vector2(0,0),new Vector2(0,0)),
            Gesture("reach", "Reach", "reach", new Vector2(0,0),new Vector2(1,0),new Vector2(2,0),new Vector2(2,0),new Vector2(1,0),new Vector2(0,0)),
            Gesture("listen", "Listen", "idle", new Vector2(0,0),new Vector2(0,0),new Vector2(0,-1),new Vector2(0,-1),new Vector2(0,0),new Vector2(0,0),new Vector2(0,0),new Vector2(0,0))
        };
        PrologueCharacterStates library = AssetDatabase.LoadAssetAtPath<PrologueCharacterStates>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<PrologueCharacterStates>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }
        library.states = states.ToArray();
        library.gestures = gestures;
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Prepared " + states.Count + " reusable character states and " + gestures.Length +
            " pixel-aligned gestures. Existing prologue shots, story and walking controller were preserved.");
    }

    private static PrologueCharacterState State(string character, string id, params Texture2D[] frames)
    {
        if (frames.Any(frame => frame == null)) throw new InvalidOperationException("A pose texture is missing: " + character + "/" + id);
        Vector2Int expected = character == "Jade" ? new Vector2Int(704,1056) : new Vector2Int(672,1008);
        if (frames.Any(frame => frame.width != expected.x || frame.height != expected.y))
            throw new InvalidOperationException(character + "/" + id + " must preserve its " + expected + " original-resolution tile.");
        return new PrologueCharacterState { character=character, id=id, frames=frames, framesPerSecond=6f, loop=frames.Length>1 };
    }
    private static Texture2D Old(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(ExistingRoot+"/"+name+".png");
    private static Texture2D New(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+"/"+name+".png");

    private static PrologueCharacterGesture Gesture(string id, string clipName, string pose, params Vector2[] offsets)
    {
        var gesture = new PrologueCharacterGesture { id=id, pose=pose, offsets=offsets, framesPerSecond=8f };
        string path=ClipRoot+"/"+clipName+".anim";
        AnimationClip clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(clip==null) {clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
        clip.ClearCurves();clip.name=clipName;clip.legacy=true;clip.frameRate=gesture.framesPerSecond;clip.wrapMode=WrapMode.Once;
        for(int axis=0;axis<2;axis++)
        {
            var curve=new AnimationCurve();
            for(int i=0;i<offsets.Length;i++) curve.AddKey(new Keyframe(i/gesture.framesPerSecond,offsets[i][axis],float.PositiveInfinity,float.PositiveInfinity));
            curve.AddKey(new Keyframe(gesture.Duration,0f,float.PositiveInfinity,float.PositiveInfinity));
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(RectTransform),"m_AnchoredPosition."+(axis==0?"x":"y")),curve);
        }
        EditorUtility.SetDirty(clip);gesture.clip=clip;return gesture;
    }

    private static void ConfigureTexture(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null) throw new InvalidOperationException("Texture importer missing: "+path);
        importer.textureType=TextureImporterType.Default;
        importer.filterMode=FilterMode.Point;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.mipmapEnabled=false;
        importer.anisoLevel=0;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.crunchedCompression=false;
        importer.maxTextureSize=2048;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=false;
        importer.SaveAndReimport();
    }
}

// Preview poses and gestures without editing the scene or starting a New Game.
public sealed class PrologueCharacterStatePreview : EditorWindow
{
    private PrologueCharacterStates library;
    private int characterIndex, stateIndex, gestureIndex;
    private bool playing=true;
    private double started;
    private readonly string[] characters={"Jade","Aunt"};

    [MenuItem("The Memory Shop/Cutscenes/Preview Character Actions")]
    public static void Open() => GetWindow<PrologueCharacterStatePreview>("Character Actions");
    private void OnEnable() {minSize=new Vector2(320,440);started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}
    private void OnDisable() => EditorApplication.update-=Tick;
    private void Tick() {if(playing) Repaint();}

    private void OnGUI()
    {
        if(library==null) library=AssetDatabase.LoadAssetAtPath<PrologueCharacterStates>(PrologueCharacterStateSetup.LibraryPath);
        EditorGUILayout.LabelField("Prologue Character Actions",EditorStyles.boldLabel);
        if(library==null) {EditorGUILayout.HelpBox("Build the Character Action Pack after adding its prepared textures.",MessageType.Info);return;}
        EditorGUI.BeginChangeCheck();
        characterIndex=EditorGUILayout.Popup("Character",characterIndex,characters);
        PrologueCharacterState[] states=library.states.Where(s=>s.character==characters[characterIndex] && s.id!="reach").ToArray();
        stateIndex=Mathf.Clamp(stateIndex,0,states.Length-1);
        stateIndex=EditorGUILayout.Popup("Pose",stateIndex,states.Select(s=>s.id).ToArray());
        string[] gestures=new[]{"None"}.Concat(library.gestures.Select(g=>g.id)).ToArray();
        gestureIndex=EditorGUILayout.Popup("Gesture",Mathf.Clamp(gestureIndex,0,gestures.Length-1),gestures);
        playing=EditorGUILayout.Toggle("Play preview",playing);
        if(EditorGUI.EndChangeCheck()) started=EditorApplication.timeSinceStartup;
        if(GUILayout.Button("Restart preview")) started=EditorApplication.timeSinceStartup;
        float elapsed=playing?(float)(EditorApplication.timeSinceStartup-started):0f;
        PrologueCharacterState pose=states[stateIndex];
        PrologueCharacterGesture gesture=gestureIndex>0?library.gestures[gestureIndex-1]:null;
        float gestureTime=gesture!=null?elapsed%Mathf.Max(.001f,gesture.Duration+1f):0f;
        bool gesturing=gesture!=null && gestureTime<gesture.Duration;
        if(gesturing && !string.IsNullOrWhiteSpace(gesture.pose)) pose=library.Find(characters[characterIndex],gesture.pose)??pose;
        Vector2 offset=gesturing?gesture.OffsetAt(gestureTime):Vector2.zero;
        Texture2D texture=pose.FrameAt(elapsed);
        Rect preview=GUILayoutUtility.GetRect(280,300,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(preview,new Color(.17f,.14f,.11f));
        float scale=Mathf.Min((preview.width-32)/64f,(preview.height-32)/96f);
        Rect actor=new Rect(preview.center.x-32f*scale+offset.x*scale,preview.center.y-48f*scale-offset.y*scale,64f*scale,96f*scale);
        if(texture!=null) GUI.DrawTexture(actor,texture,ScaleMode.StretchToFill,true);
        EditorGUILayout.HelpBox("Preview only. Poses hold until changed; gestures return to the previous pose. Existing story placement is unchanged.",MessageType.None);
        EditorGUILayout.SelectableLabel("<<prologuePose "+characters[characterIndex]+" "+states[stateIndex].id+">>\n"+
            (gesture!=null?"<<prologueGesture "+characters[characterIndex]+" "+gesture.id+">>":"<<prologuePose "+characters[characterIndex]+" idle>>"),GUILayout.Height(38));
    }
}
