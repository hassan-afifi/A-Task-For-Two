using System;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using TMPro;

// Manages pause menu state, pages, and actions.
public class PauseMenu : MonoBehaviour
{
    private enum TriggerName
    {
        Copied
    }

    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject pauseMainPanel;
    [SerializeField] private GameObject optionsMenuPanel;
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private Animator codeCopiedAnimator;
    private InputActions input;
    private string lastJoinCode = string.Empty;

    // Indicates whether the pause menu is currently open.
    public static bool isOpen = false;

    // Notifies listeners when pause state changes.
    public static event Action<bool> PauseStateChanged;

    // Validates references and creates input action instances.
    void Awake()
    {
        EnsureSetup();
        input = new InputActions();
    }

    // Enables pause bindings and refreshes join code text.
    void OnEnable()
    {
        input.System.Pause.performed += OnPauseInput;
        input.System.Enable();
        input.UI.Disable();
        RefreshJoinCode(true);
    }

    // Toggles menu visibility and updates input and cursor states.
    void Toggle()
    {
        if (isOpen)
        {
            ResetCopy();
        }

        isOpen=!isOpen;
        panel.SetActive(isOpen);
        SetHud(!isOpen);

        if (isOpen)
        {
            RefreshJoinCode(true);
            ResetCopy();
            ShowMain();
            input.UI.Enable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            HidePages();
            input.UI.Disable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        PauseStateChanged?.Invoke(isOpen);
    }

    // Closes the pause menu and resumes gameplay.
    public void ContinueGame()
    {
        if (isOpen)
        {
            Toggle();
        }
    }

    // Opens the options page inside the pause menu.
    public void OpenOptions()
    {
        if (!isOpen)
        {
            return;
        }

        SetPages(showOptions: true);
    }

    // Closes the options page and shows the pause main page.
    public void CloseOptions()
    {
        if (!isOpen)
        {
            return;
        }

        ShowMain();
        ResetOptionsTab();
    }

    // Leaves the game and returns to the main menu.
    public void ReturnToMainMenu()
    {
        ResetPause();
        MenuActions.ReturnToMainMenu();
    }

    // Exits play mode or the built application.
    public void ExitGame()
    {
        ResetPause();
        MenuActions.ExitGame();
    }

    // Forces the pause menu closed from external systems.
    public static void ForceClose()
    {
        PauseMenu pauseMenu = UnityEngine.Object.FindFirstObjectByType<PauseMenu>();

        if (pauseMenu != null)
        {
            pauseMenu.ResetPause();
            return;
        }

        if (isOpen)
        {
            isOpen = false;
            PauseStateChanged?.Invoke(false);
        }
    }

    // Unsubscribes pause input and closes menu state when disabled.
    void OnDisable()
    {
        if (input != null)
        {
            input.System.Pause.performed -= OnPauseInput;
            input.UI.Disable();
            input.System.Disable();
        }

        ResetPause();
    }

    // Resets pause state and disposes input resources on destroy.
    void OnDestroy()
    {
        ResetPause();

        if (input != null)
        {
            input.Dispose();
        }
    }

    // Copies the current join code to the clipboard.
    public void CopyJoinCode()
    {
        if (GameSession.Instance == null || string.IsNullOrEmpty(GameSession.Instance.JoinCode))
        {
            return;
        }

        GUIUtility.systemCopyBuffer = GameSession.Instance.JoinCode;

        if (codeCopiedAnimator.runtimeAnimatorController != null)
        {
            string copiedTrigger = Trigger(TriggerName.Copied);
            codeCopiedAnimator.ResetTrigger(copiedTrigger);
            codeCopiedAnimator.SetTrigger(copiedTrigger);
        }
    }

    // Forces all pause UI state back to closed defaults.
    void ResetPause()
    {
        bool wasOpen = isOpen;
        isOpen = false;
        ResetCopy();
        HidePages();
        SetHud(true);
        SetActiveSafe(panel, false);

        if (input != null)
        {
            input.UI.Disable();
        }

        if (wasOpen)
        {
            PauseStateChanged?.Invoke(false);
        }
    }

    // Shows or hides the gameplay HUD canvas.
    void SetHud(bool isVisible)
    {
        if (hudCanvas != null)
        {
            hudCanvas.enabled = isVisible;
        }
    }

    // Shows the main pause page.
    void ShowMain()
    {
        SetPages(showOptions: false);
    }

    // Hides all pause pages.
    void HidePages()
    {
        SetActiveSafe(pauseMainPanel, false);
        SetActiveSafe(optionsMenuPanel, false);
    }

    // Switches between the main and options pause pages.
    void SetPages(bool showOptions)
    {
        SetActiveSafe(pauseMainPanel, !showOptions);
        SetActiveSafe(optionsMenuPanel, showOptions);
    }

    // Resets the copied-code animation state.
    void ResetCopy()
    {
        if (codeCopiedAnimator == null || codeCopiedAnimator.gameObject == null || !codeCopiedAnimator.gameObject.activeInHierarchy)
        {
            return;
        }

        codeCopiedAnimator.Rebind();
        codeCopiedAnimator.Update(0f);
    }

    // Maps trigger keys to animator trigger names.
    static string Trigger(TriggerName trigger)
    {
        switch (trigger)
        {
        case TriggerName.Copied: return "Copied";
            default: throw new ArgumentOutOfRangeException(nameof(trigger), trigger, null);
        }
    }

    // Updates join code text only when value changes or forced.
    void RefreshJoinCode(bool force)
    {
        string joinCode = string.Empty;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && GameSession.Instance != null)
        {
            joinCode = GameSession.Instance.JoinCode ?? string.Empty;
        }

        if (!force && string.Equals(lastJoinCode, joinCode, StringComparison.Ordinal))
        {
            return;
        }

        lastJoinCode = joinCode;
        joinCodeText.text = joinCode;
    }

