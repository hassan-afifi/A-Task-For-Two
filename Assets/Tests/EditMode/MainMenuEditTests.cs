using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuEditTests
{
    [Test]
    public void HasLetter_RejectsInvalidAndAcceptsValidNames()
    {
        using var scope = new ComponentScope<MainMenu>();

        Assert.That(Invoke<bool>(scope.Component, "HasLetter", ""), Is.False);
        Assert.That(Invoke<bool>(scope.Component, "HasLetter", "   "), Is.False);
        Assert.That(Invoke<bool>(scope.Component, "HasLetter", "123456"), Is.False);
        Assert.That(Invoke<bool>(scope.Component, "HasLetter", "_-!?"), Is.False);
        Assert.That(Invoke<bool>(scope.Component, "HasLetter", "player1"), Is.True);
        Assert.That(Invoke<bool>(scope.Component, "HasLetter", "abc"), Is.True);
    }

    [Test]
    public void ValidCode_RequiresExactlySixCharactersAfterTrim()
    {
        using var scope = new ComponentScope<MainMenu>();

        Assert.That(Invoke<bool>(scope.Component, "ValidCode", (object)null), Is.False);
        Assert.That(Invoke<bool>(scope.Component, "ValidCode", ""), Is.False);
        Assert.That(Invoke<bool>(scope.Component, "ValidCode", "12345"), Is.False);
        Assert.That(Invoke<bool>(scope.Component, "ValidCode", "1234567"), Is.False);
        Assert.That(Invoke<bool>(scope.Component, "ValidCode", "ABC123"), Is.True);
        Assert.That(Invoke<bool>(scope.Component, "ValidCode", "  ABC123  "), Is.True);
    }

    [Test]
    public void OnInput_UpdatesCreateAndJoinButtons()
    {
        using var scope = new ComponentScope<MainMenu>();
        var nameInput = new GameObject("NameInput").AddComponent<TMP_InputField>();
        var codeInput = new GameObject("CodeInput").AddComponent<TMP_InputField>();
        var createButton = new GameObject("CreateButton").AddComponent<Button>();
        var joinButton = new GameObject("JoinButton").AddComponent<Button>();

        SetField(scope.Component, "nameInput", nameInput);
        SetField(scope.Component, "codeInput", codeInput);
        SetField(scope.Component, "createGameButton", createButton);
        SetField(scope.Component, "joinGameButton", joinButton);

        nameInput.SetTextWithoutNotify("Hassan");
        codeInput.SetTextWithoutNotify("ABC123");
        Invoke<object>(scope.Component, "OnInput", "");
        Assert.That(createButton.interactable, Is.True);
        Assert.That(joinButton.interactable, Is.True);

        nameInput.SetTextWithoutNotify("123456");
        codeInput.SetTextWithoutNotify("ABC123");
        Invoke<object>(scope.Component, "OnInput", "");
        Assert.That(createButton.interactable, Is.False);
        Assert.That(joinButton.interactable, Is.False);

        UnityEngine.Object.DestroyImmediate(nameInput.gameObject);
        UnityEngine.Object.DestroyImmediate(codeInput.gameObject);
        UnityEngine.Object.DestroyImmediate(createButton.gameObject);
        UnityEngine.Object.DestroyImmediate(joinButton.gameObject);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
        field.SetValue(target, value);
    }

    private static T Invoke<T>(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing method {methodName}");
        object result = method.Invoke(target, args);
        return (T)result;
    }

    private sealed class ComponentScope<T> : IDisposable where T : Component
    {
        public T Component { get; }
        private readonly GameObject gameObject;

        public ComponentScope()
        {
            gameObject = new GameObject(typeof(T).Name + "_Test");
            Component = gameObject.AddComponent<T>();
        }

        public void Dispose()
        {
            if (gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
