using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yarn.Unity;

// The dialogue owns progression; this stage owns pictures and finite actions.
// Idle animation continues while reading and pauses with the History overlay.
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed class PrologueCutsceneController : DialoguePresenterBase
{
    private sealed class Layer
    {
        public PrologueLayer definition;
        public RawImage image;
        public Vector2 home;
        public PrologueCharacterState state;
        public float stateElapsed;
        public int poseVersion;
        public int gestureVersion;
        public bool gesturing;
        public Vector2 gestureOrigin;
    }

    private sealed class RectState
    {
        public RectTransform rect;
        private Transform parent;
        private int sibling;
        private Vector2 min, max, pivot, position, size;
        private Vector3 scale;
        private Quaternion rotation;
        public RectState(RectTransform value)
        {
            rect = value; parent = value.parent; sibling = value.GetSiblingIndex();
            min = value.anchorMin; max = value.anchorMax; pivot = value.pivot;
            position = value.anchoredPosition; size = value.sizeDelta;
            scale = value.localScale; rotation = value.localRotation;
        }
        public void Restore()
        {
            if (rect == null || parent == null) return;
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            rect.localScale = scale; rect.localRotation = rotation;
            rect.SetSiblingIndex(sibling);
        }
    }

    private readonly Dictionary<string, Layer> layers = new Dictionary<string, Layer>();
    private readonly Dictionary<Behaviour, bool> suspended = new Dictionary<Behaviour, bool>();
    private readonly Dictionary<TutorialUIController, bool> uiLocks = new Dictionary<TutorialUIController, bool>();
    private readonly GameplayInputPause gameplayPause = new GameplayInputPause();
    private DialogueRunner runner;
    private PrologueCutsceneLibrary library;
    private PrologueCharacterStates characterStates;
    private PrologueAmbientEffects ambience;
    private GameObject stage;
    private RectTransform viewport;
    private RectTransform shotRoot;
    private Animation motion;
    private PrologueShot shot;
    private PrologueAction action;
    private Button skip;
    private float actionElapsed;
    private float ambientElapsed;
    private int lastFootstep;
    private bool configured;
    private bool finishing;
    private bool skipping;
    private bool lineReady;
    private OptionsPresenter optionsPresenter;
    private bool optionsRunning;
    private int stageOpenedFrame;
    private LinePresenter caption;
    private readonly List<RectState> headerControls = new List<RectState>();

    public bool IsPlaying { get; private set; }
    public bool IsAnimating { get; private set; }
    public string CurrentShot => shot != null ? shot.id : "";
    public int CompletedActions { get; private set; }

    public void Configure(DialogueRunner dialogueRunner)
    {
        if (configured || dialogueRunner == null) return;
        runner = dialogueRunner;
        library = Resources.Load<PrologueCutsceneLibrary>("Prologue/PrologueCutscenes");
        characterStates = Resources.Load<PrologueCharacterStates>("Prologue/CharacterStates");
        runner.AddCommandHandler("prologueShot", new Func<string, IEnumerator>(ShowShot));
        runner.AddCommandHandler("prologueAct", new Func<string, IEnumerator>(PlayAction));
        runner.AddCommandHandler("prologuePose", new Action<string, string>(SetCharacterPose));
        runner.AddCommandHandler("prologueGesture", new Func<string, string, IEnumerator>(PlayCharacterGesture));
        runner.AddCommandHandler("prologueEnd", new Action(EndPrologue));
        runner.onDialogueComplete.AddListener(EndPrologue);
        var presenters = new List<DialoguePresenterBase>(runner.DialoguePresenters);
        // Forward the existing options UI through this presenter so completion
        // can wait for its cancellation/fade task before Yarn tears down state.
        optionsPresenter = runner.GetComponentInChildren<OptionsPresenter>(true);
        if (optionsPresenter != null) presenters.Remove(optionsPresenter);
        if (!presenters.Contains(this))
        {
            presenters.Add(this);
            runner.DialoguePresenters = presenters;
        }
        configured = true;
        // Cover the shop before its first rendered frame while Yarn starts up.
        if (runner.autoStart && !TutorialRewardBridge.TutorialCompleted && library != null)
            StartCoroutine(ShowShot("courtyard"));
    }

    public override YarnTask OnDialogueStartedAsync() => optionsPresenter != null
        ? optionsPresenter.OnDialogueStartedAsync() : YarnTask.CompletedTask;
    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        lineReady = true;
        return optionsPresenter != null ? optionsPresenter.RunLineAsync(line, token) : YarnTask.CompletedTask;
    }

    public override async YarnTask<DialogueOption> RunOptionsAsync(DialogueOption[] options, LineCancellationToken token)
    {
        if (optionsPresenter == null) return null;
        // A finite action may lead directly to choices. Choices are also a safe
        // command boundary for Skip; their cancellation task is drained below.
        lineReady = true;
        optionsRunning = true;
        try { return await optionsPresenter.RunOptionsAsync(options, token); }
        finally { optionsRunning = false; }
    }

    public override async YarnTask OnDialogueCompleteAsync()
    {
        // This installed Yarn runner clears its cancellation source as soon as
        // presenters finish. Drain the existing choice UI first, including the
        // dispatch continuation, so it cannot select an option on a stopped VM.
        while (optionsRunning) await YarnTask.Yield();
        await YarnTask.Yield();
        if (optionsPresenter != null) await optionsPresenter.OnDialogueCompleteAsync();
    }

    private bool BeginStage()
    {
        if (IsPlaying) return true;
        if (library == null)
        {
            Debug.LogError("The prologue cutscene library is missing. Run The Memory Shop > Cutscenes > Rebuild Prologue Assets.", this);
            return false;
        }
        if (stage == null) BuildStage();
        CaptureShop();
        caption = runner.GetComponentInChildren<LinePresenter>(true);
        IsPlaying = true;
        skipping = false;
        stageOpenedFrame = Time.frameCount;
        ambientElapsed = 0f;
        CompletedActions = 0;
        stage.SetActive(true);
        FitStage();
        PositionHeaderControls();
        return true;
    }

    private IEnumerator ShowShot(string id)
    {
        PrologueShot next = library != null ? library.Find(id) : null;
        if (next == null)
        {
            Debug.LogError("Unknown prologue shot: " + id, this);
            yield break;
        }
        if (!BeginStage() || skipping) yield break;
        lineReady = false;
        IsAnimating = false;
        action = null;
        if (motion != null) motion.Stop();
        ResetCharacterStates();
        ambience?.Dispose();
        ambience = null;
        if (shotRoot != null)
        {
            shotRoot.gameObject.SetActive(false);
            Destroy(shotRoot.gameObject);
        }
        layers.Clear();
        shot = next;
        shotRoot = Rect("ShotRoot", viewport);
        TopLeft(shotRoot, Vector2.zero, new Vector2(320, 180));
        float scale = 320f / Mathf.Max(1f, shot.cameraRect.width);
        shotRoot.localScale = new Vector3(scale, scale, 1f);
        shotRoot.anchoredPosition = new Vector2(-shot.cameraRect.x * scale, shot.cameraRect.y * scale);
        RawImage background = Picture("Background", shotRoot, shot.background, Color.white);
        TopLeft(background.rectTransform, Vector2.zero, new Vector2(320, 180));
        foreach (PrologueLayer definition in shot.layers)
        {
            Texture2D texture = definition.idleTexture != null ? definition.idleTexture : definition.texture;
            if (definition.sprite != null) texture = definition.sprite.texture;
            RawImage image = Picture(definition.name, shotRoot, texture, definition.color);
            TopLeft(image.rectTransform, definition.position, definition.size);
            if (definition.sprite != null)
            {
                Rect source = definition.sprite.rect;
                image.uvRect = new Rect(source.x / texture.width, source.y / texture.height,
                    source.width / texture.width, source.height / texture.height);
            }
            else if (definition.frameCount > 1 && definition.idleTexture == null)
                image.uvRect = new Rect(0, 0, 1f / definition.frameCount, 1);
            layers.Add(definition.name, new Layer { definition = definition, image = image, home = image.rectTransform.anchoredPosition });
        }
        ambience = new PrologueAmbientEffects(shotRoot, shot.id, FindLayerImage);
        ambience.Sample(ambientElapsed);
        if (motion == null)
        {
            motion = viewport.gameObject.AddComponent<Animation>();
            motion.playAutomatically = false;
            motion.cullingType = AnimationCullingType.AlwaysAnimate;
        }
        foreach (PrologueAction item in shot.actions)
        {
            if (item.clip == null) continue;
            motion.AddClip(item.clip, item.id);
        }
        // A scene cut does not consume the click/Space that advances its next line.
        yield return null;
    }

    private IEnumerator PlayAction(string id)
    {
        if (!IsPlaying || shot == null || skipping) yield break;
        PrologueAction next = Array.Find(shot.actions, candidate => candidate.id == id);
        if (next == null || next.clip == null)
        {
            Debug.LogError("Missing prologue action '" + id + "' in shot '" + CurrentShot + "'.", this);
            yield break;
        }
        action = next;
        lineReady = false;
        actionElapsed = 0f;
        lastFootstep = -1;
        IsAnimating = true;
        motion.Stop();
        AnimationState state = motion[id];
        state.wrapMode = WrapMode.ClampForever;
        state.speed = 1f;
        state.time = 0f;
        motion.Play(id, PlayMode.StopAll);
        motion.Sample();
        if (action.interactionCue) AudioManager.Instance?.PlayInteraction();
        // This finite action blocks Yarn, but never replaces its player-paced lines.
        while (IsPlaying && !skipping && actionElapsed < next.clip.length)
        {
            yield return null;
            actionElapsed += Time.deltaTime;
        }
        if (IsPlaying && !skipping)
        {
            state.time = next.clip.length;
            motion.Sample();
            CompletedActions++;
        }
        // Keep ClampForever's final pose until another action or shot is selected.
        IsAnimating = false;
    }

    private void Update()
    {
        if (!IsPlaying) return;
        FitStage();
        PositionHeaderControls();
        ambientElapsed += Time.deltaTime;
        if (skip != null) skip.interactable = !skipping && !DialogueHistory.AnyOpen && Time.frameCount > stageOpenedFrame + 1;
        int index = 0;
        foreach (Layer layer in layers.Values)
        {
            if (layer.image == null) continue;
            PrologueLayer definition = layer.definition;
            if (layer.state != null)
            {
                layer.stateElapsed += Time.deltaTime;
                if (!layer.state.loop && layer.state.returnToIdle && layer.stateElapsed >= layer.state.Duration)
                {
                    layer.state = null;
                    layer.stateElapsed = 0f;
                    layer.image.texture = definition.texture;
                }
                else
                {
                    Texture2D frame = layer.state.FrameAt(layer.stateElapsed);
                    if (frame != null) layer.image.texture = frame;
                }
            }
            else if (definition.frameCount > 1)
            {
                bool walking = IsAnimating && action != null && action.walkingLayer == definition.name;
                int step = walking ? Mathf.FloorToInt(actionElapsed * Mathf.Max(1f, definition.framesPerSecond)) : 0;
                int frame = step % definition.frameCount;
                bool resting = !walking && definition.idleTexture != null;
                layer.image.texture = resting ? definition.idleTexture : definition.texture;
                layer.image.uvRect = resting ? new Rect(0, 0, 1, 1)
                    : new Rect(frame / (float)definition.frameCount, 0, 1f / definition.frameCount, 1);
                if (walking && Time.deltaTime > 0f && step != lastFootstep)
                {
                    AudioManager.Instance?.PlayFootstep();
                    lastFootstep = step;
                }
            }
            else if (definition.blink != null)
            {
                float phase = (ambientElapsed + index * 1.17f) % 4.1f;
                layer.image.texture = phase > 3.93f ? definition.blink : definition.texture;
            }
            if (definition.sway)
                layer.image.rectTransform.anchoredPosition = layer.home + new Vector2(Mathf.Round(Mathf.Sin(ambientElapsed * .75f)), 0);
            index++;
        }
    }

    // These optional commands do not advance dialogue or alter the story.
    // A named pose holds until another pose or shot is selected; idle restores
    // the original sprite and its existing blink timing.
    public void SetCharacterPose(string character, string state)
    {
        if (!TryFindCharacter(character, out Layer layer)) return;
        bool idle = string.Equals(state, "idle", StringComparison.OrdinalIgnoreCase);
        PrologueCharacterState next = idle ? null : characterStates != null ? characterStates.Find(character, state) : null;
        if (!idle && (next == null || next.frames == null || next.frames.Length == 0))
        {
            Debug.LogWarning("Unknown prologue pose '" + character + "/" + state + "'. Build the Character Action Pack first.", this);
            return;
        }
        CancelCharacterGesture(layer);
        ApplyCharacterState(layer, next);
    }

    // Gestures are finite and use whole logical-pixel offsets. Time.deltaTime
    // also pauses them when History pauses the rest of the prologue.
    public IEnumerator PlayCharacterGesture(string character, string gesture)
    {
        if (!TryFindCharacter(character, out Layer layer)) yield break;
        PrologueCharacterGesture definition = characterStates != null ? characterStates.FindGesture(gesture) : null;
        if (definition == null)
        {
            Debug.LogWarning("Unknown prologue gesture '" + gesture + "'. Build the Character Action Pack first.", this);
            yield break;
        }

        CancelCharacterGesture(layer);
        int version = ++layer.gestureVersion;
        PrologueCharacterState previousState = layer.state;
        float previousElapsed = layer.stateElapsed;
        PrologueCharacterState gesturePose = !string.IsNullOrWhiteSpace(definition.pose)
            ? characterStates.Find(character, definition.pose) : null;
        if (gesturePose != null) ApplyCharacterState(layer, gesturePose);
        int poseVersion = layer.poseVersion;
        layer.gestureOrigin = layer.image.rectTransform.anchoredPosition;
        layer.gesturing = true;
        lineReady = false;
        IsAnimating = true;
        action = null;
        if (motion != null) motion.Stop();
        float elapsed = 0f;
        try
        {
            while (IsPlaying && !skipping && layer.image != null && layer.gestureVersion == version && elapsed < definition.Duration)
            {
                layer.image.rectTransform.anchoredPosition = layer.gestureOrigin + definition.OffsetAt(elapsed);
                yield return null;
                elapsed += Time.deltaTime;
            }
            if (IsPlaying && !skipping && layer.gestureVersion == version) CompletedActions++;
        }
        finally
        {
            if (layer.image != null && layer.gestureVersion == version)
            {
                layer.image.rectTransform.anchoredPosition = layer.gestureOrigin;
                layer.gesturing = false;
                if (layer.poseVersion == poseVersion)
                {
                    ApplyCharacterState(layer, previousState);
                    layer.stateElapsed = previousElapsed;
                }
            }
            IsAnimating = false;
        }
    }

    private bool TryFindCharacter(string character, out Layer result)
    {
        result = null;
        if (!IsPlaying || skipping) return false;
        foreach (Layer candidate in layers.Values)
        {
            string id = string.IsNullOrWhiteSpace(candidate.definition.characterId)
                ? candidate.definition.name : candidate.definition.characterId;
            // Only the standing cutscene actors share the character action pack.
            if (candidate.definition.frameCount != 1 || candidate.definition.sprite != null ||
                !string.Equals(id, character, StringComparison.OrdinalIgnoreCase)) continue;
            result = candidate;
            return candidate.image != null;
        }
        Debug.LogWarning("No standing prologue actor named '" + character + "' exists in shot '" + CurrentShot + "'.", this);
        return false;
    }

    private static void ApplyCharacterState(Layer layer, PrologueCharacterState state)
    {
        layer.state = state;
        layer.stateElapsed = 0f;
        layer.poseVersion++;
        if (layer.image == null) return;
        layer.image.texture = state != null ? state.FrameAt(0f) : layer.definition.texture;
        layer.image.uvRect = new Rect(0f, 0f, 1f, 1f);
    }

    private static void CancelCharacterGesture(Layer layer)
    {
        layer.gestureVersion++;
        if (layer.gesturing && layer.image != null) layer.image.rectTransform.anchoredPosition = layer.gestureOrigin;
        layer.gesturing = false;
    }

    private void ResetCharacterStates()
    {
        foreach (Layer layer in layers.Values)
        {
            CancelCharacterGesture(layer);
            if (layer.state != null) ApplyCharacterState(layer, null);
        }
    }

    private void LateUpdate()
    {
        if (!IsPlaying) return;
        // Native clips can interpolate positions; keep the final raster on its grid.
        foreach (Layer layer in layers.Values) if (layer.image != null) Snap(layer.image.rectTransform);
        if (shotRoot != null) Snap(shotRoot);
        // Sample after native clips so hidden keys cannot leave a visible glint.
        if (!skipping && action?.id == "handover" && actionElapsed >= PrologueAmbientEffects.KeyLandingCue)
            ambience?.TriggerKeyLanding(ambientElapsed);
        ambience?.Sample(ambientElapsed);
    }

    private static void Snap(RectTransform rect)
    {
        Vector2 p = rect.anchoredPosition;
        rect.anchoredPosition = new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
    }

    private RawImage FindLayerImage(string name) => layers.TryGetValue(name, out Layer layer) ? layer.image : null;

    private void BuildStage()
    {
        stage = new GameObject("Prologue_Stage", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        stage.layer = 5;
        stage.transform.SetParent(transform, false);
        Canvas canvas = stage.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = -100;
        CanvasScaler scaler = stage.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        RawImage black = Picture("Letterbox", stage.transform, null, new Color(.055f, .04f, .03f));
        black.raycastTarget = true;
        Stretch(black.rectTransform);
        viewport = Rect("Stage", stage.transform);
        viewport.anchorMin = viewport.anchorMax = viewport.pivot = new Vector2(.5f, .5f);
        viewport.sizeDelta = new Vector2(320, 180);
        viewport.gameObject.AddComponent<RectMask2D>();

        GameObject control = new GameObject("Skip_Prologue", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        control.layer = 5;
        control.transform.SetParent(stage.transform, false);
        RectTransform r = control.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = Vector2.one;
        r.anchoredPosition = new Vector2(-28, -24);
        r.sizeDelta = new Vector2(178, 48);
        Image face = control.GetComponent<Image>();
        face.color = new Color(.17f, .105f, .055f, .94f);
        Outline edge = control.AddComponent<Outline>();
        edge.effectColor = new Color(.68f, .47f, .23f);
        edge.effectDistance = new Vector2(2, -2);
        skip = control.GetComponent<Button>();
        skip.targetGraphic = face;
        skip.navigation = new Navigation { mode = Navigation.Mode.None };
        skip.onClick.AddListener(SkipPrologue);
        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.layer = 5;
        label.transform.SetParent(control.transform, false);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        LinePresenter presenter = runner.GetComponentInChildren<LinePresenter>(true);
        text.text = "Skip prologue";
        DialogueBehaviour.StyleText(text, presenter?.lineText?.font, 24f, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        stage.SetActive(false);
    }

    private void FitStage()
    {
        if (viewport == null) return;
        Canvas canvas = stage.GetComponent<Canvas>();
        // An integer screen-pixel scale avoids uneven or blurred sprite pixels.
        float scalePixels = Mathf.Floor(Mathf.Min(Screen.width / 320f, Screen.height / 180f));
        if (scalePixels < 1) scalePixels = Mathf.Min(Screen.width / 320f, Screen.height / 180f);
        float scale = scalePixels / Mathf.Max(.001f, canvas.scaleFactor);
        viewport.localScale = new Vector3(scale, scale, 1);
    }

    private void CaptureShop()
    {
        suspended.Clear();
        uiLocks.Clear();
        foreach (TutorialUIController ui in FindObjectsByType<TutorialUIController>(FindObjectsInactive.Include))
        {
            if (ui.gameObject.scene != gameObject.scene) continue;
            ui.CloseAllPanels();
            uiLocks[ui] = ui.ExternalInputBlocked;
            ui.ExternalInputBlocked = true;
        }
        foreach (ShopUIController shop in FindObjectsByType<ShopUIController>(FindObjectsInactive.Include))
            if (shop.gameObject.scene == gameObject.scene) Suspend(shop);
        foreach (ShopManualDrag drag in FindObjectsByType<ShopManualDrag>(FindObjectsInactive.Include))
            if (drag.gameObject.scene == gameObject.scene) Suspend(drag);
        Canvas dialogueCanvas = runner.GetComponentInChildren<LinePresenter>(true)?.GetComponentInParent<Canvas>();
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas.gameObject.scene != gameObject.scene || canvas.gameObject == stage ||
                canvas == dialogueCanvas || canvas.transform.IsChildOf(runner.transform)) continue;
            Suspend(canvas);
            foreach (GraphicRaycaster raycaster in canvas.GetComponents<GraphicRaycaster>()) Suspend(raycaster);
        }
        gameplayPause.Capture(null, pauseTime: false, suspendDialogueInput: false);
    }

    private void PositionHeaderControls()
    {
        if (headerControls.Count > 0 || caption == null) return;
        DialogueBehaviour controls = runner.GetComponentInChildren<DialogueBehaviour>(true);
        if (controls == null || controls.HistoryControl == null || controls.AutoControl == null || controls.SpeedControl == null) return;
        Canvas canvas = caption.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        Button[] buttons = { controls.HistoryControl, controls.AutoControl, controls.SpeedControl };
        float[] x = { 28, 190, 300 };
        foreach (Button button in buttons)
            headerControls.Add(new RectState(button.transform as RectTransform));
        for (int i = 0; i < buttons.Length; i++)
        {
            RectTransform rect = headerControls[i].rect;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x[i], -24);
            rect.localScale = Vector3.one;
        }
    }

    private void RestoreHeaderControls()
    {
        foreach (RectState control in headerControls) control.Restore();
        headerControls.Clear();
        caption = null;
    }

    private void Suspend(Behaviour behaviour)
    {
        if (suspended.ContainsKey(behaviour)) return;
        suspended.Add(behaviour, behaviour.enabled);
        behaviour.enabled = false;
    }

    public async void SkipPrologue()
    {
        if (!IsPlaying || skipping || DialogueHistory.AnyOpen || Time.frameCount <= stageOpenedFrame + 1) return;
        skipping = true;
        if (skip != null) skip.interactable = false;
        DialogueBehaviour controls = runner.GetComponentInChildren<DialogueBehaviour>(true);
        if (controls != null) Suspend(controls);
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            if (EventSystem.current.currentInputModule != null) Suspend(EventSystem.current.currentInputModule);
        }
        // Let the command bridge return to a dialogue line before cancelling.
        // Stopping Yarn inside its awaited coroutine lets that old coroutine
        // resume an already stopped VM in this installed Yarn version.
        try
        {
            while (runner != null && runner.IsDialogueRunning && IsPlaying && !lineReady)
                await YarnTask.Yield();
            await YarnTask.Yield();
            if (runner != null && runner.IsDialogueRunning) await runner.Stop();
        }
        catch (Exception error) { Debug.LogException(error, this); }
        finally { EndPrologue(); }
    }

    private void EndPrologue()
    {
        if (!IsPlaying || finishing) return;
        finishing = true;
        DialogueHistory history = runner != null ? runner.GetComponent<DialogueHistory>() : null;
        if (history != null && history.IsOpen) history.Close();
        IsPlaying = false;
        IsAnimating = false;
        if (motion != null) motion.Stop();
        ResetCharacterStates();
        ambience?.Dispose();
        ambience = null;
        if (stage != null) stage.SetActive(false);
        RestoreHeaderControls();
        foreach (var pair in suspended) if (pair.Key != null) pair.Key.enabled = pair.Value;
        suspended.Clear();
        foreach (var pair in uiLocks) if (pair.Key != null) pair.Key.ExternalInputBlocked = pair.Value;
        uiLocks.Clear();
        gameplayPause.Restore();
        finishing = false;
    }

    private void OnDisable() => EndPrologue();
    private void OnDestroy()
    {
        EndPrologue();
        if (runner != null && configured)
        {
            runner.onDialogueComplete.RemoveListener(EndPrologue);
            runner.RemoveCommandHandler("prologueShot");
            runner.RemoveCommandHandler("prologueAct");
            runner.RemoveCommandHandler("prologuePose");
            runner.RemoveCommandHandler("prologueGesture");
            runner.RemoveCommandHandler("prologueEnd");
        }
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject item = new GameObject(name, typeof(RectTransform));
        item.layer = 5;
        item.transform.SetParent(parent, false);
        return item.GetComponent<RectTransform>();
    }

    private static RawImage Picture(string name, Transform parent, Texture texture, Color color)
    {
        RectTransform rect = Rect(name, parent);
        RawImage image = rect.gameObject.AddComponent<RawImage>();
        image.texture = texture != null ? texture : Texture2D.whiteTexture;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void TopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(position.x, -position.y);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