    // Resets the embedded options menu to its default tab.
    void ResetOptionsTab()
    {
        OptionsMenu menu = FindOptionsMenu();

        if (menu != null)
        {
            menu.ResetTab();
        }
    }

    // Finds an options menu component near the options panel.
    OptionsMenu FindOptionsMenu()
    {
        if (optionsMenuPanel == null)
        {
            return null;
        }

        OptionsMenu menu = optionsMenuPanel.GetComponent<OptionsMenu>();

        if (menu != null)
        {
            return menu;
        }

        menu = optionsMenuPanel.GetComponentInChildren<OptionsMenu>(true);

        if (menu != null)
        {
            return menu;
        }

        Transform parent = optionsMenuPanel.transform.parent;

        while (parent != null)
        {
            menu = parent.GetComponent<OptionsMenu>();

            if (menu != null)
            {
                return menu;
            }

            parent = parent.parent;
        }

        return null;
    }

    // Handles pause input and routes to close options or toggle pause.
    void OnPauseInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.IsClient)
        {
            return;
        }

        if (ConnectionLost.IsShown)
        {
            return;
        }

        if (isOpen && optionsMenuPanel != null && optionsMenuPanel.activeSelf)
        {
            CloseOptions();
            return;
        }

        Toggle();
    }

    // Sets a GameObject active only when the reference exists.
    static void SetActiveSafe(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    // Throws when required pause menu references are missing.
    void EnsureSetup()
    {
        if (panel == null)
        {
            throw new InvalidOperationException("PauseMenu setup failed: panel reference is missing.");
        }

        if (pauseMainPanel == null)
        {
            throw new InvalidOperationException("PauseMenu setup failed: pauseMainPanel reference is missing.");
        }

        if (optionsMenuPanel == null)
        {
            throw new InvalidOperationException("PauseMenu setup failed: optionsMenuPanel reference is missing.");
        }

        if (hudCanvas == null)
        {
            throw new InvalidOperationException("PauseMenu setup failed: hudCanvas reference is missing.");
        }

        if (joinCodeText == null)
        {
            throw new InvalidOperationException("PauseMenu setup failed: joinCodeText reference is missing.");
        }

        if (codeCopiedAnimator == null)
        {
            throw new InvalidOperationException("PauseMenu setup failed: codeCopiedAnimator reference is missing.");
        }
    }
}
