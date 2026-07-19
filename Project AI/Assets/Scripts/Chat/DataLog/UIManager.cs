using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Confirm Popup UI")]
    [SerializeField] private GameObject confirmPopupPanel;   // 팝업 전체 오브젝트 (평소엔 꺼져있어야 함)
    [SerializeField] private TMP_Text confirmMessageText;    // "수집된 데이터를 삭제하시겠습니까?" 텍스트
    [SerializeField] private Button confirmYesButton;        // 예 버튼
    [SerializeField] private Button confirmNoButton;         // 아니오 버튼

    private UnityAction currentOnConfirm;
    private UnityAction currentOnCancel;

    private void Awake()
    {
        Instance = this;

        if (confirmPopupPanel != null)
        {
            confirmPopupPanel.SetActive(false);
        }

        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.AddListener(OnConfirmYesClicked);
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.AddListener(OnConfirmNoClicked);
        }
    }

    public void ShowConfirmPopup(string message, UnityAction onConfirm, UnityAction onCancel)
    {
        if (confirmPopupPanel == null)
        {
            Debug.LogWarning("[UIManager] confirmPopupPanel이 Inspector에 연결되지 않았습니다!");
            return;
        }

        currentOnConfirm = onConfirm;
        currentOnCancel = onCancel;

        if (confirmMessageText != null)
        {
            confirmMessageText.text = message;
        }

        confirmPopupPanel.SetActive(true);
        confirmPopupPanel.transform.SetAsLastSibling();
    }

    private void OnConfirmYesClicked()
    {
        confirmPopupPanel.SetActive(false);

        UnityAction confirmAction = currentOnConfirm;
        currentOnConfirm = null;
        currentOnCancel = null;

        confirmAction?.Invoke();
    }

    private void OnConfirmNoClicked()
    {
        confirmPopupPanel.SetActive(false);

        UnityAction cancelAction = currentOnCancel;
        currentOnConfirm = null;
        currentOnCancel = null;

        cancelAction?.Invoke();
    }
}