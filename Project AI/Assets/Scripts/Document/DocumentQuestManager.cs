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

    private static readonly Dictionary<string, DocumentQuestManager> registry = new Dictionary<string, DocumentQuestManager>();

    private List<SentenceBlock> spawnedBlocks = new List<SentenceBlock>();
    private bool isAnalysisOpen = false;
    private bool isScanning = false;

    private bool isAnalysisActive = false;

    public bool IsScanning => isScanning;
    public bool IsAnalysisActive => isAnalysisActive;
    public bool IsCompleted { get; private set; } = false;

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
        toggleAnalysisBtn.onClick.AddListener(ToggleAnalysisPanel);
        summaryExecuteBtn.onClick.AddListener(ExecuteSummary);

        ResetAllUI();
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(documentID) && registry.TryGetValue(documentID, out var self) && self == this)
        {
            registry.Remove(documentID);
        }
    }

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

    private bool triggerCountedForDataLog = false;
    private Coroutine scanRoutine;
    public void TriggerScanComplete()
    {
        if (IsCompleted)
        {
            Debug.LogWarning($"[스캔 차단] '{documentTitle}' 문서는 이미 요약 분석이 완료된 상태입니다.");
            return;
        }

        if (isScanning) return;
        isScanning = true;

        isAnalysisActive = true;

        OpenAnalysis();
        toggleAnalysisBtn.gameObject.SetActive(true);
        

        if (sentencesContainer != null) sentencesContainer.gameObject.SetActive(false);
        if (loadingGroup != null) loadingGroup.SetActive(true);
        if (progressBarFill != null) progressBarFill.fillAmount = 0f;

        SetupFixedBlocks();

        scanRoutine = StartCoroutine(ScanAndRevealRoutine()); // 💡 [변경] 참조 저장

        // 💡 [변경] 카운트 중복 증가만 막습니다. 패널을 여는 로직(위쪽 전부)은 그대로 매번 실행됩니다.
        if (!triggerCountedForDataLog && DataLogManager.Instance != null)
        {
            triggerCountedForDataLog = true;
            DataLogManager.Instance.NotifyTriggerStarted();
        }
    }

    // 💡 [추가] 이 패널이 비활성화될 때(창이 닫힐 때), 아직 완료되지 않았다면
    // 진행 중이던 스캔을 취소하고 처음 상태(원본 문서 화면)로 되돌립니다.
    private void OnDisable()
    {
        if (!IsCompleted && isScanning)
        {
            if (scanRoutine != null)
            {
                StopCoroutine(scanRoutine);
                scanRoutine = null;
            }
            ResetAllUI();
        }
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
        if (IsCompleted) return;
        int userCorrectCount = 0;
        List<int> selectedIndices = new List<int>();

        // 1. 플레이어가 선택한 블록 집계 및 정답 개수 계산
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

        int totalUserSelectedCount = selectedIndices.Count;
        int totalCorrectAnswersCount = correctSentenceIndices.Count;
        float achievementRate = 0f;

        // 2. F1-Score 기반 점수 계산
        // 공식: (내가 맞힌 정답 수 * 2) / (전체 정답 수 + 내가 선택한 총 개수) * 100
        int denominator = totalCorrectAnswersCount + totalUserSelectedCount;
        if (denominator > 0)
        {
            achievementRate = ((float)(userCorrectCount * 2) / denominator) * 100f;
        }

        // 3. F1-Score 점수가 80% 이상일 때만 성공 처리
        bool success = achievementRate >= 80f;

        QuestResult result = new QuestResult
        {
            questTitle = documentTitle,
            isSuccess = success,
            achievementRate = achievementRate,
            selectedParagraphs = selectedIndices
        };

        // 완료 처리 및 저장 (재스캔 방지)
        IsCompleted = true;

        if (summaryExecuteBtn != null) summaryExecuteBtn.interactable = false;
        string json = JsonUtility.ToJson(result);
        PlayerPrefs.SetString("QuestResult_" + result.questTitle, json);
        PlayerPrefs.Save();

        Debug.Log($"[F1-Score 판정 결과]\n" +
                  $"전체 정답 수: {totalCorrectAnswersCount}, 내가 선택한 수: {totalUserSelectedCount}, 맞힌 정답 수: {userCorrectCount}\n" +
                  $"최종 F1 점수: {achievementRate:F1}% -> {(success ? "성공" : "실패")}\n{json}");

        OnQuestComplete?.Invoke(result);

        if (ChatDialogueManager.Instance != null)
        {
            int targetDialogueID = success ? successDialogueID : failureDialogueID;
            ChatDialogueManager.Instance.JumpToDialogue(targetDialogueID);
        }

        ResetAllUI();

        // 💡 [추가] 단서 수집 버튼 활성화 트리거 종료 알림
        if (DataLogManager.Instance != null)
        {
            Debug.Log($"[진단-문서] NotifyTriggerEnded 호출! documentID:{documentID}");
            DataLogManager.Instance.NotifyTriggerEnded();
        }
        triggerCountedForDataLog = false;
    }

    public static DocumentQuestManager GetByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (registry.TryGetValue(id, out var manager) && manager != null)
        {
            return manager;
        }

        DocumentQuestManager[] allDocuments = FindObjectsByType<DocumentQuestManager>(FindObjectsInactive.Include);
        foreach (var doc in allDocuments)
        {
            if (doc.documentID == id)
            {
                registry[id] = doc;
                return doc;
            }
        }

        return null;
    }

    public void OpenFromChatBubble()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (panelWindowManager != null)
        {
            panelWindowManager.RestoreWindow();
        }

        // 💡 [변경] 이미 완료된 문서라면 스캔을 다시 돌리지 않고,
        // 완료된 결과 화면 상태를 바로 적용합니다.
        if (IsCompleted)
        {
            ApplyCompletedUIState();
        }
        else
        {
            TriggerScanComplete();
        }
    }
}