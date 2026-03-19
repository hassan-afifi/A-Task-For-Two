using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MenuSfx : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IBeginDragHandler, IEndDragHandler
{
    private float sliderTickGap = 0.01f;

    private Selectable selectable;
    private Button button;
    private Toggle toggle;
    private Slider slider;
    private TMP_Dropdown tmpDropdown;
    private Dropdown dropdown;
    private bool dragOn;
    private float lastVal;
    private int lastStep;
    private float lastTickAt;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
        button = GetComponent<Button>();
        toggle = GetComponent<Toggle>();
        slider = GetComponent<Slider>();
        tmpDropdown = GetComponent<TMP_Dropdown>();
        dropdown = GetComponent<Dropdown>();

        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }

        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggle);
        }

        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSlide);
            lastVal = slider.value;
            lastStep = Mathf.RoundToInt(slider.value);
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

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
        }

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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slider == null || !CanPlay())
        {
            return;
        }

        dragOn = true;
        lastVal = slider.value;
        lastStep = Mathf.RoundToInt(slider.value);
        lastTickAt = -1f;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (slider == null)
        {
            dragOn = false;
            return;
        }

        dragOn = false;
    }

    void OnClick()
    {
        AudioManager.PlayClick();
    }

    void OnToggle(bool _)
    {
        AudioManager.PlayClick();
    }

    void OnTmpDrop(int _)
    {
        AudioManager.PlayClick();
    }

    void OnDrop(int _)
    {
        AudioManager.PlayClick();
    }

    void OnSlide(float value)
    {
        if (slider == null)
        {
            return;
        }

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

    void SetupHover(RectTransform template)
    {
        if (template == null)
        {
            return;
        }

        Toggle itemToggle = template.GetComponentInChildren<Toggle>(true);

        if (itemToggle == null)
        {
            return;
        }

        DropHoverSfx hoverSfx = itemToggle.GetComponent<DropHoverSfx>();

        if (hoverSfx == null)
        {
            hoverSfx = itemToggle.gameObject.AddComponent<DropHoverSfx>();
        }
    }

    bool CanPlay()
    {
        if (selectable == null)
        {
            return true;
        }

        return selectable.IsInteractable();
    }
}

public class DropHoverSfx : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.PlayHover();
    }
}
