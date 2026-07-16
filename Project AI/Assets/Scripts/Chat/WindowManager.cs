using UnityEngine;
using DG.Tweening;

public class WindowManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform chatWindowRect;    // 오른쪽 고정 채팅창(Chat_Panel)
    [SerializeField] private RectTransform datalogWindowRect; // 독립된 DATALOG 창

    [Header("Slide Settings")]
    [SerializeField] private float slideDuration = 0.4f;      // 슬라이드 애니메이션 시간

    [Header("Target Position Settings (열렸을 때 정지 위치)")]
    [SerializeField] private Vector2 targetPosition = new Vector2(-460f, -137f); // 정지 좌표

    private bool isDatalogOpen = false;
    private Vector2 initialHidePosition; // 화면 우측 밖(기본 숨김 위치)

    void Start()
    {
        if (datalogWindowRect != null)
        {
            // 초기 숨김 위치 지정
            initialHidePosition = new Vector2(500f, targetPosition.y);
            datalogWindowRect.anchoredPosition = initialHidePosition;

            datalogWindowRect.gameObject.SetActive(true);

            // 투명도 조절로 코루틴 에러 방지
            CanvasGroup canvasGroup = datalogWindowRect.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = datalogWindowRect.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            isDatalogOpen = false; // 시작 상태는 확실하게 닫힘으로 초기화
        }
    }

    /// <summary>
    /// 버튼 클릭 시 자동으로 열고 닫아주는 토글 함수 (인스펙터 버튼 On Click용)
    /// </summary>
    public void ToggleDatalogWindow()
    {
        if (datalogWindowRect == null) return;
        if (DOTween.IsTweening(datalogWindowRect)) return;

        isDatalogOpen = !isDatalogOpen;

        if (isDatalogOpen)
        {
            OpenDatalogDirect();
        }
        else
        {
            CloseDatalogDirect();
        }
    }

    /// <summary>
    /// 강제 열기 핵심 로직
    /// </summary>
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

        datalogWindowRect.DOAnchorPos(targetPosition, slideDuration)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// [수정] public으로 변경하여 인스펙터 버튼에서 직접 등록이 가능하도록 수정했습니다.
    /// 채팅창을 끄는 독립 버튼에 연결하시면 됩니다.
    /// </summary>
    public void CloseDatalogDirect()
    {
        if (datalogWindowRect == null) return;

        datalogWindowRect.DOKill();
        isDatalogOpen = false; // 수동으로 끌 때 토글 상태도 완전히 닫힘 상태로 동기화

        CanvasGroup canvasGroup = datalogWindowRect.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, slideDuration);
            canvasGroup.blocksRaycasts = false;
        }

        datalogWindowRect.DOAnchorPos(initialHidePosition, slideDuration)
            .SetEase(Ease.OutQuad);
    }

    // 엑셀 동적 생성 등 기존 호환성 유지를 위한 보조 함수
    public void RepositionWindow(RectTransform targetWindow)
    {
        if (targetWindow == datalogWindowRect)
        {
            OpenDatalogDirect();
            isDatalogOpen = true;
        }
    }

    #region ChatTab 연동용 함수 (에러 방지용 기존 구조 유지)

    public void PushWindowsLeft(float pushAmount, float duration) { }

    public void PullWindowsRight(float pushAmount, float duration)
    {
        if (isDatalogOpen)
        {
            isDatalogOpen = false;
            datalogWindowRect.DOKill();
            CanvasGroup canvasGroup = datalogWindowRect.GetComponent<CanvasGroup>();

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, duration);
                canvasGroup.blocksRaycasts = false;
            }

            datalogWindowRect.DOAnchorPos(initialHidePosition, duration)
                .SetEase(Ease.OutQuad);
        }
    }

    #endregion
}
