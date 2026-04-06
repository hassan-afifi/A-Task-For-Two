using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(AudioSource))]

// Animates and synchronizes die rolls over the network.
public class DieClickAnimate : NetworkBehaviour
{
    [Serializable]
    private struct FaceConfig
    {
        // Stores the local Euler rotation for this face.
        public Vector3 euler;

        // Stores the next face index after one roll.
        public int nextFace;
    }

    private const float Duration = 0.28f;
    [SerializeField] private Transform dieVisual;
    [SerializeField] private AudioSource rollAudioSource;
    [SerializeField] private AudioClip rollClip;

    private static readonly FaceConfig[] Faces =
    {
        new FaceConfig { euler = new Vector3(338f, 331f, 326f), nextFace = 1 },
        new FaceConfig { euler = new Vector3(22f, 29f, 326f), nextFace = 2 },
        new FaceConfig { euler = new Vector3(37.5f, 113f, 14.5f), nextFace = 3 },
        new FaceConfig { euler = new Vector3(0f, 180f, 40f), nextFace = 4 },
        new FaceConfig { euler = new Vector3(322.5f, 247f, 14.5f), nextFace = 5 },
        new FaceConfig { euler = new Vector3(338f, 331f, 146f), nextFace = 6 },
        new FaceConfig { euler = new Vector3(22f, 29f, 146f), nextFace = 7 },
        new FaceConfig { euler = new Vector3(37.5f, 113f, 194.5f), nextFace = 8 },
        new FaceConfig { euler = new Vector3(0f, 180f, 220f), nextFace = 9 },
        new FaceConfig { euler = new Vector3(322.5f, 247f, 194.5f), nextFace = 0 }
    };

    private readonly NetworkVariable<int> netFace = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> netLocked = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Vector3 baseLocalPosition;
    private int currentFace;
    private readonly Queue<int> pendingFaces = new Queue<int>();
    private Coroutine rollRoutine;
    private bool locked;

    // Returns the currently visible face index.
    public int CurrentFace => currentFace;

    // Returns whether rolling is currently locked.
    public bool Locked => locked;

    // Returns whether a roll animation or queued roll is currently active.
    public bool IsRolling => rollRoutine != null || pendingFaces.Count > 0;

    // Captures defaults and applies the initial face orientation.
    void Awake()
    {
        rollAudioSource ??= GetComponent<AudioSource>();
        EnsureSetup();
        baseLocalPosition = dieVisual.localPosition;
        currentFace = 0;
        locked = false;
        dieVisual.localRotation = FaceRotation(currentFace);
    }

    // Subscribes and syncs state when this object spawns on the network.
    public override void OnNetworkSpawn()
    {
        netLocked.OnValueChanged += OnLockedChanged;
        SyncFromNetworkState();
        base.OnNetworkSpawn();
    }

    // Unsubscribes from network callbacks when this object despawns.
    public override void OnNetworkDespawn()
    {
        netLocked.OnValueChanged -= OnLockedChanged;
        base.OnNetworkDespawn();
    }

    // Requests a single die roll if the die is not locked.
    public void RollDie()
    {
        if (locked)
        {
            return;
        }

        if (!IsSpawned)
        {
            QueueFace(Faces[currentFace].nextFace);
            return;
        }

        RequestRollServerRpc();
    }

    // Sets whether the die can be rolled.
    public void SetLocked(bool isLocked)
    {
        if (!IsSpawned)
        {
            ApplyLocked(isLocked);
            return;
        }

        if (IsServer)
        {
            netLocked.Value = isLocked;
            ApplyLocked(isLocked);
            return;
        }

        SetLockedServerRpc(isLocked);
    }

    // Validates required die animation references.
    void EnsureSetup()
    {
        if (dieVisual == null)
        {
            throw new InvalidOperationException("DieClickAnimate setup failed: dieVisual reference is missing.");
        }

        if (rollAudioSource == null)
        {
            throw new InvalidOperationException("DieClickAnimate setup failed: rollAudioSource reference is missing.");
        }

        if (rollClip == null)
        {
            throw new InvalidOperationException("DieClickAnimate setup failed: rollClip reference is missing.");
        }
    }

