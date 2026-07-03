using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WindowManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform windowRect;
    [SerializeField] private Button hideButton;
    [SerializeField] private Button showButton;

    [Header("Settings")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float hidePositionX = 500f;

    private Vector2 originPosition;

    void Start()
    {
        // 1. 기준이 되는 원래 위치(좌측 등장 상태)를 기억
        originPosition = windowRect.anchoredPosition;

        // 2. [시작 상태 변경] 창을 처음부터 오른쪽 밖(숨김 위치)으로 강제 이동
        windowRect.anchoredPosition = new Vector2(hidePositionX, originPosition.y);

        // 3. [시작 상태 변경] 새 버튼은 보이게, 원래 숨기기 버튼은 비활성화
        showButton.gameObject.SetActive(true);
        hideButton.interactable = false;

        // 4. 버튼 리스너 연결
        hideButton.onClick.AddListener(HideWindow);
        showButton.onClick.AddListener(ShowWindow);
    }

    // 창을 오른쪽으로 내보내기 (접기)
    private void HideWindow()
    {
        hideButton.interactable = false;

        windowRect.DOAnchorPosX(hidePositionX, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                showButton.gameObject.SetActive(true);
            });
    }

    // 창을 왼쪽으로 다시 가져오기 (펼치기)
    private void ShowWindow()
    {
        showButton.gameObject.SetActive(false);

        windowRect.DOAnchorPosX(originPosition.x, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                hideButton.interactable = true;
            });
    }
}
