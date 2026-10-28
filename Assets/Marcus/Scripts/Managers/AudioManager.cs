using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yarn.Unity;

// One persistent mix for both authored scene audio and the generated default pack.
public class AudioManager : MonoBehaviour
{
    [Serializable] private struct BackgroundMusicOnSceneLoad
    {
        public List<int> buildIndexes;
        public string bgmName;
    }
    [Serializable] public class AudioData { public string name; public AudioClip clip; }
    public static AudioManager Instance { get; private set; }
    [SerializeField] private List<AudioData> backgroundMusicList = new List<AudioData>();
    [SerializeField] private List<BackgroundMusicOnSceneLoad> backgroundMusicOnSceneLoads = new List<BackgroundMusicOnSceneLoad>();
    [SerializeField] private List<AudioData> sfxList = new List<AudioData>();
    private readonly Dictionary<string, AudioClip> music = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, AudioClip> effects = new Dictionary<string, AudioClip>();
    private readonly AudioSource[] musicSources = new AudioSource[2];
    private AudioSource sfxSource, dialogueSource, footstepSource;
    private DialogueRunner[] runners = Array.Empty<DialogueRunner>();
    private float musicVolume = .30f, sfxVolume = .8f, nextScan, nextDialogueSound;
    private int currentMusic, step;
    private bool musicStopped;
    public float MusicVolume => musicVolume;
    public float SFXVolume => sfxVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null) new GameObject("Game Audio").AddComponent<AudioManager>();
    }

    private AudioSource Source(bool loop = false)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        source.loop = loop;
        return source;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Retain scene-authored registrations if a scene supplies a manager.
            Instance.Register(backgroundMusicList, Instance.music);
            Instance.Register(sfxList, Instance.effects);
            if (backgroundMusicOnSceneLoads != null) Instance.backgroundMusicOnSceneLoads.AddRange(backgroundMusicOnSceneLoads);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        musicSources[0] = Source(true); musicSources[1] = Source(true);
        musicSources[0].volume = musicSources[1].volume = 0;
        sfxSource = Source(); dialogueSource = Source(); footstepSource = Source();
        foreach (AudioClip clip in Resources.LoadAll<AudioClip>("MemoryMerchantAudio"))
            (clip.name.StartsWith("music_", StringComparison.Ordinal) ? music : effects)[clip.name] = clip;
        Register(backgroundMusicList, music); Register(sfxList, effects);
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("MemoryMerchant.MasterVolume", 1f));
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private void Register(List<AudioData> list, Dictionary<string, AudioClip> destination)
    {
        if (list == null) return;
        foreach (AudioData entry in list)
            if (entry != null && !string.IsNullOrEmpty(entry.name) && entry.clip != null) destination[entry.name] = entry.clip;
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;
        bool mapped = false;
        foreach (var entry in backgroundMusicOnSceneLoads)
            if (entry.buildIndexes != null && entry.buildIndexes.Contains(scene.buildIndex))
            { PlayBackgroundMusic(entry.bgmName); mapped = true; break; }
        if (!mapped)
        {
            string name = scene.name;
            PlayBackgroundMusic(name.IndexOf("MemoryWorld", StringComparison.OrdinalIgnoreCase) >= 0 ? "music_memory" :
                name.IndexOf("Minigame", StringComparison.OrdinalIgnoreCase) >= 0 ? "music_puzzle" : "music_shop");
        }
        ScanScene();
    }

    private void ScanScene()
    {
        // Dynamic workshop, Yarn options and puzzle buttons also receive sounds.
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include))
            if (button.GetComponent<UIButtonSound>() == null) button.gameObject.AddComponent<UIButtonSound>();
        foreach (LinePresenter presenter in FindObjectsByType<LinePresenter>())
            if (presenter.Typewriter is LetterTypewriter original && !(original is AdjustableDialogueTypewriter))
                presenter.Typewriter = new AdjustableDialogueTypewriter
                {
                    TextElement = original.TextElement,
                    ActionMarkupHandlers = original.ActionMarkupHandlers,
                    CharactersPerSecond = original.CharactersPerSecond
                };
        runners = FindObjectsByType<DialogueRunner>();
        nextScan = Time.unscaledTime + .25f;
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextScan) ScanScene();
        bool speaking = false;
        foreach (DialogueRunner runner in runners)
            if (runner != null && runner.IsDialogueRunning) { speaking = true; break; }
        float target = musicStopped ? 0 : musicVolume * (speaking ? .65f : 1f);
        for (int i = 0; i < musicSources.Length; i++)
        {
            AudioSource source = musicSources[i];
            source.volume = Mathf.MoveTowards(source.volume, i == currentMusic ? target : 0, Time.unscaledDeltaTime * .65f);
            if (i != currentMusic && source.volume <= 0 && source.isPlaying) source.Stop();
        }
    }

    public void PlayBackgroundMusic(string name)
    {
        if (!music.TryGetValue(name, out AudioClip clip) || clip == null) return;
        musicStopped = false;
        AudioSource current = musicSources[currentMusic];
        if (current.clip == clip && current.isPlaying) return;
        currentMusic = 1 - currentMusic;
        current = musicSources[currentMusic];
        current.Stop(); current.volume = 0; current.clip = clip; current.Play();
    }
    public void StopBackgroundMusic() { musicStopped = true; foreach (var source in musicSources) source.Stop(); }
    public bool IsBackgroundMusicPlaying() => !musicStopped && musicSources[currentMusic].isPlaying;
    public void SetMusicVolume(float value) => musicVolume = Mathf.Clamp01(value);
    public void SetSFXVolume(float value) => sfxVolume = Mathf.Clamp01(value);

    public void PlaySFX(string name) => PlayEffect(sfxSource, name, 1f);
    private void PlayEffect(AudioSource source, string name, float gain)
    {
        if (source != null && effects.TryGetValue(name, out AudioClip clip) && clip != null)
            source.PlayOneShot(clip, sfxVolume * gain);
    }
    public void PlayButton() => PlayEffect(sfxSource, "button", .55f);
    public void PlayInteraction() => PlayEffect(sfxSource, "interact", .55f);
    public void PlayPurchase() => PlayEffect(sfxSource, "purchase", .75f);
    public void PlayCollection() => PlayEffect(sfxSource, "collect", .65f);
    public void PlayPlacement() => PlayEffect(sfxSource, "place", .55f);
    public void PlayFootstep() => PlayEffect(footstepSource, "footstep_" + (step++ % 2), .5f);
    public void PlayDialogueCharacter(char character)
    {
        // No blip storm on hurry-up/2x; ignore spaces, punctuation and paused text.
        if (!char.IsLetterOrDigit(character) || Time.timeScale <= 0f || Time.unscaledTime < nextDialogueSound) return;
        nextDialogueSound = Time.unscaledTime + .075f;
        PlayEffect(dialogueSource, "dialogue", .35f);
    }
    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= SceneLoaded;
        Instance = null;
    }
}
