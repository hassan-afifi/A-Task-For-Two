using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UiStateEditTests
{
    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteAll();
        DestroyAll<MainMenu>();
        DestroyAll<PauseMenu>();
        DestroyAll<ConnectionLost>();
        DestroyAll<EndScreen>();
        DestroyAll<CharacterSelection>();
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteAll();
        DestroyAll<MainMenu>();
        DestroyAll<PauseMenu>();
        DestroyAll<ConnectionLost>();
        DestroyAll<EndScreen>();
        DestroyAll<CharacterSelection>();
    }

    [Test]
    public void ReturnToMainMenuTest()
    {
        Assert.Throws<InvalidOperationException>(MenuActions.ReturnToMainMenu);
    }

    [Test]
    public void MenuActionsExitGameTest()
    {
        Assert.DoesNotThrow(MenuActions.ExitGame);
        Assert.That(EditorApplication.isPlaying, Is.False);
    }

    [Test]
    public void MainMenuExitGameTest()
    {
        GameObject go = new GameObject("MainMenuExitTest");
        go.SetActive(false);
        MainMenu menu = go.AddComponent<MainMenu>();
        Assert.DoesNotThrow(menu.ExitGame);
        Assert.That(EditorApplication.isPlaying, Is.False);
    }

    [Test]
    public void PauseMenuExitGameTest()
    {
        PauseMenu menu = BuildPauseMenu("PauseMenuExitTest");
        Assert.DoesNotThrow(menu.ExitGame);
        Assert.That(EditorApplication.isPlaying, Is.False);
    }

    [Test]
    public void ConnectionLostExitGameTest()
    {
        ConnectionLost connectionLost = BuildConnectionLost("ConnectionLostExitTest");
        Assert.DoesNotThrow(connectionLost.ExitGame);
        Assert.That(EditorApplication.isPlaying, Is.False);
    }

    [Test]
    public void EndScreenExitGameTest()
    {
        GameObject go = new GameObject("EndScreenExitTest");
        go.SetActive(false);
        EndScreen endScreen = go.AddComponent<EndScreen>();
        Assert.DoesNotThrow(endScreen.ExitGame);
        Assert.That(EditorApplication.isPlaying, Is.False);
    }

    [Test]
    public void OnDestroyTest()
    {
        GameObject go = new GameObject("EndScreenOnDestroyTest");
        go.SetActive(false);
        EndScreen endScreen = go.AddComponent<EndScreen>();
        Assert.DoesNotThrow(endScreen.OnDestroy);
    }

    [Test]
    public void PrevCharTest()
    {
        GameObject go = new GameObject("CharacterSelectionPrevTest");
        CharacterSelection selection = go.AddComponent<CharacterSelection>();
        Assert.DoesNotThrow(selection.PrevChar);
    }

    [Test]
    public void NextCharTest()
    {
        GameObject go = new GameObject("CharacterSelectionNextTest");
        CharacterSelection selection = go.AddComponent<CharacterSelection>();
        Assert.DoesNotThrow(selection.NextChar);
    }

    private static PauseMenu BuildPauseMenu(string name)
    {
        GameObject root = new GameObject(name);
        root.SetActive(false);
        PauseMenu menu = root.AddComponent<PauseMenu>();
        SetPrivate(menu, "panel", new GameObject("Panel"));
        SetPrivate(menu, "pauseMainPanel", new GameObject("PauseMain"));
        SetPrivate(menu, "optionsMenuPanel", new GameObject("Options"));
        SetPrivate(menu, "hudCanvas", new GameObject("HudCanvas", typeof(Canvas)).GetComponent<Canvas>());
        SetPrivate(menu, "joinCodeText", new GameObject("JoinCode", typeof(TextMeshProUGUI)).GetComponent<TMP_Text>());
        SetPrivate(menu, "codeCopiedAnimator", new GameObject("CopiedAnimator", typeof(Animator)).GetComponent<Animator>());
        SetPrivate(menu, "input", new InputActions());
        return menu;
    }

    private static ConnectionLost BuildConnectionLost(string name)
    {
        GameObject root = new GameObject(name);
        root.SetActive(false);
        ConnectionLost connectionLost = root.AddComponent<ConnectionLost>();
        SetPrivate(connectionLost, "panel", new GameObject("Panel"));
        SetPrivate(connectionLost, "hudCanvas", new GameObject("HudCanvas", typeof(Canvas)).GetComponent<Canvas>());
        return connectionLost;
    }

    private static void SetPrivate(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
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
