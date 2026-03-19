using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.InputSystem;

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
        input = new InputActions();

        if (nameInput != null)
        {
            nameInput.onValueChanged.AddListener(OnInput);
        }

        if (codeInput != null)
        {
            codeInput.onValueChanged.AddListener(OnInput);
        }

        UpdateButtons();
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

        LoadInputs();
        UpdateButtons();
        CloseOptions();
    }

    void OnDisable()
    {
        if (input == null)
        {
            return;
        }

        input.System.Pause.performed -= OnPauseInput;
        input.System.Disable();
        input.UI.Disable();
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
            input = null;
        }
    }

    public void OpenOptions()
    {
        if (optionsMenuPanel != null)
        {
            optionsMenuPanel.SetActive(true);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
    }

    public void CloseOptions()
    {
        if (optionsMenuPanel != null)
        {
            optionsMenuPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        ResetOptionsTab();
    }

    public void ExitGame()
    {
        MenuActions.ExitGame();
    }

    public async void OnCreateGame()
    {
        if (!CanCreate())
        {
            return;
        }

        if (GameSession.Instance == null || relay == null)
        {
            throw new System.InvalidOperationException("OnCreateGame failed: GameSession or RelayManager is missing.");
        }

        GameSession.Instance.SetName(nameInput.text.Trim());
        await relay.CreateGame();
    }


    public async void OnJoinGame()
    {
        if (!CanJoin())
        {
            return;
        }

        if (GameSession.Instance == null || relay == null)
        {
            throw new System.InvalidOperationException("OnJoinGame failed: GameSession or RelayManager is missing.");
        }

        string cleanedName = nameInput.text.Trim();
        string cleanedCode = codeInput.text.Trim().ToUpperInvariant();
        GameSession.Instance.SetName(cleanedName);
        GameSession.Instance.JoinCode = cleanedCode;
        await relay.JoinGame(cleanedCode);
    }

    void OnInput(string _)
    {
        if (GameSession.Instance != null && nameInput != null)
        {
            GameSession.Instance.SetName(nameInput.text);
        }

        UpdateButtons();
    }

    void LoadInputs()
    {
        if (GameSession.Instance == null)
        {
            return;
        }

        if (nameInput != null && string.IsNullOrWhiteSpace(nameInput.text))
        {
            nameInput.SetTextWithoutNotify(GameSession.Instance.PlayerName ?? string.Empty);
        }
    }

    void UpdateButtons()
    {
        if (createGameButton != null)
        {
            createGameButton.interactable = CanCreate();
        }

        if (joinGameButton != null)
        {
            joinGameButton.interactable = CanJoin();
        }
    }

    bool CanCreate()
    {
        return HasLetter(nameInput != null ? nameInput.text : string.Empty);
    }

    bool CanJoin()
    {
        return CanCreate() && ValidCode(codeInput != null ? codeInput.text : string.Empty);
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

        if (optionsMenuPanel != null && optionsMenuPanel.activeInHierarchy)
        {
            return;
        }

        if (mainMenuPanel != null && !mainMenuPanel.activeInHierarchy)
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
}
