using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yarn.Unity;

public class ShopUIController : MonoBehaviour
{
    [Header("Shop HUD")]
    [SerializeField] private TMP_Text objectivesText;
    [TextArea(3, 8)]
    [SerializeField] private string objectives = "Drag the manual into the memory machine to enter the Memory World.";
    [TextArea(2, 5)] [SerializeField] private string postTutorialObjectives = "Tutorial complete.\nExplore Minigame and Memory Machine upgrades.";
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text fragmentPointsText;
    [Min(0)] [SerializeField] private int coins;
    [Min(0)] [SerializeField] private int fragmentPoints;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject manualPanel;
    [SerializeField] private GameObject upgradesPanel;
    private readonly GameplayInputPause upgradesPause = new GameplayInputPause();
    [SerializeField] private TutorialUIController tutorialUI;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeValue;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Picture Destination")]
    [SerializeField] private GameObject minigameEntrance;
    [SerializeField] private GameObject upgradesEntrance;
    [SerializeField] private GameObject tutorialMemoryAnchor;
    [SerializeField] private string minigameSceneName = "ShopMinigamePlaceholder";
    [SerializeField] private string memoryWorldSceneName = "Tutorial_MemoryWorld_Marcus";
    private bool sceneLoading;
    private DialogueRunner dialogueRunner;

    public bool IsShopInputBlocked => sceneLoading ||
        (settingsPanel != null && settingsPanel.activeSelf) ||
        (manualPanel != null && manualPanel.activeSelf) ||
        (upgradesPanel != null && upgradesPanel.activeSelf);
    public bool CanEnterTutorialMemoryWorld => !TutorialRewardBridge.TutorialCompleted && !IsShopInputBlocked;

    // Use the same preferences as the start menu.
    private const string VolumeKey = "MemoryMerchant.MasterVolume";
    private const string FullscreenKey = "MemoryMerchant.Fullscreen";

