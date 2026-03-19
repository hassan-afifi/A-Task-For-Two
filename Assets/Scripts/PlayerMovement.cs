using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovement : NetworkBehaviour
{
    public static Camera LocalCamera { get; private set; }

    [SerializeField] private Camera playerCamera;

    private float walkSpeed = 4f;
    public float sprintMultiplier = 2f;
    private float gravity = -9.81f;
    private float gravityMultiplier = 2f;
    private float jumpPower = 8f;

    public float mouseSensitivity = 0.1f;
    private float minLook = -80f;
    private float maxLook = 80f;

    private float standingHeight = 2.8f;
    private float crouchHeight = 1.8f;
    private float standingRadius = 0.8f;
    private float crouchRadius = 1.2f;
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

    public bool IsCrouching;
    public bool JumpTriggered;

    public Camera PlayerCamera => playerCamera;

    public Vector3 FinalMove
    {
        get
        {
            if (controller == null)
            {
                return Vector3.zero;
            }

            Vector3 localVelocity = transform.InverseTransformDirection(controller.velocity);
            float baseSpeed = Mathf.Max(0.01f, walkSpeed);
            return new Vector3(
                Mathf.Clamp(localVelocity.x / baseSpeed, -2f, 2f),
                0f,
                Mathf.Clamp(localVelocity.z / baseSpeed, -2f, 2f)
            );
        }
    }

    public bool IsRunning => inputHandler != null && inputHandler.SprintHeld && !IsCrouching && moveInput.sqrMagnitude > 0.0001f;
    public bool IsGrounded => controller != null && controller.isGrounded;

    void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        playerCamera = GetComponentInChildren<Camera>(true);
    }

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            throw new InvalidOperationException("PlayerMovement setup failed: CharacterController is missing.");
        }

        if (inputHandler == null)
        {
            throw new InvalidOperationException("PlayerMovement setup failed: PlayerInputHandler is missing.");
        }

        if (!controller.enabled)
        {
            controller.enabled = true;
        }

        AudioListener listener = GetComponentInChildren<AudioListener>(true);

        if (IsOwner)
        {
            if (playerCamera == null)
            {
                throw new InvalidOperationException("PlayerMovement setup failed: owner player has no assigned camera.");
            }

            if (playerCamera != null)
            {
                playerCamera.enabled = true;
                playerCamera.fieldOfView = OptionsMenu.SavedFov(playerCamera.fieldOfView);
                float sensPercent = OptionsMenu.SavedSensPct(mouseSensitivity * 100f);
                mouseSensitivity = OptionsMenu.SensMap(sensPercent);
                LocalCamera = playerCamera;
            }

            if (listener != null)
            {
                listener.enabled = true;
            }
        }
        else
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
            }

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

        if (controller == null || !controller.enabled)
        {
            return;
        }

        if (ConnectionLost.IsShown)
        {
            return;
        }

        if (inputHandler != null)
        {
            moveInput = inputHandler.MoveInput;
            lookInput = inputHandler.LookInput;
        }
        else
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
        }

        HandleLook();
        HandleMovement();
        HandleJump();
        HandleCrouch();
    }

    void HandleLook()
    {
        if (inputHandler == null || !inputHandler.InputOn || PauseMenu.isOpen)
        {
            return;
        }

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minLook, maxLook);

        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        Vector2 input = Vector2.ClampMagnitude(moveInput, 1f);
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        float speed = walkSpeed;

        if (inputHandler != null && inputHandler.SprintHeld && !IsCrouching)
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
        bool jumpPressed = inputHandler != null && inputHandler.ConsumeJump();

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
        bool crouchPressed = inputHandler != null && inputHandler.ConsumeCrouch();

        if (crouchPressed && controller != null && controller.isGrounded && Time.time >= lastJumpTime + jumpCooldown)
        {
            IsCrouching = !IsCrouching;
        }

        float targetHeight = IsCrouching ? crouchHeight : standingHeight;
        float targetRadius = IsCrouching ? crouchRadius : standingRadius;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchLerpSpeed);
        controller.radius = Mathf.Lerp(controller.radius, targetRadius, Time.deltaTime * crouchLerpSpeed);
        controller.center = new Vector3(0f, controller.height / 2f, 0f);

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = new Vector3(0f, controller.height - 0.3f, 0f);
        }
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
