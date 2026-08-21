using UnityEngine;
using UnityEngine.UI;

public class TaskbarPopupButton : MonoBehaviour
{
    // 💡 [추가] Community, HumanDB 추가
    public enum PopupType { Archive, News, Community, HumanDB }

    [Header("이 버튼이 열고 닫을 팝업 종류 선택")]
    public PopupType popupType;

    [Header("불빛 표시할 UI 이미지 컴포넌트")]
    public Image indicatorImage;

    [Header("상태별 스프라이트 등록")]
    public Sprite turnedOnSprite;  // 불 켜진 이미지
    public Sprite turnedOffSprite; // 불 꺼진 이미지

    private void Start()
    {
        // 시작할 때 불 꺼진 상태로 초기화 (TaskbarManager가 Awake에서 다 끄기 때문)
        UpdateSprite(false);
    }

    // 🌟 버튼의 On Click()에 이 함수를 연결하세요!
    public void OnButtonClick()
    {
        if (TaskbarManager.Instance == null) return;

        bool nextState = false;

        if (popupType == PopupType.Archive)
        {
            TaskbarManager.Instance.ToggleArchivePopup();
            nextState = TaskbarManager.Instance.IsArchivePanelActive();
        }
        else if (popupType == PopupType.News)
        {
            TaskbarManager.Instance.ToggleNewsPopup();
            nextState = TaskbarManager.Instance.IsNewsPanelActive();
        }
        // 💡 [추가] 커뮤니티 케이스
        else if (popupType == PopupType.Community)
        {
            TaskbarManager.Instance.ToggleCommunityPopup();
            nextState = TaskbarManager.Instance.IsCommunityPanelActive();
        }
        // 💡 [추가] HUMANDB 케이스
        else if (popupType == PopupType.HumanDB)
        {
            TaskbarManager.Instance.ToggleHumanDBPopup();
            nextState = TaskbarManager.Instance.IsHumanDBPanelActive();
        }

        UpdateSprite(nextState);
    }

    private void UpdateSprite(bool isPanelOn)
    {
        if (indicatorImage == null) return;
        indicatorImage.sprite = isPanelOn ? turnedOnSprite : turnedOffSprite;
    }

    // TaskbarManager의 private 필드 우회를 위해 activeSelf 상태를 안전하게 판별
    private bool GetArchivePanelState()
    {
        return indicatorImage.sprite == turnedOffSprite;
    }

    private bool GetNewsPanelState()
    {
        return indicatorImage.sprite == turnedOffSprite;
    }

    // 💡 [추가] 커뮤니티도 동일 패턴
    private bool GetCommunityPanelState()
    {
        return indicatorImage.sprite == turnedOffSprite;
    }
}