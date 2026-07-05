using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SidebarController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform sidebarRect;      // 사이드바 전체 RectTransform
    [SerializeField] private RectTransform[] menuTextRects;  // 숨길 텍스트들의 RectTransform 배열

    // ★ [버그 해결 핵심 추가] 제미나이 스타일 마스터 버튼들(Menu_Item_Archive 등)의 RectTransform 배열
    [SerializeField] private RectTransform[] menuButtonRects;

    [Header("Overlay MiniIcon References")]
    [SerializeField] private RectTransform[] menuMiniIconRects;

    [Header("Button References")]
    [SerializeField] private Button openButton;          // 사이드바를 '여는' 마스터 버튼
    [SerializeField] private Button closeButton;         // 사이드바를 '닫는' 버튼

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
                    menuMiniIconRects[i].gameObject.SetActive(false);
                }
            }
        }

        SetSidebarStateImmediate(isOpen);
    }

    public void UpdateTaskbarStatus(int index, int status)
    {
        if (menuMiniIconRects == null || index < 0 || index >= menuMiniIconRects.Length) return;
        if (menuMiniIconRects[index] == null || miniIconImages[index] == null) return;

        if (status == 0)
        {
            menuMiniIconRects[index].gameObject.SetActive(false);
        }
        else
        {
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

            // ★ [버그 해결 핵심] 마스터 버튼 컴포넌트들의 전체 클릭 범위(Width)도 사이드바 크기와 실시간 동기화
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

        // ★ [버그 해결 핵심] 즉시 상태가 변할 때도 버튼 범위를 최종 크기(70 또는 280)로 강제 고정
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