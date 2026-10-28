using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[DefaultExecutionOrder(150)]
public sealed class PlayerFootstepAudio : MonoBehaviour
{
    private PlayerController player;
    private Vector3 previous;
    private float elapsed, distance, stationary;
    private void Awake() => player = GetComponent<PlayerController>();
    private void OnEnable() { previous = transform.position; elapsed = distance = stationary = 0; }
    private void LateUpdate()
    {
        float travelled = Vector2.Distance(previous, transform.position);
        previous = transform.position;
        if (player == null || !player.isActiveAndEnabled || Time.timeScale <= 0f ||
            player.MovementInput.sqrMagnitude < .01f || travelled > .3f)
        { elapsed = distance = stationary = 0; return; }
        // Measure actual movement: holding a direction against a wall stays quiet.
        elapsed += Time.deltaTime;
        if (travelled < .00001f)
        {
            // Render frames can occur between physics steps, especially at 120 Hz.
            stationary += Time.deltaTime;
            if (stationary > .1f) elapsed = distance = 0;
            return;
        }
        stationary = 0; distance += travelled;
        if (distance >= .025f && elapsed >= (player.IsRunning ? .24f : .34f))
        { AudioManager.Instance?.PlayFootstep(); elapsed = distance = 0; }
    }
}
