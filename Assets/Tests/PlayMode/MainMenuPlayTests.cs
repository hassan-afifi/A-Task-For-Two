using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class MainMenuPlayTests
{
    [UnityTest]
    public IEnumerator OpenCloseOptions_SwitchesPanels()
    {
        var root = new GameObject("MainMenu_Test");
        var mainPanel = new GameObject("MainPanel");
        var optionsPanel = new GameObject("OptionsPanel");
        mainPanel.transform.SetParent(root.transform, false);
        optionsPanel.transform.SetParent(root.transform, false);

        var menu = root.AddComponent<MainMenu>();
        SetField(menu, "mainMenuPanel", mainPanel);
        SetField(menu, "optionsMenuPanel", optionsPanel);

        menu.OpenOptions();
        Assert.That(mainPanel.activeSelf, Is.False);
        Assert.That(optionsPanel.activeSelf, Is.True);

        menu.CloseOptions();
        Assert.That(mainPanel.activeSelf, Is.True);
        Assert.That(optionsPanel.activeSelf, Is.False);

        Object.Destroy(root);
        yield return null;
    }

    [UnityTest]
    public IEnumerator OnEnable_LoadsSavedName_UpdatesButtons()
    {
        var sessionRoot = new GameObject("GameSession_Test");
        var session = sessionRoot.AddComponent<GameSession>();
        session.PlayerName = "SavedPlayer";
        SetGameSessionInstance(session);

        var root = new GameObject("MainMenu_Test");
        var mainPanel = new GameObject("MainPanel");
        var optionsPanel = new GameObject("OptionsPanel");
        var nameInput = new GameObject("NameInput").AddComponent<TMP_InputField>();
        var codeInput = new GameObject("CodeInput").AddComponent<TMP_InputField>();
        var createButton = new GameObject("CreateButton").AddComponent<Button>();
        var joinButton = new GameObject("JoinButton").AddComponent<Button>();

        mainPanel.transform.SetParent(root.transform, false);
        optionsPanel.transform.SetParent(root.transform, false);
        nameInput.transform.SetParent(root.transform, false);
        codeInput.transform.SetParent(root.transform, false);
        createButton.transform.SetParent(root.transform, false);
        joinButton.transform.SetParent(root.transform, false);

        var menu = root.AddComponent<MainMenu>();
        SetField(menu, "mainMenuPanel", mainPanel);
        SetField(menu, "optionsMenuPanel", optionsPanel);
        SetField(menu, "nameInput", nameInput);
        SetField(menu, "codeInput", codeInput);
        SetField(menu, "createGameButton", createButton);
        SetField(menu, "joinGameButton", joinButton);

        Invoke(menu, "OnEnable");

        Assert.That(nameInput.text, Is.EqualTo("SavedPlayer"));
        Assert.That(createButton.interactable, Is.True);
        Assert.That(joinButton.interactable, Is.False);

        SetGameSessionInstance(null);
        Object.Destroy(sessionRoot);
        Object.Destroy(root);
        yield return null;
    }

    [UnityTest]
    public IEnumerator OnInput_UpdatesSessionName_UpdatesJoinState()
    {
        var sessionRoot = new GameObject("GameSession_Test");
        var session = sessionRoot.AddComponent<GameSession>();
        SetGameSessionInstance(session);

        var root = new GameObject("MainMenu_Input");
        var nameInput = new GameObject("NameInput").AddComponent<TMP_InputField>();
        var codeInput = new GameObject("CodeInput").AddComponent<TMP_InputField>();
        var createButton = new GameObject("CreateButton").AddComponent<Button>();
        var joinButton = new GameObject("JoinButton").AddComponent<Button>();

        nameInput.transform.SetParent(root.transform, false);
        codeInput.transform.SetParent(root.transform, false);
        createButton.transform.SetParent(root.transform, false);
        joinButton.transform.SetParent(root.transform, false);

        var menu = root.AddComponent<MainMenu>();
        SetField(menu, "nameInput", nameInput);
        SetField(menu, "codeInput", codeInput);
        SetField(menu, "createGameButton", createButton);
        SetField(menu, "joinGameButton", joinButton);

        nameInput.SetTextWithoutNotify("Alice");
        codeInput.SetTextWithoutNotify("ABC123");
        Invoke(menu, "OnInput", "");
        Assert.That(GameSession.Instance.PlayerName, Is.EqualTo("Alice"));
        Assert.That(joinButton.interactable, Is.True);

        codeInput.SetTextWithoutNotify("A");
        Invoke(menu, "OnInput", "");
        Assert.That(joinButton.interactable, Is.False);

        SetGameSessionInstance(null);
        Object.Destroy(sessionRoot);
        Object.Destroy(root);
        yield return null;
    }

    static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
        field.SetValue(target, value);
    }

    static void Invoke(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing method {methodName}");
        method.Invoke(target, args);
    }

    static void SetGameSessionInstance(GameSession session)
    {
        PropertyInfo instanceProperty = typeof(GameSession).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
        Assert.That(instanceProperty, Is.Not.Null, "Missing GameSession.Instance property");
        MethodInfo setMethod = instanceProperty.GetSetMethod(true);
        Assert.That(setMethod, Is.Not.Null, "Missing non-public setter for GameSession.Instance");
        setMethod.Invoke(null, new object[] { session });
    }
}
