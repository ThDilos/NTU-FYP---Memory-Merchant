using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    
    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeed = 4f;
    [SerializeField, Min(0f)] private float runSpeed = 7f;

    // Runtime vars
    private InputAction movementActionInput;
    private InputAction runningActionInput;
    private Rigidbody2D rb;
    public Vector2 MovementInput { get; private set; }
    public bool IsRunning { get; private set; }

    // Set up actions
    void OnEnable()
    {
        movementActionInput.Enable();
        runningActionInput.Enable();
    }

    void OnDisable()
    {
        MovementInput = Vector2.zero;
        IsRunning = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        movementActionInput.Disable();
        runningActionInput.Disable();
    }

    void Awake()
    {
        InputActionMap inputActionMap = inputActions.FindActionMap("Player");
        movementActionInput = inputActionMap.FindAction("Move");
        runningActionInput = inputActionMap.FindAction("Sprint");
        rb = GetComponent<Rigidbody2D>();
        if (GetComponent<PlayerFootstepAudio>() == null) gameObject.AddComponent<PlayerFootstepAudio>();
    }

    void Update()
    {
        bool playing = Time.timeScale > 0f;
        MovementInput = playing ? Vector2.ClampMagnitude(movementActionInput.ReadValue<Vector2>(), 1f) : Vector2.zero;
        IsRunning = playing && runningActionInput.IsPressed();
    }

    void FixedUpdate()
    {
        // Rigidbody velocity is already measured in world units per second.
        rb.linearVelocity = MovementInput * (IsRunning ? runSpeed : walkSpeed);
    }
}
