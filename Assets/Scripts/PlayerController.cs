using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;

[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkAnimator))]
public class PlayerController : NetworkBehaviour
{
    public Camera playerCamera;
    [SerializeField] private NetworkAnimator networkAnimator;
    [SerializeField] private Transform nameTagRoot;
    [SerializeField] private TMP_Text nameTagText;

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
    public float crouchSpeedMultiplier = 0.5f;
    private float crouchLerpSpeed = 10f;
    private float jumpCooldown = 1.5f;
    private float lastJumpTime = -999f;

    private InputActions input;
    private CharacterController controller;
    private readonly List<GameObject> characterVisuals = new List<GameObject>();
    private readonly NetworkVariable<int> netChar = new NetworkVariable<int>( 0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString64Bytes> netName = new NetworkVariable<FixedString64Bytes>(new FixedString64Bytes("Player"), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool inputOn;

    public Vector2 MoveInput;
    private Vector2 lookInput;
    private Vector3 finalMove;

    private float verticalVelocity;
    private float cameraPitch;

    private bool isSprinting;
    public bool IsCrouching;
    public bool JumpTriggered;
    private bool jumpPressed;

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
            return new Vector3(Mathf.Clamp(localVelocity.x / baseSpeed, -2f, 2f), 0f, Mathf.Clamp(localVelocity.z / baseSpeed, -2f, 2f));
        }
    }
    public bool IsRunning => isSprinting && !IsCrouching && MoveInput.sqrMagnitude > 0.0001f;
    public bool IsGrounded => controller != null && controller.isGrounded;

