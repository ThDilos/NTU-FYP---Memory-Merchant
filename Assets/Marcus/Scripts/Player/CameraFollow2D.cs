using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private float smoothTime = 0.2f;

    [Header("Offset")]
    [SerializeField]
    private Vector2 offset =
        new Vector2(0f, 1f);

    private Vector3 velocity;


    // Runtime vars
    private Transform cameraFollowTargetStore;
    private Vector3 startingPosition;
    private bool returningToStart;
    private Camera viewCamera;
    private float startingSize;
    private float focusSize;
    private float zoomVelocity;

    void Awake()
    {
        cameraFollowTargetStore = target;
        startingPosition = transform.position;
        viewCamera = GetComponent<Camera>();
        if (viewCamera != null)
            startingSize = focusSize = viewCamera.orthographicSize;
    }

    private void LateUpdate()
    {
        // Zoom and pan use the same smoothing so the shop edges stay outside the shot.
        // Keep restoring zoom even after a fixed camera has reached its starting position.
        if (viewCamera != null && viewCamera.orthographic)
        {
            viewCamera.orthographicSize = Mathf.SmoothDamp(
                viewCamera.orthographicSize, focusSize, ref zoomVelocity, smoothTime);
            if (Mathf.Abs(viewCamera.orthographicSize - focusSize) < 0.00001f)
            {
                viewCamera.orthographicSize = focusSize;
                zoomVelocity = 0f;
            }
        }

        if (target == null && !returningToStart)
            return;

        Vector3 targetPosition = target == null
            ? startingPosition
            :
            new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z
            );

        transform.position =
            Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime
            );

        if (returningToStart && (transform.position - startingPosition).sqrMagnitude < 0.000001f)
        {
            transform.position = startingPosition;
            velocity = Vector3.zero;
            returningToStart = false;
        }
    }

    // Return to the player, or to the original view in fixed-camera scenes like the shop.
    public void CameraFocusOn(Transform target = null, float orthographicSize = 0f)
    {
        this.target = target != null ? target : cameraFollowTargetStore;
        returningToStart = target == null && cameraFollowTargetStore == null;
        velocity = Vector3.zero;
        focusSize = target != null && orthographicSize > 0f ? orthographicSize : startingSize;
        zoomVelocity = 0f;
    }
}
