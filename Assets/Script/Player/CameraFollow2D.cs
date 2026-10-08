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
}