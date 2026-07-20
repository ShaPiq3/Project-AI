using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DocumentQuestManager : MonoBehaviour
{
    [System.Serializable]
    public class QuestResult
    {
        public string questTitle;
        public bool isSuccess;
        public float achievementRate;
        public List<int> selectedParagraphs;
    }

    [Header("--- UI Panels ---")]
    [SerializeField] private GameObject originalPanel;       // DocuGame_panel_1 (좌측)
    [SerializeField] private GameObject dimmedOverlay;       // Dimmed_Overlay (좌측 음영 패널)
    [SerializeField] private GameObject analysisPanel;       // DocuGame_panel_1_analysis (우측)

    [Header("--- Progress Bar Settings ---")]
    [SerializeField] private GameObject loadingGroup;        // 로딩 바와 텍스트를 담은 최상위 부모 오브젝트 (Loading_Group)
    [SerializeField] private Image progressBarFill;          // 실제 차오를 게이지 이미지 (Fill_Image)

    [Header("--- Toggle Buttons ---")]
    [SerializeField] private Button toggleAnalysisBtn;      // Toggle_Analysis_Btn (경계선 화살표 버튼)
    [SerializeField] private TextMeshProUGUI toggleBtnText;  // 화살표 버튼 내부 TMP 텍스트 (<<, >> 변경용)
    [SerializeField] private Button summaryExecuteBtn;      // 우측 패널 하단 '요약 실행' 버튼

    [Header("--- Fixed Sentences Parent ---")]
    [SerializeField] private Transform sentencesContainer;  // 버튼들을 품고 있는 부모 오브젝트 (Promes_Analysis_Sentences)

    [Header("--- Quest Settings (Designer Area) ---")]
    [SerializeField] private string documentTitle = "Promes AI 사용자 피드백";
    [Tooltip("정답 문장의 인덱스 리스트 (0부터 시작하므로 문장1=0, 문장2=1, 문장3=2)")]
    [SerializeField] private List<int> correctSentenceIndices = new List<int> { 0, 1, 2, 4 };

    [Header("--- FX Timings ---")]
    [Tooltip("게이지 바가 0%에서 100%까지 채워지는 총 로딩 시간입니다. (초 단위)")]
    [SerializeField] private float loadingDuration = 1.5f;
    [Tooltip("로딩이 완전히 끝난 후 문장 버튼들이 순차적으로 나타나는 간격 시간입니다.")]
    [SerializeField] private float sentenceAppearInterval = 0.25f;

    private List<SentenceBlock> spawnedBlocks = new List<SentenceBlock>();
    private bool isAnalysisOpen = false;
    private bool isScanning = false;

    // 분석 연출이 완전히 끝나서 우측 패널이 작동 중인지 여부 (중복 실행 차단용 스위치)
    private bool isAnalysisActive = false;

    // 외부 DataLogManager에서 연출 진행 여부를 확인하기 위한 프로퍼티
    public bool IsScanning => isScanning;

    // 외부 DataLogManager에서 이미 분석 패널이 활성화되어 진행 중인지 확인하기 위한 프로퍼티
    public bool IsAnalysisActive => isAnalysisActive;

    // 외부 시스템에서 이 문서가 이미 요약 성공 완료 상태인지 확인할 수 있는 판별값
    public bool IsCompleted { get; private set; } = false;

    // 외부 시스템에서 구독하여 데이터 분기 및 연출을 처리할 완성 이벤트
    public static event Action<QuestResult> OnQuestComplete;

    private void Start()
    {
        // 1) 버튼 이벤트 연결
        toggleAnalysisBtn.onClick.AddListener(ToggleAnalysisPanel);
        summaryExecuteBtn.onClick.AddListener(ExecuteSummary);

        // 2) 초기 상태 세팅 (모두 정리된 원본 문서 모드)
        ResetAllUI();
    }

    // 동료 개발자가 창을 활성화(OnEnable)할 때 호출되는 유니티 생명주기 함수
    private void OnEnable()
    {
        if (IsCompleted)
        {
            ApplyCompletedUIState();
        }
    }

    private void SetupFixedBlocks()
    {
        if (sentencesContainer == null) return;

        spawnedBlocks.Clear();
        int index = 0;

        foreach (Transform child in sentencesContainer)
        {
            if (child.GetComponent<Button>() != null)
            {
                SentenceBlock block = child.gameObject.GetComponent<SentenceBlock>();
                if (block == null)
                {
                    block = child.gameObject.AddComponent<SentenceBlock>();
                }

                block.Initialize(index);
                spawnedBlocks.Add(block);

                child.gameObject.SetActive(false);
                index++;
            }
        }
    }

    // 외부 스캔 시스템 연동 완료 시 호출하는 퍼블릭 함수
    public void TriggerScanComplete()
    {
        if (IsCompleted)
        {
            Debug.LogWarning($"[스캔 차단] '{documentTitle}' 문서는 이미 요약 분석이 완료된 상태입니다.");
            return;
        }

        if (isScanning) return;
        isScanning = true;

        // 분석 프로세스 활성화 상태 스위치 On
        isAnalysisActive = true;

        toggleAnalysisBtn.gameObject.SetActive(true);
        OpenAnalysis();

        if (sentencesContainer != null) sentencesContainer.gameObject.SetActive(false);
        if (loadingGroup != null) loadingGroup.SetActive(true);
        if (progressBarFill != null) progressBarFill.fillAmount = 0f;

        SetupFixedBlocks();

        StartCoroutine(ScanAndRevealRoutine());
    }

    private IEnumerator ScanAndRevealRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < loadingDuration)
        {
            elapsedTime += Time.deltaTime;
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = Mathf.Clamp01(elapsedTime / loadingDuration);
            }
            yield return null;
        }

        if (progressBarFill != null) progressBarFill.fillAmount = 1f;

        if (loadingGroup != null) loadingGroup.SetActive(false);
        if (sentencesContainer != null) sentencesContainer.gameObject.SetActive(true);

        for (int i = 0; i < spawnedBlocks.Count; i++)
        {
            if (spawnedBlocks[i] != null)
            {
                spawnedBlocks[i].gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(sentencesContainer.GetComponent<RectTransform>());
            }
            yield return new WaitForSeconds(sentenceAppearInterval);
        }

        isScanning = false;
    }

    private void ToggleAnalysisPanel()
    {
        if (isAnalysisOpen) CloseAnalysis();
        else OpenAnalysis();
    }

    private void OpenAnalysis()
    {
        isAnalysisOpen = true;
        analysisPanel.SetActive(true);
        dimmedOverlay.SetActive(true);
        if (toggleBtnText != null) toggleBtnText.text = "<<";
    }

    private void CloseAnalysis()
    {
        isAnalysisOpen = false;
        analysisPanel.SetActive(false);
        dimmedOverlay.SetActive(false);
        if (toggleBtnText != null) toggleBtnText.text = ">>";
    }

    private void ResetAllUI()
    {
        isAnalysisOpen = false;
        isScanning = false;
        isAnalysisActive = false;

        originalPanel.SetActive(true);
        dimmedOverlay.SetActive(false);
        analysisPanel.SetActive(false);
        toggleAnalysisBtn.gameObject.SetActive(false);
        if (loadingGroup != null) loadingGroup.SetActive(false);
        if (sentencesContainer != null) sentencesContainer.gameObject.SetActive(false);
    }

    private void ApplyCompletedUIState()
    {
        isAnalysisOpen = true;
        isScanning = false;
        isAnalysisActive = true;

        originalPanel.SetActive(true);
        dimmedOverlay.SetActive(true);
        analysisPanel.SetActive(true);
        toggleAnalysisBtn.gameObject.SetActive(true);
        if (toggleBtnText != null) toggleBtnText.text = "<<";

        if (loadingGroup != null) loadingGroup.SetActive(false);

        if (sentencesContainer != null)
        {
            sentencesContainer.gameObject.SetActive(true);
            foreach (Transform child in sentencesContainer)
            {
                if (child.GetComponent<Button>() != null)
                {
                    child.gameObject.SetActive(true);
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(sentencesContainer.GetComponent<RectTransform>());
        }
    }

    private void ExecuteSummary()
    {
        int totalCorrectAnswers = correctSentenceIndices.Count;
        int userCorrectCount = 0;
        List<int> selectedIndices = new List<int>();

        foreach (var block in spawnedBlocks)
        {
            if (block.IsSelected)
            {
                selectedIndices.Add(block.Index);
                if (correctSentenceIndices.Contains(block.Index))
                {
                    userCorrectCount++;
                }
            }
        }

        float achievementRate = 0f;
        if (totalCorrectAnswers > 0)
        {
            achievementRate = (float)userCorrectCount / totalCorrectAnswers * 100f;
        }

        bool success = achievementRate >= 80f;

        QuestResult result = new QuestResult
        {
            questTitle = documentTitle,
            isSuccess = success,
            achievementRate = achievementRate,
            selectedParagraphs = selectedIndices
        };

        if (success)
        {
            IsCompleted = true;
        }

        string json = JsonUtility.ToJson(result);
        PlayerPrefs.SetString("QuestResult_" + result.questTitle, json);
        PlayerPrefs.Save();
        Debug.Log($"[미니게임 데이터 로컬 저장 완료]\n{json}");

        OnQuestComplete?.Invoke(result);

        ResetAllUI();
    }
}