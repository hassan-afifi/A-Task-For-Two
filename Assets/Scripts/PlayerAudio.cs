using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAudio : NetworkBehaviour
{
    [SerializeField] private AudioSource loopAudioSource;
    [SerializeField] private AudioClip movementLoopClip;
    [SerializeField] private AudioClip landClip;

    private PlayerMovement playerMovement;
    private bool wasGrounded;
    private bool groundReady;
    private bool syncLoop;
    private float syncPitch = 1f;
    private bool loopOn;
    private bool jumpStarted;
    private bool jumpAirborne;
    private readonly List<ulong> targetClientIds = new List<ulong>(4);
    private readonly ulong[] singleTargetId = new ulong[1];

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            throw new InvalidOperationException("PlayerAudio setup failed: PlayerMovement component is missing.");
        }

        if (loopAudioSource == null)
        {
            loopAudioSource = GetComponent<AudioSource>();
        }

        if (loopAudioSource == null)
        {
            throw new InvalidOperationException("PlayerAudio setup failed: loop AudioSource reference is missing.");
        }

        InitLoop();
    }

    public override void OnNetworkSpawn()
    {
        if (playerMovement == null || loopAudioSource == null)
        {
            throw new InvalidOperationException("PlayerAudio setup failed before spawn.");
        }

        InitLoop();
        wasGrounded = playerMovement.IsGrounded;
        groundReady = true;
        syncLoop = false;
        syncPitch = 1f;
        loopOn = false;
        jumpStarted = false;
        jumpAirborne = false;
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        syncLoop = false;
        syncPitch = 1f;
        loopOn = false;
        jumpStarted = false;
        jumpAirborne = false;
        SetLoop(false, 1f);
        base.OnNetworkDespawn();
    }

    void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        if (IsOwner)
        {
            bool grounded = playerMovement.IsGrounded;
            bool menuBlocking = PauseMenu.isOpen || ConnectionLost.IsShown;
            Vector3 move = playerMovement.FinalMove;
            bool isMoving = (move.x * move.x + move.z * move.z) > 0.0001f;
            bool playMoveLoop = !menuBlocking && grounded && isMoving;
            float targetPitch = playMoveLoop ? MovePitch() : 1f;

            if (playerMovement.JumpTriggered)
            {
                jumpStarted = true;
            }

            if (jumpStarted && !grounded)
            {
                jumpAirborne = true;
            }

            bool justLanded = groundReady && !menuBlocking && grounded && !wasGrounded && jumpAirborne;
            bool loopStateChanged = LoopChanged(playMoveLoop, targetPitch);

            if (loopStateChanged)
            {
                syncLoop = playMoveLoop;
                syncPitch = targetPitch;
            }

            if (justLanded)
            {
                PlayLand();
                PushLand();
                jumpStarted = false;
                jumpAirborne = false;
            }
            else if (grounded && !playerMovement.JumpTriggered)
            {
                jumpStarted = false;
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
        if (playerMovement.IsRunning)
        {
            return Mathf.Max(0.01f, playerMovement.sprintMultiplier);
        }

        if (playerMovement.IsCrouching)
        {
            return Mathf.Max(0.01f, playerMovement.crouchSpeedMultiplier);
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

        targetClientIds.Clear();

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (clientId == OwnerClientId)
            {
                continue;
            }

            targetClientIds.Add(clientId);
        }

        if (targetClientIds.Count == 0)
        {
            return System.Array.Empty<ulong>();
        }

        if (targetClientIds.Count == 1)
        {
            singleTargetId[0] = targetClientIds[0];
            return singleTargetId;
        }

        return targetClientIds.ToArray();
    }

    void SetLoop(bool shouldPlay, float pitch)
    {
        loopAudioSource.volume = 1f;

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
        if (landClip == null)
        {
            return;
        }

        loopAudioSource.pitch = 1f;
        loopAudioSource.PlayOneShot(landClip, 1f);
    }

    void InitLoop()
    {
        loopAudioSource.playOnAwake = false;
        loopAudioSource.loop = true;

        if (movementLoopClip != null)
        {
            loopAudioSource.clip = movementLoopClip;
        }
    }
}
