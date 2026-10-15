using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MemoryWorldSettingsController : MonoBehaviour
{
    [SerializeField] private TutorialUIController tutorialUI;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeValue;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private InputActionAsset[] gameplayInputAssets;
    private readonly GameplayInputPause pause = new GameplayInputPause();
    private bool loading;
    private const string VolumeKey = "MemoryMerchant.MasterVolume";
    private const string FullscreenKey = "MemoryMerchant.Fullscreen";
    public static void ApplySavedPreferences()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        if (PlayerPrefs.HasKey(FullscreenKey)) Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey) != 0;
    }
    private void OnEnable() { pause.Capture(gameplayInputAssets); ApplySavedPreferences(); Refresh(); }
    private void OnDisable() { PlayerPrefs.Save(); pause.Restore(); }
    private void Refresh()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(AudioListener.volume);
        if (masterVolumeValue != null) masterVolumeValue.text = Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0);
    }
    public void SetMasterVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
        Refresh();
    }
    public void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
    }
    public void ClosePanels() { if (tutorialUI != null) tutorialUI.CloseSettings(); }
    public void ReturnToStartMenu()
    {
        if (loading || !Application.CanStreamedLevelBeLoaded("Start Menu")) return;
        loading = true;
        PlayerPrefs.Save(); ClosePanels();
        Time.timeScale = 1f;
        if (GameController.Instance != null) GameController.Instance.isPaused = false;
        TutorialRewardBridge.FinishArchiveReplay();
        SceneManager.LoadScene("Start Menu");
    }
}
