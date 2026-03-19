using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class GenderChangedEvent : UnityEvent<bool>
{
}

public class GenderToggle : MonoBehaviour
{
    private enum BoolParam
    {
        IsMale
    }

    private enum AnimState
    {
        MaleSelected,
        FemaleSelected
    }

    [SerializeField] private Animator selectorAnimator;
    [SerializeField] private RectTransform selectorRect;
    [SerializeField] private Graphic maleIconGraphic;
    [SerializeField] private Graphic femaleIconGraphic;
    private Color activeIconColor = new Color32(26, 26, 26, 255);
    private Color inactiveIconColor = new Color32(200, 150, 50, 128);
    private float malePositionX = -40f;
    private float femalePositionX = 40f;

    public GenderChangedEvent genderChanged;
    private bool isMale;
    private bool initialized;
    private bool blendInit;
    private float lastSelectorX;

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
        if (selectorRect == null)
        {
            return;
        }

        float currentX = selectorRect.anchoredPosition.x;
        if (blendInit && Mathf.Abs(currentX - lastSelectorX) <= 0.0001f)
        {
            return;
        }

        lastSelectorX = currentX;
        blendInit = true;
        ApplyBlend();
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

        selectorAnimator.SetBool(BoolName(BoolParam.IsMale), isMale);

        if (instant)
        {
            selectorAnimator.Play(isMale ? StateName(AnimState.MaleSelected) : StateName(AnimState.FemaleSelected), 0, 1f);
        }
    }

    void ApplyColors()
    {
        if (selectorRect != null)
        {
            blendInit = false;
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
        if (selectorRect != null)
        {
            lastSelectorX = selectorRect.anchoredPosition.x;
            blendInit = true;
        }

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

    static string BoolName(BoolParam param)
    {
        switch (param)
        {
            case BoolParam.IsMale:
                return "IsMale";
            default:
                throw new System.ArgumentOutOfRangeException(nameof(param), param, null);
        }
    }

    static string StateName(AnimState state)
    {
        switch (state)
        {
            case AnimState.MaleSelected:
                return "MaleSelected";
            case AnimState.FemaleSelected:
                return "FemaleSelected";
            default:
                throw new System.ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
}
