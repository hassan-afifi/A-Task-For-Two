using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]

// Plays UI hover and click sounds for menu controls.
public class MenuSfx : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, ISelectHandler, IBeginDragHandler, IEndDragHandler
{
    private float sliderTickGap = 0.01f;
    private Selectable selectable;
    private Button button;
    private Toggle toggle;
    private Slider slider;
    private TMP_Dropdown tmpDropdown;
    private Dropdown dropdown;
    private bool isSliderControl;
    private bool dragOn;
    private float lastVal;
    private int lastStep;
    private float lastTickAt;

    // Caches control components and subscribes audio callbacks.
    void Awake()
    {
        selectable = GetComponent<Selectable>();
        button = GetComponent<Button>();
        toggle = GetComponent<Toggle>();
        slider = GetComponent<Slider>();
        tmpDropdown = GetComponent<TMP_Dropdown>();
        dropdown = GetComponent<Dropdown>();

        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggle);
        }

        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSlide);
            lastVal = slider.value;
            lastStep = Mathf.RoundToInt(slider.value);
            isSliderControl = true;
        }

        if (tmpDropdown != null)
        {
            tmpDropdown.onValueChanged.AddListener(OnTmpDrop);
            SetupHover(tmpDropdown.template);
        }

        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(OnDrop);
            SetupHover(dropdown.template);
        }
    }

    // Unsubscribes all registered UI callbacks.
    void OnDestroy()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggle);
        }

        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSlide);
        }

        if (tmpDropdown != null)
        {
            tmpDropdown.onValueChanged.RemoveListener(OnTmpDrop);
        }

        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveListener(OnDrop);
        }
    }

    // Plays hover audio when the pointer enters the control.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slider != null)
        {
            return;
        }

        if (!CanPlay())
        {
            return;
        }

        AudioManager.PlayHover();
    }

    // Plays button click audio on pointer-down.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (button == null)
        {
            return;
        }

        if (!CanPlay())
        {
            return;
        }

        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        AudioManager.PlayClick();
    }

    // Plays hover audio when the control is selected by navigation.
    public void OnSelect(BaseEventData eventData)
    {
        if (slider != null)
        {
            return;
        }

        if (!CanPlay())
        {
            return;
        }

        AudioManager.PlayHover();
    }

    // Starts slider drag sound tracking.
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isSliderControl || !CanPlay())
        {
            return;
        }

        dragOn = true;
        lastVal = slider.value;
        lastStep = Mathf.RoundToInt(slider.value);
        lastTickAt = -1f;
    }

    // Stops slider drag sound tracking.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isSliderControl)
        {
            dragOn = false;
            return;
        }

        dragOn = false;
    }

    // Plays click audio for toggle controls.
    void OnToggle(bool _)
    {
        AudioManager.PlayClick();
    }

    // Plays click audio for TMP dropdown selection changes.
    void OnTmpDrop(int _)
    {
        AudioManager.PlayClick();
    }

    // Plays click audio for legacy dropdown selection changes.
    void OnDrop(int _)
    {
        AudioManager.PlayClick();
    }

    // Plays slider tick audio while dragging across values.
    void OnSlide(float value)
    {
        if (!dragOn || !CanPlay())
        {
            lastVal = value;
            lastStep = Mathf.RoundToInt(value);
            return;
        }

        if (slider.wholeNumbers)
        {
            int currentWhole = Mathf.RoundToInt(value);

            if (currentWhole == lastStep)
            {
                return;
            }

            lastStep = currentWhole;
            lastVal = value;
            PlaySlideTick();
            return;
        }

        if (Mathf.Approximately(value, lastVal))
        {
            return;
        }

        lastVal = value;
        PlaySlideTick();
    }

    // Throttles and plays one slider tick sound.
    void PlaySlideTick()
    {
        float now = Time.unscaledTime;
        float gap = Mathf.Max(0f, sliderTickGap);

        if (lastTickAt >= 0f && now - lastTickAt < gap)
        {
            return;
        }

        lastTickAt = now;
        AudioManager.PlayHover();
    }

    // Ensures dropdown item hover sounds are wired.
    void SetupHover(RectTransform template)
    {
        if (template == null)
        {
            throw new InvalidOperationException("MenuSfx setup failed: dropdown template is missing.");
        }

        Toggle itemToggle = template.GetComponentInChildren<Toggle>(true);

        if (itemToggle == null)
        {
            throw new InvalidOperationException("MenuSfx setup failed: dropdown item Toggle is missing.");
        }

        DropHoverSfx hoverSfx = itemToggle.GetComponent<DropHoverSfx>();

        if (hoverSfx == null)
        {
            hoverSfx = itemToggle.gameObject.AddComponent<DropHoverSfx>();
        }
    }

    // Returns whether this selectable can currently play UI sounds.
    bool CanPlay()
    {
        return selectable.IsInteractable();
    }
}

// Plays hover audio for dropdown list items.
public class DropHoverSfx : MonoBehaviour, IPointerEnterHandler
{
    // Plays hover audio when the pointer enters a dropdown item.
    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.PlayHover();
    }
}
