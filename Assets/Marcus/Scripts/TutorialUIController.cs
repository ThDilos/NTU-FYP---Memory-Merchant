using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialUIController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject manualPanel;
    public GameObject settingsPanel;
    [Tooltip("Disable when another scene controller already handles Q / R / Escape.")]
    public bool handleKeyboardInput = true;
    public bool ExternalInputBlocked { get; set; }

    void Start()
    {
        MemoryWorldSettingsController.ApplySavedPreferences();
        CloseAllPanels();
    }

    void Update()
    {
        if (ExternalInputBlocked || !handleKeyboardInput || Keyboard.current == null)
            return;

        // Q = open / close Settings
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            ToggleSettings();
        }

        // R = open / close Manual
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ToggleManual();
        }

        // ESC = close everything
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseAllPanels();
        }
    }

    public void ToggleManual()
    {
        if (ExternalInputBlocked) return;
        if (manualPanel == null) return;
        if (manualPanel.activeSelf) CloseManual();
        else OpenManual();
    }

    public void ToggleSettings()
    {
        if (ExternalInputBlocked) return;
        if (settingsPanel == null) return;
        if (settingsPanel.activeSelf) CloseSettings();
        else OpenSettings();
    }

    public void OpenManual()
    {
        if (ExternalInputBlocked) return;
        CloseSettings();
        if (manualPanel == null) return;
        manualPanel.transform.SetAsLastSibling();
        manualPanel.SetActive(true);
    }

    public void CloseManual()
    {
        if (manualPanel != null) manualPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (ExternalInputBlocked) return;
        CloseManual();
        if (settingsPanel == null) return;
        settingsPanel.transform.SetAsLastSibling();
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void CloseAllPanels()
    {
        CloseManual();
        CloseSettings();
    }
}
