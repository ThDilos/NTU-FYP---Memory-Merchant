using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialUIController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject manualPanel;
    public GameObject settingsPanel;

    void Start()
    {
        manualPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current == null)
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
        bool isOpen = manualPanel.activeSelf;

        manualPanel.SetActive(!isOpen);

        if (!isOpen)
        {
            settingsPanel.SetActive(false);
        }
    }

    public void ToggleSettings()
    {
        bool isOpen = settingsPanel.activeSelf;

        settingsPanel.SetActive(!isOpen);

        if (!isOpen)
        {
            manualPanel.SetActive(false);
        }
    }

    public void OpenManual()
    {
        manualPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    public void CloseManual()
    {
        manualPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        manualPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void CloseAllPanels()
    {
        manualPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }
}