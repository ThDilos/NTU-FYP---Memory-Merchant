using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Shared by Manual and Settings; close one panel before opening the other.
public sealed class GameplayInputPause
{
    private readonly Dictionary<Behaviour, bool> behaviours = new Dictionary<Behaviour, bool>();
    private readonly Dictionary<InputAction, bool> actions = new Dictionary<InputAction, bool>();
    private bool captured;
    private float timeScale;
    private GameController controller;
    private bool paused;
    public void Capture(InputActionAsset[] assets, bool pauseTime = true, bool suspendDialogueInput = true)
    {
        if (captured) return;
        captured = true;
        timeScale = Time.timeScale;
        controller = GameController.Instance;
        if (controller != null) { paused = controller.isPaused; controller.isPaused = true; }
        if (assets != null) foreach (InputActionAsset asset in assets)
        {
            InputActionMap map = asset != null ? asset.FindActionMap("Player", false) : null;
            if (map != null) foreach (InputAction action in map.actions) actions[action] = action.enabled;
        }
        Suspend<PlayerController>(); Suspend<Interactor>(); Suspend<TutorialVisualStateController>();
        if (!pauseTime)
        {
            Suspend<TutorialMemoryEcho>(); Suspend<TutorialLockedCabinet>(); Suspend<TutorialMemoryFragment>();
        }
        if (suspendDialogueInput) Suspend<Yarn.Unity.LineAdvancerInput.KeyCodes>();
        foreach (InputAction action in actions.Keys) action.Disable();
        if (pauseTime) Time.timeScale = 0f;
    }
    private void Suspend<T>() where T : Behaviour
    {
        foreach (T item in Object.FindObjectsByType<T>(FindObjectsInactive.Exclude))
        { behaviours[item] = item.enabled; item.enabled = false; }
    }
    public void Restore()
    {
        if (!captured) return;
        foreach (var item in behaviours) if (item.Key != null) item.Key.enabled = item.Value;
        foreach (var item in actions) { if (item.Value) item.Key.Enable(); else item.Key.Disable(); }
        Time.timeScale = timeScale;
        if (controller != null) controller.isPaused = paused;
        behaviours.Clear(); actions.Clear(); captured = false;
    }
}
