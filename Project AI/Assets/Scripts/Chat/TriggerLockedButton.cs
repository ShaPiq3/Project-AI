using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ChatDialogueManager.IsTriggerActive 값에 맞춰 지정한 Button 의 interactable 을
/// 자동으로 열고 잠급니다. (isTrigger=true 인 대화가 진행 중일 때만 열림)
///
/// 사용법: 데이터 수집 버튼(또는 잠가두고 싶은 아무 버튼) 오브젝트에 이 스크립트를 붙이고,
/// targetButton 에 그 버튼을 연결하세요. DataLogManager.cs 는 전혀 수정하지 않아도 됩니다.
/// </summary>
public class TriggerLockedButton : MonoBehaviour
{
    [SerializeField] private Button targetButton;

    [Tooltip("잠겨 있을 때 버튼을 완전히 숨길지(false) 여부. 체크 해제 시 interactable 만 꺼짐(보이긴 함)")]
    [SerializeField] private bool hideWhenLocked = false;

    void OnEnable()
    {
        if (ChatDialogueManager.Instance != null)
        {
            ChatDialogueManager.Instance.OnTriggerActiveChanged += HandleTriggerActiveChanged;
            // 이미 트리거가 켜져있는 상태에서 이 오브젝트가 나중에 활성화될 수도 있으니 현재 상태로 즉시 동기화
            HandleTriggerActiveChanged(ChatDialogueManager.Instance.IsTriggerActive);
        }
        else
        {
            // 아직 매니저가 준비 안됐다면 기본은 잠금
            HandleTriggerActiveChanged(false);
        }
    }

    void OnDisable()
    {
        if (ChatDialogueManager.Instance != null)
        {
            ChatDialogueManager.Instance.OnTriggerActiveChanged -= HandleTriggerActiveChanged;
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