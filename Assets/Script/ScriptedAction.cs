using UnityEngine;
using System.Collections;

public class ScriptedAction : MonoBehaviour
{
    private enum ActionResult
    {
        ACTIVATE_GAMEOBJECT,
        DEACTIVATE_GAMEOBJECT,
        INTERACTABLE_SET_ALLOW,
        INTERACTABLE_SET_DISALLOW
    }

    [Header("Action Setting")]
    [SerializeField] private ActionResult actionResult = ActionResult.ACTIVATE_GAMEOBJECT;
    [SerializeField] private GameObject targetObj;

    public void Trigger()
    {
        if (targetObj == null)
        {
            Debug.LogError("[ScriptedAction] Target Object is not set!");
            return;
        }

        Interactible interactible = targetObj.GetComponent<Interactible>();
        switch (actionResult)
        {
            case ActionResult.ACTIVATE_GAMEOBJECT:
                targetObj.SetActive(true);
                break;
            case ActionResult.DEACTIVATE_GAMEOBJECT:
                targetObj.SetActive(false);
                break;
            case ActionResult.INTERACTABLE_SET_ALLOW:
                if (interactible == null) return;
                interactible.SetInteractable(true);
                break;
            case ActionResult.INTERACTABLE_SET_DISALLOW:
                if (interactible == null) return;
                interactible.SetInteractable(false);
                break;
        }
    }
}