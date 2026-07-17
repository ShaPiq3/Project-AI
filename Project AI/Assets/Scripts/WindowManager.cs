using UnityEngine;
using System.Collections.Generic; // 👈 이 부분이 표준 List 사용을 위해 꼭 필요합니다.
using DG.Tweening;

public class WindowManager : MonoBehaviour
{
    public static WindowManager Instance { get; private set; }

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
    [SerializeField] private float sidebarClosedWidth = 88f;  // 닫혔을 때 기본 너비 (항상 보장됨)
    [SerializeField] private float sidebarOpenWidth = 250f;   // 사이드바가 완전히 열렸을 때 전체 너비

    [Header("Chat Dynamic Move Settings (채팅창 연동 밀기)")]
    [SerializeField] private float chatPanelWidth = 350f;  // 오른쪽 채팅창이 차지하는 실제 가로 너비

    [Header("New: Datalog Move Settings (데이터로그 연동 밀기)")]
    [SerializeField] private float datalogPushAmount = 300f; // 데이터로그 창이 열릴 때 기존 창들을 밀어낼 거리

    private bool isDatalogOpen = false;
    private bool isSidebarOpen = false;                   // 시작할 때는 기본(닫힌 상태, 88)으로 가정
    public bool isChatOpen = false;                      // 현재 채팅창이 열려있는지 상태 기억
    private Vector2 initialHidePosition;                  // 화면 우측 밖(기본 숨김 위치)

    // ⭐ [CS0122 해결] ChatDialogueManager에서 접근 가능하도록 public 프로퍼티를 제공합니다.
    public bool IsChatOpen => isChatOpen;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 중복 방지
        }
    }

    void Start()
    {
        if (datalogWindowRect != null)
        {
            // Datalog 창의 가로 너비(+ 여백 20)를 자동으로 Push Amount로 설정
            datalogPushAmount = datalogWindowRect.rect.width + 20f;

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

    // ⭐ [CS1061 해결] ChatDialogueManager에서 호출하는 갱신 메서드를 추가합니다.
    // 현재 활성화된 모든 팝업 창들이 변경된 화면 벽(사이드바, 채팅창 등) 안으로 들어오도록 위치를 제한(Clamp)해 주는 역할을 합니다.
    public void RefreshAllWindows()
    {
        List<RectTransform> activePopups = GetActivePopupWindows();
        foreach (var win in activePopups)
        {
            if (win == null) continue;

            // 현재 위치를 기준으로 제한 영역 내에 가둬둡니다.
            Vector2 clampedPos = ClampWindowPosition(win, win.anchoredPosition);

            win.DOKill();
            win.DOAnchorPos(clampedPos, slideDuration).SetEase(Ease.OutQuad);
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

        System.Collections.Generic.List<RectTransform> activePopups = GetActivePopupWindows();
        foreach (var win in activePopups)
        {
            if (win == null) continue;

            float targetX = win.anchoredPosition.x - chatPanelWidth;

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

        System.Collections.Generic.List<RectTransform> activePopups = GetActivePopupWindows();
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
        System.Collections.Generic.List<RectTransform> activePopups = GetActivePopupWindows();
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
        System.Collections.Generic.List<RectTransform> activePopups = GetActivePopupWindows();
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
        System.Collections.Generic.List<RectTransform> activePopups = GetActivePopupWindows();

        foreach (var win in activePopups)
        {
            if (win == null) continue;

            float winLeftX = win.anchoredPosition.x - (win.rect.width * win.pivot.x);
            float spawnAreaLeftBoundary = -spawnArea.rect.width / 2f;

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
        System.Collections.Generic.List<RectTransform> activePopups = GetActivePopupWindows();

        float shiftWidth = sidebarOpenWidth - sidebarClosedWidth;

        foreach (var win in activePopups)
        {
            if (win == null) continue;

            float spawnAreaLeftBoundary = -spawnArea.rect.width / 2f;
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

    #region 드래그 및 배치 시 영역을 벽처럼 가두는 제한 함수 (Clamp)

    public Vector2 ClampWindowPosition(RectTransform targetWin, Vector2 currentPos)
    {
        if (targetWin == null || spawnArea == null) return currentPos;

        float spawnAreaLeftBoundary = -spawnArea.rect.width / 2f;
        float sizeX = targetWin.rect.width;
        float sizeY = targetWin.rect.height;

        float currentSidebarWidth = isSidebarOpen ? sidebarOpenWidth : sidebarClosedWidth;

        float minX = spawnAreaLeftBoundary + currentSidebarWidth + (sizeX * targetWin.pivot.x) + padding;
        float maxX = spawnArea.rect.width / 2f - (sizeX * (1f - targetWin.pivot.x)) - padding;

        if (isChatOpen)
        {
            maxX -= chatPanelWidth;
        }

        if (isDatalogOpen)
        {
            maxX -= datalogPushAmount;
        }

        float minY = -spawnArea.rect.height / 2f + (sizeY * targetWin.pivot.y) + padding;
        float maxY = spawnArea.rect.height / 2f - (sizeY * (1f - targetWin.pivot.y)) - padding;

        if (minX > maxX) minX = maxX;

        float clampedX = Mathf.Clamp(currentPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(currentPos.y, minY, maxY);

        return new Vector2(clampedX, clampedY);
    }

    #endregion

    #region 일반 UIObject 내부 창들 랜덤 배치 기능

    private System.Collections.Generic.List<RectTransform> GetActivePopupWindows()
    {
        System.Collections.Generic.List<RectTransform> activeList = new System.Collections.Generic.List<RectTransform>();
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

        System.Collections.Generic.List<RectTransform> activePopups = GetActivePopupWindows();
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