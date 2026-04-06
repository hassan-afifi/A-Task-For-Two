using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Keeps a slider, numeric input field, and step buttons in sync.
public class SliderHelper : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button decreaseButton;
    [SerializeField] private Button increaseButton;
    [SerializeField] private float holdDelay = 0.35f;
    [SerializeField] private float holdInterval = 0.08f;
    private bool suppressCallbacks;
    private Coroutine holdRoutine;
    private float holdDelta;

    // Validates references and subscribes slider and input callbacks.
    void Awake()
    {
        EnsureSetup();
        slider.onValueChanged.AddListener(OnSliderChanged);
        inputField.onEndEdit.AddListener(OnInputEdit);
        UpdateButtons();
    }

    // Syncs input text and button states whenever the object becomes enabled.
    void OnEnable()
    {
        SyncInput();
        UpdateButtons();
    }

    // Syncs input text and button states once after start for first frame accuracy.
    void Start()
    {
        SyncInput();
        UpdateButtons();
    }

    // Stops hold state when this object is disabled.
    void OnDisable()
    {
        EndHold();
    }

    // Unsubscribes slider and input callbacks on destroy.
    void OnDestroy()
    {
        EndHold();

        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderChanged);
        }

        if (inputField != null)
        {
            inputField.onEndEdit.RemoveListener(OnInputEdit);
        }
    }

    // Mirrors slider changes into the numeric input field.
    void OnSliderChanged(float _)
    {
        if (suppressCallbacks)
        {
            return;
        }

        SyncInput();
        UpdateButtons();
    }

    // Parses manual input text and applies clamped slider value.
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
            UpdateButtons();
            return;
        }

        float clamped = Mathf.Clamp(parsedValue, slider.minValue, slider.maxValue);
        clamped = Mathf.Round(clamped);
        suppressCallbacks = true;
        slider.value = clamped;
        inputField.SetTextWithoutNotify(FormatValue(clamped));
        suppressCallbacks = false;
        UpdateButtons();
    }

    // Decreases slider value by one step for button-driven input.
    public void DecreaseByOne()
    {
        ChangeBy(-1f);
    }

    // Increases slider value by one step for button-driven input.
    public void IncreaseByOne()
    {
        ChangeBy(1f);
    }

    // Starts repeated decrease while the button is held.
    public void BeginDecreaseHold()
    {
        BeginHold(-1f);
    }

    // Starts repeated increase while the button is held.
    public void BeginIncreaseHold()
    {
        BeginHold(1f);
    }

    // Stops repeated step changes.
    public void EndHold()
    {
        if (holdRoutine == null)
        {
            return;
        }

        StopCoroutine(holdRoutine);
        holdRoutine = null;
    }

    // Applies a delta and keeps slider/input synchronized.
    bool ChangeBy(float delta)
    {
        if (suppressCallbacks)
        {
            return false;
        }

        float clamped = Mathf.Clamp(slider.value + delta, slider.minValue, slider.maxValue);
        clamped = Mathf.Round(clamped);

        if (Mathf.Approximately(clamped, slider.value))
        {
            UpdateButtons();
            return false;
        }

        suppressCallbacks = true;
        slider.value = clamped;
        inputField.SetTextWithoutNotify(FormatValue(clamped));
        suppressCallbacks = false;
        UpdateButtons();
        PlayTick();
        return true;
    }

    // Applies one step immediately, then starts hold repeat loop.
    void BeginHold(float delta)
    {
        EndHold();
        holdDelta = delta;

        if (!ChangeBy(delta))
        {
            return;
        }

        holdRoutine = StartCoroutine(HoldLoop());
    }

    // Repeats step changes while the button remains held.
    IEnumerator HoldLoop()
    {
        float delay = Mathf.Max(0f, holdDelay);

        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        float interval = Mathf.Max(0.01f, holdInterval);

        while (true)
        {
            if (!ChangeBy(holdDelta))
            {
                holdRoutine = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(interval);
        }
    }

    // Updates button interactable states based on slider bounds.
    void UpdateButtons()
    {
        if (decreaseButton != null)
        {
            decreaseButton.interactable = slider.value > slider.minValue;
        }

        if (increaseButton != null)
        {
            increaseButton.interactable = slider.value < slider.maxValue;
        }
    }

    // Plays one slider tick sound for button-driven step changes.
    void PlayTick()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.PlayHover();
    }

    // Writes the current slider value into the input field.
    void SyncInput()
    {
        inputField.SetTextWithoutNotify(FormatValue(slider.value));
    }

    // Formats slider values as rounded integer strings.
    string FormatValue(float value)
    {
        return Mathf.RoundToInt(value).ToString();
    }

    // Throws when slider, input field, or step button references are missing.
    void EnsureSetup()
    {
        if (slider == null)
        {
            throw new InvalidOperationException("SliderHelper setup failed: slider reference is missing.");
        }

        if (inputField == null)
        {
            throw new InvalidOperationException("SliderHelper setup failed: inputField reference is missing.");
        }

        if (decreaseButton == null)
        {
            throw new InvalidOperationException("SliderHelper setup failed: decreaseButton reference is missing.");
        }

        if (increaseButton == null)
        {
            throw new InvalidOperationException("SliderHelper setup failed: increaseButton reference is missing.");
        }
    }
}

