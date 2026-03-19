using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PauseMenuPlayTests
{
    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        PauseMenu.isOpen = false;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        PauseMenu.isOpen = false;
        PauseMenu[] menus = Object.FindObjectsByType<PauseMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i] != null)
            {
                Object.Destroy(menus[i].gameObject);
            }
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator OpenCloseOptions_SwitchesPanels()
    {
        var root = new GameObject("PauseMenu_Test");
        var panel = new GameObject("Panel");
        var main = new GameObject("Main");
        var options = new GameObject("Options");
        panel.transform.SetParent(root.transform, false);
        main.transform.SetParent(panel.transform, false);
        options.transform.SetParent(panel.transform, false);

        var hudGo = new GameObject("Hud");
        var hud = hudGo.AddComponent<Canvas>();

        var menu = root.AddComponent<PauseMenu>();
        SetField(menu, "panel", panel);
        SetField(menu, "pauseMainPanel", main);
        SetField(menu, "optionsMenuPanel", options);
        SetField(menu, "hudCanvas", hud);

        PauseMenu.isOpen = true;
        menu.OpenOptions();
        Assert.That(options.activeSelf, Is.True);
        Assert.That(main.activeSelf, Is.False);

        menu.CloseOptions();
        Assert.That(options.activeSelf, Is.False);
        Assert.That(main.activeSelf, Is.True);

        Object.Destroy(root);
        Object.Destroy(hudGo);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ContinueGame_WhenOpen_ClosesMenu()
    {
        var root = new GameObject("PauseMenu_Test");
        var panel = new GameObject("Panel");
        var main = new GameObject("Main");
        var options = new GameObject("Options");
        panel.transform.SetParent(root.transform, false);
        main.transform.SetParent(panel.transform, false);
        options.transform.SetParent(panel.transform, false);
        var hudGo = new GameObject("Hud");
        var hud = hudGo.AddComponent<Canvas>();

        var menu = root.AddComponent<PauseMenu>();
        SetField(menu, "panel", panel);
        SetField(menu, "pauseMainPanel", main);
        SetField(menu, "optionsMenuPanel", options);
        SetField(menu, "hudCanvas", hud);

        Invoke(menu, "Awake");
        Invoke(menu, "Toggle");
        Assert.That(PauseMenu.isOpen, Is.True);
        Assert.That(panel.activeSelf, Is.True);

        menu.ContinueGame();
        Assert.That(PauseMenu.isOpen, Is.False);
        Assert.That(panel.activeSelf, Is.False);

        Object.Destroy(root);
        Object.Destroy(hudGo);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ForceClose_WithoutInstance_ResetsStaticState()
    {
        PauseMenu.isOpen = true;

        Assert.DoesNotThrow(() => PauseMenu.ForceClose());
        Assert.That(PauseMenu.isOpen, Is.False);

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
}
