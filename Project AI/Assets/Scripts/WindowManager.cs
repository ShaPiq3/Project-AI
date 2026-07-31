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
    public bool IsDatalogOpen => isDatalogOpen;
    public bool IsImageGenOpen => isImageGenOpen;

    private bool isDatalogPinnedOpen = false;

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

        // 💡 추가: DataLog 매니저 쪽 << / >> 아이콘 동기화
        isDatalogPinnedOpen = isDatalogOpen;
        DataLogManager.Instance?.UpdateEdgeToggleButtonSprite();
    }

    /// <summary>호버 진입 시: 이미 열려있지 않을 때만 임시로 엶 (고정 아님)</summary>
    public void PreviewOpenDatalog()
    {
        if (datalogWindowRect == null) return;
        if (isDatalogOpen) return; // 이미 열려있으면(고정이든 미리보기든) 아무것도 안 함
        if (DOTween.IsTweening(datalogWindowRect)) return;

        isDatalogOpen = true;

        if (isImageGenOpen)
        {
            isImageGenOpen = false;
            CloseImageGenDirect();
        }

        OpenDatalogDirect();
        PushWindowsLeftOnDatalog(datalogPushAmount, slideDuration);

        DataLogManager.Instance?.UpdateEdgeToggleButtonSprite();
    }

    /// <summary>호버 이탈 시: 고정된 상태가 아닐 때만 닫음</summary>
    public void PreviewCloseDatalog()
    {
        if (datalogWindowRect == null) return;
        if (isDatalogPinnedOpen) return; // 클릭으로 고정돼 있으면 무시
        if (!isDatalogOpen) return;
        if (DOTween.IsTweening(datalogWindowRect)) return;

        isDatalogOpen = false;
        CloseDatalogDirect();
        PullWindowsRightOnDatalog(datalogPushAmount, slideDuration);

        DataLogManager.Instance?.UpdateEdgeToggleButtonSprite();
    }

    /// <summary>
    /// 여닫기 탭 버튼의 클릭 전용 함수.
    /// 미리보기로 열려있는 상태에서 클릭하면 "고정"만 시키고,
    /// 그 외(닫혀있거나 이미 고정 열림)에는 기존 토글을 그대로 수행합니다.
    /// </summary>
    public void ToggleOrPinDatalog()
    {
        if (datalogWindowRect == null) return;
        if (DOTween.IsTweening(datalogWindowRect)) return;

        if (isDatalogOpen && !isDatalogPinnedOpen)
        {
            isDatalogPinnedOpen = true; // 프리뷰 상태 → 고정
            DataLogManager.Instance?.UpdateEdgeToggleButtonSprite();
        }
        else
        {
            ToggleDatalogWindow(); // 닫혀있음 → 열고 고정 / 이미 고정 열림 → 닫음
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
        isDatalogPinnedOpen = false; // 💡 추가

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

    public void NotifyDatalogOpenedExternally()
    {
        if (isDatalogOpen) return;

        isDatalogOpen = true;
        isDatalogPinnedOpen = true; // 💡 추가: 사이드바 버튼 등 다른 진입점은 고정 열림으로 취급
        PushWindowsLeftOnDatalog(datalogPushAmount, slideDuration);
    }

    public void NotifyDatalogClosedExternally()
    {
        if (!isDatalogOpen) return;

        isDatalogOpen = false;
        isDatalogPinnedOpen = false; // 💡 추가
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

        // 💡 [핵심 수정] currentPos(부모 기준 anchoredPosition 후보값)를,
        // targetWin이 그 위치에 있다고 가정했을 때의 spawnArea 기준 좌표로 변환합니다.
        Vector2 spawnAreaLocalPos = ConvertParentLocalToSpawnAreaLocal(targetWin, currentPos);

        float clampedX = Mathf.Clamp(spawnAreaLocalPos.x, minX, maxX);
        float clampedY = Mathf.Clamp(spawnAreaLocalPos.y, minY, maxY);

        Vector2 clamped = new Vector2(clampedX, clampedY);

        clamped = PushOutOfBlockingPanels(targetWin, clamped, minX, maxX, minY, maxY);

        clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
        clamped.y = Mathf.Clamp(clamped.y, minY, maxY);

        // 💡 [핵심 수정] spawnArea 기준으로 계산된 결과를 다시 targetWin의 실제 부모 기준으로 역변환
        return ConvertSpawnAreaLocalToParentLocal(targetWin, clamped);
    }

    /// <summary>
    /// 💡 [추가] targetWin이 candidateParentLocalPos(부모 기준 anchoredPosition) 위치에 있다고 가정했을 때,
    /// 그 중심점을 spawnArea 로컬 좌표계 기준으로 변환합니다.
    /// (드래그 중처럼, 아직 실제로 적용되지 않은 "후보 위치"를 검사할 때 사용)
    /// </summary>
    private Vector2 ConvertParentLocalToSpawnAreaLocal(RectTransform targetWin, Vector2 candidateParentLocalPos)
    {
        Transform parent = targetWin.parent;
        if (parent == null) return candidateParentLocalPos;

        Vector2 pivotOffset = new Vector2(
            targetWin.rect.width * (0.5f - targetWin.pivot.x),
            targetWin.rect.height * (0.5f - targetWin.pivot.y)
        );
        Vector2 centerParentLocalPos = candidateParentLocalPos + pivotOffset;

        Vector3 worldPos = parent.TransformPoint(new Vector3(centerParentLocalPos.x, centerParentLocalPos.y, 0f));
        return spawnArea.InverseTransformPoint(worldPos);
    }

    /// <summary>
    /// 💡 [수정] datalog, imageGen, chat 중 현재 "열려있는" 패널들을 장애물 사각형으로 취급하여
    /// targetWin이 그 사각형과 겹치면 밀어냅니다.
    /// 이전에는 "가장 짧은 이동 거리" 방향만 골랐는데, 그 방향이 화면 경계에 막혀
    /// 완전히 벗어나지 못하고 경계에 눌린 채 겹침이 유지되는 문제(위로 밀려 화면 맨 위에 붙어
    /// 다시 내려올 수 없는 현상)가 있었습니다.
    /// 이제는 화면 경계(minX/maxX/minY/maxY)가 주어지면, 네 방향 각각 "밀어낸 뒤 화면 경계로
    /// 고정했을 때도 실제로 겹침이 풀리는지"를 확인해서, 그 중 가장 짧은 방향을 우선 선택합니다.
    /// (경계 정보가 없으면 기존처럼 가장 짧은 방향 하나만 사용합니다.)
    /// </summary>
    public Vector2 PushOutOfBlockingPanels(RectTransform targetWin, Vector2 candidatePos,
        float? minX = null, float? maxX = null, float? minY = null, float? maxY = null)
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

            float pushLeft = blockRectArea.xMin - winRectArea.xMax;   // 음수
            float pushRight = blockRectArea.xMax - winRectArea.xMin; // 양수
            float pushDown = blockRectArea.yMin - winRectArea.yMax;  // 음수
            float pushUp = blockRectArea.yMax - winRectArea.yMin;    // 양수

            // 4방향 후보를 거리 순으로 정렬
            var options = new List<(float distance, Vector2 result)>
            {
                (Mathf.Abs(pushLeft),  candidatePos + new Vector2(pushLeft, 0f)),
                (Mathf.Abs(pushRight), candidatePos + new Vector2(pushRight, 0f)),
                (Mathf.Abs(pushDown),  candidatePos + new Vector2(0f, pushDown)),
                (Mathf.Abs(pushUp),    candidatePos + new Vector2(0f, pushUp)),
            };
            options.Sort((a, b) => a.distance.CompareTo(b.distance));

            bool foundValid = false;

            foreach (var option in options)
            {
                Vector2 result = option.result;

                // 화면 경계 정보가 있다면, 그 경계로 고정했을 때도 겹침이 풀리는지 확인
                if (minX.HasValue && maxX.HasValue && minY.HasValue && maxY.HasValue)
                {
                    Vector2 boundedResult = new Vector2(
                        Mathf.Clamp(result.x, minX.Value, maxX.Value),
                        Mathf.Clamp(result.y, minY.Value, maxY.Value)
                    );

                    Rect boundedRectArea = new Rect(boundedResult - pivotOffset, size);
                    if (!boundedRectArea.Overlaps(blockRectArea))
                    {
                        candidatePos = boundedResult;
                        foundValid = true;
                        break;
                    }
                }
                else
                {
                    // 경계 정보가 없으면 기존처럼 첫 번째(가장 짧은) 방향을 그대로 사용
                    candidatePos = result;
                    foundValid = true;
                    break;
                }
            }

            // 💡 네 방향 다 시도해도 화면 안에서 벗어날 수 없는 극단적 상황이면,
            // 어쩔 수 없이 가장 짧은 방향으로라도 밀어냄 (기존 동작으로 폴백)
            if (!foundValid)
            {
                candidatePos = options[0].result;
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

                // 💡 [변경] 경계값을 같이 넘김
                candidate = PushOutOfBlockingPanels(targetWin, candidate, minX, maxX, minY, maxY);
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

    /// <summary>
    /// 💡 [수정] InGameWindowManager처럼 spawnArea 좌표계를 직접 모르는 외부 스크립트가
    /// 안전하게 벽 회피 위치를 요청할 수 있도록, 화면 경계 계산까지 포함해서 처리해주는 헬퍼입니다.
    /// targetWin이 spawnArea의 직계 자식이 아니어도(예: DocumentQuestGroup_First의 자식인 경우)
    /// 정확하게 동작하도록, spawnArea 기준 좌표로 변환해서 계산한 뒤 다시 원래 부모 기준
    /// anchoredPosition으로 역변환합니다.
    /// </summary>
    public Vector2 PushOutOfBlockingPanelsWithBounds(RectTransform targetWin, Vector2 candidatePos)
    {
        if (targetWin == null || spawnArea == null) return candidatePos;

        float spawnAreaLeftBoundary = -spawnArea.rect.width / 2f;
        float sizeX = targetWin.rect.width;
        float sizeY = targetWin.rect.height;
        float currentSidebarWidth = isSidebarOpen ? sidebarOpenWidth : sidebarClosedWidth;

        float minX = spawnAreaLeftBoundary + currentSidebarWidth + (sizeX * targetWin.pivot.x) + padding;
        float maxX = spawnArea.rect.width / 2f - (sizeX * (1f - targetWin.pivot.x)) - padding;
        float minY = -spawnArea.rect.height / 2f + (sizeY * targetWin.pivot.y) + padding;
        float maxY = spawnArea.rect.height / 2f - (sizeY * (1f - targetWin.pivot.y)) - padding;

        if (minX > maxX) minX = maxX;

        // 💡 [핵심] targetWin의 현재 위치를, 부모 기준이 아니라 spawnArea 기준 로컬 좌표로 변환
        Vector2 spawnAreaLocalCurrentPos = GetSpawnAreaLocalPosition(targetWin);

        Vector2 clamped = new Vector2(
            Mathf.Clamp(spawnAreaLocalCurrentPos.x, minX, maxX),
            Mathf.Clamp(spawnAreaLocalCurrentPos.y, minY, maxY)
        );

        clamped = PushOutOfBlockingPanels(targetWin, clamped, minX, maxX, minY, maxY);

        clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
        clamped.y = Mathf.Clamp(clamped.y, minY, maxY);

        // 💡 [핵심] spawnArea 기준으로 계산된 좌표를, targetWin의 실제 부모 기준
        // anchoredPosition으로 다시 역변환해서 반환
        return ConvertSpawnAreaLocalToParentLocal(targetWin, clamped);
    }

    /// <summary>
    /// 💡 [추가] targetWin의 중심점을 spawnArea의 로컬 좌표계 기준으로 변환합니다.
    /// targetWin이 spawnArea의 직계 자식이 아니어도(중간에 다른 부모가 껴 있어도) 정확합니다.
    /// </summary>
    private Vector2 GetSpawnAreaLocalPosition(RectTransform targetWin)
    {
        Vector3 worldCenter = targetWin.TransformPoint(
            new Vector3(
                targetWin.rect.width * (0.5f - targetWin.pivot.x),
                targetWin.rect.height * (0.5f - targetWin.pivot.y),
                0f
            )
        );
        return spawnArea.InverseTransformPoint(worldCenter);
    }

    /// <summary>
    /// 💡 [추가] spawnArea 기준 로컬 좌표(창의 중심 기준)를, targetWin의 실제 부모가
    /// 사용하는 anchoredPosition 값으로 역변환합니다.
    /// </summary>
    private Vector2 ConvertSpawnAreaLocalToParentLocal(RectTransform targetWin, Vector2 spawnAreaLocalCenterPos)
    {
        Vector3 worldPos = spawnArea.TransformPoint(new Vector3(spawnAreaLocalCenterPos.x, spawnAreaLocalCenterPos.y, 0f));

        Transform parent = targetWin.parent;
        if (parent == null) return spawnAreaLocalCenterPos;

        Vector3 parentLocalPos = parent.InverseTransformPoint(worldPos);

        Vector2 pivotOffset = new Vector2(
            targetWin.rect.width * (0.5f - targetWin.pivot.x),
            targetWin.rect.height * (0.5f - targetWin.pivot.y)
        );

        return new Vector2(parentLocalPos.x, parentLocalPos.y) - pivotOffset;
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

            targetPos = PushOutOfBlockingPanels(targetWin, targetPos, minX, maxX, minY, maxY);

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