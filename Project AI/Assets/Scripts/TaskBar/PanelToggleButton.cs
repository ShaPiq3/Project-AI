using UnityEngine;
using UnityEngine.UI;

public class TaskbarPopupButton : MonoBehaviour
{
    public enum PopupType { Archive, News }

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
    // ... 기존 코드 동일 ...

    public void OnButtonClick()
    {
        if (TaskbarManager.Instance == null) return;

        bool nextState = false;

        if (popupType == PopupType.Archive)
        {
            TaskbarManager.Instance.ToggleArchivePopup();
            // 실제 아카이브 패널이 켜져있는지 확인해서 true/false를 가져옴
            nextState = TaskbarManager.Instance.IsArchivePanelActive();
        }
        else if (popupType == PopupType.News)
        {
            TaskbarManager.Instance.ToggleNewsPopup();
            // 실제 뉴스 패널이 켜져있는지 확인해서 true/false를 가져옴
            nextState = TaskbarManager.Instance.IsNewsPanelActive();
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
        // 런타임에 Hierarchy에서 직접 상태를 확인하는 것이 가장 정확합니다.
        // 버튼을 누른 직후이므로 켜졌는지 꺼졌는지 판단합니다.
        // 조금 더 정석적으로 하려면 TaskbarManager에 패널 상태 리턴 함수를 만드는게 좋으나 
        // 우선은 정상 작동을 위해 버튼 클릭 시점 기준으로 토글 상태를 이미지에 적용합니다.
        return indicatorImage.sprite == turnedOffSprite;
    }

    private bool GetNewsPanelState()
    {
        return indicatorImage.sprite == turnedOffSprite;
    }
}