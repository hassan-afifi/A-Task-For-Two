using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;

// Handles character carousel selection in the menu.
public class CharacterSelection : MonoBehaviour
{
    private const string CloneSuffix = "(Clone)";
    [SerializeField] private Transform charactersParent;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private GenderToggle genderToggleUI;
    private int maleStartIndex = 0;
    private int femaleStartIndex = 3;
    private int charactersPerGender = 3;
    private float carouselRadius = 1f;
    private float rotationDuration = 0.28f;
    private float backScale = 0.5f;
    private float depthScaleExponent = 1.2f;
    private GameObject[] characters = Array.Empty<GameObject>();
    private Vector3[] baseScales = Array.Empty<Vector3>();
    private Vector3 frontPos;
    private int index;
    private int rotVal = 1;
    private bool maleGroup = true;
    private bool inTransit;
    void Start()
    {
        EnsureSetup();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && GetComponent<NetworkObject>() != null)
        {
            enabled = false;
            return;
        }

        Transform parent = charactersParent;
        List<GameObject> characterList = new List<GameObject>();

        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject child = parent.GetChild(i).gameObject;
            bool hasCharacterVisual = child.GetComponentInChildren<Animator>(true) != null || child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;
            bool hasCamera = child.GetComponentInChildren<Camera>(true) != null;

            if (!hasCharacterVisual || hasCamera)
            {
                continue;
            }