    // Processes queued roll steps one-by-one with position and rotation animation.
    IEnumerator ProcessQueue()
    {
        Vector3 backPosition = baseLocalPosition - new Vector3(0.2f, 0f, 0f);

        // Each queued roll runs as: back -> rotate -> forward.
        while (pendingFaces.Count > 0)
        {
            PlayRollSfx();
            yield return MoveLocalPosition(backPosition, Duration);
            int nextFace = pendingFaces.Dequeue();
            Quaternion targetRotation = FaceRotation(nextFace);
            yield return RotateLocalTo(targetRotation, Duration);
            yield return MoveLocalPosition(baseLocalPosition, Duration);
            currentFace = nextFace;
        }

        rollRoutine = null;
    }

    // Moves the die visual to a target local position over time.
    IEnumerator MoveLocalPosition(Vector3 target, float duration)
    {
        Vector3 from = dieVisual.localPosition;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / duration);
            // Smoothstep easing gives softer start/stop than linear lerp.
            float eased = alpha * alpha * (3f - 2f * alpha);
            dieVisual.localPosition = Vector3.LerpUnclamped(from, target, eased);
            yield return null;
        }

        dieVisual.localPosition = target;
    }

    // Rotates the die visual to a target local rotation over time.
    IEnumerator RotateLocalTo(Quaternion target, float duration)
    {
        Quaternion from = dieVisual.localRotation;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / duration);
            // Match position easing so motion timing feels consistent.
            float eased = alpha * alpha * (3f - 2f * alpha);
            dieVisual.localRotation = Quaternion.SlerpUnclamped(from, target, eased);
            yield return null;
        }

        dieVisual.localRotation = target;
    }

    // Returns the local rotation for a face index.
    Quaternion FaceRotation(int faceIndex)
    {
        return Quaternion.Euler(Faces[faceIndex].euler);
    }

    // Enqueues a new target face and starts queue processing if idle.
    void QueueFace(int nextFace)
    {
        if (nextFace < 0 || nextFace >= Faces.Length)
        {
            throw new InvalidOperationException("DieClickAnimate state failed: next face index is out of range.");
        }

        pendingFaces.Enqueue(nextFace);

        if (rollRoutine == null)
        {
            rollRoutine = StartCoroutine(ProcessQueue());
        }
    }

    // Applies lock state and clears pending rolls when locked.
    void ApplyLocked(bool isLocked)
    {
        locked = isLocked;

        if (!locked)
        {
            return;
        }

        pendingFaces.Clear();
    }

    // Applies replicated lock state changes.
    void OnLockedChanged(bool _, bool currentValue)
    {
        ApplyLocked(currentValue);
    }

    // Synchronizes local state from current network variables.
    void SyncFromNetworkState()
    {
        ApplyLocked(netLocked.Value);
        currentFace = netFace.Value;
        pendingFaces.Clear();

        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        dieVisual.localPosition = baseLocalPosition;
        dieVisual.localRotation = FaceRotation(currentFace);
    }

    // Handles roll requests on the server and broadcasts the next face.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RequestRollServerRpc()
    {
        if (netLocked.Value)
        {
            return;
        }

        // Server computes next face to keep progression authoritative.
        int nextFace = Faces[netFace.Value].nextFace;
        netFace.Value = nextFace;
        RollClientRpc(nextFace);
    }

    // Queues the received face roll on each client.
    [ClientRpc]
    void RollClientRpc(int nextFace)
    {
        QueueFace(nextFace);
    }

    // Handles lock state requests on the server.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void SetLockedServerRpc(bool isLocked)
    {
        netLocked.Value = isLocked;
    }

    // Plays the die roll sound effect.
    void PlayRollSfx()
    {
        rollAudioSource.PlayOneShot(rollClip, 1f);
    }
}
