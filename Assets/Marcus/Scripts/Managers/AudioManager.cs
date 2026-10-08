using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Serializable] public class AudioData
    {
        public string name;
        public AudioClip clip;
    }

    [Header("Background Music")]
    [SerializeField] private List<AudioData> backgroundMusicList;
    [SerializeField] private bool playBackgroundMusicOnMainMenu = true;

    [Header("Sound Effects")]
    [SerializeField] private List<AudioData> sfxList;

    private Dictionary<string, AudioClip> backgroundMusicDictionary;
    private Dictionary<string, AudioClip> sfxDictionary;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    public float MusicVolume => bgmSource.volume;
    public float SFXVolume => sfxSource.volume;

    private void Awake()
    {
        // If there's already one instance, destroy the new one
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Assign and make persistent throughout scenes
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create the two AudioSources automatically
        bgmSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        // Background music should loop
        bgmSource.loop = true;
        
        // Create dictionaries
        backgroundMusicDictionary = new Dictionary<string, AudioClip>();
        sfxDictionary = new Dictionary<string, AudioClip>();

        // Add background music into dictionary
        foreach (AudioData audio in backgroundMusicList)
        {
            backgroundMusicDictionary[audio.name] = audio.clip;
        }

        // Add sound effects into dictionary
        foreach (AudioData audio in sfxList)
        {
            sfxDictionary[audio.name] = audio.clip;
        }
    }

    public void PlayBackgroundMusic(string name)
    {
        Debug.Log("Trying to play BGM: " + name);

        if (!backgroundMusicDictionary.TryGetValue(name, out AudioClip clip))
        {
            Debug.LogWarning("Background music not found: " + name);
            return;
        }

        if (clip == null)
        {
            Debug.LogError("AudioClip for " + name + " is NULL!");
            return;
        }

        Debug.Log("Found BGM: " + clip.name);

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.Play();

        Debug.Log(
            "BGM playing: " + bgmSource.isPlaying +
            " | Volume: " + bgmSource.volume
        );
    }

    public void StopBackgroundMusic()
    {
        bgmSource.Stop();
    }

    public bool IsBackgroundMusicPlaying()
    {
        return bgmSource.isPlaying;
    }

    public void PlaySFX(string name)
    {
        if (!sfxDictionary.TryGetValue(name, out AudioClip clip))
        {
            Debug.LogWarning("SFX not found: " + name);
            return;
        }

        sfxSource.PlayOneShot(clip);
    }
    
    // ----------------------------
    // VOLUME CONTROL
    // ----------------------------

    public void SetMusicVolume(float volume)
    {
        bgmSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }
}