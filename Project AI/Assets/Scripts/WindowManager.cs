using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class WindowManager : MonoBehaviour
{
    public static WindowManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private RectTransform chatWindowRect;
    [SerializeField] private RectTransform datalogWindowRect;
    [SerializeField] private RectTransform spawnArea;

    [Header("ImageGen Panel Settings")]
    [SerializeField] private RectTransform imageGenWindowRect;
    [SerializeField] private Vector2 imageGenTargetPosition = new Vector2(-460f, 200f);
    private Vector2 imageGenInitialHidePosition;
    private bool isImageGenOpen = false;
    private float imageGenPushAmount;

    [Header("Slide Settings (Datalog)")]
    [SerializeField] private float slideDuration = 0.4f;

    [Header("Target Position Settings (Datalog 정지 위치)")]
    [SerializeField] private Vector2 targetPosition = new Vector2(-460f, -137f);

    [Header("UIObject Spawn Settings (일반 창 랜덤 배치)")]
    [SerializeField] private int maxTryCount = 30;
    [SerializeField] private float padding = 30f;

    [Header("Sidebar Settings (사이드바 너비 설정)")]
    [SerializeField] private float sidebarClosedWidth = 88f;
    [SerializeField] private float sidebarOpenWidth = 250f;

    [Header("Chat Dynamic Move Settings (채팅창 연동 밀기)")]
    [SerializeField] private float chatPanelWidth = 350f;

    [Header("New: Datalog Move Settings (데이터로그 연동 밀기)")]
    [SerializeField] private float datalogPushAmount = 300f;

    [Header("Cascade Placement Settings (이전 창 근처 배치)")]
    [SerializeField] private float nearSearchStartRadius = 60f;
    [SerializeField] private float nearSearchRadiusStep = 60f;
    [SerializeField] private int nearSearchMaxRadiusSteps = 6;
    private RectTransform lastOpenedWindow;

    private bool isDatalogOpen = false;
    private bool isSidebarOpen = false;
    public bool isChatOpen = false;
    private Vector2 initialHidePosition;

    public bool IsChatOpen => isChatOpen;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (datalogWindowRect != null)
        {
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

        // 💡 [추가] 이미지 생성 패널도 datalog와 동일하게 초기화
        if (imageGenWindowRect != null)
        {
            imageGenPushAmount = imageGenWindowRect.rect.width + 20f;

            imageGenInitialHidePosition = new Vector2(500f, imageGenTargetPosition.y);
            imageGenWindowRect.anchoredPosition = imageGenInitialHidePosition;
            imageGenWindowRect.gameObject.SetActive(true);

            CanvasGroup canvasGroup = imageGenWindowRect.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = imageGenWindowRect.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            isImageGenOpen = false;
        }
    }

    public void RefreshAllWindows()
    {
        List<RectTransform> activePopups = GetActivePopupWindows();
        foreach (var win in activePopups)
        {
            if (win == null) continue;

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
            // 💡 [추가] imageGen이 열려있다면 먼저 닫음 (동시 등장 방지)
            if (isImageGenOpen)
            {
                isImageGenOpen = false;
                CloseImageGenDirect();
            }

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

    // 💡 [추가] 이미지 생성 패널 열기/닫기 (Datalog와 동일한 패턴)
    public void ToggleImageGenWindow()
    {
        if (imageGenWindowRect == null) return;
        if (DOTween.IsTweening(imageGenWindowRect)) return;

        isImageGenOpen = !isImageGenOpen;

        if (isImageGenOpen)
        {
            // 💡 [추가] datalog가 열려있다면 먼저 닫음 (동시 등장 방지)
            if (isDatalogOpen)
            {
                isDatalogOpen = false;
                CloseDatalogDirect();
                PullWindowsRightOnDatalog(datalogPushAmount, slideDuration);
            }

            OpenImageGenDirect();
            PushWindowsLeftOnImageGen(imageGenPushAmount, slideDuration);
        }
        else
        {
            CloseImageGenDirect();
            PullWindowsRightOnImageGen(imageGenPushAmount, slideDuration);
        }
    }

    private void PushWindowsLeftOnImageGen(float amount, float duration)
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

    private void PullWindowsRightOnImageGen(float amount, float duration)
    {
        List<RectTransform> activePopups = GetActivePopupWindows();
        foreach (var win in activePopups)
        {
            if (win == null) continue;

            float targetX = win.anchoredPosition.x + amount;
            float maxX = spawnArea.rect.width / 2f - (win.rect.width * (1f - win.pivot.x)) - padding;
            if (isChatOpen) maxX -= chatPanelWidth;
            if (isDatalogOpen) maxX -= datalogPushAmount;

            if (targetX > maxX) targetX = maxX;

            win.DOKill();
            win.DOAnchorPosX(targetX, duration).SetEase(Ease.OutQuad);
        }
    }

    private void OpenImageGenDirect()
    {
        imageGenWindowRect.DOKill();
        CanvasGroup canvasGroup = imageGenWindowRect.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, slideDuration);
            canvasGroup.blocksRaycasts = true;
        }

        imageGenWindowRect.DOAnchorPos(imageGenTargetPosition, slideDuration).SetEase(Ease.OutQuad);
    }

    public void CloseImageGenDirect()
    {
        if (imageGenWindowRect == null) return;

        imageGenWindowRect.DOKill();
        isImageGenOpen = false;

        CanvasGroup canvasGroup = imageGenWindowRect.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, slideDuration);
            canvasGroup.blocksRaycasts = false;
        }

        imageGenWindowRect.DOAnchorPos(imageGenInitialHidePosition, slideDuration).SetEase(Ease.OutQuad);
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
        else if (targetWindow == imageGenWindowRect)
        {
            if (!isImageGenOpen)
            {
                isImageGenOpen = true;
                OpenImageGenDirect();
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

    // 💡 [추가] ChatDialogueManager 등 외부에서 chatPanelWidth 값을 읽을 수 있도록
    public float GetChatPanelWidth()
    {
        return chatPanelWidth;
    }

    // 💡 [추가] DataLogManager가 자체적으로 패널을 열었을 때(ToggleLogPanel 경유),
    // isDatalogOpen 상태와 다른 창 밀어내기만 동기화합니다. 패널 자체의 위치/애니메이션은 건드리지 않습니다.
    public void NotifyDatalogOpenedExternally()
    {
        if (isDatalogOpen) return;

        isDatalogOpen = true;
        PushWindowsLeftOnDatalog(datalogPushAmount, slideDuration);
    }

    // 💡 [추가] DataLogManager가 자체적으로 패널을 닫았을 때(HideLogPanel 경유) 동기화합니다.
    public void NotifyDatalogClosedExternally()
    {
        if (!isDatalogOpen) return;

        isDatalogOpen = false;
        PullWindowsRightOnDatalog(datalogPushAmount, slideDuration);
    }

    public void NotifyImageGenOpenedExternally()
    {
        if (isImageGenOpen) return;

        if (isDatalogOpen)
        {
            isDatalogOpen = false;
            CloseDatalogDirect();
            PullWindowsRightOnDatalog(datalogPushAmount, slideDuration);
        }

        isImageGenOpen = true;
        PushWindowsLeftOnImageGen(imageGenPushAmount, slideDuration);
    }

    // 💡 [추가] ImageGenerationManager가 자체적으로 패널을 닫았을 때(ClosePanel 경유) 동기화합니다.
    public void NotifyImageGenClosedExternally()
    {
        if (!isImageGenOpen) return;

        isImageGenOpen = false;
        PullWindowsRightOnImageGen(imageGenPushAmount, slideDuration);
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

        float minY = -spawnArea.rect.height / 2f + (sizeY * targetWin.pivot.y) + padding;
        float maxY = spawnArea.rect.height / 2f - (sizeY * (1f - targetWin.pivot.y)) - padding;

        if (minX > maxX) minX = maxX;

        float clampedX = Mathf.Clamp(currentPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(currentPos.y, minY, maxY);

        Vector2 clamped = new Vector2(clampedX, clampedY);

        // 💡 [핵심 추가] 데이터로그/이미지생성/채팅창처럼 열려있는 "벽" 패널과 겹치면 밀어냄
        clamped = PushOutOfBlockingPanels(targetWin, clamped);

        // 💡 [추가] 벽에서 밀려난 결과가 화면 경계를 벗어날 수 있으므로, 다시 한번 경계 안으로 고정
        clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
        clamped.y = Mathf.Clamp(clamped.y, minY, maxY);

        return clamped;
    }

    /// <summary>
    /// 💡 [신규] datalog, imageGen, chat 중 현재 "열려있는" 패널들을 장애물 사각형으로 취급하여
    /// targetWin이 그 사각형과 겹치면, 가장 짧은 이동 거리로 바깥으로 밀어냅니다.
    /// 드래그 중이든(OnDrag), 새로 배치될 때든(RepositionPopupWindow 등) 이 함수를 거치면
    /// 절대 해당 패널 내부로 들어갈 수 없습니다.
    /// </summary>
    public Vector2 PushOutOfBlockingPanels(RectTransform targetWin, Vector2 candidatePos)
    {
        if (targetWin == null) return candidatePos;

        Vector2 size = targetWin.rect.size;
        Vector2 pivotOffset = new Vector2(size.x * targetWin.pivot.x, size.y * targetWin.pivot.y);

        foreach (var blockerRect in GetActiveBlockingRects())
        {
            if (blockerRect == targetWin) continue;

            Rect blockRectArea = GetLocalRectOf(blockerRect);
            Rect winRectArea = new Rect(candidatePos - pivotOffset, size);

            if (!winRectArea.Overlaps(blockRectArea)) continue;

            // 4방향 중 밀어내는 데 필요한 이동 거리가 가장 짧은 방향으로 밀어냄
            float pushLeft = blockRectArea.xMin - winRectArea.xMax;   // 음수
            float pushRight = blockRectArea.xMax - winRectArea.xMin; // 양수
            float pushDown = blockRectArea.yMin - winRectArea.yMax;  // 음수
            float pushUp = blockRectArea.yMax - winRectArea.yMin;    // 양수

            float[] options = { Mathf.Abs(pushLeft), Mathf.Abs(pushRight), Mathf.Abs(pushDown), Mathf.Abs(pushUp) };
            int minIndex = 0;
            for (int i = 1; i < options.Length; i++)
            {
                if (options[i] < options[minIndex]) minIndex = i;
            }

            switch (minIndex)
            {
                case 0: candidatePos.x += pushLeft; break;
                case 1: candidatePos.x += pushRight; break;
                case 2: candidatePos.y += pushDown; break;
                case 3: candidatePos.y += pushUp; break;
            }
        }

        return candidatePos;
    }

    /// <summary>
    /// 💡 [신규] 현재 "열려있는" 벽 패널들의 RectTransform 목록을 반환합니다.
    /// </summary>
    private List<RectTransform> GetActiveBlockingRects()
    {
        List<RectTransform> result = new List<RectTransform>();

        if (datalogWindowRect != null && isDatalogOpen)
            result.Add(datalogWindowRect);

        if (imageGenWindowRect != null && isImageGenOpen)
            result.Add(imageGenWindowRect);

        if (chatWindowRect != null && isChatOpen)
            result.Add(chatWindowRect);

        return result;
    }

    /// <summary>
    /// 💡 [신규] RectTransform의 실제 사각형을 spawnArea 로컬 좌표 기준으로 계산합니다.
    /// </summary>
    /// <summary>
    /// 💡 [수정] target이 spawnArea와 다른 부모를 가지고 있어도 정확하게 동작하도록,
    /// 월드 좌표를 거쳐서 spawnArea의 로컬 좌표계로 변환합니다.
    /// (anchoredPosition은 각자의 부모 기준이라 서로 다른 부모끼리 직접 빼면 안 됩니다)
    /// </summary>
    private Rect GetLocalRectOf(RectTransform target)
    {
        Vector3[] worldCorners = new Vector3[4];
        target.GetWorldCorners(worldCorners); // [0]=좌하단, [1]=좌상단, [2]=우상단, [3]=우하단 (월드 좌표)

        Vector2 min = spawnArea.InverseTransformPoint(worldCorners[0]);
        Vector2 max = spawnArea.InverseTransformPoint(worldCorners[2]);

        return new Rect(min, max - min);
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
                child != datalogWindowRect && child != chatWindowRect && child != imageGenWindowRect)
            {
                activeList.Add(child);
            }
        }
        return activeList;
    }

    public void RepositionPopupWindow(RectTransform targetWindow)
    {
        if (targetWindow == null || spawnArea == null) return;
        if (targetWindow == datalogWindowRect || targetWindow == chatWindowRect || targetWindow == imageGenWindowRect) return;

        targetWindow.transform.SetAsLastSibling();

        Vector2 validPosition = GetValidNearPreviousPosition(targetWindow, targetWindow.rect.size);
        targetWindow.anchoredPosition = validPosition;

        lastOpenedWindow = targetWindow;
    }

    /// <summary>
    /// 💡 이전에 연 창(lastOpenedWindow) 근처에서 겹치지 않는 위치를 찾습니다.
    /// 가까운 반경부터 시작해서 점점 넓혀가며 재시도하고, 최종 후보는
    /// datalog/imageGen 벽 밖으로도 밀어냅니다.
    /// </summary>
    private Vector2 GetValidNearPreviousPosition(RectTransform targetWin, Vector2 size)
    {
        ComputeSpawnBounds(targetWin, size, out float minX, out float maxX, out float minY, out float maxY);

        Vector2 referencePos;
        if (lastOpenedWindow != null && lastOpenedWindow.gameObject.activeInHierarchy)
        {
            referencePos = lastOpenedWindow.anchoredPosition;
        }
        else
        {
            referencePos = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        }

        for (int radiusStep = 0; radiusStep <= nearSearchMaxRadiusSteps; radiusStep++)
        {
            float currentRadius = nearSearchStartRadius + nearSearchRadiusStep * radiusStep;

            for (int i = 0; i < maxTryCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(currentRadius * 0.3f, currentRadius);

                Vector2 candidate = referencePos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
                candidate.x = Mathf.Clamp(candidate.x, minX, maxX);
                candidate.y = Mathf.Clamp(candidate.y, minY, maxY);

                // 💡 벽 패널과도 겹치지 않아야 함
                candidate = PushOutOfBlockingPanels(targetWin, candidate);
                candidate.x = Mathf.Clamp(candidate.x, minX, maxX);
                candidate.y = Mathf.Clamp(candidate.y, minY, maxY);

                if (!CheckOverlap(targetWin, candidate, size))
                {
                    return candidate;
                }
            }
        }

        Debug.LogWarning("[WindowManager] 이전 창 근처에서 빈 자리를 못 찾아 전체 영역에서 랜덤 배치합니다.");
        return GetValidRandomPositionInBounds(targetWin, size, minX, maxX, minY, maxY);
    }

    private void ComputeSpawnBounds(RectTransform targetWin, Vector2 size, out float minX, out float maxX, out float minY, out float maxY)
    {
        float spawnAreaLeftBoundary = -spawnArea.rect.width / 2f;
        float currentSidebarWidth = isSidebarOpen ? sidebarOpenWidth : sidebarClosedWidth;

        minX = spawnAreaLeftBoundary + currentSidebarWidth + (size.x * targetWin.pivot.x) + padding;
        maxX = spawnArea.rect.width / 2f - (size.x * (1f - targetWin.pivot.x)) - padding;
        minY = -spawnArea.rect.height / 2f + (size.y * targetWin.pivot.y) + padding;
        maxY = spawnArea.rect.height / 2f - (size.y * (1f - targetWin.pivot.y)) - padding;

        if (isChatOpen) maxX -= chatPanelWidth;

        if (minX > maxX) minX = maxX;
    }

    private Vector2 GetValidRandomPositionInBounds(RectTransform targetWin, Vector2 size, float minX, float maxX, float minY, float maxY)
    {
        Vector2 targetPos = Vector2.zero;
        bool isOverlapping = true;
        int tries = 0;

        while (isOverlapping && tries < maxTryCount)
        {
            float randX = Random.Range(minX, maxX);
            float randY = Random.Range(minY, maxY);
            targetPos = new Vector2(randX, randY);

            targetPos = PushOutOfBlockingPanels(targetWin, targetPos);

            isOverlapping = CheckOverlap(targetWin, targetPos, size);
            tries++;
        }

        return targetPos;
    }

    private bool CheckOverlap(RectTransform targetWin, Vector2 targetPos, Vector2 size)
    {
        Vector2 targetPivotOffset = new Vector2(size.x * targetWin.pivot.x, size.y * targetWin.pivot.y);
        Rect newRect = new Rect(targetPos - targetPivotOffset, size);

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