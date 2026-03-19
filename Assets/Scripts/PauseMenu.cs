using System;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using TMPro;

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

    public static bool isOpen = false;
    public static event Action<bool> PauseStateChanged;

    void Awake()
    {
        input = new InputActions();
    }

    void OnEnable()
    {
        if (input == null)
        {
            input = new InputActions();
        }

        input.System.Pause.performed += OnPauseInput;
        input.System.Enable();
        input.UI.Disable();
        RefreshJoinCode(true);
    }

    void Toggle()
    {
        if (isOpen)
        {
            ResetCopy();
        }

        isOpen = !isOpen;

        if (panel != null)
        {
            panel.SetActive(isOpen);
        }

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

    public void ContinueGame()
    {
        if (isOpen)
        {
            Toggle();
        }
    }

    public void OpenOptions()
    {
        if (!isOpen)
        {
            return;
        }

        SetPages(showOptions: true);
    }

    public void CloseOptions()
    {
        if (!isOpen)
        {
            return;
        }

        ShowMain();
        ResetOptionsTab();
    }

    public void ReturnToMainMenu()
    {
        ResetPause();
        MenuActions.ReturnToMainMenu();
    }

    public void ExitGame()
    {
        ResetPause();
        MenuActions.ExitGame();
    }

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

    void OnDestroy()
    {
        ResetPause();

        if (input != null)
        {
            input.Dispose();
            input = null;
        }
    }

    public void CopyJoinCode()
    {
        if (GameSession.Instance == null || string.IsNullOrEmpty(GameSession.Instance.JoinCode))
        {
            return;
        }

        GUIUtility.systemCopyBuffer = GameSession.Instance.JoinCode;

        if (codeCopiedAnimator != null)
        {
            string copiedTrigger = Trigger(TriggerName.Copied);
            codeCopiedAnimator.ResetTrigger(copiedTrigger);
            codeCopiedAnimator.SetTrigger(copiedTrigger);
        }
    }

    void ResetPause()
    {
        bool wasOpen = isOpen;
        isOpen = false;
        ResetCopy();
        HidePages();
        SetHud(true);

        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (input != null)
        {
            input.UI.Disable();
        }

        if (wasOpen)
        {
            PauseStateChanged?.Invoke(false);
        }
    }

    void SetHud(bool isVisible)
    {
        if (hudCanvas == null)
        {
            return;
        }

        hudCanvas.enabled = isVisible;
    }

    void ShowMain()
    {
        SetPages(showOptions: false);
    }

    void HidePages()
    {
        if (pauseMainPanel != null)
        {
            pauseMainPanel.SetActive(false);
        }

        if (optionsMenuPanel != null)
        {
            optionsMenuPanel.SetActive(false);
        }
    }

    void SetPages(bool showOptions)
    {
        if (pauseMainPanel != null)
        {
            pauseMainPanel.SetActive(!showOptions);
        }

        if (optionsMenuPanel != null)
        {
            optionsMenuPanel.SetActive(showOptions);
        }
    }

    void ResetCopy()
    {
        if (codeCopiedAnimator == null)
        {
            return;
        }

        if (!codeCopiedAnimator.gameObject.activeInHierarchy)
        {
            return;
        }

        codeCopiedAnimator.Rebind();
        codeCopiedAnimator.Update(0f);
    }

    static string Trigger(TriggerName trigger)
    {
        switch (trigger)
        {
            case TriggerName.Copied:
                return "Copied";
            default:
                throw new ArgumentOutOfRangeException(nameof(trigger), trigger, null);
        }
    }

    void RefreshJoinCode(bool force)
    {
        if (joinCodeText == null)
        {
            return;
        }

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

    void ResetOptionsTab()
    {
        OptionsMenu menu = FindOptionsMenu();

        if (menu != null)
        {
            menu.ResetTab();
        }
    }

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

    void OnPauseInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
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
}
