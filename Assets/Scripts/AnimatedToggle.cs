using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
[Serializable]

// Emits the current toggle state when it changes.
public class ToggleChangedEvent : UnityEvent<bool>
{
}

[RequireComponent(typeof(Button))]

// Provides an animated on/off toggle with event callbacks.
public class AnimatedToggle : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    [SerializeField] private RectTransform selectorRect;
    private float offPositionX = -60f;
    private float onPositionX = 60f;

    // Invoked when the toggle value changes.
    public ToggleChangedEvent onValueChanged = new ToggleChangedEvent();
    private bool isOn = true;

    // Returns the current toggle state.
    public bool IsOn => isOn;

    // Validates setup and binds the toggle button callback.
    void Awake()
    {
        onValueChanged ??= new ToggleChangedEvent();
        EnsureSetup();
        ApplyPos();
        toggleButton.onClick.AddListener(Toggle);
    }

    // Unbinds the toggle button callback on destroy.
    void OnDestroy()
    {
        toggleButton.onClick.RemoveListener(Toggle);
    }

    // Flips the toggle to the opposite state.
    public void Toggle()
    {
        SetValue(!isOn);
    }

    // Sets the toggle state and invokes callbacks.
    public void SetValue(bool value)
    {
        SetValue(value, true);
    }

    // Sets the toggle state with optional callback invocation.
    public void SetValue(bool value, bool invokeEvent)
    {
        bool changed = isOn != value;
        isOn = value;
        ApplyPos();

        if (changed || invokeEvent)
        {
            onValueChanged.Invoke(isOn);
        }
    }

    // Moves the selector rect to match the current toggle state.
    void ApplyPos()
    {
        Vector2 anchoredPosition = selectorRect.anchoredPosition;
        anchoredPosition.x = isOn ? onPositionX : offPositionX;
        selectorRect.anchoredPosition = anchoredPosition;
    }

    // Validates required toggle references.
    void EnsureSetup()
    {
        toggleButton ??= GetComponent<Button>();

        if (selectorRect == null)
        {
            throw new InvalidOperationException("AnimatedToggle setup failed: selectorRect reference is missing.");
        }
    }
}
