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

    // Initializes required references and starts hidden.
    void Awake()
    {
        EnsureSetup();
        HideNow();
    }

    // Resets state and subscribes to network disconnect events.
    void OnEnable()
    {
        HideNow();
        Subscribe();
    }

    // Unsubscribes and clears shown state when disabled.
    void OnDisable()
    {
        Unsubscribe();
        IsShown = false;
    }

    // Unsubscribes and clears shown state when destroyed.
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

    // Hooks the local disconnect callback when networking is available.
    void Subscribe()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientLeft;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientLeft;
    }

    // Unhooks the local disconnect callback when networking is available.
    void Unsubscribe()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientLeft;
    }

    // Shows the disconnect screen when this client is unexpectedly dropped.
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

    // Displays the disconnect UI and unlocks cursor control.
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

    // Hides the disconnect screen and restores HUD visibility.
    void HideNow()
    {
        IsShown = false;
        panel.SetActive(false);
        SetHud(true);
    }

    // Toggles the HUD canvas while the disconnect panel is active.
    void SetHud(bool isVisible)
    {
        hudCanvas.enabled = isVisible;
    }

    // Validates required disconnect screen references.
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
