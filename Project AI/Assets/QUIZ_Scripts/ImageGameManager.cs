using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ImageGameManager : MonoBehaviour
{
 

    [Header("UI Windows & Panels")]
    // ★ 유니티 시작 시 꺼두어도 상관없는 미니게임 전체 창 (imageGame 오브젝트 자신)
    public GameObject imageGameWindow;
  

    [Header("Grid Setup")]
    public Transform imageGrid; // Button들이 들어있는 부모 오브젝트 (ImageGrid)

    [Header("Control Buttons")]
    public Button resetBtn;
    public Button submitBtn;
    public NewChatSystem chatSystem;

    [Header("Game Answer (0 ~ 11 중 설정)")]
    public List<int> correctAnswers = new List<int>() { 0, 1, 2, 3, 4 };

    private List<GridButton> allButtons = new List<GridButton>();
    private HashSet<int> selectedIndices = new HashSet<int>();

    void Awake()
    {
        // 오브젝트가 꺼져 있어도 Awake는 최초 1회 실행되므로 
        // 하위 제어 버튼들의 리스너를 안전하게 먼저 연결해 둡니다.
        if (resetBtn != null) resetBtn.onClick.AddListener(ResetAll);
        if (submitBtn != null) submitBtn.onClick.AddListener(SubmitResult);
    }

    // ★ 외부의 다른 버튼(예: 시작 버튼)이 이 함수를 호출해서 미니게임을 켭니다.
    public void OpenAndStartGame()
    {
        Debug.Log("숨겨져 있던 미니게임 창 등장!");

        // 1. 미니게임 창 활성화
        if (imageGameWindow != null)
        {
            imageGameWindow.SetActive(true);
        }

        // 2. 게임판 초기화 및 버튼 세팅 시작
        InitGame();
    }

    void InitGame()
    {
        selectedIndices.Clear();
        allButtons.Clear();

        if (imageGrid == null) return;

        int index = 0;
        foreach (Transform child in imageGrid)
        {
            GridButton gridBtn = child.GetComponent<GridButton>();
            if (gridBtn == null)
            {
                gridBtn = child.gameObject.AddComponent<GridButton>();
            }

            Transform outlineTransform = child.Find("Outline");
            if (outlineTransform != null)
            {
                gridBtn.outline = outlineTransform.gameObject;
            }

            gridBtn.Setup(index, this);
            allButtons.Add(gridBtn);
            index++;
        }

        UpdateSubmitButtonState();
    }

    public bool CanSelectMore()
    {
        return selectedIndices.Count < correctAnswers.Count;
    }

    public void OnButtonSelected(int index)
    {
        selectedIndices.Add(index);
        UpdateSubmitButtonState();
    }

    public void OnButtonDeselected(int index)
    {
        selectedIndices.Remove(index);
        UpdateSubmitButtonState();
    }

    void UpdateSubmitButtonState()
    {
        if (submitBtn != null)
        {
            submitBtn.interactable = (selectedIndices.Count == correctAnswers.Count);
        }
    }

    public void ResetAll()
    {
        selectedIndices.Clear();
        foreach (var btn in allButtons)
        {
            btn.ResetButton();
        }
        UpdateSubmitButtonState();
    }

    // 제출 버튼 클릭 시 실행
    public void SubmitResult()
    {
        if (selectedIndices.Count != correctAnswers.Count) return;

        bool isAllCorrect = true;
        foreach (int index in selectedIndices)
        {
            if (!correctAnswers.Contains(index))
            {
                isAllCorrect = false;
                break;
            }
        }

        // 1. 콘솔창에 결과 출력 (여기에 UI 텍스트 출력이나 이펙트 코드를 추가하셔도 됩니다)
        if (isAllCorrect)
        {
            Debug.Log("★ [결과] 미니게임 클리어 성공! ★");
            chatSystem.PlayDialogueGroup("Q1_SuccessSubmit");

        }
        else
        {
            Debug.Log("Ⅹ [결과] 미니게임 클리어 실패... Ⅹ");
            chatSystem.PlayDialogueGroup("Q1_FailSubmit");
        }

        chatSystem.CloseImageButton();        // ?? 추가

        // 2. 미니게임 종료 및 창 닫기
        CloseGame();
    }

    void CloseGame()
    {
        if (imageGameWindow != null)
        {
            imageGameWindow.SetActive(false); // 창 완전히 사라짐
        }
    }
}