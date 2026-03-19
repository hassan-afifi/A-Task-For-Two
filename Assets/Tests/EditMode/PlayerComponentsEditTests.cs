using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class PlayerComponentsEditTests
{
    [Test]
    public void Wrap_HandlesNegativeAndOverflowValues()
    {
        using var scope = new ComponentScope<PlayerVisuals>();

        Assert.That(Invoke<int>(scope.Component, "Wrap", -1, 3), Is.EqualTo(2));
        Assert.That(Invoke<int>(scope.Component, "Wrap", 4, 3), Is.EqualTo(1));
        Assert.That(Invoke<int>(scope.Component, "Wrap", 100, 0), Is.EqualTo(0));
    }

    [Test]
    public void CleanName_DefaultsTrimsAndTruncates()
    {
        using var scope = new ComponentScope<PlayerVisuals>();

        Assert.That(Invoke<string>(scope.Component, "CleanName", (string)null), Is.EqualTo("Player"));
        Assert.That(Invoke<string>(scope.Component, "CleanName", "  Ali  "), Is.EqualTo("Ali"));
        Assert.That(Invoke<string>(scope.Component, "CleanName", "12345678901"), Is.EqualTo("1234567890"));
    }

    [Test]
    public void SetInput_Disable_ClearsTransientInputState()
    {
        using var scope = new ComponentScope<PlayerInputHandler>();

        if (GetField(scope.Component, "input") == null)
        {
            Invoke<object>(scope.Component, "Awake");
        }

        Invoke<object>(scope.Component, "SetInput", true);

        SetField(scope.Component, "moveInput", new Vector2(1f, 1f));
        SetField(scope.Component, "lookInput", new Vector2(1f, -1f));
        SetField(scope.Component, "jumpPressed", true);
        SetField(scope.Component, "sprintHeld", true);

        Invoke<object>(scope.Component, "SetInput", false);

        Assert.That((Vector2)GetField(scope.Component, "moveInput"), Is.EqualTo(Vector2.zero));
        Assert.That((Vector2)GetField(scope.Component, "lookInput"), Is.EqualTo(Vector2.zero));
        Assert.That((bool)GetField(scope.Component, "jumpPressed"), Is.False);
        Assert.That((bool)GetField(scope.Component, "sprintHeld"), Is.False);
    }

    [Test]
    public void ShowName_CleansAndAssignsToLabel()
    {
        using var scope = new ComponentScope<PlayerVisuals>();
        var textGo = new GameObject("NameText");
        var text = textGo.AddComponent<TextMeshProUGUI>();
        SetField(scope.Component, "nameTagText", text);

        Invoke<object>(scope.Component, "ShowName", "  12345678901  ");
        Assert.That(text.text, Is.EqualTo("1234567890"));

        Object.DestroyImmediate(textGo);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
        field.SetValue(target, value);
    }

    private static object GetField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
        return field.GetValue(target);
    }

    private static T Invoke<T>(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing method {methodName}");
        object result = method.Invoke(target, args);
        return (T)result;
    }

    private sealed class ComponentScope<T> : System.IDisposable where T : Component
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
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
