using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    
    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 50f;
    [SerializeField] private float runSpeed = 80f;

    // Runtime vars
    private InputAction movementActionInput;
    private InputAction runningActionInput;
    private Rigidbody2D rb;

    // Set up actions
    void OnEnable()
    {
        movementActionInput.Enable();
        runningActionInput.Enable();
    }

    void OnDisable()
    {
        movementActionInput.Disable();
        runningActionInput.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        InputActionMap inputActionMap = inputActions.FindActionMap("Player");
        movementActionInput = inputActionMap.FindAction("Move");
        runningActionInput = inputActionMap.FindAction("Sprint");
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (movementActionInput == null) return;
        Vector2 movementInput = movementActionInput.ReadValue<Vector2>();

        float speed = walkSpeed;
        if (runningActionInput.IsPressed())
        {
            speed = runSpeed;
        }

        rb.linearVelocity = movementInput * speed * Time.deltaTime * 100;

        //if (movementInput.magnitude < 0.1f) rb.linearVelocity = Vector2.zero; // Force Stop if no input
    }
}
