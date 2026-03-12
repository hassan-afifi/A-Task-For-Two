using UnityEngine;
using Unity.Netcode;

public class ConnectionLost : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Canvas hudCanvas;

    public static bool IsShown;

    void Awake()
    {
        HideNow();
    }

    void OnEnable()
    {
        HideNow();
        Subscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
        IsShown = false;
    }

    void OnDestroy()
    {
        Unsubscribe();
        IsShown = false;
    }

    public void ReturnToMainMenu()
    {
        HideNow();
        MenuActions.ReturnToMainMenu();
    }

    public void ExitGame()
    {
        HideNow();
        MenuActions.ExitGame();
    }

    void Subscribe()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientLeft;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientLeft;
    }

    void Unsubscribe()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientLeft;
    }

    void OnClientLeft(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (NetworkManager.Singleton.ShutdownInProgress)
        {
            return;
        }

        if (NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        Show();
    }

    void Show()
    {
        if (IsShown)
        {
            return;
        }

        PauseMenu pauseMenu = UnityEngine.Object.FindFirstObjectByType<PauseMenu>();

        if (pauseMenu != null)
        {
            pauseMenu.CloseOptions();
            pauseMenu.ContinueGame();
        }

        PauseMenu.ForceClose();
        IsShown = true;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        SetHud(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void HideNow()
    {
        IsShown = false;

        if (panel != null)
        {
            panel.SetActive(false);
        }

        SetHud(true);
    }

    void SetHud(bool isVisible)
    {
        if (hudCanvas == null)
        {
            return;
        }

        hudCanvas.enabled = isVisible;
    }
}
