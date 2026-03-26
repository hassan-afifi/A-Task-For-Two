using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.InputSystem;

// Controls the main menu flow and relay actions.
public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private Button createGameButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private RelayManager relay;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsMenuPanel;
    private InputActions input;
    void Awake()
    {
        EnsureSetup();
        input = new InputActions();
        nameInput.onValueChanged.AddListener(OnInput);
        codeInput.onValueChanged.AddListener(OnInput);
        UpdateButtons();
    }

    void OnEnable()
    {
        input.System.Pause.performed += OnPauseInput;
        input.System.Enable();
        input.UI.Disable();
        LoadInputs();
        UpdateButtons();
        CloseOptions();
    }

    void OnDisable()
    {
        if (input != null)
        {
            input.System.Pause.performed -= OnPauseInput;
            input.System.Disable();
            input.UI.Disable();
        }
    }

    void OnDestroy()
    {
        if (nameInput != null)
        {
            nameInput.onValueChanged.RemoveListener(OnInput);
        }

        if (codeInput != null)
        {
            codeInput.onValueChanged.RemoveListener(OnInput);
        }

        if (input != null)
        {
            input.Dispose();
        }
    }

    // Opens the options panel from the main menu.
    public void OpenOptions()
    {
        optionsMenuPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    // Closes the options panel and returns to the main menu panel.
    public void CloseOptions()
    {
        optionsMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        ResetOptionsTab();
    }

    // Exits play mode or the built application.
    public void ExitGame()
    {
        MenuActions.ExitGame();
    }

    // Starts hosting a new relay game.
    public async void OnCreateGame()
    {
        if (!CanCreate())
        {
            return;
        }

        if (GameSession.Instance == null)
        {
            throw new InvalidOperationException("OnCreateGame failed: GameSession is missing.");
        }

        GameSession.Instance.SetName(nameInput.text.Trim());
        await relay.CreateGame();
    }

    // Joins an existing relay game using the entered code.
    public async void OnJoinGame()
    {
        if (!CanJoin())
        {
            return;
        }

        if (GameSession.Instance == null)
        {
            throw new InvalidOperationException("OnJoinGame failed: GameSession is missing.");
        }

        string cleanedName = nameInput.text.Trim();
        string cleanedCode = codeInput.text.Trim().ToUpperInvariant();
        GameSession.Instance.SetName(cleanedName);
        GameSession.Instance.JoinCode = cleanedCode;
        await relay.JoinGame(cleanedCode);
    }

    void OnInput(string _)
    {
        if (GameSession.Instance != null)
        {
            GameSession.Instance.SetName(nameInput.text);
        }

        UpdateButtons();
    }

    void LoadInputs()
    {
        if (GameSession.Instance == null)
        {
            throw new InvalidOperationException("MainMenu.LoadInputs failed: GameSession.Instance is missing.");
        }

        if (string.IsNullOrWhiteSpace(nameInput.text))
        {
            nameInput.SetTextWithoutNotify(GameSession.Instance.PlayerName ?? string.Empty);
        }
    }

    void UpdateButtons()
    {
        createGameButton.interactable = CanCreate();
        joinGameButton.interactable = CanJoin();
    }

    bool CanCreate()
    {
        return HasLetter(nameInput.text);
    }

    bool CanJoin()
    {
        return CanCreate() && ValidCode(codeInput.text);
    }

    bool HasLetter(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Any(char.IsLetter);
    }

    bool ValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return code.Trim().Length == 6;
    }

    void OnPauseInput(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        if (optionsMenuPanel.activeInHierarchy)
        {
            return;
        }

        if (!mainMenuPanel.activeInHierarchy)
        {
            return;
        }

        OpenOptions();
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

    void EnsureSetup()
    {
        if (nameInput == null)
        {
            throw new InvalidOperationException("MainMenu setup failed: nameInput reference is missing.");
        }

        if (codeInput == null)
        {
            throw new InvalidOperationException("MainMenu setup failed: codeInput reference is missing.");
        }

        if (createGameButton == null)
        {
            throw new InvalidOperationException("MainMenu setup failed: createGameButton reference is missing.");
        }

        if (joinGameButton == null)
        {
            throw new InvalidOperationException("MainMenu setup failed: joinGameButton reference is missing.");
        }

        if (relay == null)
        {
            throw new InvalidOperationException("MainMenu setup failed: relay reference is missing.");
        }

        if (mainMenuPanel == null)
        {
            throw new InvalidOperationException("MainMenu setup failed: mainMenuPanel reference is missing.");
        }

        if (optionsMenuPanel == null)
        {
            throw new InvalidOperationException("MainMenu setup failed: optionsMenuPanel reference is missing.");
        }
    }
}
