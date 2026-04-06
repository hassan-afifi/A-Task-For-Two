using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(NetworkObject))]

// Captures and exposes local player input state.
public class PlayerInputHandler : NetworkBehaviour
{
    private InputActions input;
    private bool inputOn;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool interactPressed;
    private bool sprintHeld;
    private bool crouchPressed;

    // Returns the current movement input vector.
    public Vector2 MoveInput => moveInput;

    // Returns the current look input vector.
    public Vector2 LookInput => lookInput;

    // Returns whether player input is currently enabled.
    public bool InputOn => inputOn;

    // Returns whether sprint input is currently held.
    public bool SprintHeld => sprintHeld;

    // Creates the input action map instance.
    void Awake()
    {
        input = new InputActions();
        EnsureInput();
    }

    // Configures input ownership state on network spawn.
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            PauseMenu.PauseStateChanged -= OnPause;
            PauseMenu.PauseStateChanged += OnPause;
            SetInput(!PauseMenu.isOpen && !ConnectionLost.IsShown);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            SetInput(false);
        }

        base.OnNetworkSpawn();
    }

    // Clears input ownership state on network despawn.
    public override void OnNetworkDespawn()
    {
        PauseMenu.PauseStateChanged -= OnPause;
        SetInput(false);
        base.OnNetworkDespawn();
    }

    // Subscribes to action callbacks when enabled.
    void OnEnable()
    {
        EnsureInput();
        input.Player.Jump.performed += OnJump;
        input.Player.Interact.performed += OnInteract;
        input.Player.Sprint.performed += OnSprint;
        input.Player.Sprint.canceled += OnSprintOff;
        input.Player.Crouch.performed += OnCrouch;
        SetInput(false);
    }

    // Unsubscribes action callbacks and disables input.
    void OnDisable()
    {
        EnsureInput();
        input.Player.Jump.performed -= OnJump;
        input.Player.Interact.performed -= OnInteract;
        input.Player.Sprint.performed -= OnSprint;
        input.Player.Sprint.canceled -= OnSprintOff;
        input.Player.Crouch.performed -= OnCrouch;
        SetInput(false);
    }

    // Disposes input resources when the object is destroyed.
    public override void OnDestroy()
    {
        PauseMenu.PauseStateChanged -= OnPause;

        if (input != null)
        {
            input.Dispose();
        }

        base.OnDestroy();
    }

    // Reads owner input each frame when input is active.
    void Update()
    {
        if (!IsOwner || !IsSpawned)
        {
            return;
        }

        if (ConnectionLost.IsShown || PauseMenu.isOpen)
        {
            if (inputOn)
            {
                SetInput(false);
            }

            return;
        }

        if (!inputOn)
        {
            SetInput(true);
        }

        moveInput = input.Player.Move.ReadValue<Vector2>();
        lookInput = input.Player.Look.ReadValue<Vector2>();
    }

    // Returns and clears the pending jump press.
    public bool ConsumeJump()
    {
        bool value = jumpPressed;
        jumpPressed = false;
        return value;
    }

    // Returns and clears the pending interact press.
    public bool ConsumeInteract()
    {
        bool value = interactPressed;
        interactPressed = false;
        return value;
    }

    // Returns and clears the pending crouch press.
    public bool ConsumeCrouch()
    {
        bool value = crouchPressed;
        crouchPressed = false;
        return value;
    }

    // Toggles input in response to pause state changes.
    void OnPause(bool isPaused)
    {
        if (!IsOwner)
        {
            return;
        }

        SetInput(!isPaused && !ConnectionLost.IsShown);
    }

    // Enables or disables gameplay input collection.
    void SetInput(bool enabled)
    {
        EnsureInput();

        if (inputOn == enabled)
        {
            return;
        }

        inputOn = enabled;

        if (enabled)
        {
            input.Player.Enable();
            return;
        }

        input.Player.Disable();
        ClearState();
    }

    // Validates that input actions were initialized.
    void EnsureInput()
    {
        if (input == null)
        {
            throw new InvalidOperationException("PlayerInputHandler setup failed: InputActions is not initialized.");
        }
    }

    // Clears cached input values when input is turned off.
    void ClearState()
    {
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        jumpPressed = false;
        interactPressed = false;
        sprintHeld = false;
        crouchPressed = false;
    }

    // Captures jump input when gameplay input is active.
    void OnJump(InputAction.CallbackContext _)
    {
        if (!inputOn || PauseMenu.isOpen || ConnectionLost.IsShown)
        {
            return;
        }

        jumpPressed = true;
    }

    // Captures interact input when gameplay input is active.
    void OnInteract(InputAction.CallbackContext _)
    {
        if (!inputOn || PauseMenu.isOpen || ConnectionLost.IsShown)
        {
            return;
        }

        interactPressed = true;
    }

    // Captures sprint held state when gameplay input is active.
    void OnSprint(InputAction.CallbackContext _)
    {
        if (!inputOn || PauseMenu.isOpen || ConnectionLost.IsShown)
        {
            return;
        }

        sprintHeld = true;
    }

    // Clears sprint held state on sprint cancel.
    void OnSprintOff(InputAction.CallbackContext _)
    {
        sprintHeld = false;
    }

    // Captures crouch input when gameplay input is active.
    void OnCrouch(InputAction.CallbackContext _)
    {
        if (!inputOn || PauseMenu.isOpen || ConnectionLost.IsShown)
        {
            return;
        }

        crouchPressed = true;
    }
}
