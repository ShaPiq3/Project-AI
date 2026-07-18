using UnityEngine;
using UnityEngine.Events;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private void Awake() { Instance = this; }

    // 팝업을 띄우는 함수 (다른 스크립트에서 호출할 수 있게 public)
    public void ShowConfirmPopup(string message, UnityAction onConfirm, UnityAction onCancel)
    {
        // 여기에 실제 팝업 UI를 활성화하고, 버튼 이벤트에 onConfirm/onCancel을 연결하는 로직을 넣으세요.
        Debug.Log("팝업이 호출되었습니다: " + message);
    }
}