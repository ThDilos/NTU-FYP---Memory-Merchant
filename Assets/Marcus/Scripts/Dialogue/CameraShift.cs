using UnityEngine;
using System.Collections;
using Yarn.Unity;

public class CameraShift : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Update()
    {
        
    }

    [Header("References")]
    [SerializeField] private Transform targetCamera;
    [SerializeField] private CameraFollow2D cameraFollow;

    [Header("Settings")]
    [SerializeField] private float moveDuration = 0.5f;

    private Vector3 returnPosition;
    private Coroutine movement;

    public bool IsShiftActive { get; private set; }
    private bool firstLineAfterShift = false;

    [YarnCommand("cameraShift")]

    public void ShiftCamera(float x, float y)
    {
        if (targetCamera == null || cameraFollow == null)
        {
            Debug.LogWarning("CameraShiftController: Assign the camera and camera-follow script.");
            return;
        }

        // Remember the camera's current position.
        returnPosition = targetCamera.position;

        // Stop the camera-follow script from overriding our movement.
       // cameraFollow.isFollowing = false;

        IsShiftActive = true;
        firstLineAfterShift = false;

        MoveTo(new Vector3(x, y, targetCamera.position.z));
    }

    public void ReturnCamera()
    {
        if (!IsShiftActive)
            return;

        IsShiftActive = false;
        MoveTo(returnPosition, true);
    }

    private void MoveTo(Vector3 destination, bool resumeFollowing = false)
    {
        if (movement != null)
            StopCoroutine(movement);

        movement = StartCoroutine(SmoothMove(destination, resumeFollowing));
    }

    private IEnumerator SmoothMove(Vector3 destination, bool resumeFollowing)
    {
        Vector3 start = targetCamera.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            targetCamera.position = Vector3.Lerp(start, destination, t);
            yield return null;
        }

        targetCamera.position = destination;
        movement = null;

        //if (resumeFollowing)
           // cameraFollow.isFollowing = true;
    }

    public void OnDialogueLineStarted()
    {
        if (!IsShiftActive)
            return;

        // The first line is the line we shifted the camera for.
        if (!firstLineAfterShift)
        {
            firstLineAfterShift = true;
        }
        else
        {
            // A subsequent line has started: return to the player.
            ReturnCamera();
        }
    }
}
