using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class ToggleChangedEvent : UnityEvent<bool>
{
}

[RequireComponent(typeof(Button))]
public class AnimatedToggle : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    [SerializeField] private RectTransform selectorRect;
    [SerializeField] private float offPositionX = -60f;
    [SerializeField] private float onPositionX = 60f;

    public ToggleChangedEvent onValueChanged;
    private bool isOn;

    void Awake()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(Toggle);
        }
    }

    void Start()
    {
        SetValue(true, true);
    }

    void OnDestroy()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(Toggle);
        }
    }

    public void Toggle()
    {
        SetValue(!isOn);
    }

    public void SetValue(bool value)
    {
        SetValue(value, true);
    }

    public void SetValue(bool value, bool invokeEvent)
    {
        bool changed = isOn != value;
        isOn = value;

        ApplyPos();

        if ((changed || invokeEvent) && onValueChanged != null)
        {
            onValueChanged.Invoke(isOn);
        }
    }

    void ApplyPos()
    {
        if (selectorRect == null)
        {
            return;
        }

        Vector2 anchoredPosition = selectorRect.anchoredPosition;
        anchoredPosition.x = isOn ? onPositionX : offPositionX;
        selectorRect.anchoredPosition = anchoredPosition;
    }
}
