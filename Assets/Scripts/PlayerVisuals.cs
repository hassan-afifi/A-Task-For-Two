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

// Synchronizes selected character visuals and name tags.
public class PlayerVisuals : NetworkBehaviour
{
    private const string DefaultPlayerName = "Player";
    private const string TagAnchorName = "NameTagAnchor";
    private const int PlayerLayer = 3;
    [SerializeField] private NetworkAnimator networkAnimator;
    [SerializeField] private Transform nameTagRoot;
    [SerializeField] private TMP_Text nameTagText;
    private readonly List<GameObject> characterVisuals = new List<GameObject>();
    private readonly List<Animator> characterAnimators = new List<Animator>();
    private readonly NetworkVariable<int> netChar = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString64Bytes> netName = new NetworkVariable<FixedString64Bytes>(new FixedString64Bytes(DefaultPlayerName), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Transform currentTagAnchor;
    private PlayerMovement movement;
    private Animator activeAnimator;

    // Returns the animator of the currently active character.
    public Animator ActiveAnimator => activeAnimator;

    // Caches character visuals and prepares initial animator binding.
    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        networkAnimator ??= GetComponent<NetworkAnimator>();
        CacheChars();

        if (networkAnimator.Animator == null && characterAnimators.Count > 0)
        {
            networkAnimator.Animator = characterAnimators[0];
        }

        ShowChar(0);
    }

    // Applies synced character and name data on network spawn.
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
            // Owner pushes local persisted choices into replicated network variables.
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

            if (PlayerLayer >= 0)
            {
                // Hide local body meshes from the owner camera by layer routing.
                SetLayers(gameObject, PlayerLayer);
            }

            nameTagRoot.gameObject.SetActive(false);
        }
        else
        {
            nameTagRoot.gameObject.SetActive(true);
        }

        base.OnNetworkSpawn();
    }

    // Unsubscribes from network variables on network despawn.
    public override void OnNetworkDespawn()
    {
        netChar.OnValueChanged -= OnChar;
        netName.OnValueChanged -= OnName;
        base.OnNetworkDespawn();
    }

    // Keeps remote player name tags aligned and facing the local camera.
    void LateUpdate()
    {
        if (IsOwner || !IsSpawned)
        {
            return;
        }

        UpdateTagPos();
        FaceTag();
    }

    // Aligns the name tag position to the active anchor height.
    void UpdateTagPos()
    {
        Vector3 tagPosition = nameTagRoot.position;
        tagPosition.y = currentTagAnchor.position.y;
        nameTagRoot.position = tagPosition;
    }

    // Updates selected character index on the server.
    [ServerRpc]
    void SetCharServerRpc(int index)
    {
        netChar.Value = Wrap(index, characterVisuals.Count);
    }

    // Updates player name on the server.
    [ServerRpc]
    void SetNameServerRpc(FixedString64Bytes playerName)
    {
        netName.Value = new FixedString64Bytes(CleanName(playerName.ToString()));
    }

    // Rebuilds cached character roots and animator references.
    void CacheChars()
    {
        characterVisuals.Clear();
        characterAnimators.Clear();

        foreach (Transform child in transform)
        {
            // Skip camera containers and keep only actual character visual roots.
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

    // Applies replicated character selection changes.
    void OnChar(int _, int currentValue)
    {
        ShowChar(currentValue);
    }

    // Applies replicated player name changes.
    void OnName(FixedString64Bytes _, FixedString64Bytes currentValue)
    {
        ShowName(currentValue.ToString());
    }

    // Shows one character visual and updates animator/name anchor bindings.
    void ShowChar(int index)
    {
        if (characterVisuals.Count == 0)
        {
            throw new InvalidOperationException("PlayerVisuals setup failed: no character visual roots were found.");
        }

        int wrappedIndex = Wrap(index, characterVisuals.Count);

        // Keep exactly one visual root active at a time.
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
            throw new InvalidOperationException($"PlayerVisuals setup failed: '{TagAnchorName}' is missing on active character '{currentCharacterRoot.name}'.");
        }

        networkAnimator.Animator = selectedAnimator;
    }

    // Reads locally saved character index fallback.
    int SavedChar()
    {
        if (GameSession.Instance != null)
        {
            return GameSession.Instance.CharIndex;
        }

        return 0;
    }

    // Finds the name tag anchor under a character root.
    Transform FindTagAnchor(Transform root)
    {
        Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == TagAnchorName)
            {
                return allTransforms[i];
            }
        }

        return null;
    }

    // Reads locally saved player name fallback.
    string SavedName()
    {
        if (GameSession.Instance == null)
        {
            return DefaultPlayerName;
        }

        return CleanName(GameSession.Instance.PlayerName);
    }

    // Normalizes name text and applies default/length constraints.
    string CleanName(string value)
    {
        string cleaned = string.IsNullOrWhiteSpace(value) ? DefaultPlayerName : value.Trim();

        if (cleaned.Length > 10)
        {
            cleaned = cleaned.Substring(0, 10);
        }

        return cleaned;
    }

    // Writes normalized name text to the tag label.
    void ShowName(string value)
    {
        nameTagText.text = CleanName(value);
    }

    // Rotates remote name tag to face the current camera.
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

    // Returns the active local gameplay camera fallback.
    Camera MainCamera()
    {
        if (PlayerMovement.LocalCamera != null && PlayerMovement.LocalCamera.isActiveAndEnabled)
        {
            return PlayerMovement.LocalCamera;
        }

        return Camera.main;
    }

    // Wraps index values into a valid range.
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

    // Validates required visual sync references.
    void EnsureSetup()
    {
        if (nameTagRoot == null)
        {
            throw new InvalidOperationException("PlayerVisuals setup failed: nameTagRoot reference is missing.");
        }

        if (nameTagText == null)
        {
            throw new InvalidOperationException("PlayerVisuals setup failed: nameTagText reference is missing.");
        }
    }

    // Applies a layer recursively while excluding camera and name tag hierarchy.
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
