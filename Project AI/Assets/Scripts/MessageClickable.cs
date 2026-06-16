using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))] // 버튼 컴포넌트 자동 추가 및 필수화
public class MessageClickable : MonoBehaviour
{
    private string targetPanelName;
    private NewChatSystem chatSystem;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void Setup(string panelName, NewChatSystem system)
    {
        targetPanelName = panelName;
        chatSystem = system;

        button.onClick.RemoveListener(OnClick); // 중복 등록 방지
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        // chatSystem 참조가 유효한지 확인
        if (chatSystem != null && !string.IsNullOrEmpty(targetPanelName))
        {
            chatSystem.OpenTargetPanel(targetPanelName);
        }
    }




    private void OnDestroy()
    {
        // 오브젝트 파괴 시 리스너 제거
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
        }
    }
}