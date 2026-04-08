using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class MainPausePlayTests
{
    [SetUp]
    public void SetUp()
    {
        PauseMenu.isOpen = false;
        PlayerPrefs.DeleteAll();
        CleanupNetworkManagers();
    }

    [TearDown]
    public void TearDown()
    {
        PauseMenu.isOpen = false;
        DestroyAll<MainMenu>();
        DestroyAll<PauseMenu>();
        DestroyAll<GameSession>();
        CleanupNetworkManagers();
        PlayerPrefs.DeleteAll();
    }

    [Test]
    public void MainMenuOpenOptionsTest()
    {
        MainMenu menu = BuildMainMenu(out GameObject mainPanel, out GameObject optionsPanel, out _, out _);
        menu.OpenOptions();
        Assert.That(optionsPanel.activeSelf, Is.True);
        Assert.That(mainPanel.activeSelf, Is.False);
    }

    [Test]
    public void MainMenuCloseOptionsTest()
    {
        MainMenu menu = BuildMainMenu(out GameObject mainPanel, out GameObject optionsPanel, out _, out _);
        optionsPanel.SetActive(true);
        mainPanel.SetActive(false);
        menu.CloseOptions();
        Assert.That(optionsPanel.activeSelf, Is.False);
        Assert.That(mainPanel.activeSelf, Is.True);
    }

    [Test]
    public void OnCreateGameTest()
    {
        MainMenu menu = BuildMainMenu(out _, out _, out TMP_InputField nameInput, out _);
        nameInput.text = string.Empty;
        Assert.DoesNotThrow(() => menu.OnCreateGame());
    }

    [Test]
    public void OnJoinGameTest()
    {
        MainMenu menu = BuildMainMenu(out _, out _, out TMP_InputField nameInput, out TMP_InputField codeInput);
        nameInput.text = "A";
        codeInput.text = "12";
        Assert.DoesNotThrow(() => menu.OnJoinGame());
    }

    [Test]
    public void ContinueGameTest()
    {
        PauseMenu menu = BuildPauseMenu(out GameObject panel, out _, out _, out _, out _);
        PauseMenu.isOpen = true;
        panel.SetActive(true);
        menu.ContinueGame();
        Assert.That(PauseMenu.isOpen, Is.False);
        Assert.That(panel.activeSelf, Is.False);
        menu.ContinueGame();
        Assert.That(PauseMenu.isOpen, Is.False);
    }

    [Test]
    public void PauseMenuOpenOptionsTest()
    {
        PauseMenu menu = BuildPauseMenu(out _, out GameObject mainPanel, out GameObject optionsPanel, out _, out _);
        PauseMenu.isOpen = true;
        menu.OpenOptions();
        Assert.That(mainPanel.activeSelf, Is.False);
        Assert.That(optionsPanel.activeSelf, Is.True);
        PauseMenu.isOpen = false;
        menu.OpenOptions();
        Assert.That(optionsPanel.activeSelf, Is.True);
    }

    [Test]
    public void PauseMenuCloseOptionsTest()
    {
        PauseMenu menu = BuildPauseMenu(out _, out GameObject mainPanel, out GameObject optionsPanel, out _, out _);
        PauseMenu.isOpen = true;
        optionsPanel.SetActive(true);
        mainPanel.SetActive(false);
        menu.CloseOptions();
        Assert.That(mainPanel.activeSelf, Is.True);
        Assert.That(optionsPanel.activeSelf, Is.False);
        PauseMenu.isOpen = false;
        optionsPanel.SetActive(true);
        menu.CloseOptions();
        Assert.That(optionsPanel.activeSelf, Is.True);
    }

    [Test]
    public void CopyJoinCodeTest()
    {
        PauseMenu menu = BuildPauseMenu(out _, out _, out _, out _, out _);
        GameObject sessionGo = new GameObject("GameSessionTest");
        GameSession session = sessionGo.AddComponent<GameSession>();
        session.JoinCode = "ABC123";
        menu.CopyJoinCode();
        Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo("ABC123"));
        session.JoinCode = string.Empty;
        menu.CopyJoinCode();
        Assert.That(GUIUtility.systemCopyBuffer, Is.EqualTo("ABC123"));
    }

    [Test]
    public void ForceCloseTest()
    {
        PauseMenu menu = BuildPauseMenu(out GameObject panel, out _, out _, out _, out _);
        menu.gameObject.SetActive(true);
        PauseMenu.isOpen = true;
        panel.SetActive(true);
        Assert.DoesNotThrow(() => PauseMenu.ForceClose());
        Assert.That(PauseMenu.isOpen, Is.False);
        Assert.That(panel.activeSelf, Is.False);
        DestroyAll<PauseMenu>();
        PauseMenu.isOpen = true;
        Assert.DoesNotThrow(() => PauseMenu.ForceClose());
        Assert.That(PauseMenu.isOpen, Is.False);
    }

    [UnityTest]
    public IEnumerator ReturnToMainMenuTest()
    {
        EnsureGameSession();
        PauseMenu menu = BuildPauseMenu(out _, out _, out _, out _, out _);
        menu.ReturnToMainMenu();
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
    }

    private static MainMenu BuildMainMenu(out GameObject mainPanel, out GameObject optionsPanel, out TMP_InputField nameInput, out TMP_InputField codeInput)
    {
        GameObject root = new GameObject("MainMenuTestRoot");
        root.SetActive(false);
        MainMenu menu = root.AddComponent<MainMenu>();
        nameInput = CreateInput("NameInput");
        codeInput = CreateInput("CodeInput");
        Button createButton = new GameObject("CreateButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
        Button joinButton = new GameObject("JoinButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
        RelayManager relay = new GameObject("Relay", typeof(RelayManager)).GetComponent<RelayManager>();
        mainPanel = new GameObject("MainPanel");
        optionsPanel = new GameObject("OptionsPanel");
        SetField(menu, "nameInput", nameInput);
        SetField(menu, "codeInput", codeInput);
        SetField(menu, "createGameButton", createButton);
        SetField(menu, "joinGameButton", joinButton);
        SetField(menu, "relay", relay);
        SetField(menu, "mainMenuPanel", mainPanel);
        SetField(menu, "optionsMenuPanel", optionsPanel);
        return menu;
    }

    private static PauseMenu BuildPauseMenu(out GameObject panel, out GameObject mainPanel, out GameObject optionsPanel, out Canvas hudCanvas, out TMP_Text joinCodeText)
    {
        GameObject root = new GameObject("PauseMenuTestRoot");
        root.SetActive(false);
        PauseMenu menu = root.AddComponent<PauseMenu>();
        panel = new GameObject("Panel");
        mainPanel = new GameObject("Main");
        optionsPanel = new GameObject("Options");
        hudCanvas = new GameObject("HudCanvas", typeof(Canvas)).GetComponent<Canvas>();
        joinCodeText = new GameObject("JoinCode", typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        Animator copiedAnimator = new GameObject("CopiedAnimator", typeof(Animator)).GetComponent<Animator>();
        SetField(menu, "panel", panel);
        SetField(menu, "pauseMainPanel", mainPanel);
        SetField(menu, "optionsMenuPanel", optionsPanel);
        SetField(menu, "hudCanvas", hudCanvas);
        SetField(menu, "joinCodeText", joinCodeText);
        SetField(menu, "codeCopiedAnimator", copiedAnimator);
        SetField(menu, "input", new InputActions());
        return menu;
    }

    private static TMP_InputField CreateInput(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TMP_InputField));
        TMP_InputField input = go.GetComponent<TMP_InputField>();
        TextMeshProUGUI text = new GameObject($"{name}Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(go.transform, false);
        TextMeshProUGUI placeholder = new GameObject($"{name}Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        placeholder.transform.SetParent(go.transform, false);
        input.textComponent = text;
        input.placeholder = placeholder;
        input.text = string.Empty;
        return input;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static void CleanupNetworkManagers()
    {
        NetworkManager[] managers = UnityEngine.Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] == null)
            {
                continue;
            }

            managers[i].Shutdown();
            UnityEngine.Object.DestroyImmediate(managers[i].gameObject);
        }
    }

    private static void EnsureGameSession()
    {
        if (GameSession.Instance != null)
        {
            return;
        }

        new GameObject("GameSessionTest", typeof(GameSession));
    }

    private static void DestroyAll<T>() where T : UnityEngine.Object
    {
        T[] objects = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            if (objects[i] is Component component)
            {
                UnityEngine.Object.DestroyImmediate(component.gameObject);
                continue;
            }

            UnityEngine.Object.DestroyImmediate(objects[i]);
        }
    }
}
