using System.Collections;
using UnityEngine;

public class SidebarController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform sidebarRect;      // 사이드바 전체 RectTransform
    [SerializeField] private RectTransform[] menuTextRects;  // 숨길 텍스트들의 RectTransform 배열

    // ★ [추가] 여는 버튼과 닫는 버튼 오브젝트를 각각 등록합니다.
    [Header("Button References")]
    [SerializeField] private GameObject openButton;          // 사이드바를 '여는' 버튼
    [SerializeField] private GameObject closeButton;         // 사이드바를 '닫는' 버튼

    [Header("Sidebar Animation Settings")]
    [SerializeField] private float slideSpeed = 15f;
    [SerializeField] private float openWidth = 260f;
    [SerializeField] private float closedWidth = 70f;

    [Header("Text Animation Settings")]
    [SerializeField] private float maxTextWidth = 160f;

    private bool isOpen = false;
    private Coroutine currentCoroutine;

    void Start()
    {
        // 시작할 때는 완전히 접힌 상태의 수치로 강제 초기화
        SetSidebarStateImmediate(isOpen);
    }

    // ★ [수정] 이제 이 하나의 함수를 '여는 버튼'과 '닫는 버튼'의 On Click()에 둘 다 연결하면 됩니다.
    public void ToggleSidebar()
    {
        isOpen = !isOpen;

        float targetSidebarWidth = isOpen ? openWidth : closedWidth;
        float targetTextWidth = isOpen ? maxTextWidth : 0f;

        // ★ [핵심 추가] 버튼을 누른 0초 만에 열기/닫기 버튼의 활성화 상태를 즉시 교체합니다.
        ToggleActionButtons(isOpen);

        if (isOpen)
        {
            // [열 때] : 버튼 누르자마자 텍스트 오브젝트를 켜서 함께 늘어나게 만듭니다.
            ToggleTextObjects(true);
        }
        else
        {
            // [닫을 때] : 버튼 누른 즉시 텍스트를 화면에서 없앱니다.
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

            if (isOpen)
            {
                foreach (var textRect in menuTextRects)
                {
                    if (textRect != null)
                    {
                        float currentTextWidth = Mathf.Lerp(textRect.sizeDelta.x, targetTextWidth, Time.deltaTime * slideSpeed);
                        textRect.sizeDelta = new Vector2(currentTextWidth, textRect.sizeDelta.y);
                    }
                }
            }
            yield return null;
        }

        SetSidebarStateImmediate(isOpen);
    }

    // 텍스트 오브젝트 제어
    private void ToggleTextObjects(bool visible)
    {
        foreach (var textRect in menuTextRects)
            if (textRect != null) textRect.gameObject.SetActive(visible);
    }

    // ★ [추가] 열기/닫기 버튼 상태를 스위칭하는 함수
    private void ToggleActionButtons(bool open)
    {
        if (openButton != null) openButton.SetActive(!open);  // 열려있을 때는 '여는 버튼'을 숨김
        if (closeButton != null) closeButton.SetActive(open); // 열려있을 때는 '닫는 버튼'을 보여줌
    }

    // 최종 상태 강제 고정 함수
    private void SetSidebarStateImmediate(bool open)
    {
        sidebarRect.sizeDelta = new Vector2(open ? openWidth : closedWidth, sidebarRect.sizeDelta.y);
        ToggleTextObjects(open);
        ToggleActionButtons(open); // 초기 상태에 맞춰 버튼 세팅

        foreach (var textRect in menuTextRects)
        {
            if (textRect != null) textRect.sizeDelta = new Vector2(open ? maxTextWidth : 0f, textRect.sizeDelta.y);
        }
    }
}
