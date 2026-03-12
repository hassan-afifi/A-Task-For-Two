using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PlayerController))]
public class PlayerAudio : NetworkBehaviour
{
    [SerializeField] private AudioSource loopAudioSource;
    [SerializeField] private AudioClip movementLoopClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] [Range(0f, 1f)] private float movementLoopVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float landVolume = 1f;

    private PlayerController playerController;
    private bool wasGrounded;
    private bool groundReady;
    private bool syncLoop;
    private float syncPitch = 1f;
    private bool loopOn;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();

        InitLoop();
    }

    public override void OnNetworkSpawn()
    {
        InitLoop();
        wasGrounded = (playerController != null) && playerController.IsGrounded;
        groundReady = true;
        syncLoop = false;
        syncPitch = 1f;
        loopOn = false;
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        syncLoop = false;
        syncPitch = 1f;
        loopOn = false;
        SetLoop(false, 1f);
        base.OnNetworkDespawn();
    }

    void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        if (IsOwner && playerController != null)
        {
            bool grounded = playerController.IsGrounded;
            bool menuBlocking = PauseMenu.isOpen || ConnectionLost.IsShown;
            bool isMoving = playerController.MoveInput.sqrMagnitude > 0.01f;
            bool shouldPlayMovementLoop = !menuBlocking && grounded && isMoving;
            float targetPitch = shouldPlayMovementLoop ? MovePitch() : 1f;
            bool justLanded = groundReady && !menuBlocking && grounded && !wasGrounded;
            bool loopStateChanged = LoopChanged(shouldPlayMovementLoop, targetPitch);

            if (loopStateChanged)
            {
                syncLoop = shouldPlayMovementLoop;
                syncPitch = targetPitch;
            }

            if (justLanded)
            {
                PlayLand();
                PushLand();
            }

            if (loopStateChanged)
            {
                PushLoop(syncLoop, syncPitch);
            }

            wasGrounded = grounded;
            groundReady = true;
        }

        SetLoop(syncLoop, syncPitch);
    }

    [ServerRpc]
    void LoopServerRpc(bool shouldPlay, float pitch)
    {
        PushLoopClient(shouldPlay, pitch);
    }

    [ServerRpc]
    void PlayLandServerRpc()
    {
        SendLandClient();
    }

    [ClientRpc]
    void LoopClientRpc(bool shouldPlay, float pitch, ClientRpcParams clientRpcParams = default)
    {
        syncLoop = shouldPlay;
        syncPitch = pitch;
        SetLoop(syncLoop, syncPitch);
    }

    [ClientRpc]
    void PlayLandClientRpc(ClientRpcParams clientRpcParams = default)
    {
        PlayLand();
    }

    bool LoopChanged(bool shouldPlay, float pitch)
    {
        if (syncLoop != shouldPlay)
        {
            return true;
        }

        return shouldPlay && !Mathf.Approximately(syncPitch, pitch);
    }

    float MovePitch()
    {
        if (playerController == null)
        {
            return 1f;
        }

        if (playerController.IsRunning)
        {
            return Mathf.Max(0.01f, playerController.sprintMultiplier);
        }

        if (playerController.IsCrouching)
        {
            return Mathf.Max(0.01f, playerController.crouchSpeedMultiplier);
        }

        return 1f;
    }

    void PushLoop(bool shouldPlay, float pitch)
    {
        if (IsServer)
        {
            PushLoopClient(shouldPlay, pitch);
            return;
        }

        LoopServerRpc(shouldPlay, pitch);
    }

    void PushLand()
    {
        if (IsServer)
        {
            SendLandClient();
            return;
        }

        PlayLandServerRpc();
    }

    void PushLoopClient(bool shouldPlay, float pitch)
    {
        ulong[] targetClientIds = TargetIds();

        if (targetClientIds.Length == 0)
        {
            return;
        }

        LoopClientRpc(shouldPlay, pitch,
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = targetClientIds
                }
            }
        );
    }

    void SendLandClient()
    {
        if (landClip == null)
        {
            return;
        }

        ulong[] targetClientIds = TargetIds();

        if (targetClientIds.Length == 0)
        {
            return;
        }

        PlayLandClientRpc(
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = targetClientIds
                }
            }
        );
    }

    ulong[] TargetIds()
    {
        if (NetworkManager == null)
        {
            return System.Array.Empty<ulong>();
        }

        List<ulong> targetClientIds = new List<ulong>();

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (clientId == OwnerClientId)
            {
                continue;
            }

            targetClientIds.Add(clientId);
        }

        return targetClientIds.ToArray();
    }

    void SetLoop(bool shouldPlay, float pitch)
    {
        if (loopAudioSource == null)
        {
            return;
        }

        loopAudioSource.volume = movementLoopVolume;

        if (movementLoopClip == null)
        {
            loopAudioSource.pitch = 1f;

            if (loopOn && loopAudioSource.isPlaying)
            {
                loopAudioSource.Stop();
            }

            loopOn = false;
            return;
        }

        if (loopAudioSource.clip != movementLoopClip)
        {
            loopAudioSource.clip = movementLoopClip;
        }

        if (shouldPlay)
        {
            loopAudioSource.pitch = Mathf.Max(0.01f, pitch);

            if (loopOn)
            {
                if (!loopAudioSource.isPlaying)
                {
                    loopAudioSource.Play();
                }
                return;
            }

            if (loopAudioSource.isPlaying)
            {
                return;
            }

            loopAudioSource.Play();
            loopOn = true;
            return;
        }

        if (loopOn && loopAudioSource.isPlaying)
        {
            loopAudioSource.Stop();
        }

        loopAudioSource.pitch = 1f;
        loopOn = false;
    }

    void PlayLand()
    {
        if (loopAudioSource == null || landClip == null)
        {
            return;
        }

        loopAudioSource.pitch = 1f;
        loopAudioSource.PlayOneShot(landClip, landVolume);
    }

    void InitLoop()
    {
        if (loopAudioSource == null)
        {
            return;
        }

        loopAudioSource.playOnAwake = false;
        loopAudioSource.loop = true;

        if (movementLoopClip != null)
        {
            loopAudioSource.clip = movementLoopClip;
        }
    }
}
