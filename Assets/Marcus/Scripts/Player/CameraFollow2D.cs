using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private float smoothTime = 0.2f;
    [Tooltip("Use a shorter smoothing time for normal player tracking. Zero uses the same time as focus shots.")]
    [SerializeField, Min(0f)] private float playerFollowSmoothTime;

    [Header("Offset")]
    [SerializeField]
    private Vector2 offset =
        new Vector2(0f, 1f);

    [Header("Fixed room framing (optional)")]
    [Tooltip("Frame the room from this sprite's world bounds and keep focus shots inside its artwork.")]
    [SerializeField] private SpriteRenderer framingBackground;

    [Tooltip("Fill the viewport width with the room artwork while keeping both sides of the room visible.")]
    [SerializeField] private bool fillFramingWidth;

    [Tooltip("Vertical alignment when the room is taller than the viewport. Centre is 0.5.")]
    [SerializeField, Range(0f, 1f)] private float verticalCropAlignment = .5f;

    private Vector3 velocity;


    // Runtime vars
    private Transform cameraFollowTargetStore;
    private Vector3 startingPosition;
    private bool returningToStart;
    private Camera viewCamera;
    private float startingSize;
    private float focusSize;
    private float zoomVelocity;
    private float framingAspect;
    private float requestedFocusSize;

    void Awake()
    {
        cameraFollowTargetStore = target;
        startingPosition = transform.position;
        viewCamera = GetComponent<Camera>();
        if (viewCamera != null)
            startingSize = focusSize = viewCamera.orthographicSize;
        RefreshRoomFraming();
    }

    private void LateUpdate()
    {
        RefreshRoomFraming();
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
        {
            if (HasRoomFraming) transform.position = ClampToRoom(transform.position);
            return;
        }

        Vector3 targetPosition = target == null
            ? startingPosition
            :
            new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z
            );

        if (HasRoomFraming) targetPosition = ClampToRoom(targetPosition);

        float panSmoothTime = target != null && target == cameraFollowTargetStore && playerFollowSmoothTime > 0f
            ? playerFollowSmoothTime : smoothTime;
        transform.position =
            Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                panSmoothTime
            );

        if (HasRoomFraming) transform.position = ClampToRoom(transform.position);

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
        RefreshRoomFraming();
        this.target = target != null ? target : cameraFollowTargetStore;
        returningToStart = target == null && cameraFollowTargetStore == null;
        velocity = Vector3.zero;
        requestedFocusSize = target != null && orthographicSize > 0f ? orthographicSize : 0f;
        focusSize = GetFocusSize();
        zoomVelocity = 0f;
    }

    private bool HasRoomFraming => framingBackground != null && viewCamera != null && viewCamera.orthographic;

    private void RefreshRoomFraming()
    {
        if (!HasRoomFraming) return;
        float aspect = Mathf.Max(.001f, viewCamera.aspect);
        if (Mathf.Approximately(aspect, framingAspect)) return;
        framingAspect = aspect;
        Bounds room = framingBackground.bounds;
        float sizeToFitWidth = room.extents.x / aspect;
        startingSize = fillFramingWidth
            ? sizeToFitWidth
            : Mathf.Max(room.extents.y, sizeToFitWidth);
        float verticalCrop = Mathf.Max(0f, room.extents.y - startingSize);
        float verticalOffset = Mathf.Lerp(-verticalCrop, verticalCrop, verticalCropAlignment);
        startingPosition = new Vector3(room.center.x, room.center.y + verticalOffset, startingPosition.z);
        focusSize = GetFocusSize();
        if (target == null && !returningToStart)
        {
            viewCamera.orthographicSize = startingSize;
            transform.position = startingPosition;
            velocity = Vector3.zero;
            zoomVelocity = 0f;
        }
        else if (fillFramingWidth && viewCamera.orthographicSize > startingSize)
        {
            // A resize can make the previous zoom expose the edge of the room for a frame.
            viewCamera.orthographicSize = startingSize;
            zoomVelocity = 0f;
        }
    }

    private float GetFocusSize()
    {
        if (requestedFocusSize <= 0f) return startingSize;
        return fillFramingWidth && HasRoomFraming
            ? Mathf.Min(requestedFocusSize, startingSize)
            : requestedFocusSize;
    }

    private Vector3 ClampToRoom(Vector3 position)
    {
        Bounds room = framingBackground.bounds;
        float halfHeight = viewCamera.orthographicSize;
        float halfWidth = halfHeight * viewCamera.aspect;
        position.x = halfWidth >= room.extents.x ? room.center.x
            : Mathf.Clamp(position.x, room.min.x + halfWidth, room.max.x - halfWidth);
        position.y = halfHeight >= room.extents.y ? room.center.y
            : Mathf.Clamp(position.y, room.min.y + halfHeight, room.max.y - halfHeight);
        return position;
    }
}
