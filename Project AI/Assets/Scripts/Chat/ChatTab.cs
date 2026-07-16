using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ChatTab : MonoBehaviour
{
    [Header("이 버튼과 연결할 WindowManager")]
    public WindowManager windowManager;

    private Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();

        // 토글 버튼의 상태가 바뀔 때(켜지거나 꺼질 때) 실행될 이벤트 연결
        toggle.onValueChanged.AddListener(OnTabChanged);
    }

    // 토글 상태가 변경될 때 호출되는 함수 (isOn: 현재 켜졌는지 여부)
    private void OnTabChanged(bool isOn)
    {
        if (windowManager == null) return;

        if (isOn)
        {
            // 이 버튼이 On 되면 연결된 채팅창을 열어줌
     
        }
        else
        {
            // 다른 버튼이 On 되면서 이 버튼이 Off 되면 연결된 채팅창을 닫아줌
            if (windowManager.gameObject.activeSelf)
            {
     
            }
        }
    }
}