    void Awake()
    {
        input = new InputActions();
        playerCamera = GetComponentInChildren<Camera>(true);

        CacheChars();
        ShowChar(0);
    }

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            return;
        }

        if (!controller.enabled)
        {
            controller.enabled = true;
        }

        CacheChars();
        netChar.OnValueChanged += OnChar;
        ShowChar(netChar.Value);
        netName.OnValueChanged += OnName;
        ShowName(netName.Value.ToString());
        var listener = GetComponentInChildren<AudioListener>(true);

        if (IsOwner)
        {
            int localCharacterIndex = SavedChar();
            ShowChar(localCharacterIndex);

            if (IsServer)
            {
                netChar.Value = Wrap(localCharacterIndex, characterVisuals.Count);
            }
            else
            {
                SetCharServerRpc(localCharacterIndex);
            }

            string localName = SavedName();
            ShowName(localName);

            if (IsServer)
            {
                netName.Value = new FixedString64Bytes(localName);
            }
            else
            {
                SetNameServerRpc(new FixedString64Bytes(localName));
            }

            PauseMenu.PauseStateChanged -= OnPause;
            PauseMenu.PauseStateChanged += OnPause;
            SetInput(!PauseMenu.isOpen);

            if (playerCamera != null)
            {
                playerCamera.fieldOfView = OptionsMenu.SavedFov(playerCamera.fieldOfView);
            }

            int playerLayer = LayerMask.NameToLayer("Player");

            if (playerLayer >= 0)
            {
                SetLayers(gameObject, playerLayer);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (listener != null)
            {
                listener.enabled = true;
            }

            if (nameTagRoot != null)
            {
                nameTagRoot.gameObject.SetActive(false);
            }
        }
        else
        {
            PauseMenu.PauseStateChanged -= OnPause;
            SetInput(false);

            if (playerCamera != null)
            {
                playerCamera.enabled = false;
            }

            if (listener != null)
            {
                listener.enabled = false;
            }

            if (nameTagRoot != null)
            {
                nameTagRoot.gameObject.SetActive(true);
            }
        }

        if (IsServer || IsOwner)
        {
            Vector3 spawnPos = GetSpawn();
            StartCoroutine(SetSpawn(spawnPos));
        }
    }

    public override void OnNetworkDespawn()
    {
        PauseMenu.PauseStateChanged -= OnPause;
        SetInput(false);
        netChar.OnValueChanged -= OnChar;
        netName.OnValueChanged -= OnName;
        base.OnNetworkDespawn();
    }

    void OnEnable()
    {
        input.Player.Jump.performed += OnJump;
        input.Player.Sprint.performed += OnSprint;
        input.Player.Sprint.canceled += OnSprintOff;
        input.Player.Crouch.performed += OnCrouch;
        SetInput(false);
    }

    void OnDisable()
    {
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
        if (!IsOwner || controller == null || !controller.enabled)
        {
            return;
        }

        if (PauseMenu.isOpen || ConnectionLost.IsShown)
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

        ReadInput();
        HandleLook();
        HandleMovement();
        HandleJump();
        HandleCrouchHeight();
    }

    void LateUpdate()
    {
        FaceTagToCamera();
    }

    void ReadInput()
    {
        MoveInput = input.Player.Move.ReadValue<Vector2>();
        lookInput = input.Player.Look.ReadValue<Vector2>();
    }

    void HandleLook()
    {
        if (PauseMenu.isOpen)
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
        Vector2 input = Vector2.ClampMagnitude(MoveInput, 1f);
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        float speed = walkSpeed;

        if (isSprinting && !IsCrouching)
        {
            speed *= sprintMultiplier;
        }

        if (IsCrouching)
        {
            speed *= crouchSpeedMultiplier;
        }

        if (controller.isGrounded && verticalVelocity < 0)
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

        if (jumpPressed && controller.isGrounded && !IsCrouching && Time.time >= lastJumpTime + jumpCooldown)
        {
            JumpTriggered = true;
            verticalVelocity = jumpPower;      
            lastJumpTime = Time.time;
        }
        else if (jumpPressed && IsCrouching)
        { 
            IsCrouching = false; 
        }

        jumpPressed = false;
    }


    void HandleCrouchHeight()
    {
        float targetHeight = IsCrouching ? crouchHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchLerpSpeed);
        controller.center = new Vector3(0, controller.height / 2f, 0);

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition = new Vector3(0, controller.height - 0.3f, 0);
        }
    }

    IEnumerator SetSpawn(Vector3 spawnPos)
    {
        if (controller == null)
        {
            yield break;
        }

        controller.enabled = false;
        transform.SetPositionAndRotation(spawnPos, transform.rotation);
        yield return null;
        controller.enabled = true;
    }

    Vector3 GetSpawn()
    {
        return OwnerClientId == NetworkManager.ServerClientId ? new Vector3(6f, 0f, 0f) : new Vector3(-6f, 0f, 0f);
    }

    [ServerRpc]
    void SetCharServerRpc(int index)
    {
        netChar.Value = Wrap(index, characterVisuals.Count);
    }

    [ServerRpc]
    void SetNameServerRpc(FixedString64Bytes playerName)
    {
        netName.Value = new FixedString64Bytes(CleanName(playerName.ToString()));
    }

    void CacheChars()
    {
        characterVisuals.Clear();

        foreach (Transform child in transform)
        {
            if (child.GetComponentInChildren<Camera>(true) != null)
            {
                continue;
            }

            if (child.GetComponentInChildren<Animator>(true) == null)
            {
                continue;
            }

            characterVisuals.Add(child.gameObject);
        }
    }

    void OnChar(int _, int currentValue)
    {
        ShowChar(currentValue);
    }

    void OnName(FixedString64Bytes _, FixedString64Bytes currentValue)
    {
        ShowName(currentValue.ToString());
    }

    void OnPause(bool isPaused)
    {
        if (!IsOwner)
        {
            return;
        }

        SetInput(!isPaused);
    }

    void ShowChar(int index)
    {
        if (characterVisuals.Count == 0)
        {
            return;
        }

        int wrappedIndex = Wrap(index, characterVisuals.Count);

        for (int i = 0; i < characterVisuals.Count; i++)
        {
            characterVisuals[i].SetActive(i == wrappedIndex);
        }

        if (networkAnimator == null)
        {
            return;
        }

        Animator selectedAnimator = characterVisuals[wrappedIndex].GetComponentInChildren<Animator>(true);

        if (selectedAnimator != null)
        {
            networkAnimator.Animator = selectedAnimator;
        }
    }

    int SavedChar()
    {
        if (GameSession.Instance != null)
        {
            return GameSession.Instance.CharIndex;
        }

        return 0;
    }

    string SavedName()
    {
        if (GameSession.Instance == null)
        {
            return "Player";
        }

        return CleanName(GameSession.Instance.PlayerName);
    }

    string CleanName(string value)
    {
        string cleaned = string.IsNullOrWhiteSpace(value) ? "Player" : value.Trim();

        if (cleaned.Length > 10)
        {
            cleaned = cleaned.Substring(0, 10);
        }

        return cleaned;
    }

    void ShowName(string value)
    {
        if (nameTagText == null)
        {
            return;
        }

        nameTagText.text = CleanName(value);
    }

    void FaceTagToCamera()
    {
        if (!IsSpawned || nameTagRoot == null)
        {
            return;
        }

        Camera currentCamera = Camera.main;

        if (currentCamera == null)
        {
            return;
        }

        Vector3 lookDirection = nameTagRoot.position - currentCamera.transform.position;

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        nameTagRoot.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
    }

    void SetInput(bool enabled)
    {
        if (input == null || inputOn == enabled)
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
        MoveInput = Vector2.zero;
        lookInput = Vector2.zero;
        jumpPressed = false;
        isSprinting = false;
    }

    int Wrap(int value, int length)
    {
        if (length <= 0)
        {
            return 0;
        }

        value %= length;
        
        if (value < 0)
        {
            value += length;
        }

        return value;
    }

    void OnJump(InputAction.CallbackContext _)
    {
        if (!inputOn || PauseMenu.isOpen)
        {
            return;
        }

        jumpPressed = true;
    }

    void OnSprint(InputAction.CallbackContext _)
    {
        if (!inputOn || PauseMenu.isOpen)
        {
            return;
        }

        isSprinting = true;
    }

    void OnSprintOff(InputAction.CallbackContext _)
    {
        isSprinting = false;
    }

    void OnCrouch(InputAction.CallbackContext _)
    {
        if (!inputOn || PauseMenu.isOpen)
        {
            return;
        }

        if (controller == null || !controller.isGrounded)
        {
            return;
        }

        if (Time.time < lastJumpTime + jumpCooldown)
        {
            return;
        }

        IsCrouching = !IsCrouching;
    }

    void SetLayers(GameObject obj, int newLayer)
    {
        if (obj.GetComponent<Camera>() != null || obj.transform == nameTagRoot || obj.transform.IsChildOf(nameTagRoot))
        {
            return;
        }


        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayers(child.gameObject, newLayer);
        }
    }
}
