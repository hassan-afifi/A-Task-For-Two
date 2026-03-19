using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.Collections;

public class AnimatedTogglePlayTests
{
    [UnityTest]
    public IEnumerator StartsOn_MovesSelectorToOnPosition()
    {
        var root = new GameObject("AnimatedToggle_Test");
        var button = root.AddComponent<Button>();
        var selector = new GameObject("Selector").AddComponent<RectTransform>();
        selector.SetParent(root.transform, false);

        var toggle = root.AddComponent<AnimatedToggle>();
        SetField(toggle, "toggleButton", button);
        SetField(toggle, "selectorRect", selector);
        Invoke(toggle, "Awake");

        yield return null;

        Assert.That(selector.anchoredPosition.x, Is.EqualTo(60f).Within(0.001f));

        Object.Destroy(root);
    }

    [UnityTest]
    public IEnumerator ButtonClick_TogglesState_InvokesEvent()
    {
        var root = new GameObject("AnimatedToggle_Test");
        var button = root.AddComponent<Button>();
        var selector = new GameObject("Selector").AddComponent<RectTransform>();
        selector.SetParent(root.transform, false);

        var toggle = root.AddComponent<AnimatedToggle>();
        SetField(toggle, "toggleButton", button);
        SetField(toggle, "selectorRect", selector);
        Invoke(toggle, "Awake");

        yield return null;

        bool callbackCalled = false;
        bool callbackValue = true;
        toggle.onValueChanged.AddListener(value =>
        {
            callbackCalled = true;
            callbackValue = value;
        });

        button.onClick.Invoke();

        Assert.That(callbackCalled, Is.True);
        Assert.That(callbackValue, Is.False);
        Assert.That(selector.anchoredPosition.x, Is.EqualTo(-60f).Within(0.001f));

        Object.Destroy(root);
    }

    [UnityTest]
    public IEnumerator MissingSelector_DoesNotThrow()
    {
        var root = new GameObject("AnimatedToggle_Test");
        var button = root.AddComponent<Button>();
        var toggle = root.AddComponent<AnimatedToggle>();
        SetField(toggle, "toggleButton", button);
        Invoke(toggle, "Awake");

        yield return null;

        Assert.DoesNotThrow(() => toggle.SetValue(false));
        Assert.DoesNotThrow(() => button.onClick.Invoke());

        Object.Destroy(root);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
        field.SetValue(target, value);
    }

    private static void Invoke(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Missing method {methodName}");
        method.Invoke(target, args);
    }
}
