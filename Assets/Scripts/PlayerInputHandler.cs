using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(NetworkObject))]
public class PlayerInputHandler : NetworkBehaviour
{
    private InputActions input;
    private bool inputOn;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool sprintHeld;
    private bool crouchPressed;

    public Vector2 MoveInput => moveInput;
    public Vector2 LookInput => lookInput;
    public bool InputOn => inputOn;
    public bool SprintHeld => sprintHeld;

    void Awake()
    {
        input = new InputActions();
        EnsureInput();
    }

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

    public override void OnNetworkDespawn()
    {
        PauseMenu.PauseStateChanged -= OnPause;
        SetInput(false);
        base.OnNetworkDespawn();
    }

    void OnEnable()
    {
        EnsureInput();

        input.Player.Jump.performed += OnJump;
        input.Player.Sprint.performed += OnSprint;
        input.Player.Sprint.canceled += OnSprintOff;
        input.Player.Crouch.performed += OnCrouch;
        SetInput(false);
    }

    void OnDisable()
    {
        EnsureInput();

        input.Player.Jump.performed -= OnJump;
        input.Player.Sprint.performed -= OnSprint;
        input.Player.Sprint.canceled -= OnSprintOff;
        input.Player.Crouch.performed -= OnCrouch;
        SetInput(false);
    }

    public override void OnDestroy()
    {
        PauseMenu.PauseStateChanged -= OnPause;

        if (input != null)
        {
            input.Dispose();
        }

        base.OnDestroy();
    }

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

    public bool ConsumeJump()
    {
        bool value = jumpPressed;
        jumpPressed = false;
        return value;
    }

    public bool ConsumeCrouch()
    {
        bool value = crouchPressed;
        crouchPressed = false;
        return value;
    }

    void OnPause(bool isPaused)
    {
        if (!IsOwner)
        {
            return;
        }

        SetInput(!isPaused && !ConnectionLost.IsShown);
    }

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

    void EnsureInput()
    {
        if (input == null)
        {
            throw new InvalidOperationException("PlayerInputHandler setup failed: InputActions is not initialized.");
        }
    }

    void ClearState()
    {
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        jumpPressed = false;
        sprintHeld = false;
        crouchPressed = false;
    }

    void OnJump(InputAction.CallbackContext _)
    {
        if (!inputOn || PauseMenu.isOpen || ConnectionLost.IsShown)
        {
            return;
        }

        jumpPressed = true;
    }

    void OnSprint(InputAction.CallbackContext _)
    {
        if (!inputOn || PauseMenu.isOpen || ConnectionLost.IsShown)
        {
            return;
        }

        sprintHeld = true;
    }

    void OnSprintOff(InputAction.CallbackContext _)
    {
        sprintHeld = false;
    }

    void OnCrouch(InputAction.CallbackContext _)
    {
        if (!inputOn || PauseMenu.isOpen || ConnectionLost.IsShown)
        {
            return;
        }

        crouchPressed = true;
    }
}
