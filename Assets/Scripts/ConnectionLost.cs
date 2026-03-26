using System;
using UnityEngine;
using Unity.Netcode;

// Shows a disconnect screen when the network session drops.
public class ConnectionLost : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Canvas hudCanvas;

    // Tracks whether the disconnect screen is currently shown.
    public static bool IsShown;
    void Awake()
    {
        EnsureSetup();
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

    // Returns to the main menu from the disconnect screen.
    public void ReturnToMainMenu()
    {
        HideNow();
        MenuActions.ReturnToMainMenu();
    }

    // Exits the game from the disconnect screen.
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

        if (EndScreen.IsShown)
        {
            return;
        }

        Show();
    }

    void Show()
    {
        if (EndScreen.IsShown)
        {
            return;
        }

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
        OptionsMenu.StopGameMusic();
        panel.SetActive(true);
        SetHud(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void HideNow()
    {
        IsShown = false;
        panel.SetActive(false);
        SetHud(true);
    }

    void SetHud(bool isVisible)
    {
        hudCanvas.enabled = isVisible;
    }

    void EnsureSetup()
    {
        if (panel == null)
        {
            throw new InvalidOperationException("ConnectionLost setup failed: panel reference is missing.");
        }

        if (hudCanvas == null)
        {
            throw new InvalidOperationException("ConnectionLost setup failed: hudCanvas reference is missing.");
        }
    }
}
