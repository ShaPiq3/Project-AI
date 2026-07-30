using UnityEngine;

/// <summary>
/// 💡 채팅창 좌측에 고정된 "재오픈(<<)" 버튼 오브젝트에 붙입니다.
/// 대상 패널이 열려있으면 이 버튼을 숨기고, 닫혀있으면 보여줍니다.
///
/// 패널 안의 닫기(">>") 버튼은 이 스크립트가 필요 없습니다.
/// 패널의 자식으로 두면, 패널이 슬라이드 인/아웃 될 때 자연스럽게 같이 나타나고 사라집니다.
/// </summary>
public class ReopenButtonVisibility : MonoBehaviour
{
    public enum TargetPanel
    {
        Datalog,
        ImageGen
    }

    [SerializeField] private TargetPanel targetPanel = TargetPanel.Datalog;

    void Update()
    {
        bool shouldShow;

        if (targetPanel == TargetPanel.Datalog)
        {
            if (WindowManager.Instance == null || DataLogManager.Instance == null) return;

            bool isOpen = WindowManager.Instance.IsDatalogOpen;
            bool isFeatureActive = DataLogManager.Instance.HasActiveTrigger;

            Debug.Log($"[재오픈버튼] isOpen:{isOpen}, isFeatureActive:{isFeatureActive}");

            shouldShow = !isOpen && isFeatureActive;
        }
        else
        {
            if (ImageGenerationManager.Instance == null) return;

            bool isOpen = ImageGenerationManager.Instance.IsPanelOpen;
            bool isFeatureActive = ImageGenerationManager.Instance.IsUnlocked; // 💡 이미 있는 잠금 상태 재사용

            shouldShow = !isOpen && isFeatureActive;
        }

        if (gameObject.activeSelf != shouldShow)
        {
            gameObject.SetActive(shouldShow);
        }
    }
}