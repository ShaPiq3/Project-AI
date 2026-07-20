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

    [Header("--- Document Identity (채팅 버블 연동용) ---")]
    [Tooltip("채팅 버블에서 이 문서를 지정할 때 쓰는 고유 ID (CSV의 documentID 컬럼과 일치해야 함)")]
    [SerializeField] private string documentID;
    public string DocumentID => documentID;

    [Header("--- 이 문서 창 자체를 여닫는 InGameWindowManager (선택 사항) ---")]
    [SerializeField] private InGameWindowManager panelWindowManager;

    [Header("--- 성공/실패 대화 분기 (엑셀 dialogueID) ---")]
    [Tooltip("요약이 성공(isSuccess=true)했을 때 점프할 대화 CSV의 ID")]
    [SerializeField] private int successDialogueID;
    [Tooltip("요약이 실패(isSuccess=false)했을 때 점프할 대화 CSV의 ID")]
    [SerializeField] private int failureDialogueID;

    // 💡 여러 개의 문서(DocumentQuestManager)가 씬에 있을 수 있으므로,
    // documentID로 찾을 수 있도록 정적 레지스트리를 둡니다.
    private static readonly Dictionary<string, DocumentQuestManager> registry = new Dictionary<string, DocumentQuestManager>();

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

    private void Awake()
    {
        if (!string.IsNullOrEmpty(documentID))
        {
            registry[documentID] = this;
        }
    }

    private void Start()
    {
        // 1) 버튼 이벤트 연결
        toggleAnalysisBtn.onClick.AddListener(ToggleAnalysisPanel);
        summaryExecuteBtn.onClick.AddListener(ExecuteSummary);

        // 2) 초기 상태 세팅 (모두 정리된 원본 문서 모드)
        ResetAllUI();
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(documentID) && registry.TryGetValue(documentID, out var self) && self == this)
        {
            registry.Remove(documentID);
        }
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

        // 💡 [추가] 성공/실패 여부에 따라 지정된 대화 ID로 점프해서
        // 그 이후 대화(CSV에 작성된 내용)를 이어서 재생합니다.
        if (ChatDialogueManager.Instance != null)
        {
            int targetDialogueID = success ? successDialogueID : failureDialogueID;
            ChatDialogueManager.Instance.JumpToDialogue(targetDialogueID);
        }

        ResetAllUI();
    }

    public static DocumentQuestManager GetByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (registry.TryGetValue(id, out var manager) && manager != null)
        {
            return manager;
        }

        // 💡 [추가] 씬 시작 시점에 비활성화된 문서 패널은 Awake가 아직 실행되지 않아
        // 레지스트리에 등록이 안 되어 있을 수 있습니다. 이 경우를 대비해
        // 비활성화된 오브젝트까지 포함해서 한 번 더 찾아봅니다.
        DocumentQuestManager[] allDocuments = FindObjectsByType<DocumentQuestManager>(FindObjectsInactive.Include);
        foreach (var doc in allDocuments)
        {
            if (doc.documentID == id)
            {
                registry[id] = doc; // 다음부터는 바로 찾을 수 있도록 캐싱
                return doc;
            }
        }

        return null;
    }

    // 💡 채팅 버블의 "문서 열기" 버튼이 호출하는 함수.
    // 창이 최소화되어 있었다면 먼저 복원한 뒤, 기존 스캔 연출(TriggerScanComplete)을 시작합니다.
    public void OpenFromChatBubble()
    {
        // 💡 [추가] 코루틴은 GameObject가 활성화된 상태에서만 시작할 수 있습니다.
        // panelWindowManager 연결 여부와 상관없이, 이 오브젝트 자체가 꺼져있다면 먼저 켭니다.
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (panelWindowManager != null)
        {
            panelWindowManager.RestoreWindow();
        }

        TriggerScanComplete();
    }
}