using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SidebarController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform sidebarRect;
    [SerializeField] private RectTransform[] menuTextRects;
    [SerializeField] private RectTransform[] menuButtonRects;

    [Header("Overlay MiniIcon References")]
    [SerializeField] private RectTransform[] menuMiniIconRects;

    [Header("Button References")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Sidebar Animation Settings")]
    [SerializeField] private float slideSpeed = 15f;
    [SerializeField] private float openWidth = 280f;
    [SerializeField] private float closedWidth = 70f;

    [Header("Text Animation Settings")]
    [SerializeField] private float maxTextWidth = 180f;

    [Header("Windows Taskbar Settings")]
    [Range(0f, 1f)][SerializeField] private float activeAlpha = 0.2f;
    [Range(0f, 1f)][SerializeField] private float minimizedAlpha = 0.7f;

    private bool isOpen = false;
    private Coroutine currentCoroutine;
    private float[] miniIconHeights;
    private Image[] miniIconImages;

    // 💡 [추가] 인덱스별로 "열려있는(open)" 창과 "최소화(minimized)"된 창을 참조로 추적합니다.
    // 같은 인덱스를 공유하는 여러 창(메인 창 + 상세 창들) 중 하나가 닫혀도,
    // 나머지가 남아있으면 아이콘이 꺼지지 않도록 하기 위함입니다.
    private Dictionary<int, HashSet<object>> openWindowsByIndex = new Dictionary<int, HashSet<object>>();
    private Dictionary<int, HashSet<object>> minimizedWindowsByIndex = new Dictionary<int, HashSet<object>>();

    void Start()
    {
        if (menuMiniIconRects != null)
        {
            miniIconHeights = new float[menuMiniIconRects.Length];
            miniIconImages = new Image[menuMiniIconRects.Length];

            for (int i = 0; i < menuMiniIconRects.Length; i++)
            {
                if (menuMiniIconRects[i] != null)
                {
                    miniIconHeights[i] = menuMiniIconRects[i].rect.height;
                    miniIconImages[i] = menuMiniIconRects[i].GetComponent<Image>();
                    menuMiniIconRects[i].gameObject.SetActive(false);
                }
            }
        }

        SetSidebarStateImmediate(isOpen);
    }

    /// <summary>
    /// 💡 [변경] windowRef(호출한 창 자기 자신, 보통 this)를 함께 넘겨서
    /// 인덱스별 참조 카운팅을 합니다.
    /// status: 0=완전히 닫힘, 1=최소화, 2=열림(활성)
    /// </summary>
    public void UpdateTaskbarStatus(int index, int status, object windowRef)
    {
        if (menuMiniIconRects == null || index < 0 || index >= menuMiniIconRects.Length) return;
        if (menuMiniIconRects[index] == null || miniIconImages[index] == null) return;
        if (windowRef == null) return;

        if (!openWindowsByIndex.ContainsKey(index)) openWindowsByIndex[index] = new HashSet<object>();
        if (!minimizedWindowsByIndex.ContainsKey(index)) minimizedWindowsByIndex[index] = new HashSet<object>();

        var openSet = openWindowsByIndex[index];
        var minSet = minimizedWindowsByIndex[index];

        // 일단 두 집합에서 이 창을 제거 (상태를 새로 반영하기 위해)
        openSet.Remove(windowRef);
        minSet.Remove(windowRef);

        if (status == 2) openSet.Add(windowRef);
        else if (status == 1) minSet.Add(windowRef);
        // status == 0(완전히 닫힘)이면 그냥 제거된 채로 둠

        RefreshIconState(index);
    }

    private void RefreshIconState(int index)
    {
        bool hasOpen = openWindowsByIndex.TryGetValue(index, out var openSet) && openSet.Count > 0;
        bool hasMinimized = minimizedWindowsByIndex.TryGetValue(index, out var minSet) && minSet.Count > 0;

        if (!hasOpen && !hasMinimized)
        {
            menuMiniIconRects[index].gameObject.SetActive(false);
            return;
        }

        menuMiniIconRects[index].gameObject.SetActive(true);

        // 💡 열려있는 창이 하나라도 있으면 activeAlpha, 전부 최소화 상태면 minimizedAlpha
        float targetAlpha = hasOpen ? activeAlpha : minimizedAlpha;
        Color color = miniIconImages[index].color;
        color.a = targetAlpha;
        miniIconImages[index].color = color;
    }

    public void ToggleSidebar()
    {
        isOpen = !isOpen;

        float targetSidebarWidth = isOpen ? openWidth : closedWidth;
        float targetTextWidth = isOpen ? maxTextWidth : 0f;

        ToggleActionButtons(isOpen);

        if (isOpen) ToggleTextObjects(true);
        else ToggleTextObjects(false);

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(AnimateSidebar(targetSidebarWidth, targetTextWidth));
    }

    private IEnumerator AnimateSidebar(float targetSidebarWidth, float targetTextWidth)
    {
        while (Mathf.Abs(sidebarRect.sizeDelta.x - targetSidebarWidth) > 0.5f)
        {
            float currentSidebarWidth = Mathf.Lerp(sidebarRect.sizeDelta.x, targetSidebarWidth, Time.deltaTime * slideSpeed);
            sidebarRect.sizeDelta = new Vector2(currentSidebarWidth, sidebarRect.sizeDelta.y);

            if (menuButtonRects != null)
            {
                foreach (var btnRect in menuButtonRects)
                {
                    if (btnRect != null)
                        btnRect.sizeDelta = new Vector2(currentSidebarWidth, btnRect.sizeDelta.y);
                }
            }

            for (int i = 0; i < menuMiniIconRects.Length; i++)
            {
                if (menuMiniIconRects[i] != null)
                {
                    menuMiniIconRects[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentSidebarWidth);
                    menuMiniIconRects[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, miniIconHeights[i]);
                }
            }

            foreach (var textRect in menuTextRects)
            {
                if (textRect != null)
                {
                    float currentTextWidth = Mathf.Lerp(textRect.sizeDelta.x, targetTextWidth, Time.deltaTime * slideSpeed);
                    textRect.sizeDelta = new Vector2(currentTextWidth, textRect.sizeDelta.y);
                }
            }
            yield return null;
        }

        SetSidebarStateImmediate(isOpen);
    }

    private void ToggleTextObjects(bool visible)
    {
        foreach (var textRect in menuTextRects)
        {
            if (textRect != null)
            {
                textRect.gameObject.SetActive(visible);

                var tmpText = textRect.GetComponent<TMPro.TMP_Text>();
                if (tmpText != null) tmpText.raycastTarget = visible;

                var normalText = textRect.GetComponent<UnityEngine.UI.Text>();
                if (normalText != null) normalText.raycastTarget = visible;
            }
        }
    }

    private void ToggleActionButtons(bool open)
    {
        if (openButton != null)
        {
            Image openBtnImage = openButton.GetComponent<Image>();
            if (openBtnImage != null) openBtnImage.raycastTarget = !open;
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(open);
        }
    }

    private void SetSidebarStateImmediate(bool open)
    {
        float finalWidth = open ? openWidth : closedWidth;
        sidebarRect.sizeDelta = new Vector2(finalWidth, sidebarRect.sizeDelta.y);

        ToggleTextObjects(open);
        ToggleActionButtons(open);

        if (menuButtonRects != null)
        {
            foreach (var btnRect in menuButtonRects)
            {
                if (btnRect != null)
                    btnRect.sizeDelta = new Vector2(finalWidth, btnRect.sizeDelta.y);
            }
        }

        foreach (var textRect in menuTextRects)
        {
            if (textRect != null)
            {
                textRect.sizeDelta = new Vector2(open ? maxTextWidth : 0f, textRect.sizeDelta.y);
            }
        }

        if (menuMiniIconRects != null && miniIconHeights != null)
        {
            for (int i = 0; i < menuMiniIconRects.Length; i++)
            {
                if (menuMiniIconRects[i] != null && i < miniIconHeights.Length)
                {
                    menuMiniIconRects[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
                    menuMiniIconRects[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, miniIconHeights[i]);
                }
            }
        }
    }
}