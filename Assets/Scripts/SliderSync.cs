using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Keeps a slider and numeric input field in sync.
public class SliderSync : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_InputField inputField;
    private bool suppressCallbacks;
    void Awake()
    {
        EnsureSetup();
        slider.onValueChanged.AddListener(OnSliderChanged);
        inputField.onEndEdit.AddListener(OnInputEdit);
    }

    void OnEnable()
    {
        SyncInput();
    }

    void Start()
    {
        SyncInput();
    }

    void OnDestroy()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderChanged);
        }

        if (inputField != null)
        {
            inputField.onEndEdit.RemoveListener(OnInputEdit);
        }
    }

    void OnSliderChanged(float _)
    {
        if (suppressCallbacks)
        {
            return;
        }

        SyncInput();
    }

    void OnInputEdit(string text)
    {
        if (suppressCallbacks)
        {
            return;
        }

        bool valid = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue) || float.TryParse(text, out parsedValue);

        if (!valid)
        {
            SyncInput();
            return;
        }

        float clamped = Mathf.Clamp(parsedValue, slider.minValue, slider.maxValue);
        clamped = Mathf.Round(clamped);
        suppressCallbacks = true;
        slider.value = clamped;
        inputField.SetTextWithoutNotify(FormatValue(clamped));
        suppressCallbacks = false;
    }

    void SyncInput()
    {
        inputField.SetTextWithoutNotify(FormatValue(slider.value));
    }

    string FormatValue(float value)
    {
        return Mathf.RoundToInt(value).ToString();
    }

    void EnsureSetup()
    {
        if (slider == null)
        {
            throw new InvalidOperationException("SliderSync setup failed: slider reference is missing.");
        }

        if (inputField == null)
        {
            throw new InvalidOperationException("SliderSync setup failed: inputField reference is missing.");
        }
    }
}
