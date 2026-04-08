using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UiControlEditTests
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
        DestroyAll<SliderHelper>();
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
        DestroyAll<SliderHelper>();
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

    [Test]
    public void BeginDecreaseHoldTest()
    {
        SliderHelper helper = BuildSliderHelper("SliderHelperBeginDecreaseHoldTest", 0f, 3f, 0f, out Slider slider, out _, out _, out _);
        helper.gameObject.SetActive(true);
        helper.BeginDecreaseHold();
        Assert.That(GetPrivate<Coroutine>(helper, "holdRoutine"), Is.Null);
        slider.value = 2f;
        helper.BeginDecreaseHold();
        Assert.That(slider.value, Is.EqualTo(1f).Within(0.001f));
        Assert.That(GetPrivate<float>(helper, "holdDelta"), Is.EqualTo(-1f).Within(0.001f));
        Assert.That(GetPrivate<Coroutine>(helper, "holdRoutine"), Is.Not.Null);
        helper.EndHold();
        Assert.That(GetPrivate<Coroutine>(helper, "holdRoutine"), Is.Null);
    }

    [Test]
    public void BeginIncreaseHoldTest()
    {
        SliderHelper helper = BuildSliderHelper("SliderHelperBeginIncreaseHoldTest", 0f, 3f, 3f, out Slider slider, out _, out _, out _);
        helper.gameObject.SetActive(true);
        helper.BeginIncreaseHold();
        Assert.That(GetPrivate<Coroutine>(helper, "holdRoutine"), Is.Null);
        slider.value = 1f;
        helper.BeginIncreaseHold();
        Assert.That(slider.value, Is.EqualTo(2f).Within(0.001f));
        Assert.That(GetPrivate<float>(helper, "holdDelta"), Is.EqualTo(1f).Within(0.001f));
        Assert.That(GetPrivate<Coroutine>(helper, "holdRoutine"), Is.Not.Null);
        helper.EndHold();
        Assert.That(GetPrivate<Coroutine>(helper, "holdRoutine"), Is.Null);
    }

    [Test]
    public void EndHoldTest()
    {
        SliderHelper helper = BuildSliderHelper("SliderHelperEndHoldTest", 0f, 3f, 1f, out _, out _, out _, out _);
        helper.gameObject.SetActive(true);
        Assert.DoesNotThrow(helper.EndHold);
        Assert.That(GetPrivate<Coroutine>(helper, "holdRoutine"), Is.Null);
        helper.BeginIncreaseHold();
        Assert.That(GetPrivate<Coroutine>(helper, "holdRoutine"), Is.Not.Null);
        helper.EndHold();
        Assert.That(GetPrivate<Coroutine>(helper, "holdRoutine"), Is.Null);
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

    private static SliderHelper BuildSliderHelper(string rootName, float minValue, float maxValue, float startValue, out Slider slider, out TMP_InputField inputField, out Button decreaseButton, out Button increaseButton)
    {
        GameObject root = new GameObject(rootName, typeof(RectTransform));
        root.SetActive(false);
        slider = new GameObject("Slider", typeof(RectTransform), typeof(Slider)).GetComponent<Slider>();
        slider.transform.SetParent(root.transform, false);
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = startValue;
        inputField = new GameObject("Input", typeof(RectTransform), typeof(TMP_InputField)).GetComponent<TMP_InputField>();
        inputField.transform.SetParent(root.transform, false);
        decreaseButton = new GameObject("DecreaseButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
        decreaseButton.transform.SetParent(root.transform, false);
        increaseButton = new GameObject("IncreaseButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
        increaseButton.transform.SetParent(root.transform, false);
        SliderHelper helper = root.AddComponent<SliderHelper>();
        SetPrivate(helper, "slider", slider);
        SetPrivate(helper, "inputField", inputField);
        SetPrivate(helper, "decreaseButton", decreaseButton);
        SetPrivate(helper, "increaseButton", increaseButton);
        return helper;
    }

    private static T GetPrivate<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
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
