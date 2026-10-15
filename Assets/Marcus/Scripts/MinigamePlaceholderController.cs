using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MinigamePlaceholderController : MonoBehaviour
{
    [SerializeField] private string shopSceneName = "Marcus_TutorialEnvironment";
    private bool returning;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ReturnToShop();
    }

    public void ReturnToShop()
    {
        if (returning)
            return;
        if (!Application.CanStreamedLevelBeLoaded(shopSceneName))
        {
            Debug.LogError("Add the shop scene to the scene build list: " + shopSceneName, this);
            return;
        }
        returning = true;
        SceneManager.LoadScene(shopSceneName);
    }
}
