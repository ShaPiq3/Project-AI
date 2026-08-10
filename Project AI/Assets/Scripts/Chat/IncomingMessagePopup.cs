using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// "메시지 도착" 알림 오버레이. ChatProfileBar를 가리고 있다가, 플레이어가 확인 버튼을
/// 누르면 사라지면서 실제로 대화방이 열리고(ChatProfileBar도 그때 새 연락처 정보로 갱신됨).
/// </summary>
public class IncomingMessagePopup : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;

    private Action onConfirmed;

    void Awake()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(HandleConfirmClicked);
        Hide();
    }

    public void Show(string displayName, Action onConfirmedCallback)
    {
        onConfirmed = onConfirmedCallback;
        if (messageText != null)
        {
            messageText.text = string.IsNullOrEmpty(displayName) ? "메시지가 도착했습니다." : $"{displayName}님에게 메시지가 도착했습니다.";
        }
        if (root != null) root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    private void HandleConfirmClicked()
    {
        Hide();
        Action callback = onConfirmed;
        onConfirmed = null;
        callback?.Invoke();
    }
}
