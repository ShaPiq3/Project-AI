using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance { get; private set; }

    [Header("UI Components")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Transform imageGrid;
    [SerializeField] private Button resetBtn;
    [SerializeField] private Button submitBtn;

    private List<QuizButton> quizButtons = new List<QuizButton>();
    private int maxSelectCount = 0;

    private Action<float> onQuizCompleteCallback;

    private struct ButtonData
    {
        public string imageName;
        public bool isCorrect;
    }

    private struct StageData
    {
        public string stageID;
        public int correctCount;
        public List<ButtonData> buttons;
    }

    private Dictionary<string, StageData> stageDatabase = new Dictionary<string, StageData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (Transform child in imageGrid)
        {
            QuizButton qBtn = child.GetComponent<QuizButton>();
            if (qBtn != null)
            {
                quizButtons.Add(qBtn);
            }
        }
    }

    private void Start()
    {
        if (resetBtn != null) resetBtn.onClick.AddListener(ResetAllButtons);
        if (submitBtn != null) submitBtn.onClick.AddListener(SubmitAnswers);

        LoadCsvData();
        if (mainPanel != null) mainPanel.SetActive(false);
        
    }

    private void LoadCsvData()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("QuizData");
        if (csvFile == null)
        {
            Debug.LogError("Assets/Resources/QuizData.csv 파일을 찾을 수 없습니다!");
            return;
        }

        string[] lines = csvFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] parts = line.Split(',');

            if (parts.Length < 25) continue;

            string stageID = parts[0].Trim();
            List<ButtonData> btnList = new List<ButtonData>();
            int trueCount = 0;

            for (int j = 0; j < 12; j++)
            {
                int imgIdx = 1 + (j * 2);
                int correctIdx = imgIdx + 1;

                string imgName = parts[imgIdx].Trim();
                string correctStr = parts[correctIdx].Trim().ToUpper();

                bool isCorrect = (correctStr == "TRUE" || correctStr == "O" || correctStr == "1");

                if (isCorrect) trueCount++;

                btnList.Add(new ButtonData { imageName = imgName, isCorrect = isCorrect });
            }

            StageData data = new StageData
            {
                stageID = stageID,
                correctCount = trueCount,
                buttons = btnList
            };

            stageDatabase[stageID] = data;
        }
        Debug.Log($"엑셀 데이터베이스 빌드 완료: 총 {stageDatabase.Count}개의 스테이지 ID 등록됨.");
    }

    public void PlayStage(string stageID, Action<float> onComplete)
    {
        if (!stageDatabase.ContainsKey(stageID))
        {
            Debug.LogError($"스테이지 ID [{stageID}] 데이터가 엑셀 데이터베이스에 없습니다!");
            return;
        }

        onQuizCompleteCallback = onComplete;
        StageData data = stageDatabase[stageID];
        maxSelectCount = data.correctCount;

        List<ButtonSetupData> setupDataList = new List<ButtonSetupData>();

        for (int i = 0; i < quizButtons.Count; i++)
        {
            ButtonData btnData = data.buttons[i];

            Sprite loadedSprite = null;
            if (!string.IsNullOrEmpty(btnData.imageName))
            {
                string path = "StageImages/" + btnData.imageName;
                loadedSprite = Resources.Load<Sprite>(path);
            }

            setupDataList.Add(new ButtonSetupData(i, loadedSprite, btnData.isCorrect));
        }

        ShuffleList(setupDataList);

        for (int i = 0; i < quizButtons.Count; i++)
        {
            quizButtons[i].SetupButton(setupDataList[i].logicIndex, setupDataList[i].sprite, setupDataList[i].isCorrect);
        }

        if (mainPanel != null) mainPanel.SetActive(true);

        // [추가] 새 스테이지 시작 시에는 아무것도 안 골라진 상태이므로 제출 버튼 비활성화
        UpdateSubmitButtonState();

        Debug.Log($"[{stageID} 시작] 목표 정답 수: {maxSelectCount}개");
    }

    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public bool CanSelectMore()
    {
        return GetSelectedCount() < maxSelectCount;
    }

    // ★ [추가] 현재 플레이어가 몇 개나 선택했는지 세어주는 함수
    private int GetSelectedCount()
    {
        int count = 0;
        foreach (var btn in quizButtons)
        {
            if (btn.IsSelected) count++;
        }
        return count;
    }

    // ★ [추가] QuizButton에서 토글될 때마다 호출되어 제출 버튼 락을 풀거나 잠그는 함수
    public void UpdateSubmitButtonState()
    {
        if (submitBtn != null)
        {
            // 현재 고른 개수가 목표 정답 개수와 정확히 일치할 때만 interactable을 true로 바꿈
            submitBtn.interactable = (GetSelectedCount() == maxSelectCount);
        }
    }

    public void ResetAllButtons()
    {
        foreach (var btn in quizButtons)
        {
            btn.SetSelection(false);
        }
        // 초기화했으니 다시 제출 버튼 잠금
        UpdateSubmitButtonState();
        Debug.Log("모든 선택이 초기화되었습니다.");
    }

    public void SubmitAnswers()
    {
        // 만약 비정상적인 방법으로 클릭되었다면 차단
        if (GetSelectedCount() != maxSelectCount) return;

        int totalCorrectCount = maxSelectCount;
        int selectedCorrectCount = 0;

        foreach (var btn in quizButtons)
        {
            if (btn.IsSelected && btn.IsCorrectAnswer)
            {
                selectedCorrectCount++;
            }
        }

        string finalResult = "오답";

        if (selectedCorrectCount == totalCorrectCount)
        {
            finalResult = "정답";
        }

        Debug.Log($"[제출 확인] 맞춘 정답 개수: {selectedCorrectCount} / {totalCorrectCount}");
        Debug.Log($"<b><color=lime>[최종 판정 결과] : {finalResult}</color></b>");

        if (mainPanel != null) mainPanel.SetActive(false);

        if (onQuizCompleteCallback != null)
        {
            float floatResult = (finalResult == "정답") ? 1f : 0f;
            onQuizCompleteCallback.Invoke(floatResult);
        }
    }

    private struct ButtonSetupData
    {
        public int logicIndex;
        public Sprite sprite;
        public bool isCorrect;

        public ButtonSetupData(int index, Sprite sp, bool correct)
        {
            logicIndex = index;
            sprite = sp;
            isCorrect = correct;
        }
    }
}