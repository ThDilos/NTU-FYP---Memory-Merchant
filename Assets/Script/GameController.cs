using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("Frame rate Control (Changes require restarting the game)")]
    [Tooltip("How many game calculations per second?")]
    [SerializeField] private int updatePerSecond = 60;
    [Tooltip("Render Framerate, does not affect gameplay.\nThe conventional 'FPS'")]
    [SerializeField] private int frameRate = 120;

    // Runtime Vars
    public bool isPaused = false;
    private float timeScale;
    private void Awake()
    {
        // If there's already one instance, destroy the new one
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            Debug.Log("Duplicated GameController Destroyed!");
            return;
        }

        // Assign and make persistent throughout scenes
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Limit UpdateRate [Runtime]
        Time.fixedDeltaTime = 1f / updatePerSecond;
        timeScale = Time.timeScale;

        // Limit Framerate [Render Pipeline]
        PlayerPrefs.SetInt("FPS", frameRate);
        try { frameRate = PlayerPrefs.GetInt("FPS"); } catch { };
        QualitySettings.vSyncCount = 0; // Set vSyncCount to 0 so that using .targetFrameRate is enabled.
        Application.targetFrameRate = frameRate; // Default fps is set to 60, so that your GPU won't scream eve
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0;
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = timeScale;
        Debug.Log("Game Resumed");
    }
}
