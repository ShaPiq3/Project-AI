using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 트리거(퀘스트) 활성 상태에 따라 지정된 Button 의 interactable 을 자동으로 잠급니다.
/// (isTrigger=true 인 대화가 진행 중일 때는 잠금)
///
/// ChatCoordinator에 현재 포커스된 연락처 스레드의 트리거 상태를 따라간다.
///
/// 사용법: 잠그고 싶은 버튼(또는 뭐가 됐든 아무 버튼) 오브젝트에 이 스크립트를 붙이고,
/// targetButton 에 그 버튼을 연결하세요. DataLogManager.cs 는 따로 건드리지 않아도 됩니다.
/// </summary>
public class TriggerLockedButton : MonoBehaviour
{
    [SerializeField] private Button targetButton;

    [Tooltip("잠겼을 때 버튼을 완전히 숨길지(false) 여부. 체크 안 하면 interactable 만 꺼짐(보이기는 함)")]
    [SerializeField] private bool hideWhenLocked = false;

    void OnEnable()
    {
        if (ChatCoordinator.Instance != null)
        {
            ChatCoordinator.Instance.OnTriggerActiveChanged += HandleTriggerActiveChanged;
            HandleTriggerActiveChanged(ChatCoordinator.Instance.IsTriggerActive);
        }
        else
        {
            // 아직 매니저가 준비 안 됐다면 기본값 사용
            HandleTriggerActiveChanged(false);
        }
    }

    void OnDisable()
    {
        if (ChatCoordinator.Instance != null)
        {
            ChatCoordinator.Instance.OnTriggerActiveChanged -= HandleTriggerActiveChanged;
        }
    }

    private void HandleTriggerActiveChanged(bool isActive)
    {
        if (targetButton == null) return;

        targetButton.interactable = isActive;

        if (hideWhenLocked)
        {
            targetButton.gameObject.SetActive(isActive);
        }
    }
}
