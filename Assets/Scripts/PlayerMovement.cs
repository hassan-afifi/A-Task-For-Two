using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerInputHandler))]

// Handles local player movement, camera look, and crouch/jump logic.
public class PlayerMovement : NetworkBehaviour
{
    // Stores the active local player camera.
    public static Camera LocalCamera { get; private set; }
    [SerializeField] private Camera playerCamera;
    private float walkSpeed = 4f;

    // Multiplies base speed while sprinting.
    public float sprintMultiplier = 2f;
    private float gravity = -9.81f;
    private float gravityMultiplier = 2f;
    private float jumpPower = 8f;

    // Controls horizontal and vertical look sensitivity.
    public float mouseSensitivity = 0.1f;
    private float minLook = -80f;
    private float maxLook = 80f;
    private float standingHeight = 2.8f;
    private float crouchHeight = 1.8f;
    private float standingRadius = 0.8f;
    private float crouchRadius = 1.2f;

    // Multiplies base speed while crouching.
    public float crouchSpeedMultiplier = 0.5f;
    private float crouchLerpSpeed = 10f;
    private float jumpCooldown = 1.5f;
    private float lastJumpTime = -999f;
    private float maxJumpY = 0.2f;
    private CharacterController controller;
    private PlayerInputHandler inputHandler;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 finalMove;
    private float verticalVelocity;
    private float cameraPitch;

    // Indicates whether the player is currently crouching.
    public bool IsCrouching;

    // Becomes true on the frame a jump is triggered.
    public bool JumpTriggered;

    // Returns this player's camera reference.
    public Camera PlayerCamera => playerCamera;

    // Returns normalized local movement values for animation logic.
    public Vector3 FinalMove
    {
        get
        {
            Vector3 localVelocity = transform.InverseTransformDirection(controller.velocity);
            float baseSpeed = Mathf.Max(0.01f, walkSpeed);
            return new Vector3(Mathf.Clamp(localVelocity.x / baseSpeed, -2f, 2f), 0f, Mathf.Clamp(localVelocity.z / baseSpeed, -2f, 2f));
        }
    }

    // Returns whether sprint movement conditions are currently met.
    public bool IsRunning => inputHandler.SprintHeld && !IsCrouching && moveInput.sqrMagnitude > 0.0001f;

    // Returns whether the character controller is grounded.
    public bool IsGrounded => controller.isGrounded;
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputHandler = GetComponent<PlayerInputHandler>();
        playerCamera = GetComponentInChildren<Camera>(true);

        if (playerCamera == null)
        {
            throw new InvalidOperationException("PlayerMovement setup failed: player camera is missing.");
        }
    }

    // Initializes ownership-dependent state on network spawn.
    public override void OnNetworkSpawn()
    {
        if (!controller.enabled)
        {
            controller.enabled = true;
        }

        AudioListener listener = GetComponentInChildren<AudioListener>(true);

        if (IsOwner)
        {
            playerCamera.enabled = true;
            playerCamera.fieldOfView = OptionsMenu.SavedFov(playerCamera.fieldOfView);
            float sensPercent = OptionsMenu.SavedSensPct(mouseSensitivity * 100f);
            mouseSensitivity = OptionsMenu.SensMap(sensPercent);
            LocalCamera = playerCamera;

            if (listener != null)
            {
                listener.enabled = true;
            }
        }
        else
        {
            playerCamera.enabled = false;

            if (listener != null)
            {
                listener.enabled = false;
            }
        }

        if (IsServer || IsOwner)
        {
            Vector3 spawnPos = GetSpawn();
            StartCoroutine(SetSpawn(spawnPos));
        }

        base.OnNetworkSpawn();
    }

    // Clears shared camera state on network despawn.
    public override void OnNetworkDespawn()
    {
        if (IsOwner && LocalCamera == playerCamera)
        {
            LocalCamera = null;
        }

        base.OnNetworkDespawn();
    }

    void Update()
    {
        if (!IsSpawned || !IsOwner)
        {
            return;
        }

        if (!controller.enabled)
        {
            return;
        }

        if (ConnectionLost.IsShown)
        {
            return;
        }

        moveInput = inputHandler.MoveInput;
        lookInput = inputHandler.LookInput;
        HandleLook();
        HandleMovement();
        HandleJump();
        HandleCrouch();
    }

    void HandleLook()
    {
        if (!inputHandler.InputOn || PauseMenu.isOpen)
        {
            return;
        }

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minLook, maxLook);
        playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    void HandleMovement()
    {
        Vector2 input = Vector2.ClampMagnitude(moveInput, 1f);
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        float speed = walkSpeed;

        if (inputHandler.SprintHeld && !IsCrouching)
        {
            speed *= sprintMultiplier;
        }

        if (IsCrouching)
        {
            speed *= crouchSpeedMultiplier;
        }

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity += gravity * gravityMultiplier * Time.deltaTime;
        }

        finalMove = move * speed;
        finalMove.y = verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);
    }

    void HandleJump()
    {
        JumpTriggered = false;
        bool jumpPressed = inputHandler.ConsumeJump();
        if (jumpPressed &&
        controller.isGrounded &&
        !IsCrouching &&
        Time.time >= lastJumpTime + jumpCooldown &&
        transform.position.y < maxJumpY)
        {
            JumpTriggered = true;
            verticalVelocity = jumpPower;
            lastJumpTime = Time.time;
        }
        else if (jumpPressed && IsCrouching)
        {
            IsCrouching = false;
        }
    }

    void HandleCrouch()
    {
        bool crouchPressed = inputHandler.ConsumeCrouch();

        if (crouchPressed && controller.isGrounded && Time.time >= lastJumpTime + jumpCooldown)
        {
            IsCrouching = !IsCrouching;
        }

        float targetHeight = IsCrouching ? crouchHeight : standingHeight;
        float targetRadius = IsCrouching ? crouchRadius : standingRadius;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchLerpSpeed);
        controller.radius = Mathf.Lerp(controller.radius, targetRadius, Time.deltaTime * crouchLerpSpeed);
        controller.center = new Vector3(0f, controller.height / 2f, 0f);
        playerCamera.transform.localPosition = new Vector3(0f, controller.height - 0.3f, 0f);
    }

    IEnumerator SetSpawn(Vector3 spawnPos)
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(spawnPos, transform.rotation);
        yield return null;
        controller.enabled = true;
    }

    Vector3 GetSpawn()
    {
        return OwnerClientId == NetworkManager.ServerClientId ? new Vector3(6f, 0f, 0f) : new Vector3(-6f, 0f, 0f);
    }
}
