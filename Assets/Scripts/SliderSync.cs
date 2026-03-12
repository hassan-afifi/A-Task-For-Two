using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderSync : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_InputField inputField;

    private bool suppressCallbacks;

    void Awake()
    {
        if (slider == null || inputField == null)
        {
            enabled = false;
            return;
        }

        slider.onValueChanged.AddListener(OnSliderChanged);
        inputField.onEndEdit.AddListener(OnInputEndEdit);
    }

    void OnEnable()
    {
        if (slider == null || inputField == null)
        {
            return;
        }

        RefreshInputFromSlider();
    }

    void Start()
    {
        if (slider == null || inputField == null)
        {
            return;
        }

        RefreshInputFromSlider();
    }

    void OnDestroy()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderChanged);
        }

        if (inputField != null)
        {
            inputField.onEndEdit.RemoveListener(OnInputEndEdit);
        }
    }

    void OnSliderChanged(float _)
    {
        if (suppressCallbacks)
        {
            return;
        }

        RefreshInputFromSlider();
    }

    void OnInputEndEdit(string text)
    {
        if (suppressCallbacks)
        {
            return;
        }

        bool valid = float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedValue)
            || float.TryParse(text, out parsedValue);

        if (!valid)
        {
            RefreshInputFromSlider();
            return;
        }

        float clamped = Mathf.Clamp(parsedValue, slider.minValue, slider.maxValue);
        clamped = Mathf.Round(clamped);

        suppressCallbacks = true;
        slider.value = clamped;
        inputField.SetTextWithoutNotify(FormatValue(clamped));
        suppressCallbacks = false;
    }

    void RefreshInputFromSlider()
    {
        inputField.SetTextWithoutNotify(FormatValue(slider.value));
    }

    string FormatValue(float value)
    {
        return Mathf.RoundToInt(value).ToString();
    }
}
