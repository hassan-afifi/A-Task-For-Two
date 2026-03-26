using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(TMP_Dropdown))]

// Keeps dropdown UI visuals and scroll positioning consistent.
public class DropdownHelper : MonoBehaviour
{
    private TMP_Dropdown dropdown;
    [SerializeField] private Graphic arrowGraphic;
    private bool wasExpanded;
    private bool arrowVisible;
    private bool arrowInit;
    private Coroutine centerRoutine;
    void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();

        if (arrowGraphic == null)
        {
            throw new InvalidOperationException("DropdownHelper setup failed: arrowGraphic reference is missing.");
        }

        UpdateArrow(true);
    }

    void OnEnable()
    {
        if (arrowGraphic == null)
        {
            throw new InvalidOperationException("DropdownHelper setup failed: arrowGraphic reference is missing.");
        }

        UpdateArrow(true);
    }

    void LateUpdate()
    {
        UpdateArrow(false);
        bool isExpanded = dropdown.IsExpanded;

        if (isExpanded && !wasExpanded)
        {
            if (centerRoutine != null)
            {
                StopCoroutine(centerRoutine);
            }

            centerRoutine = StartCoroutine(CenterOnOpen());
        }

        wasExpanded = isExpanded;
    }

    void UpdateArrow(bool force)
    {
        bool shouldShow = dropdown.IsInteractable();

        if (!force && arrowInit && shouldShow == arrowVisible)
        {
            return;
        }

        arrowGraphic.enabled = shouldShow;
        arrowVisible = shouldShow;
        arrowInit = true;
    }

    IEnumerator CenterOnOpen()
    {
        yield return null;
        yield return null;
        ScrollRect scrollRect = FindOpenList();

        if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null)
        {
            centerRoutine = null;
            yield break;
        }

        int optionCount = Mathf.Max(dropdown.options.Count, 1);
        int selectedIndex = Mathf.Clamp(dropdown.value, 0, optionCount - 1);
        RectTransform content = scrollRect.content;
        float contentHeight = content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;

        if (contentHeight <= viewportHeight)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            centerRoutine = null;
            yield break;
        }

        float itemHeight = GetItemHeight(content, optionCount);
        float centerFromTop = (selectedIndex + 0.5f) * itemHeight;
        float topOffset = centerFromTop - (viewportHeight * 0.5f);
        float maxOffset = Mathf.Max(0.0001f, contentHeight - viewportHeight);
        float normalized = 1f - Mathf.Clamp01(topOffset / maxOffset);
        scrollRect.verticalNormalizedPosition = normalized;
        centerRoutine = null;
    }

    float GetItemHeight(RectTransform content, int optionCount)
    {
        if (content.childCount > 0)
        {
            RectTransform first = content.GetChild(0) as RectTransform;

            if (first != null)
            {
                float firstHeight = first.rect.height;

                if (firstHeight > 0f)
                {
                    return firstHeight;
                }
            }
        }

        float estimated = content.rect.height / optionCount;
        return Mathf.Max(1f, estimated);
    }

    ScrollRect FindOpenList()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            throw new InvalidOperationException("DropdownHelper setup failed: parent Canvas is missing.");
        }

        ScrollRect[] all = canvas.rootCanvas.GetComponentsInChildren<ScrollRect>(true);
        ScrollRect best = null;
        int bestSibling = int.MinValue;

        for (int i = 0; i < all.Length; i++)
        {
            ScrollRect current = all[i];

            if (current == null || !current.gameObject.activeInHierarchy)
            {
                continue;
            }

            int sibling = current.transform.GetSiblingIndex();

            if (sibling >= bestSibling)
            {
                bestSibling = sibling;
                best = current;
            }
        }

        return best;
    }
}
