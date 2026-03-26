using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
[Serializable]

// Emits the selected gender state.
public class GenderChangedEvent : UnityEvent<bool>
{
}

// Handles animated gender selection UI.
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

    // Invoked when the selected gender changes.
    public GenderChangedEvent genderChanged;
    private bool isMale;
    private bool initialized;
    private bool blendInit;
    private float lastSelectorX;
    void Awake()
    {
        EnsureSetup();
        genderChanged ??= new GenderChangedEvent();
    }

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
        float currentX = selectorRect.anchoredPosition.x;

        if (blendInit && Mathf.Abs(currentX - lastSelectorX) <= 0.0001f)
        {
            return;
        }

        lastSelectorX = currentX;
        blendInit = true;
        ApplyBlend();
    }

    // Switches to the opposite gender option.
    public void ToggleGender()
    {
        SetGender(!isMale);
    }

    // Sets the selected gender and triggers callbacks.
    public void SetGender(bool male)
    {
        SetGender(male, false, true);
    }

    // Sets the selected gender with animation and callback control.
    public void SetGender(bool male, bool instant, bool invokeEvent)
    {
        bool changed = isMale != male;
        isMale = male;
        SyncAnim(instant);
        ApplyColors();

        if (changed || invokeEvent)
        {
            genderChanged.Invoke(isMale);
        }
    }

    void SyncAnim(bool instant)
    {
        selectorAnimator.SetBool(BoolName(BoolParam.IsMale), isMale);

        if (instant)
        {
            selectorAnimator.Play(isMale ? StateName(AnimState.MaleSelected) : StateName(AnimState.FemaleSelected), 0, 1f);
        }
    }

    void ApplyColors()
    {
        blendInit = false;
        ApplyBlend();
    }

    void ApplyBlend()
    {
        lastSelectorX = selectorRect.anchoredPosition.x;
        blendInit = true;
        float t = Mathf.InverseLerp(malePositionX, femalePositionX, selectorRect.anchoredPosition.x);
        t = Mathf.Clamp01(t);
        maleIconGraphic.color = Color.Lerp(activeIconColor, inactiveIconColor, t);
        femaleIconGraphic.color = Color.Lerp(inactiveIconColor, activeIconColor, t);
    }

    void EnsureSetup()
    {
        if (selectorAnimator == null)
        {
            throw new InvalidOperationException("GenderToggle setup failed: selectorAnimator reference is missing.");
        }

        if (selectorRect == null)
        {
            throw new InvalidOperationException("GenderToggle setup failed: selectorRect reference is missing.");
        }

        if (maleIconGraphic == null)
        {
            throw new InvalidOperationException("GenderToggle setup failed: maleIconGraphic reference is missing.");
        }

        if (femaleIconGraphic == null)
        {
            throw new InvalidOperationException("GenderToggle setup failed: femaleIconGraphic reference is missing.");
        }
    }

    static string BoolName(BoolParam param)
    {
        switch (param)
        {
        case BoolParam.IsMale: return "IsMale";
        default: throw new ArgumentOutOfRangeException(nameof(param), param, null);
        }
    }

    static string StateName(AnimState state)
    {
        switch (state)
        {
        case AnimState.MaleSelected: return "MaleSelected";
        case AnimState.FemaleSelected: return "FemaleSelected";
        default: throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
}
