using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DropdownHelper : MonoBehaviour
{
    private const string ArrowObjectName = "Arrow";
    private const string DropdownNameToken = "Dropdown";

    private TMP_Dropdown dropdown;
    private Graphic arrowGraphic;
    private bool arrowSearchDone;
    private bool wasExpanded;
    private bool arrowVisible;
    private bool arrowInit;
    private Coroutine centerRoutine;

    void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null)
        {
            enabled = false;
            return;
        }

        CacheArrow();
        UpdateArrow(true);
    }

    void OnEnable()
    {
        if (dropdown == null)
        {
            dropdown = GetComponent<TMP_Dropdown>();
            if (dropdown == null)
            {
                enabled = false;
                return;
            }
        }

        CacheArrow();
        UpdateArrow(true);
    }

    void LateUpdate()
    {
        if (dropdown == null)
        {
            return;
        }

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

    void CacheArrow()
    {
        if (arrowGraphic != null || dropdown == null || arrowSearchDone)
        {
            return;
        }

        Transform directArrow = dropdown.transform.Find(ArrowObjectName);
        if (directArrow != null)
        {
            arrowGraphic = directArrow.GetComponent<Graphic>();
        }

        if (arrowGraphic != null)
        {
            arrowSearchDone = true;
            return;
        }

        Graphic[] graphics = dropdown.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic current = graphics[i];
            if (current == null)
            {
                continue;
            }

            if (string.Equals(current.gameObject.name, ArrowObjectName, StringComparison.OrdinalIgnoreCase))
            {
                arrowGraphic = current;
                arrowSearchDone = true;
                return;
            }
        }

        arrowSearchDone = true;
    }

    void UpdateArrow(bool force)
    {
        if (dropdown == null)
        {
            return;
        }

        CacheArrow();
        if (arrowGraphic == null)
        {
            return;
        }

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
            return null;
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

            string name = current.gameObject.name;
            if (!name.Contains(DropdownNameToken))
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
