using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

// Older scene variants share Yarn's presenters but do not all have the shop's
// dialogue controller. Install it before Start without changing those prefabs.
public static class DialogueControlsInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DialogueBehaviour[] existing = Object.FindObjectsByType<DialogueBehaviour>(FindObjectsInactive.Include);
        foreach (DialogueRunner runner in Object.FindObjectsByType<DialogueRunner>(FindObjectsInactive.Include))
        {
            if (runner.gameObject.scene != scene || runner.GetComponentInChildren<LinePresenter>(true) == null ||
                runner.GetComponentInChildren<LineAdvancer>(true) == null) continue;
            DialogueBehaviour controls = null;
            foreach (DialogueBehaviour candidate in existing)
            {
                if (candidate.Runner == runner || candidate.transform == runner.transform ||
                    candidate.transform.IsChildOf(runner.transform))
                { controls = candidate; break; }
            }
            if (controls == null) controls = runner.gameObject.AddComponent<DialogueBehaviour>();
            controls.Configure(runner);
            // The shop's opening Yarn node also drives its layered prologue art.
            if (scene.name == "Marcus_TutorialEnvironment" && runner.startNode == "Prologue")
            {
                PrologueCutsceneController cutscene = runner.GetComponent<PrologueCutsceneController>();
                if (cutscene == null) cutscene = runner.gameObject.AddComponent<PrologueCutsceneController>();
                cutscene.Configure(runner);
            }
        }
    }
}
