using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class WindowManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform chatWindowRect;    // 오른쪽 고정 채팅창(Chat_Panel)
    [SerializeField] private RectTransform datalogWindowRect; // 독립된 DATALOG 창
    [SerializeField] private RectTransform spawnArea;          // 일반 팝업창들이 배치될 부모 패널 (UIObject)

    [Header("Slide Settings (Datalog)")]
    [SerializeField] private float slideDuration = 0.4f;      // 슬라이드 애니메이션 시간

    [Header("Target Position Settings (Datalog 정지 위치)")]
    [SerializeField] private Vector2 targetPosition = new Vector2(-460f, -137f); // 정지 좌표

    [Header("UIObject Spawn Settings (일반 창 랜덤 배치)")]
    [SerializeField] private int maxTryCount = 30;        // 겹침 방지 재시도 최대 횟수
    [SerializeField] private float padding = 30f;         // 화면 경계면과의 최소 여백

    [Header("Sidebar Settings (사이드바 너비 설정)")]
    [SerializeField] private float sidebarClosedWidth = 88f;  // 👈 닫혔을 때 기본 너비 (항상 보장됨)
    [SerializeField] private float sidebarOpenWidth = 250f;   // 👈 사이드바가 완전히 열렸을 때 전체 너비

    [Header("Chat Dynamic Move Settings (채팅창 연동 밀기)")]
    [SerializeField] private float chatPanelWidth = 350f;  // 오른쪽 채팅창이 차지하는 실제 가로 너비

    [Header("New: Datalog Move Settings (데이터로그 연동 밀기)")]
    [SerializeField] private float datalogPushAmount = 300f; // 데이터로그 창이 열릴 때 기존 창들을 밀어낼 거리

    private bool isDatalogOpen = false;
    private bool isSidebarOpen = false;                   // 시작할 때는 기본(닫힌 상태, 88)으로 가정
    private bool isChatOpen = false;                      // 현재 채팅창이 열려있는지 상태 기억
    private Vector2 initialHidePosition;                  // 화면 우측 밖(기본 숨김 위치)

    void Start()
    {
        if (datalogWindowRect != null)
        {
            initialHidePosition = new Vector2(500f, targetPosition.y);
            datalogWindowRect.anchoredPosition = initialHidePosition;
            datalogWindowRect.gameObject.SetActive(true);

            CanvasGroup canvasGroup = datalogWindowRect.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = datalogWindowRect.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            isDatalogOpen = false;
        }
    }

    #region 데이터로그 및 채팅창 제어 관련 함수

    public void ToggleDatalogWindow()
    {
        if (datalogWindowRect == null) return;
        if (DOTween.IsTweening(datalogWindowRect)) return;

        isDatalogOpen = !isDatalogOpen;

        if (isDatalogOpen)
        {
            OpenDatalogDirect();
            PushWindowsLeftOnDatalog(datalogPushAmount, slideDuration);
        }
        else
        {
            CloseDatalogDirect();
            PullWindowsRightOnDatalog(datalogPushAmount, slideDuration);
        }
    }

    private void OpenDatalogDirect()
    {
        datalogWindowRect.DOKill();
        CanvasGroup canvasGroup = datalogWindowRect.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, slideDuration);
            canvasGroup.blocksRaycasts = true;
        }

        datalogWindowRect.DOAnchorPos(targetPosition, slideDuration).SetEase(Ease.OutQuad);
    }

    public void CloseDatalogDirect()
    {
        if (datalogWindowRect == null) return;

        datalogWindowRect.DOKill();

        if (isDatalogOpen)
        {
            PullWindowsRightOnDatalog(datalogPushAmount, slideDuration);
        }

        isDatalogOpen = false;

        CanvasGroup canvasGroup = datalogWindowRect.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, slideDuration);
            canvasGroup.blocksRaycasts = false;
        }

        datalogWindowRect.DOAnchorPos(initialHidePosition, slideDuration).SetEase(Ease.OutQuad);
    }

    public void RepositionWindow(RectTransform targetWindow)
    {
        if (targetWindow == datalogWindowRect)
        {
            if (!isDatalogOpen)
            {
                isDatalogOpen = true;
                OpenDatalogDirect();
                PushWindowsLeftOnDatalog(datalogPushAmount, slideDuration);
            }
        }
    }

    public void PushWindowsLeft(float pushAmount, float duration)
    {
        isChatOpen = true;

        List<RectTransform> activePopups = GetActivePopupWindows();
        foreach (var win in activePopups)
        {
            if (win == null) continue;

            float targetX = win.anchoredPosition.x - chatPanelWidth;

            // 현재 사이드바 상태(열림/닫힘)에 맞춰 안전 좌측 한계선 설정
            float currentSidebar = isSidebarOpen ? sidebarOpenWidth : sidebarClosedWidth;
            float minX = -spawnArea.rect.width / 2f + currentSidebar + (win.rect.width * win.pivot.x) + padding;

            if (targetX < minX) targetX = minX;

            win.DOKill();
            win.DOAnchorPosX(targetX, duration).SetEase(Ease.OutQuad);
        }
    }

    public void PullWindowsRight(float pushAmount, float duration)
    {
        isChatOpen = false;

        if (isDatalogOpen)
        {
            PullWindowsRightOnDatalog(datalogPushAmount, duration);
            isDatalogOpen = false;
            datalogWindowRect.DOKill();
            CanvasGroup canvasGroup = datalogWindowRect.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, duration);
                canvasGroup.blocksRaycasts = false;
            }
            datalogWindowRect.DOAnchorPos(initialHidePosition, duration).SetEase(Ease.OutQuad);
        }

        List<RectTransform> activePopups = GetActivePopupWindows();
        foreach (var win in activePopups)
        {
            if (win == null) continue;

            float targetX = win.anchoredPosition.x + chatPanelWidth;
            float maxX = spawnArea.rect.width / 2f - (win.rect.width * (1f - win.pivot.x)) - padding;
            if (targetX > maxX) targetX = maxX;

            win.DOKill();
            win.DOAnchorPosX(targetX, duration).SetEase(Ease.OutQuad);
        }
    }

    private void PushWindowsLeftOnDatalog(float amount, float duration)
    {
        List<RectTransform> activePopups = GetActivePopupWindows();
        foreach (var win in activePopups)
        {
            if (win == null) continue;

            float targetX = win.anchoredPosition.x - amount;

            float currentSidebar = isSidebarOpen ? sidebarOpenWidth : sidebarClosedWidth;
            float minX = -spawnArea.rect.width / 2f + currentSidebar + (win.rect.width * win.pivot.x) + padding;

            if (targetX < minX) targetX = minX;

            win.DOKill();
            win.DOAnchorPosX(targetX, duration).SetEase(Ease.OutQuad);
        }
    }

    private void PullWindowsRightOnDatalog(float amount, float duration)
    {
        List<RectTransform> activePopups = GetActivePopupWindows();
        foreach (var win in activePopups)
        {
            if (win == null) continue;

            float targetX = win.anchoredPosition.x + amount;
            float maxX = spawnArea.rect.width / 2f - (win.rect.width * (1f - win.pivot.x)) - padding;
            if (isChatOpen) maxX -= chatPanelWidth;

            if (targetX > maxX) targetX = maxX;

            win.DOKill();
            win.DOAnchorPosX(targetX, duration).SetEase(Ease.OutQuad);
        }
    }

    #endregion

    #region 사이드바 제어 관련 함수

    public void PushWindowsRightOnSidebarOpen(float duration)
    {
        isSidebarOpen = true;
        List<RectTransform> activePopups = GetActivePopupWindows();

        foreach (var win in activePopups)
        {
            if (win == null) continue;

            float winLeftX = win.anchoredPosition.x - (win.rect.width * win.pivot.x);
            float spawnAreaLeftBoundary = -spawnArea.rect.width / 2f;

            // 사이드바가 열렸을 때 영역(sidebarOpenWidth)을 침범하는 창들만 오른쪽으로 밀어내기
            float dangerZoneX = spawnAreaLeftBoundary + sidebarOpenWidth + padding;
            if (winLeftX < dangerZoneX)
            {
                float targetX = dangerZoneX + (win.rect.width * win.pivot.x);
                win.DOKill();
                win.DOAnchorPosX(targetX, duration).SetEase(Ease.OutQuad);
            }
        }
    }

    public void PullWindowsLeftOnSidebarClose(float duration)
    {
        isSidebarOpen = false;
        List<RectTransform> activePopups = GetActivePopupWindows();

        // 열린 너비와 닫힌 너비의 차이만큼만 당겨줍니다.
        float shiftWidth = sidebarOpenWidth - sidebarClosedWidth;

        foreach (var win in activePopups)
        {
            if (win == null) continue;

            float spawnAreaLeftBoundary = -spawnArea.rect.width / 2f;
            // 닫혔을 때 안전 한계선 (88 + 여백)
            float safeMinX = spawnAreaLeftBoundary + sidebarClosedWidth + (win.rect.width * win.pivot.x) + padding;

            if (win.anchoredPosition.x > safeMinX + shiftWidth)
            {
                float targetX = win.anchoredPosition.x - shiftWidth;
                if (targetX < safeMinX) targetX = safeMinX;

                win.DOKill();
                win.DOAnchorPosX(targetX, duration).SetEase(Ease.OutQuad);
            }
        }
    }

    #endregion

    #region 일반 UIObject 내부 창들 랜덤 배치 기능

    private List<RectTransform> GetActivePopupWindows()
    {
        List<RectTransform> activeList = new List<RectTransform>();
        if (spawnArea == null) return activeList;

        for (int i = 0; i < spawnArea.childCount; i++)
        {
            RectTransform child = spawnArea.GetChild(i) as RectTransform;
            if (child != null && child.gameObject.activeInHierarchy &&
                child != datalogWindowRect && child != chatWindowRect)
            {
                activeList.Add(child);
            }
        }
        return activeList;
    }

    public void RepositionPopupWindow(RectTransform targetWindow)
    {
        if (targetWindow == null || spawnArea == null) return;
        if (targetWindow == datalogWindowRect || targetWindow == chatWindowRect) return;
        targetWindow.transform.SetAsLastSibling();
        Vector2 validPosition = GetValidRandomPosition(targetWindow, targetWindow.rect.size);
        targetWindow.anchoredPosition = validPosition;
    }

    private Vector2 GetValidRandomPosition(RectTransform targetWin, Vector2 size)
    {
        float spawnAreaLeftBoundary = -spawnArea.rect.width / 2f;

        // 사이드바 상태(열림/닫힘)에 따라 왼쪽 한계선을 다이내믹하게 처리 (닫혔을 때도 무조건 88 보장)
        float currentSidebarWidth = isSidebarOpen ? sidebarOpenWidth : sidebarClosedWidth;

        float minX = spawnAreaLeftBoundary + currentSidebarWidth + (size.x * targetWin.pivot.x) + padding;
        float maxX = spawnArea.rect.width / 2f - (size.x * (1f - targetWin.pivot.x)) - padding;
        float minY = -spawnArea.rect.height / 2f + (size.y * targetWin.pivot.y) + padding;
        float maxY = spawnArea.rect.height / 2f - (size.y * (1f - targetWin.pivot.y)) - padding;

        if (isChatOpen)
        {
            maxX -= chatPanelWidth;
        }

        if (isDatalogOpen)
        {
            maxX -= datalogPushAmount;
        }

        // 예외 방어 (minX가 maxX보다 큰 경우 값 강제 조율)
        if (minX > maxX)
        {
            minX = maxX;
        }

        Vector2 targetPos = Vector2.zero;
        bool isOverlapping = true;
        int tries = 0;

        while (isOverlapping && tries < maxTryCount)
        {
            float randX = Random.Range(minX, maxX);
            float randY = Random.Range(minY, maxY);
            targetPos = new Vector2(randX, randY);

            isOverlapping = CheckOverlap(targetWin, targetPos, size);
            tries++;
        }

        return targetPos;
    }

    private bool CheckOverlap(RectTransform targetWin, Vector2 targetPos, Vector2 size)
    {
        Vector2 targetPivotOffset = new Vector2(size.x * targetWin.pivot.x, size.y * targetWin.pivot.y);
        Rect newRect = new Rect(targetPos - targetPivotOffset, size);

        // 🌟 제네릭 인수 <RectTransform> 누락을 수정하여 컴파일 에러 근본 해결!
        List<RectTransform> activePopups = GetActivePopupWindows();
        foreach (var win in activePopups)
        {
            if (win == null || win == targetWin) continue;
            Vector2 winPivotOffset = new Vector2(win.rect.size.x * win.pivot.x, win.rect.size.y * win.pivot.y);
            Rect existingRect = new Rect(win.anchoredPosition - winPivotOffset, win.rect.size);
            if (newRect.Overlaps(existingRect))
            {
                return true;
            }
        }
        return false;
    }
    #endregion
}