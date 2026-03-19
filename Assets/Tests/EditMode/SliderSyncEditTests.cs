using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderSyncEditTests
{
    [Test]
    public void OnEnable_SyncsInputFromSlider()
    {
        var root = new GameObject("SliderSync_Test");
        var sliderGo = new GameObject("Slider");
        sliderGo.transform.SetParent(root.transform, false);
        var slider = sliderGo.AddComponent<Slider>();
        var inputGo = new GameObject("Input");
        inputGo.transform.SetParent(root.transform, false);
        var input = inputGo.AddComponent<TMP_InputField>();
        var sync = root.AddComponent<SliderSync>();

        SetField(sync, "slider", slider);
        SetField(sync, "inputField", input);
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 37f;

        Invoke(sync, "Awake");
        Invoke(sync, "OnEnable");

        Assert.That(input.text, Is.EqualTo("37"));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void OnInputEdit_ClampsAndRounds()
    {
        var root = new GameObject("SliderSync_Test");
        var sliderGo = new GameObject("Slider");
        sliderGo.transform.SetParent(root.transform, false);
        var slider = sliderGo.AddComponent<Slider>();
        var inputGo = new GameObject("Input");
        inputGo.transform.SetParent(root.transform, false);
        var input = inputGo.AddComponent<TMP_InputField>();
        var sync = root.AddComponent<SliderSync>();

        SetField(sync, "slider", slider);
        SetField(sync, "inputField", input);
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 10f;

        Invoke(sync, "Awake");
        Invoke(sync, "OnInputEdit", "50.6");
        Assert.That(slider.value, Is.EqualTo(51f).Within(0.001f));
        Assert.That(input.text, Is.EqualTo("51"));

        Invoke(sync, "OnInputEdit", "-50");
        Assert.That(slider.value, Is.EqualTo(0f).Within(0.001f));
        Assert.That(input.text, Is.EqualTo("0"));

        Invoke(sync, "OnInputEdit", "999");
        Assert.That(slider.value, Is.EqualTo(100f).Within(0.001f));
        Assert.That(input.text, Is.EqualTo("100"));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void OnInputEdit_InvalidInput_RevertsToSliderValue()
    {
        var root = new GameObject("SliderSync_Test");
        var sliderGo = new GameObject("Slider");
        sliderGo.transform.SetParent(root.transform, false);
        var slider = sliderGo.AddComponent<Slider>();
        var inputGo = new GameObject("Input");
        inputGo.transform.SetParent(root.transform, false);
        var input = inputGo.AddComponent<TMP_InputField>();
        var sync = root.AddComponent<SliderSync>();

        SetField(sync, "slider", slider);
        SetField(sync, "inputField", input);
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 24f;

        Invoke(sync, "Awake");
        input.text = "abc";
        Invoke(sync, "OnInputEdit", "abc");

        Assert.That(slider.value, Is.EqualTo(24f).Within(0.001f));
        Assert.That(input.text, Is.EqualTo("24"));

        Object.DestroyImmediate(root);
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
