using UnityEngine;
using UnityEngine.UI;

public class MessageClickable : MonoBehaviour
{
    private string targetPanelName;
    private NewChatSystem chatSystem;

    public void Setup(string panelName, NewChatSystem system)
    {
        targetPanelName = panelName;
        chatSystem = system;
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (!string.IsNullOrEmpty(targetPanelName))
        {
            // 창을 여는 메서드 호출 (기존 MoveToTargetPanel 등 활용)
            chatSystem.OpenTargetPanel(targetPanelName);
        }
    }
}