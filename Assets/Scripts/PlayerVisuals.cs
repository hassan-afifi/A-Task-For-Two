using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkAnimator))]
[RequireComponent(typeof(PlayerMovement))]
[DefaultExecutionOrder(-10000)]
public class PlayerVisuals : NetworkBehaviour
{
    private const string DefaultPlayerName = "Player";

    [SerializeField] private NetworkAnimator networkAnimator;
    [SerializeField] private Transform nameTagRoot;
    [SerializeField] private TMP_Text nameTagText;
    [SerializeField] private string tagAnchorName = "NameTagAnchor";
    [SerializeField] private int playerLayer = -1;

    private readonly List<GameObject> characterVisuals = new List<GameObject>();
    private readonly List<Animator> characterAnimators = new List<Animator>();
    private readonly NetworkVariable<int> netChar = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private readonly NetworkVariable<FixedString64Bytes> netName = new NetworkVariable<FixedString64Bytes>(
        new FixedString64Bytes(DefaultPlayerName),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Transform currentTagAnchor;
    private PlayerMovement movement;
    private Animator activeAnimator;

    public Animator ActiveAnimator => activeAnimator;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        networkAnimator ??= GetComponent<NetworkAnimator>();
        CacheChars();
        if (networkAnimator != null && networkAnimator.Animator == null && characterAnimators.Count > 0)
        {
            networkAnimator.Animator = characterAnimators[0];
        }
        ShowChar(0);
    }

    public override void OnNetworkSpawn()
    {
        EnsureSetup();
        CacheChars();
        netChar.OnValueChanged += OnChar;
        netName.OnValueChanged += OnName;
        ShowChar(netChar.Value);
        ShowName(netName.Value.ToString());

        if (IsOwner)
        {
            int localChar = SavedChar();
            ShowChar(localChar);

            if (IsServer)
            {
                netChar.Value = Wrap(localChar, characterVisuals.Count);
            }
            else
            {
                SetCharServerRpc(localChar);
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

            if (playerLayer >= 0)
            {
                SetLayers(gameObject, playerLayer);
            }

            if (nameTagRoot != null)
            {
                nameTagRoot.gameObject.SetActive(false);
            }
        }
        else if (nameTagRoot != null)
        {
            nameTagRoot.gameObject.SetActive(true);
        }

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        netChar.OnValueChanged -= OnChar;
        netName.OnValueChanged -= OnName;
        base.OnNetworkDespawn();
    }

    void LateUpdate()
    {
        if (IsOwner || !IsSpawned || nameTagRoot == null)
        {
            return;
        }

        UpdateTagPos();
        FaceTag();
    }

    void UpdateTagPos()
    {
        if (currentTagAnchor == null)
        {
            return;
        }

        Vector3 tagPosition = nameTagRoot.position;
        tagPosition.y = currentTagAnchor.position.y;
        nameTagRoot.position = tagPosition;
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
        characterAnimators.Clear();

        foreach (Transform child in transform)
        {
            if (child.GetComponentInChildren<Camera>(true) != null)
            {
                continue;
            }

            Animator childAnimator = child.GetComponentInChildren<Animator>(true);
            if (childAnimator == null)
            {
                continue;
            }

            characterVisuals.Add(child.gameObject);
            characterAnimators.Add(childAnimator);
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

    void ShowChar(int index)
    {
        if (characterVisuals.Count == 0)
        {
            throw new InvalidOperationException("PlayerVisuals setup failed: no character visual roots were found.");
        }

        int wrappedIndex = Wrap(index, characterVisuals.Count);

        for (int i = 0; i < characterVisuals.Count; i++)
        {
            characterVisuals[i].SetActive(i == wrappedIndex);
        }

        Transform currentCharacterRoot = characterVisuals[wrappedIndex].transform;
        Animator selectedAnimator = characterAnimators[wrappedIndex];
        currentTagAnchor = FindTagAnchor(currentCharacterRoot);
        activeAnimator = selectedAnimator;

        if (currentTagAnchor == null)
        {
            throw new InvalidOperationException($"PlayerVisuals setup failed: '{tagAnchorName}' is missing on active character '{currentCharacterRoot.name}'.");
        }

        if (networkAnimator == null)
        {
            throw new InvalidOperationException("PlayerVisuals setup failed: NetworkAnimator reference is missing.");
        }

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

    Transform FindTagAnchor(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == tagAnchorName)
            {
                return allTransforms[i];
            }
        }

        return null;
    }

    string SavedName()
    {
        if (GameSession.Instance == null)
        {
            return DefaultPlayerName;
        }

        return CleanName(GameSession.Instance.PlayerName);
    }

    string CleanName(string value)
    {
        string cleaned = string.IsNullOrWhiteSpace(value) ? DefaultPlayerName : value.Trim();

        if (cleaned.Length > 10)
        {
            cleaned = cleaned.Substring(0, 10);
        }

        return cleaned;
    }

    void ShowName(string value)
    {
        nameTagText.text = CleanName(value);
    }

    void FaceTag()
    {
        Camera currentCamera = MainCamera();

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

    Camera MainCamera()
    {
        if (PlayerMovement.LocalCamera != null && PlayerMovement.LocalCamera.isActiveAndEnabled)
        {
            return PlayerMovement.LocalCamera;
        }

        return Camera.main;
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

    void EnsureSetup()
    {
        if (movement == null)
        {
            throw new InvalidOperationException("PlayerVisuals setup failed: PlayerMovement component is missing.");
        }

        if (networkAnimator == null)
        {
            throw new InvalidOperationException("PlayerVisuals setup failed: NetworkAnimator reference is missing.");
        }

        if (nameTagRoot == null)
        {
            throw new InvalidOperationException("PlayerVisuals setup failed: nameTagRoot reference is missing.");
        }

        if (nameTagText == null)
        {
            throw new InvalidOperationException("PlayerVisuals setup failed: nameTagText reference is missing.");
        }

        if (string.IsNullOrWhiteSpace(tagAnchorName))
        {
            throw new InvalidOperationException("PlayerVisuals setup failed: tagAnchorName is empty.");
        }
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