            characterList.Add(child);
        }

        if (characterList.Count == 0)
        {
            characters = new GameObject[0];
            UpdateName();
            return;
        }

        characters = characterList.ToArray();
        baseScales = new Vector3[characters.Length];

        for (int i = 0; i < characters.Length; i++)
        {
            baseScales[i] = characters[i].transform.localScale;
        }

        int savedIndex = GameSession.Instance != null ? GameSession.Instance.CharIndex : 0;
        index = WrapIndex(savedIndex, characters.Length);
        InitGroup(index);
        index = SlotIndex(CurSlot());

        if (genderToggleUI != null)
        {
            genderToggleUI.genderChanged.AddListener(OnGenderChanged);
            genderToggleUI.SetGender(maleGroup, true, false);
        }

        frontPos = characters[index].transform.localPosition;

        foreach (GameObject character in characters)
        {
            character.SetActive(false);
        }

        ApplyLayout();
        UpdateName();
        SaveSelection();
    }

    // Selects the previous character in the active group.
    public void PrevChar()
    {
        if (characters.Length == 0 || inTransit)
        {
            return;
        }

        if (GroupCount() <= 1)
        {
            return;
        }

        StartCoroutine(RotateCarousel(-1));
    }

    // Selects the next character in the active group.
    public void NextChar()
    {
        if (characters.Length == 0 || inTransit)
        {
            return;
        }

        if (GroupCount() <= 1)
        {
            return;
        }

        StartCoroutine(RotateCarousel(1));
    }

    IEnumerator RotateCarousel(int step)
    {
        int groupCount = GroupCount();

        if (groupCount <= 1)
        {
            yield break;
        }

        inTransit = true;
        int oldSlot = CurSlot();
        int oldCurrent = SlotIndex(oldSlot);
        int oldPrev = SlotIndex(oldSlot - 1);
        int oldNext = SlotIndex(oldSlot + 1);
        int targetSlot = WrapIndex(oldSlot + step, groupCount);

        if (groupCount < 3)
        {
            rotVal = targetSlot + 1;
            index = SlotIndex(CurSlot());
            ApplyLayout();
            UpdateName();
            SaveSelection();
            inTransit = false;
            yield break;
        }

        bool[] activeDuringTransition = new bool[characters.Length];
        activeDuringTransition[oldCurrent] = true;
        activeDuringTransition[oldPrev] = true;
        activeDuringTransition[oldNext] = true;

        for (int i = 0; i < characters.Length; i++)
        {
            if (activeDuringTransition[i])
            {
                characters[i].SetActive(true);
            }
        }

        float[] startAngles = new float[characters.Length];
        float[] endAngles = new float[characters.Length];

        if (step > 0)
        {
            startAngles[oldPrev] = 120f;
            startAngles[oldCurrent] = 0f;
            startAngles[oldNext] = -120f;
            endAngles[oldPrev] = 240f;
            endAngles[oldCurrent] = 120f;
            endAngles[oldNext] = 0f;
        }
        else
        {
            startAngles[oldPrev] = 120f;
            startAngles[oldCurrent] = 0f;
            startAngles[oldNext] = -120f;
            endAngles[oldPrev] = 0f;
            endAngles[oldCurrent] = -120f;
            endAngles[oldNext] = -240f;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, rotationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            for (int i = 0; i < characters.Length; i++)
            {
                if (!activeDuringTransition[i])
                {
                    continue;
                }

                float angle = Mathf.LerpUnclamped(startAngles[i], endAngles[i], eased);
                ApplyPose(i, angle);
            }

            yield return null;
        }

        rotVal = targetSlot + 1;
        index = SlotIndex(CurSlot());
        ApplyLayout();
        UpdateName();
        SaveSelection();
        inTransit = false;
    }

    void ApplyLayout()
    {
        if (characters.Length == 0)
        {
            return;
        }

        for (int i = 0; i < characters.Length; i++)
        {
            characters[i].SetActive(false);
            characters[i].transform.localScale = baseScales[i];
        }

        int currentSlot = CurSlot();
        index = SlotIndex(currentSlot);
        characters[index].SetActive(true);
        ApplyPose(index, 0f);

        if (GroupCount() > 1)
        {
            int prev = SlotIndex(currentSlot - 1);
            characters[prev].SetActive(true);
            ApplyPose(prev, 120f);
        }

        if (GroupCount() > 2)
        {
            int next = SlotIndex(currentSlot + 1);
            characters[next].SetActive(true);
            ApplyPose(next, -120f);
        }
    }

    void ApplyPose(int characterIndex, float angleDegrees)
    {
        Transform tr = characters[characterIndex].transform;
        tr.localPosition = AnglePos(angleDegrees);
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        float depthFactor = Mathf.InverseLerp(-1f, 1f, Mathf.Cos(angleRadians));
        depthFactor = Mathf.Pow(depthFactor, Mathf.Max(0.01f, depthScaleExponent));
        float scaleFactor = Mathf.Lerp(backScale, 1f, depthFactor);
        tr.localScale = baseScales[characterIndex] * scaleFactor;
    }

    Vector3 AnglePos(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        Vector3 center = frontPos - Vector3.forward * carouselRadius;
        Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * carouselRadius;
        return center + offset;
    }

    void UpdateName()
    {
        if (characters.Length == 0)
        {
            characterNameText.text = string.Empty;
            return;
        }

        string characterName = characters[index].name.Replace(CloneSuffix, string.Empty).Trim();
        characterNameText.text = characterName;
    }

    void SaveSelection()
    {
        if (GameSession.Instance != null)
        {
            GameSession.Instance.SetChar(index);
        }
    }

    void OnGenderChanged(bool isMale)
    {
        if (characters.Length == 0 || inTransit)
        {
            return;
        }

        maleGroup = isMale;
        rotVal = WrapIndex(rotVal - 1, GroupCount()) + 1;
        index = SlotIndex(CurSlot());
        ApplyLayout();
        UpdateName();
        SaveSelection();
    }

    void InitGroup(int globalIndex)
    {
        int clampedIndex = Mathf.Clamp(globalIndex, 0, Mathf.Max(0, characters.Length - 1));
        maleGroup = clampedIndex < femaleStartIndex;
        int groupBase = GroupBase();
        int groupCount = GroupCount();
        int slot = WrapIndex(clampedIndex - groupBase, groupCount);
        rotVal = slot + 1;
    }

    int CurSlot()
    {
        return rotVal - 1;
    }

    int SlotIndex(int slot)
    {
        int groupBase = GroupBase();
        int groupCount = GroupCount();
        return Mathf.Clamp(groupBase + WrapIndex(slot, groupCount), 0, characters.Length - 1);
    }

    int GroupBase()
    {
        int candidate = maleGroup ? maleStartIndex : femaleStartIndex;
        return Mathf.Clamp(candidate, 0, Mathf.Max(0, characters.Length - 1));
    }

    int GroupCount()
    {
        int requested = Mathf.Max(1, charactersPerGender);
        int available = Mathf.Max(1, characters.Length - GroupBase());
        return Mathf.Clamp(requested, 1, available);
    }

    void OnDestroy()
    {
        if (genderToggleUI != null)
        {
            genderToggleUI.genderChanged.RemoveListener(OnGenderChanged);
        }
    }

    int WrapIndex(int value, int length)
    {
        if (length <= 0)
        {
            return 0;
        }

        value %= length;

        if (value < 0)
        {
            value += length;
        }

        return value;
    }

    void EnsureSetup()
    {
        if (charactersParent == null)
        {
            throw new InvalidOperationException("CharacterSelection setup failed: charactersParent reference is missing.");
        }

        if (characterNameText == null)
        {
            throw new InvalidOperationException("CharacterSelection setup failed: characterNameText reference is missing.");
        }
    }
}
