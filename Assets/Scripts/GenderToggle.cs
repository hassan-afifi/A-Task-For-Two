using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class GenderChangedEvent : UnityEvent<bool>
{
}

public class GenderToggle : MonoBehaviour
{
    [SerializeField] private Animator selectorAnimator;
    [SerializeField] private RectTransform selectorRect;
    [SerializeField] private Graphic maleIconGraphic;
    [SerializeField] private Graphic femaleIconGraphic;
    [SerializeField] private Color activeIconColor = new Color32(26, 26, 26, 255);
    [SerializeField] private Color inactiveIconColor = new Color32(200, 150, 50, 128);
    [SerializeField] private float malePositionX = -40f;
    [SerializeField] private float femalePositionX = 40f;
    [SerializeField] private string isMaleParam = "IsMale";
    [SerializeField] private string maleStateName = "MaleSelected";
    [SerializeField] private string femaleStateName = "FemaleSelected";

    public GenderChangedEvent genderChanged;
    private bool isMale;
    private bool initialized;

    void Start()
    {
        SetGender(true, true, true);
        initialized = true;
    }

    void OnEnable()
    {
        if (!initialized)
        {
            return;
        }

        SetGender(isMale, true, false);
    }

    void Update()
    {
        if (selectorRect != null)
        {
            ApplyBlend();
        }
    }

    public void ToggleGender()
    {
        SetGender(!isMale);
    }

    public void SetGender(bool male)
    {
        SetGender(male, false, true);
    }

    public void SetGender(bool male, bool instant, bool invokeEvent)
    {
        bool changed = isMale != male;
        isMale = male;
        SyncAnim(instant);
        ApplyColors();

        if ((changed || invokeEvent) && genderChanged != null)
        {
            genderChanged.Invoke(isMale);
        }
    }

    void SyncAnim(bool instant)
    {
        if (selectorAnimator == null)
        {
            return;
        }

        selectorAnimator.SetBool(isMaleParam, isMale);

        if (instant)
        {
            string state = isMale ? maleStateName : femaleStateName;

            if (string.IsNullOrEmpty(state))
            {
                return;
            }

            selectorAnimator.Play(state, 0, 1f);
        }
    }

    void ApplyColors()
    {
        if (selectorRect != null)
        {
            ApplyBlend();
            return;
        }

        if (maleIconGraphic != null)
        {
            maleIconGraphic.color = isMale ? activeIconColor : inactiveIconColor;
        }

        if (femaleIconGraphic != null)
        {
            femaleIconGraphic.color = isMale ? inactiveIconColor : activeIconColor;
        }
    }

    void ApplyBlend()
    {
        float t = Mathf.InverseLerp(malePositionX, femalePositionX, selectorRect.anchoredPosition.x);
        t = Mathf.Clamp01(t);

        if (maleIconGraphic != null)
        {
            maleIconGraphic.color = Color.Lerp(activeIconColor, inactiveIconColor, t);
        }

        if (femaleIconGraphic != null)
        {
            femaleIconGraphic.color = Color.Lerp(inactiveIconColor, activeIconColor, t);
        }
    }
}
