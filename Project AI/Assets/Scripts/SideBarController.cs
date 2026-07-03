using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SidebarController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform sidebarRect;      // 사이드바 전체 RectTransform
    [SerializeField] private RectTransform[] menuTextRects;  // 숨길 텍스트들의 RectTransform 배열

    [Header("Overlay MiniIcon References")]
    [SerializeField] private RectTransform[] menuMiniIconRects;

    [Header("Button References")]
    [SerializeField] private GameObject openButton;          // 사이드바를 '여는' 버튼
    [SerializeField] private GameObject closeButton;         // 사이드바를 '닫는' 버튼

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

                    // ★ [버그 해결 수정] 무조건 강제로 끄지 않고, 
                    // 이 아이콘과 매칭된 메인 창들이 '현재 켜져 있는지 꺼져 있는지'를 판별해서 
                    // 초기 활성화 상태를 아주 스마트하게 판단하여 셋팅해 둡니다.
                    menuMiniIconRects[i].gameObject.SetActive(false);
                }
            }
        }

        SetSidebarStateImmediate(isOpen);
    }

    // ★ [외부 창들이 호출할 마스터 관리 함수]
    public void UpdateTaskbarStatus(int index, int status)
    {
        if (menuMiniIconRects == null || index < 0 || index >= menuMiniIconRects.Length) return;
        if (menuMiniIconRects[index] == null || miniIconImages[index] == null) return;

        if (status == 0)
        {
            // 0 : 완전히 꺼짐 -> 오버레이 오브젝트 자체를 완전히 숨깁니다.
            menuMiniIconRects[index].gameObject.SetActive(false);
        }
        else
        {
            // 1, 2 : 최소화 또는 켜짐 -> 오버레이 오브젝트를 확실하게 켭니다.
            menuMiniIconRects[index].gameObject.SetActive(true);

            float targetAlpha = (status == 2) ? activeAlpha : minimizedAlpha;
            Color color = miniIconImages[index].color;
            color.a = targetAlpha;
            miniIconImages[index].color = color;
        }
    }

    public void ToggleSidebar()
    {
        isOpen = !isOpen;

        float targetSidebarWidth = isOpen ? openWidth : closedWidth;
        float targetTextWidth = isOpen ? maxTextWidth : 0f;

        ToggleActionButtons(isOpen);

        if (isOpen)
        {
            ToggleTextObjects(true);
        }
        else
        {
            ToggleTextObjects(false);

            foreach (var textRect in menuTextRects)
            {
                if (textRect != null) textRect.sizeDelta = new Vector2(0f, textRect.sizeDelta.y);
            }
        }

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(AnimateSidebar(targetSidebarWidth, targetTextWidth));
    }

    private IEnumerator AnimateSidebar(float targetSidebarWidth, float targetTextWidth)
    {
        while (Mathf.Abs(sidebarRect.sizeDelta.x - targetSidebarWidth) > 0.5f)
        {
            float currentSidebarWidth = Mathf.Lerp(sidebarRect.sizeDelta.x, targetSidebarWidth, Time.deltaTime * slideSpeed);
            sidebarRect.sizeDelta = new Vector2(currentSidebarWidth, sidebarRect.sizeDelta.y);

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
            if (textRect != null) textRect.gameObject.SetActive(visible);
    }

    private void ToggleActionButtons(bool open)
    {
        if (openButton != null) openButton.SetActive(!open);
        if (closeButton != null) closeButton.SetActive(open);
    }

    private void SetSidebarStateImmediate(bool open)
    {
        float finalWidth = open ? openWidth : closedWidth;
        sidebarRect.sizeDelta = new Vector2(finalWidth, sidebarRect.sizeDelta.y);
        ToggleTextObjects(open);
        ToggleActionButtons(open);

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