    private void Awake()
    {
        dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        bool tutorialComplete = TutorialRewardBridge.TutorialCompleted;
        if (minigameEntrance != null) minigameEntrance.SetActive(tutorialComplete && !TutorialRewardBridge.NeedsFirstEchoUpgrade);
        if (upgradesEntrance != null) upgradesEntrance.SetActive(tutorialComplete);
        if (tutorialMemoryAnchor != null) tutorialMemoryAnchor.SetActive(!tutorialComplete);
        // Returning from the memory must not restart the shop's opening prologue.
        if (TutorialRewardBridge.TutorialCompleted)
        {
            DialogueRunner runner = FindAnyObjectByType<DialogueRunner>();
            if (runner != null) runner.autoStart = false;
        }
        ConfigureIconButton("Settings", 96f);
        ConfigureIconButton("Manual", 256f);
        RectTransform objectivesPanel = transform.Find("Objectives_Panel") as RectTransform;
        if (objectivesPanel != null)
            objectivesPanel.anchoredPosition = new Vector2(219f, -333f);
        ClosePanels();
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        if (PlayerPrefs.HasKey(FullscreenKey))
            Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey) != 0;
        RefreshSettings();
        RefreshHUD();
    }

    private void ConfigureIconButton(string name, float x)
    {
        RectTransform rect = transform.Find(name + "_Button") as RectTransform;
        if (rect == null) return;
        Button button = rect.GetComponent<Button>();
        if (button != null)
            button.transition = Selectable.Transition.None;
        Image background = rect.GetComponent<Image>();
        if (background != null)
        {
            // Keep a transparent raycast area so both the icon and label are clickable.
            background.sprite = null;
            background.color = Color.clear;
            background.raycastTarget = true;
        }
        rect.sizeDelta = new Vector2(144f, 144f);
        rect.anchoredPosition = new Vector2(x, -96f);
        RectTransform icon = rect.Find(name + "_Icon") as RectTransform;
        if (icon != null)
        {
            icon.sizeDelta = new Vector2(144f, 144f);
            icon.anchoredPosition = Vector2.zero;
        }
        RectTransform label = rect.Find(name + "_Label") as RectTransform;
        if (label != null)
        {
            label.sizeDelta = new Vector2(128f, 28f);
            label.anchoredPosition = new Vector2(0f, -44f);
            TMP_Text text = label.GetComponent<TMP_Text>();
            if (text != null) text.fontSize = 22f;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || sceneLoading)
            return;
        if (keyboard.escapeKey.wasPressedThisFrame)
            ClosePanels();
        else if (keyboard.qKey.wasPressedThisFrame)
            ToggleSettings();
        else if (keyboard.rKey.wasPressedThisFrame)
            ToggleManual();
    }

    public void ToggleSettings()
    {
        if (sceneLoading || settingsPanel == null)
            return;
        bool open = !settingsPanel.activeSelf;
        ClosePanels();
        if (open)
        {
            RefreshSettings();
            settingsPanel.transform.SetAsLastSibling();
            settingsPanel.SetActive(true);
        }
    }

    public void ToggleManual()
    {
        if (sceneLoading || tutorialUI == null)
            return;
        PlayerPrefs.Save();
        CloseUpgrades();
        tutorialUI.ToggleManual();
    }

    public void OpenUpgrades()
    {
        if (!TutorialRewardBridge.TutorialCompleted || sceneLoading || upgradesPanel == null || upgradesPanel.activeSelf ||
            (dialogueRunner != null && dialogueRunner.IsDialogueRunning)) return;
        ClosePanels();
        upgradesPause.Capture(null);
        upgradesPanel.transform.SetAsLastSibling();
        upgradesPanel.SetActive(true);
    }

    private void CloseUpgrades()
    {
        if (upgradesPanel != null) upgradesPanel.SetActive(false);
        upgradesPause.Restore();
    }

    public void ClosePanels()
    {
        CloseUpgrades();
        if (tutorialUI != null)
            tutorialUI.CloseAllPanels();
        else
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (manualPanel != null) manualPanel.SetActive(false);
        }
        PlayerPrefs.Save();
    }

    private void RefreshSettings()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(AudioListener.volume);
        if (masterVolumeValue != null)
            masterVolumeValue.text = Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    public void SetMasterVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
        RefreshSettings();
    }

    public void SetFullscreen(bool enabled)
    {
        Screen.fullScreen = enabled;
        PlayerPrefs.SetInt(FullscreenKey, enabled ? 1 : 0);
    }

    public void SetObjective(string text)
    {
        objectives = text;
        RefreshHUD();
    }

    public void SetCoins(int value)
    {
        coins = Mathf.Max(0, value);
        RefreshHUD();
    }

    public void SetFragmentPoints(int value)
    {
        fragmentPoints = Mathf.Max(0, value);
        RefreshHUD();
    }

    private void RefreshHUD()
    {
        if (objectivesText != null)
            objectivesText.text = TutorialRewardBridge.NeedsFirstEchoUpgrade
                ? "Open Upgrades and buy Echo Slot II for 3 Memory Points."
                : TutorialRewardBridge.TutorialCompleted ? postTutorialObjectives : objectives;
        if (minigameEntrance != null)
            minigameEntrance.SetActive(TutorialRewardBridge.TutorialCompleted && !TutorialRewardBridge.NeedsFirstEchoUpgrade);
        if (coinsText != null)
            coinsText.text = (coins + TutorialRewardBridge.Coins).ToString();
        if (fragmentPointsText != null)
            fragmentPointsText.text = (fragmentPoints + TutorialRewardBridge.MemoryPoints).ToString();
    }

    public void OpenMinigame()
    {
        if (!TutorialRewardBridge.TutorialCompleted || TutorialRewardBridge.NeedsFirstEchoUpgrade || IsShopInputBlocked ||
            (dialogueRunner != null && dialogueRunner.IsDialogueRunning))
            return;
        if (!Application.CanStreamedLevelBeLoaded(minigameSceneName))
        {
            Debug.LogError("Add the minigame destination to the scene build list: " + minigameSceneName, this);
            return;
        }
        sceneLoading = true;
        PlayerPrefs.Save();
        SceneManager.LoadScene(minigameSceneName);
    }

    public bool TryEnterMemoryWorld()
    {
        if (!CanEnterTutorialMemoryWorld) return false;
        if (!Application.CanStreamedLevelBeLoaded(memoryWorldSceneName))
        {
            Debug.LogError("Add the Memory World to the scene build list: " + memoryWorldSceneName, this);
            return false;
        }
        sceneLoading = true;
        PlayerPrefs.Save();
        SceneManager.LoadScene(memoryWorldSceneName);
        return true;
    }

    private void OnDisable()
    {
        TutorialRewardBridge.Changed -= RefreshHUD;
        CloseUpgrades();
        PlayerPrefs.Save();
    }

    private void OnEnable()
    {
        TutorialRewardBridge.Changed += RefreshHUD;
        RefreshHUD();
    }

    public void ReturnToStartMenu()
    {
        if (sceneLoading) return;
        const string menuScene = "Start Menu";
        if (!Application.CanStreamedLevelBeLoaded(menuScene))
        {
            Debug.LogError("Add Start Menu to the scene build list.", this);
            return;
        }
        sceneLoading = true;
        ClosePanels();
        Time.timeScale = 1f;
        if (GameController.Instance != null) GameController.Instance.isPaused = false;
        SceneManager.LoadScene(menuScene);
    }
}
