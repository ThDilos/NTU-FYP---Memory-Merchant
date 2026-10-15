using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StartMenuController : MonoBehaviour
{
    public GameObject settingsPanel;
    public GameObject archivesPanel;

    [Header("Continue (saved tutorial checkpoint)")]
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueLabel;
    [SerializeField] private TMP_Text continueStatus;
    [Header("Archives")]
    [SerializeField] private TMP_Text archivesStatus;
    [SerializeField] private TMP_Text archivesSubtitle;
    [SerializeField] private Button tutorialReplayButton;

    [Header("Settings Controls")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeValue;
    [SerializeField] private Toggle fullscreenToggle;

    private const string MasterVolumeKey = "MemoryMerchant.MasterVolume";
    private const string FullscreenKey = "MemoryMerchant.Fullscreen";
    private GameObject activePanel;
    private GameObject previousSelection;

    [Header("New Game Prologue")]
    [SerializeField] private GameObject prologuePanel;
    [SerializeField] private TMP_Text prologueText;
    [TextArea(5, 15)]
    [SerializeField] private string prologueContent =
        "[Placeholder prologue]\n\nOn graduation day, Jade learns that her parents once ran a mysterious memory shop.\n\nWith an old key and unanswered questions, she sets out to discover what they left behind.";

    private bool prologueVisible;
    private bool continueInputArmed;
    private bool shopLoading;
    private int prologueOpenedFrame;

    private void Awake()
    {
        if (prologuePanel != null)
            prologuePanel.SetActive(false);
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (archivesPanel != null)
            archivesPanel.SetActive(false);

        RefreshContinue();
        RefreshArchives();

        // AudioListener controls all game audio, including AudioManager sources.
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        if (PlayerPrefs.HasKey(FullscreenKey))
            Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey) != 0;
        SynchronizeSettingsControls();
    }
    private void Start()
    {
        if (TutorialRewardBridge.ConsumeArchiveReturn()) OpenArchives();
    }
    private void RefreshArchives()
    {
        bool unlocked = TutorialProgressSave.HasValidSave;
        if (archivesStatus != null) archivesStatus.text = unlocked
            ? "Completed\nReady to revisit"
            : "Locked\nComplete the tutorial to unlock";
        if (archivesSubtitle != null) archivesSubtitle.text = unlocked
            ? "Revisit your restored memories." : "No memories unlocked yet.";
        if (tutorialReplayButton != null)
        {
            tutorialReplayButton.gameObject.SetActive(true);
            tutorialReplayButton.interactable = unlocked;
            TMP_Text label = tutorialReplayButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = unlocked ? "Replay Tutorial" : "Locked";
        }
    }

    private void SynchronizeSettingsControls()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(AudioListener.volume);
        if (masterVolumeValue != null)
            masterVolumeValue.text = Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
    }
    private void RefreshContinue()
    {
        bool hasSave = TutorialProgressSave.HasValidSave;
        if (continueButton != null) continueButton.interactable = hasSave;
        if (continueStatus != null)
        {
            continueStatus.text = hasSave ? "" : "No saved game yet.";
            continueStatus.gameObject.SetActive(!hasSave);
        }
        if (continueLabel != null)
        {
            continueLabel.enableAutoSizing = false;
            continueLabel.fontSize = hasSave ? 36f : 32f;
            continueLabel.fontStyle = FontStyles.Bold;
            continueLabel.alignment = TextAlignmentOptions.Midline;
            RectTransform labelRect = continueLabel.rectTransform;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchorMin = hasSave ? Vector2.zero : new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = hasSave ? Vector2.one : new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, hasSave ? 0f : 16f);
            labelRect.sizeDelta = hasSave ? Vector2.zero : new Vector2(440f, 38f);
            continueLabel.margin = hasSave ? new Vector4(12f, 8f, 12f, 8f) : Vector4.zero;
        }
    }

    private void Update()
    {
        if (activePanel != null)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseActivePanel();
            return;
        }

        if (!prologueVisible || shopLoading || Time.frameCount <= prologueOpenedFrame)
            return;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool inputHeld = (mouse != null && mouse.leftButton.isPressed) ||
            (keyboard != null && (keyboard.enterKey.isPressed ||
                keyboard.numpadEnterKey.isPressed || keyboard.spaceKey.isPressed));

        // Require the opening input to be released before accepting a fresh press.
        if (!continueInputArmed)
        {
            continueInputArmed = !inputHeld;
            return;
        }

        bool continuePressed = (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
            (keyboard != null && (keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame));

        if (continuePressed)
        {
            // Set the guard before loading so simultaneous/repeated inputs cannot load twice.
            shopLoading = true;
            prologueVisible = false;
            SceneManager.LoadScene("Marcus_TutorialEnvironment");
        }
    }

    public void NewGame()
    {
        if (prologueVisible || shopLoading || activePanel != null)
            return;

        if (prologuePanel == null || prologueText == null)
        {
            Debug.LogError("Assign the prologue panel and text on StartMenuController.", this);
            return;
        }

        prologueText.text = prologueContent;
        TutorialRewardBridge.BeginNewGame();
        RefreshContinue();
        prologuePanel.SetActive(true);
        prologuePanel.transform.SetAsLastSibling();
        prologueOpenedFrame = Time.frameCount;
        continueInputArmed = false;
        prologueVisible = true;
    }

    public void ContinueGame()
    {
        if (prologueVisible || shopLoading || activePanel != null) return;
        if (!Application.CanStreamedLevelBeLoaded("Marcus_TutorialEnvironment"))
        {
            Debug.LogError("Add Marcus_TutorialEnvironment to the scene build list.", this);
            return;
        }
        if (!TutorialRewardBridge.ResumeSavedGame()) { RefreshContinue(); return; }
        shopLoading = true;
        Time.timeScale = 1f;
        if (GameController.Instance != null) GameController.Instance.isPaused = false;
        SceneManager.LoadScene("Marcus_TutorialEnvironment");
    }

    public void OpenArchives()
    {
        RefreshArchives();
        OpenPanel(archivesPanel);
    }
    public void ReplayTutorial()
    {
        if (shopLoading || prologueVisible || activePanel != archivesPanel) return;
        const string scene = "Tutorial_MemoryWorld_Marcus";
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("Add the tutorial Memory World to the scene build list.", this);
            return;
        }
        if (!TutorialRewardBridge.BeginArchiveReplay()) { RefreshArchives(); return; }
        shopLoading = true;
        Time.timeScale = 1f;
        if (GameController.Instance != null) GameController.Instance.isPaused = false;
        SceneManager.LoadScene(scene);
    }

    public void OpenSettings()
    {
        if (prologueVisible || shopLoading || activePanel != null)
            return;
        SynchronizeSettingsControls();
        OpenPanel(settingsPanel);
    }

    private void OpenPanel(GameObject panel)
    {
        if (panel == null || prologueVisible || shopLoading || activePanel != null)
            return;

        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        activePanel = panel;
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            Selectable firstControl = panel.GetComponentInChildren<Selectable>();
            if (firstControl != null)
                EventSystem.current.SetSelectedGameObject(firstControl.gameObject);
        }
    }

    public void CloseActivePanel()
    {
        if (activePanel == null)
            return;
        PlayerPrefs.Save();
        activePanel.SetActive(false);
        activePanel = null;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection);
        previousSelection = null;
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MasterVolumeKey, AudioListener.volume);
        if (masterVolumeValue != null)
            masterVolumeValue.text = Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
    }

    public void SetFullscreen(bool enabled)
    {
        Screen.fullScreen = enabled;
        PlayerPrefs.SetInt(FullscreenKey, enabled ? 1 : 0);
    }

    private void OnDisable()
    {
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            PlayerPrefs.Save();
    }

    public void ExitGame()
    {
        if (prologueVisible || shopLoading || activePanel != null)
            return;
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
