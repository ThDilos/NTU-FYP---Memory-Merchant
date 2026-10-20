
using UnityEngine;
using Yarn.Unity;

public class CameraShift : MonoBehaviour
{
    [SerializeField] private CameraFollow2D cameraFollow;

    private static CameraShift instance;

    private bool returnOnNextLine = false;

    private void Awake()
    {
        instance = this;
    }

    [YarnCommand("cameraShift")]
    public static void ShiftCamera(string targetName)
    {
        if (instance == null)
            return;

        GameObject targetObject = GameObject.Find(targetName);

        if (instance.cameraFollow == null || targetObject == null)
        {
            Debug.LogWarning(
                $"Camera shift failed. Check CameraFollow2D and target '{targetName}'."
            );
            return;
        }

        instance.cameraFollow.CameraFocusOn(targetObject.transform);
        instance.returnOnNextLine = true;
    }

    [YarnCommand("cameraReturn")]
    public static void ReturnCamera()
    {
        if (instance == null || instance.cameraFollow == null)
            return;

        instance.cameraFollow.CameraFocusOn(null);
        instance.returnOnNextLine = false;
    }

    [YarnCommand("cameraKeep")]
    public static void KeepCamera()
    {
        if (instance == null)
            return;

        instance.returnOnNextLine = false;
    }

    public void OnDialogueLineStarted()
    {
        if (!returnOnNextLine)
            return;

        if (cameraFollow != null)
            cameraFollow.CameraFocusOn(null);

        returnOnNextLine = false;
    }
}
