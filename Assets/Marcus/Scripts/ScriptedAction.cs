using UnityEngine;
using UnityEngine.Events;

public class ScriptedAction : MonoBehaviour
{
    private enum ActionResult { ActivateObject, DeactivateObject, CustomEvent }
    [SerializeField] private ActionResult actionResult;
    [SerializeField] private GameObject targetObj;
    [SerializeField] private UnityEvent customAction = new UnityEvent();

    // Can be called by an animation event or by a validated replay completion event.
    public void Trigger()
    {
        if (actionResult == ActionResult.CustomEvent) { customAction.Invoke(); return; }
        if (targetObj == null) { Debug.LogWarning("[ScriptedAction] Target object is missing.", this); return; }
        targetObj.SetActive(actionResult == ActionResult.ActivateObject);
    }
}
