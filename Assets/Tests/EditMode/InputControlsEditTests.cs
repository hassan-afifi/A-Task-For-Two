using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputControlsEditTests
{
    [TearDown]
    public void TearDown()
    {
        DestroyAll<PlayerInputHandler>();
        DestroyAll<GameSession>();
        DestroyAll<RelayManager>();
        DestroyAll<AnimatedToggle>();
        DestroyAll<GenderToggle>();
        DestroyAll<SliderHelper>();
    }

    [Test]
    public void ConsumeJumpTest()
    {
        GameObject go = new GameObject("PlayerInputJumpTest");
        PlayerInputHandler input = go.AddComponent<PlayerInputHandler>();
        SetField(input, "jumpPressed", true);
        Assert.That(input.ConsumeJump(), Is.True);
        Assert.That(input.ConsumeJump(), Is.False);
    }

    [Test]
    public void ConsumeInteractTest()
    {
        GameObject go = new GameObject("PlayerInputInteractTest");
        PlayerInputHandler input = go.AddComponent<PlayerInputHandler>();
        SetField(input, "interactPressed", true);
        Assert.That(input.ConsumeInteract(), Is.True);
        Assert.That(input.ConsumeInteract(), Is.False);
    }

    [Test]
    public void ConsumeCrouchTest()
    {
        GameObject go = new GameObject("PlayerInputCrouchTest");
        PlayerInputHandler input = go.AddComponent<PlayerInputHandler>();
        SetField(input, "crouchPressed", true);
        Assert.That(input.ConsumeCrouch(), Is.True);
        Assert.That(input.ConsumeCrouch(), Is.False);
    }


    [Test]
    public void OnDestroyTest()
    {
        GameObject go = new GameObject("PlayerInputOnDestroyTest");
        PlayerInputHandler input = go.AddComponent<PlayerInputHandler>();
        Assert.DoesNotThrow(() => input.OnDestroy());
        SetField(input, "input", null);
        Assert.DoesNotThrow(() => input.OnDestroy());

        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() => InvokeNonPublic(input, "EnsureInput"));
        Assert.That(invocation.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(invocation.InnerException?.Message, Does.Contain("InputActions is not initialized"));
    }

    [Test]
    public void ToggleTest()
    {
        AnimatedToggle toggle = BuildAnimatedToggle(out RectTransform selectorRect);
        toggle.SetValue(true, false);
        toggle.Toggle();
        Assert.That(toggle.IsOn, Is.False);
        Assert.That(selectorRect.anchoredPosition.x, Is.EqualTo(-60f).Within(0.001f));
        toggle.Toggle();
        Assert.That(toggle.IsOn, Is.True);
        Assert.That(selectorRect.anchoredPosition.x, Is.EqualTo(60f).Within(0.001f));
    }

    [Test]
    public void SetValueTest()
    {
        AnimatedToggle toggle = BuildAnimatedToggle(out RectTransform selectorRect);
        int calls = 0;
        bool lastValue = false;

        toggle.onValueChanged.AddListener(value =>
        {
            calls += 1;
            lastValue = value;
        });

        toggle.SetValue(false);
        Assert.That(toggle.IsOn, Is.False);
        Assert.That(selectorRect.anchoredPosition.x, Is.EqualTo(-60f).Within(0.001f));
        Assert.That(lastValue, Is.False);
        Assert.That(calls, Is.EqualTo(1));
        toggle.SetValue(true, false);
        Assert.That(toggle.IsOn, Is.True);
        Assert.That(selectorRect.anchoredPosition.x, Is.EqualTo(60f).Within(0.001f));
        Assert.That(calls, Is.EqualTo(2));
        toggle.SetValue(true, true);
        Assert.That(calls, Is.EqualTo(3));
        Assert.That(lastValue, Is.True);
    }

    [Test]
    public void ToggleGenderTest()
    {
        GenderToggle toggle = BuildGenderToggle();
        bool lastValue = false;
        toggle.genderChanged.AddListener(value => lastValue = value);
        toggle.SetGender(true, false, false);
        toggle.ToggleGender();
        Assert.That(lastValue, Is.False);
        toggle.ToggleGender();
        Assert.That(lastValue, Is.True);
    }

    [Test]
    public void SetGenderTest()
    {
        GenderToggle toggle = BuildGenderToggle();
        int calls = 0;
        bool lastValue = false;

        toggle.genderChanged.AddListener(value =>
        {
            calls += 1;
            lastValue = value;
        });

        toggle.SetGender(false);
        Assert.That(lastValue, Is.False);
        Assert.That(calls, Is.EqualTo(1));
        toggle.SetGender(false, true, false);
        Assert.That(calls, Is.EqualTo(1));
        toggle.SetGender(true, true, true);
        Assert.That(lastValue, Is.True);
        Assert.That(calls, Is.EqualTo(2));
    }

    [Test]
    public void IncreaseByOneTest()
    {
        SliderHelper helper = BuildSliderHelper("SliderHelperIncreaseTest", 0f, 3f, 1f, out Slider slider, out TMP_InputField inputField, out Button decreaseButton, out Button increaseButton);
        helper.IncreaseByOne();
        Assert.That(slider.value, Is.EqualTo(2f).Within(0.001f));
        Assert.That(inputField.text, Is.EqualTo("2"));
        Assert.That(decreaseButton.interactable, Is.True);
        Assert.That(increaseButton.interactable, Is.True);
        helper.IncreaseByOne();
        helper.IncreaseByOne();
        Assert.That(slider.value, Is.EqualTo(3f).Within(0.001f));
        Assert.That(inputField.text, Is.EqualTo("3"));
        Assert.That(decreaseButton.interactable, Is.True);
        Assert.That(increaseButton.interactable, Is.False);
        SetField(helper, "suppressCallbacks", true);
        helper.IncreaseByOne();
        Assert.That(slider.value, Is.EqualTo(3f).Within(0.001f));
        Assert.That(increaseButton.interactable, Is.False);
    }

    [Test]
    public void DecreaseByOneTest()
    {
        SliderHelper helper = BuildSliderHelper("SliderHelperDecreaseTest", 0f, 3f, 2f, out Slider slider, out TMP_InputField inputField, out Button decreaseButton, out Button increaseButton);
        helper.DecreaseByOne();
        Assert.That(slider.value, Is.EqualTo(1f).Within(0.001f));
        Assert.That(inputField.text, Is.EqualTo("1"));
        Assert.That(decreaseButton.interactable, Is.True);
        Assert.That(increaseButton.interactable, Is.True);
        helper.DecreaseByOne();
        helper.DecreaseByOne();
        Assert.That(slider.value, Is.EqualTo(0f).Within(0.001f));
        Assert.That(inputField.text, Is.EqualTo("0"));
        Assert.That(decreaseButton.interactable, Is.False);
        Assert.That(increaseButton.interactable, Is.True);
        SetField(helper, "suppressCallbacks", true);
        helper.DecreaseByOne();
        Assert.That(slider.value, Is.EqualTo(0f).Within(0.001f));
        Assert.That(decreaseButton.interactable, Is.False);
    }
    private static AnimatedToggle BuildAnimatedToggle(out RectTransform selectorRect)
    {
        GameObject root = new GameObject("AnimatedToggleTest", typeof(RectTransform), typeof(Button));
        root.SetActive(false);
        Button button = root.GetComponent<Button>();
        selectorRect = new GameObject("Selector", typeof(RectTransform)).GetComponent<RectTransform>();
        selectorRect.SetParent(root.transform, false);
        AnimatedToggle toggle = root.AddComponent<AnimatedToggle>();
        SetField(toggle, "toggleButton", button);
        SetField(toggle, "selectorRect", selectorRect);
        toggle.onValueChanged = new ToggleChangedEvent();
        return toggle;
    }

    private static GenderToggle BuildGenderToggle()
    {
        GameObject root = new GameObject("GenderToggleTest", typeof(RectTransform), typeof(Animator));
        root.SetActive(false);
        RectTransform selectorRect = root.GetComponent<RectTransform>();
        Animator animator = root.GetComponent<Animator>();
        Image maleIcon = new GameObject("MaleIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        maleIcon.transform.SetParent(root.transform, false);
        Image femaleIcon = new GameObject("FemaleIcon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        femaleIcon.transform.SetParent(root.transform, false);
        GenderToggle toggle = root.AddComponent<GenderToggle>();
        SetField(toggle, "selectorAnimator", animator);
        SetField(toggle, "selectorRect", selectorRect);
        SetField(toggle, "maleIconGraphic", maleIcon);
        SetField(toggle, "femaleIconGraphic", femaleIcon);
        toggle.genderChanged = new GenderChangedEvent();
        return toggle;
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
        SetField(helper, "slider", slider);
        SetField(helper, "inputField", inputField);
        SetField(helper, "decreaseButton", decreaseButton);
        SetField(helper, "increaseButton", increaseButton);
        return helper;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static void InvokeNonPublic(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(target, null);
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
