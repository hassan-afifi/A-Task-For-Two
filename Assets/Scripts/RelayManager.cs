using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class RelayManager : MonoBehaviour
{
    private bool isBusy;

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientJoin;
        }
    }

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

    public async Task CreateGame()
    {
        if (isBusy)
        {
            return;
        }

        if (GameSession.Instance == null)
        {
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport == null)
        {
            return;
        }

        try
        {
            isBusy = true;
            await InitServices();
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            GameSession.Instance.JoinCode = joinCode;
            transport.SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.ConnectionData);
            MuteSceneListeners();

            if (!NetworkManager.Singleton.StartHost())
            {
                return;
            }

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientJoin;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientJoin;
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
        finally
        {
            isBusy = false;
        }
    }

    public async Task JoinGame(string code)
    {
        if (isBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport == null)
        {
            return;
        }

        string cleanedCode = code.Trim().ToUpperInvariant();

        try
        {
            isBusy = true;
            await InitServices();
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(cleanedCode);
            transport.SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData);
            MuteSceneListeners();

            if (!NetworkManager.Singleton.StartClient())
            {
                return;
            }
        }
        finally
        {
            isBusy = false;
        }
    }

    void OnClientJoin(ulong clientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        StartCoroutine(EnsureOwner(clientId));
    }

    System.Collections.IEnumerator EnsureOwner(ulong clientId)
    {
        const int maxFrames = 120;
        int frameCount = 0;

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
