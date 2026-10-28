using UnityEngine;

// The movement controller remains responsible for physics and input. This component
// drives only the Sprite child's Animator, including when gameplay input is paused.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
[DefaultExecutionOrder(100)]
public sealed class PlayerSpriteAnimation : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Animator animator;
    [SerializeField, Min(1f)] private float runningPlaybackSpeed = 1.5f;
    private int facing; // Down, Up, Right, Left: matches the controller's Facing parameter.
    private bool wasMoving;
    private static readonly int Facing = Animator.StringToHash("Facing");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int PlaybackSpeed = Animator.StringToHash("PlaybackSpeed");

    private void Reset()
    {
        player = GetComponent<PlayerController>();
        animator = GetComponentInChildren<Animator>();
    }
    private void Awake()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }
    private void Update()
    {
        Vector2 movement = player != null && player.isActiveAndEnabled ? player.MovementInput : Vector2.zero;
        ApplyMovement(movement, player != null && player.IsRunning);
    }
    private void ApplyMovement(Vector2 movement, bool running)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        bool moving = movement.sqrMagnitude > .0001f;
        if (moving)
            facing = Mathf.Abs(movement.x) > Mathf.Abs(movement.y) ? (movement.x > 0 ? 2 : 3) : (movement.y > 0 ? 1 : 0);
        animator.SetInteger(Facing, facing);
        animator.SetBool(IsMoving, moving);
        animator.SetFloat(PlaybackSpeed, moving && running ? runningPlaybackSpeed : 1f);
        // The Manual pauses time, so force the idle pose once instead of leaving a
        // walking frame on screen until gameplay resumes.
        if (!moving && wasMoving && Time.timeScale == 0f && animator.isInitialized)
            animator.Update(0f);
        wasMoving = moving;
    }
    private void OnDisable()
    {
        if (animator == null || !animator.isActiveAndEnabled || !animator.isInitialized) return;
        ApplyMovement(Vector2.zero, false);
        animator.Update(0f);
    }
}
