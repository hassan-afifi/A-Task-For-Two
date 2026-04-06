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

    // Validates setup and binds menu input callbacks.
    void Awake()
    {
        EnsureSetup();
        input = new InputActions();
        nameInput.onValueChanged.AddListener(OnInput);
        codeInput.onValueChanged.AddListener(OnInput);
        UpdateButtons();
    }

    // Enables menu input and refreshes visible UI state.
    void OnEnable()
    {
        input.System.Pause.performed += OnPauseInput;
        input.System.Enable();
        input.UI.Disable();
        LoadInputs();
        UpdateButtons();
        CloseOptions();
    }

    // Unsubscribes gameplay input when the menu is disabled.
    void OnDisable()
    {
        if (input != null)
        {
            input.System.Pause.performed -= OnPauseInput;
            input.System.Disable();
            input.UI.Disable();
        }
    }

    // Removes listeners and disposes input resources.
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

    // Saves typed name changes and refreshes button states.
    void OnInput(string _)
    {
        if (GameSession.Instance != null)
        {
            GameSession.Instance.SetName(nameInput.text);
        }

        UpdateButtons();
    }

    // Loads persisted input values into empty fields.
    void LoadInputs()
    {
        if (GameSession.Instance == null)
        {
            throw new InvalidOperationException("MainMenu.LoadInputs failed: GameSession.Instance is missing.");
        }

        // Only backfill from saved session when user has not typed anything yet.
        if (string.IsNullOrWhiteSpace(nameInput.text))
        {
            nameInput.SetTextWithoutNotify(GameSession.Instance.PlayerName ?? string.Empty);
        }
    }

    // Recomputes create and join button interactability.
    void UpdateButtons()
    {
        createGameButton.interactable = CanCreate();
        joinGameButton.interactable = CanJoin();
    }

    // Returns true when the name field is valid.
    bool CanCreate()
    {
        return HasLetter(nameInput.text);
    }

    // Returns true when name and join code are both valid.
    bool CanJoin()
    {
        return CanCreate() && ValidCode(codeInput.text);
    }

    // Checks whether input contains at least one letter.
    bool HasLetter(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Any(char.IsLetter);
    }

    // Validates that join code length is exactly six.
    bool ValidCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return code.Trim().Length == 6;
    }

    // Opens options with the pause shortcut when allowed.
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

    // Resets options UI tab when options are closed.
    void ResetOptionsTab()
    {
        OptionsMenu menu = FindOptionsMenu();

        if (menu != null)
        {
            menu.ResetTab();
        }
    }

    // Locates the options menu from related hierarchy objects.
    OptionsMenu FindOptionsMenu()
    {
        // Try direct component first for simple scene setups.
        OptionsMenu menu = optionsMenuPanel.GetComponent<OptionsMenu>();

        if (menu != null)
        {
            return menu;
        }

        // Then try nested options menu under the panel.
        menu = optionsMenuPanel.GetComponentInChildren<OptionsMenu>(true);

        if (menu != null)
        {
            return menu;
        }

        Transform parent = optionsMenuPanel.transform.parent;

        // Finally walk up parents for wrapper-heavy menu hierarchies.
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

    // Throws when required main menu references are missing.
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
