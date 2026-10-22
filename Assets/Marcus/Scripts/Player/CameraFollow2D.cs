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

    void Awake()
    {
        cameraFollowTargetStore = target;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 targetPosition =
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
    }

    // Change the focus of the camera, set to Null to follow player
    public void CameraFocusOn(Transform target = null)
    {
        if (target == null) this.target = cameraFollowTargetStore;
        else this.target = target; 
    }
}