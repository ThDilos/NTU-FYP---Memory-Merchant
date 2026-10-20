
using UnityEngine;
using Yarn.Unity;

public class CameraShiftLineObserver : DialoguePresenterBase
{
    [SerializeField]
    private CameraShift cameraShiftController;

    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(
        LocalizedLine line,
        LineCancellationToken token)
    {
        if (cameraShiftController != null)
        {
            cameraShiftController.OnDialogueLineStarted();
        }

        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        return YarnTask.CompletedTask;
    }
}
