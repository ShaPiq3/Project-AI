using UnityEngine;
using UnityEngine.UI;

public class QuizButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject outlineObject;
    [SerializeField] private Image buttonImage;

    public int LogicIndex { get; private set; }
    public bool IsCorrectAnswer { get; private set; }
    public bool IsSelected { get; private set; }

    private void Awake()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnButtonClick);
            btn.transition = Selectable.Transition.None;
        }
    }

    public void SetupButton(int logicIndex, Sprite sprite, bool isCorrect)
    {
        LogicIndex = logicIndex;
        IsCorrectAnswer = isCorrect;

        if (buttonImage != null)
        {
            buttonImage.sprite = sprite;
            buttonImage.color = (sprite != null) ? Color.white : new Color(1, 1, 1, 0.5f);
        }

        SetSelection(false);
    }

    private void OnButtonClick()
    {
        if (IsSelected)
        {
            SetSelection(false);

            // 선택 해제 후 안전하게 매니저에게 신호 전달 (Null 체크 완벽)
            if (QuizManager.Instance != null)
                QuizManager.Instance.UpdateSubmitButtonState();
        }
        else
        {
            if (QuizManager.Instance != null && QuizManager.Instance.CanSelectMore())
            {
                SetSelection(true);

                // [수정 완료] 실행 순간에도 안전하게 매니저 존재 여부를 검사하도록 방어막 추가
                if (QuizManager.Instance != null)
                    QuizManager.Instance.UpdateSubmitButtonState();
            }
            else
            {
                Debug.LogWarning("이번 판의 정답 개수를 초과하여 선택할 수 없습니다!");
            }
        }
    }

    public void SetSelection(bool select)
    {
        IsSelected = select;

        if (outlineObject != null)
        {
            outlineObject.SetActive(IsSelected);
        }
    }
}