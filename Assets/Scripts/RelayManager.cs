using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

// Creates and joins relay sessions, then starts Netcode.
public class RelayManager : MonoBehaviour
{
    private string gameSceneName = "Game";
    private bool isBusy;

    // Unhooks network callbacks when this object is destroyed.
    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientJoin;
        }
    }

    // Initializes Unity Services and authenticates anonymously.
    async Task InitServices()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    // Allocates relay data and starts hosting.
    public async Task CreateGame()
    {
        if (isBusy)
        {
            throw new InvalidOperationException("CreateGame failed: relay operation is already in progress.");
        }

        if (GameSession.Instance == null)
        {
            throw new InvalidOperationException("CreateGame failed: GameSession.Instance is missing.");
        }

        if (NetworkManager.Singleton == null)
        {
            throw new InvalidOperationException("CreateGame failed: NetworkManager.Singleton is missing.");
        }

        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            throw new InvalidOperationException("RelayManager setup failed: gameSceneName is empty.");
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport == null)
        {
            throw new InvalidOperationException("CreateGame failed: UnityTransport component is missing on NetworkManager.");
        }

        try
        {
            isBusy = true;
            await InitServices();
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            GameSession.Instance.JoinCode = joinCode;
            // Use host connection data for both ends on the host transport.
            transport.SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.ConnectionData);
            MuteSceneListeners();

            if (!NetworkManager.Singleton.StartHost())
            {
                throw new InvalidOperationException("CreateGame failed: NetworkManager.StartHost returned false.");
            }

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientJoin;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientJoin;
            // Let Netcode scene management transition everyone together.
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        finally
        {
            isBusy = false;
        }
    }

    // Joins a relay session and starts the client.
    public async Task JoinGame(string code)
    {
        if (isBusy)
        {
            throw new InvalidOperationException("JoinGame failed: relay operation is already in progress.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("JoinGame failed: join code is empty.", nameof(code));
        }

        if (NetworkManager.Singleton == null)
        {
            throw new InvalidOperationException("JoinGame failed: NetworkManager.Singleton is missing.");
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport == null)
        {
            throw new InvalidOperationException("JoinGame failed: UnityTransport component is missing on NetworkManager.");
        }

        string cleanedCode = code.Trim().ToUpperInvariant();

        try
        {
            isBusy = true;
            await InitServices();
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(cleanedCode);
            // Client uses host connection payload received from relay join allocation.
            transport.SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData);
            MuteSceneListeners();

            if (!NetworkManager.Singleton.StartClient())
            {
                throw new InvalidOperationException("JoinGame failed: NetworkManager.StartClient returned false.");
            }
        }
        finally
        {
            isBusy = false;
        }
    }

    // Starts ownership enforcement for a newly connected client.
    void OnClientJoin(ulong clientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        StartCoroutine(EnsureOwner(clientId));
    }

    // Waits until the player object exists and fixes ownership if needed.
    IEnumerator EnsureOwner(ulong clientId)
    {
        const int maxFrames = 120;
        int frameCount = 0;

        // Wait briefly for PlayerObject spawn before forcing ownership correction.
        while (frameCount++ < maxFrames)
        {
            if (NetworkManager.Singleton == null)
            {
                yield break;
            }

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var connectedClient))
            {
                NetworkObject playerObject = connectedClient.PlayerObject;

                if (playerObject != null)
                {
                    if (playerObject.OwnerClientId != clientId)
                    {
                        playerObject.ChangeOwnership(clientId);
                    }

                    yield break;
                }
            }

            yield return null;
        }
    }

    // Disables scene-local audio listeners before network scene changes.
    void MuteSceneListeners()
    {
        Scene currentScene = gameObject.scene;
        AudioListener[] listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (AudioListener listener in listeners)
        {
            if (listener == null || !listener.enabled)
            {
                continue;
            }

            if (listener.gameObject.scene != currentScene)
            {
                continue;
            }

            listener.enabled = false;
        }
    }
}
