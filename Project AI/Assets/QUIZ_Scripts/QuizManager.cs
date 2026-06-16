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
    private string currentStageID = "";

    private Action<bool, Sprite> onQuizCompleteCallback;

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
        public string successImageName;
        public string failImageName;
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
            Debug.LogWarning($"[중복 매니저 감지] 이미 다른 QuizManager 인스턴스가 존재하여 현재 오브젝트({gameObject.name})를 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        RefreshButtons();
    }

    private void Start()
    {
        if (resetBtn != null) resetBtn.onClick.AddListener(ResetAllButtons);
        if (submitBtn != null) submitBtn.onClick.AddListener(SubmitAnswers);

        LoadCsvData();

        if (string.IsNullOrEmpty(currentStageID))
        {
            if (mainPanel != null) mainPanel.SetActive(false);
        }
    }

    private void RefreshButtons()
    {
        if (imageGrid != null)
        {
            quizButtons.Clear();
            quizButtons.AddRange(imageGrid.GetComponentsInChildren<QuizButton>(true));
            Debug.Log($"[시스템 체크] imageGrid 하위에서 총 {quizButtons.Count}개의 QuizButton을 완벽히 수집했습니다.");
        }
        else
        {
            Debug.LogError("[시스템 에러] QuizManager 인스펙터에 Image Grid가 연결되어 있지 않습니다!");
        }
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

            if (parts.Length < 27) continue;

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

            string successImg = parts[25].Trim();
            string failImg = parts[26].Trim();

            StageData data = new StageData
            {
                stageID = stageID,
                correctCount = trueCount,
                buttons = btnList,
                successImageName = successImg,
                failImageName = failImg
            };

            stageDatabase[stageID] = data;
        }
        Debug.Log($"엑셀 데이터베이스 빌드 완료: 총 {stageDatabase.Count}개의 스테이지 ID 등록됨.");
    }

    public void PlayStage(string stageID, Action<bool, Sprite> onComplete)
    {
        if (stageDatabase.Count == 0) LoadCsvData();

        if (!stageDatabase.ContainsKey(stageID))
        {
            Debug.LogError($"스테이지 ID [{stageID}] 데이터가 엑셀 데이터베이스에 없습니다!");
            return;
        }

        onQuizCompleteCallback = onComplete;
        currentStageID = stageID;

        StageData data = stageDatabase[stageID];
        maxSelectCount = data.correctCount;

        if (quizButtons == null || quizButtons.Count == 0)
        {
            RefreshButtons();
        }

        List<ButtonSetupData> setupDataList = new List<ButtonSetupData>();

        for (int i = 0; i < quizButtons.Count; i++)
        {
            ButtonData btnData = data.buttons[i];

            Sprite loadedSprite = null;
            if (!string.IsNullOrEmpty(btnData.imageName))
            {
                string path = "StageImages/" + btnData.imageName;

                Debug.Log($"[경로 체크] 버튼 {i}번에 로드하려는 이미지 경로: '{path}'");
                loadedSprite = Resources.Load<Sprite>(path);

                if (loadedSprite == null)
                {
                    Debug.LogError($"[로드 실패] '{path}' 경로에서 이미지를 찾지 못했습니다. Resources 폴더 구조나 파일명을 대조하세요.");
                }
            }

            setupDataList.Add(new ButtonSetupData(i, loadedSprite, btnData.isCorrect));
        }

        ShuffleList(setupDataList);

        for (int i = 0; i < quizButtons.Count; i++)
        {
            quizButtons[i].SetupButton(setupDataList[i].logicIndex, setupDataList[i].sprite, setupDataList[i].isCorrect);
        }

        if (mainPanel != null) mainPanel.SetActive(true);

        UpdateSubmitButtonState();
        Debug.Log($"[{stageID} 미니게임 기동] 목표 정답 수: {maxSelectCount}개, 가동된 버튼 개수: {quizButtons.Count}개");
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

    private int GetSelectedCount()
    {
        int count = 0;
        foreach (var btn in quizButtons)
        {
            if (btn.IsSelected) count++;
        }
        return count;
    }

    public void UpdateSubmitButtonState()
    {
        if (submitBtn != null)
        {
            submitBtn.interactable = (GetSelectedCount() == maxSelectCount);
        }
    }

    public void ResetAllButtons()
    {
        foreach (var btn in quizButtons)
        {
            btn.SetSelection(false);
        }
        UpdateSubmitButtonState();
        Debug.Log("모든 선택이 초기화되었습니다.");
    }

    public void SubmitAnswers()
    {
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

        bool isSuccess = (selectedCorrectCount == totalCorrectCount);
        string finalResult = isSuccess ? "정답" : "오답";

        Debug.Log($"[최종 판정 결과] : {finalResult}");

        Sprite resultSprite = null;

        if (stageDatabase.ContainsKey(currentStageID))
        {
            StageData data = stageDatabase[currentStageID];
            string targetImgName = isSuccess ? data.successImageName : data.failImageName;
            resultSprite = Resources.Load<Sprite>("StageImages/" + targetImgName);
        }

        // 1. 시각적으로 패널을 즉시 끕니다.
        if (mainPanel != null) mainPanel.SetActive(false);

        // 2. 외부 시스템에 결과를 전달합니다.
        if (onQuizCompleteCallback != null)
        {
            onQuizCompleteCallback.Invoke(isSuccess, resultSprite);
        }

        Debug.Log($"[{currentStageID}] 미니게임 제출 완료 및 창 완전히 닫음.");

        // ★ [종료 로직 수정] UI 캔버스를 제외한 프리팹 최상위 부모 오브젝트를 추적하여 통째로 삭제합니다.
        Transform topmostParent = transform;
        while (topmostParent.parent != null && topmostParent.parent.GetComponent<Canvas>() == null)
        {
            topmostParent = topmostParent.parent;
        }

        Destroy(topmostParent.gameObject);
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