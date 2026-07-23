using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DataLogManager : MonoBehaviour
{
    public static DataLogManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private RectTransform logPanelRect;
    [SerializeField] private GameObject clueSlotPrefab;
    [SerializeField] private Transform clueContainer;

    [Header("DOTween Settings")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private Ease showEase = Ease.OutCubic;
    [SerializeField] private Ease hideEase = Ease.InCubic;

    [Header("Filter Settings")]
    [SerializeField] private GameObject clueFilterPanel;

    [Header("UI Reference")]
    public QuestStatusUI questStatusUI;

    // 전체 단서 데이터베이스 (엑셀에서 파싱해서 담아둘 사전)
    private Dictionary<string, ClueData> clueDatabase = new Dictionary<string, ClueData>();

    // 플레이어가 실제로 인게임에서 획득/수집한 단서 목록
    private List<ClueData> collectedClues = new List<ClueData>();
    private bool isOpen = false;
    public bool IsOpen => isOpen;

    // 💡 [변경] 파일탐색기 방식 다중 선택 삭제를 위한 선택 목록
    //     (기존 isDeleteMode / selectedSlot 방식은 제거)
    private List<ClueData> selectedForDeletion = new List<ClueData>();

    public UIManager uiManager;

    private float panelWidth;

    // 💡 다른 스크립트에서 참조할 단서 수집 모드 활성화 여부
    public bool IsClueSearchModeActive { get; private set; } = false;
    // 퀘스트ID : [수집된 단서ID 리스트]
    public Dictionary<string, List<string>> questCollectedClues = new Dictionary<string, List<string>>();
    // 퀘스트ID : [목표 개수]
    public Dictionary<string, int> questTargetCounts = new Dictionary<string, int>();

    /// <summary>
    /// 💡 이 퀘스트가 실제로 시작(isTrigger 발동 -> StartQuest 호출)된 상태인지 확인합니다.
    /// 호버 효과, 클릭 수집 등에서 "아직 시작 안 된 퀘스트의 단서인데 반응이 생기는" 문제를 막는 데 씁니다.
    /// </summary>
    public bool IsQuestActive(string questID)
    {
        if (string.IsNullOrEmpty(questID)) return false;
        return questCollectedClues.ContainsKey(questID);
    }

    // 💡 퀘스트별 정답/오답 대화 시작 ID (StartQuest 호출 시 함께 등록됨)
    [System.Serializable]
    private class QuestDialogueConfig
    {
        public string questID;
        public int correctDialogueID;
        public int incorrectDialogueID;
    }
    private Dictionary<string, QuestDialogueConfig> questDialogueConfigs = new Dictionary<string, QuestDialogueConfig>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 시작할 때 패널 크기를 가져와서 정확히 그만큼 화면 '우측' 바깥으로 숨김
        if (logPanelRect != null)
        {
            panelWidth = logPanelRect.rect.width;
            logPanelRect.anchoredPosition = new Vector2(panelWidth, logPanelRect.anchoredPosition.y);
        }

        // 게임 시작 시 엑셀 데이터 파싱
        LoadClueDatabase();
    }

    /// <summary>
    /// 엑셀(CSV) 파싱 결과를 가져와서 메모리에 등록하는 함수
    /// </summary>
    private void LoadClueDatabase()
    {
        clueDatabase.Clear();

        List<Dictionary<string, object>> excelRows = CSVReader.Read("ClueExcelData"); // 예시 시트 이름

        if (excelRows == null) return;

        foreach (var row in excelRows)
        {
            ClueData clue = new ClueData();
            clue.clueID = row["ClueID"].ToString();
            clue.sourceType = row["SourceType"].ToString();
            clue.sourceTitle = row["SourceTitle"].ToString();
            clue.contentText = row["ContentText"].ToString().Replace("\\n", "\n"); // 줄바꿈 적용
            clue.imageName = row["ImageName"].ToString();
            clue.questID = row["QuestID"].ToString();

            if (row.ContainsKey("isCorrect"))
            {
                clue.isCorrect = bool.Parse(row["isCorrect"].ToString());
            }

            clueDatabase.Add(clue.clueID, clue);
        }
    }

    public List<string> activeQuestIDs = new List<string>();

    /// <summary>
    /// 💡 정답/오답 대화 ID도 함께 받아서 등록합니다.
    /// (ChatDialogueManager가 isTrigger 행을 처리할 때, 같은 행에 적힌
    ///  correctDialogueID/incorrectDialogueID를 그대로 넘겨줍니다)
    /// </summary>
    public void StartQuest(string questID, int targetCount, int correctDialogueID = 0, int incorrectDialogueID = 0)
    {
        Debug.Log("StartQuest 호출됨!");
        questTargetCounts[questID] = targetCount;
        Debug.Log($"퀘스트 추가됨! 현재 퀘스트 개수: {questTargetCounts.Count}");
        questCollectedClues[questID] = new List<string>();
        if (!activeQuestIDs.Contains(questID)) activeQuestIDs.Add(questID);

        questDialogueConfigs[questID] = new QuestDialogueConfig
        {
            questID = questID,
            correctDialogueID = correctDialogueID,
            incorrectDialogueID = incorrectDialogueID
        };

        // 💡 [디버그용] 실제로 어떤 값이 저장되는지 확인
        Debug.Log($"[디버그] StartQuest 저장값 -> questID:{questID}, correctDialogueID:{correctDialogueID}, incorrectDialogueID:{incorrectDialogueID}");

        questStatusUI?.UpdateDisplay();
    }

    /// <summary>
    /// 플레이어가 단서를 발견했을 때 퀘스트ID와 단서ID를 넘겨서 수집
    /// </summary>
    /// <param name="overrideSourceTitle">
    /// 뉴스 기사/커뮤니티 게시글의 실제 제목을 그대로 쓰고 싶을 때 넘깁니다.
    /// 비워두면 기존처럼 엑셀 ClueExcelData의 SourceTitle 값을 그대로 씁니다.
    /// </param>
    public void AcquireClue(string questID, string clueID, string overrideSourceTitle = null)
    {
        Debug.Log($"[디버그] 클릭 감지됨! 퀘스트ID: {questID}, 단서ID: {clueID}");
        Debug.Log($"[디버그] 수집 시도 -> 입력된 퀘스트ID: '{questID}', 수집된 퀘스트 목록: {string.Join(", ", questCollectedClues.Keys)}");
        if (string.IsNullOrEmpty(clueID) || string.IsNullOrEmpty(questID)) return;
        string cleanClueID = clueID.Trim();

        // 1. 이미 수집한 단서인지 체크
        if (collectedClues.Exists(c => c.clueID == cleanClueID)) return;

        // 2. 타이밍 체크
        if (ChatDialogueManager.Instance == null) return;

        // 💡 이 단서가 속한 퀘스트가 실제로 시작(isTrigger 발동)됐는지 확인
        //     아직 StartQuest가 호출되지 않은 questID라면, 단서 수집 모드가 켜져 있어도
        //     수집되지 않도록 막습니다. (isTrigger가 발동됐을 때만 수집 가능해야 함)
        if (!questCollectedClues.ContainsKey(questID))
        {
            Debug.LogWarning($"[수집 거부] 퀘스트 '{questID}'가 아직 시작되지 않아 수집할 수 없습니다.");
            return;
        }

        // 💡 이미 목표 개수(targetCount)만큼 다 모았다면 더 이상 수집하지 못하게 막습니다.
        //     (퀘스트에는 정답/오답 단서가 섞여서 여러 개 있을 수 있지만,
        //      엑셀이 정한 targetCount개까지만 모을 수 있어야 함)
        if (questTargetCounts.TryGetValue(questID, out int targetCount))
        {
            int currentCount = questCollectedClues[questID].Count;
            if (currentCount >= targetCount)
            {
                Debug.LogWarning($"[수집 거부] 퀘스트 '{questID}'는 이미 목표 개수({targetCount}개)를 다 모아서 더 이상 수집할 수 없습니다.");
                return;
            }
        }

        // 3. 데이터베이스 조회 (중요: 여기서 데이터를 찾았을 때만 다음으로 진행)
        if (!clueDatabase.TryGetValue(cleanClueID, out ClueData targetClue))
        {
            Debug.LogWarning($"데이터베이스에 없음: {cleanClueID}");
            return;
        }

        // 4. 퀘스트 카운트 체크
        if (questCollectedClues.ContainsKey(questID))
        {
            if (!questCollectedClues[questID].Contains(cleanClueID))
            {
                questCollectedClues[questID].Add(cleanClueID);
                questStatusUI?.UpdateDisplay();
            }
        }

        // 💡 [추가] clueDatabase의 targetClue는 모든 곳에서 공유되는 마스터 객체이므로
        // 직접 수정하지 않고, 복사본을 만들어서 필요하면 제목만 실제 값으로 덮어씁니다.
        ClueData collectedClue = new ClueData
        {
            clueID = targetClue.clueID,
            sourceType = targetClue.sourceType,
            sourceTitle = !string.IsNullOrEmpty(overrideSourceTitle) ? overrideSourceTitle : targetClue.sourceTitle,
            contentText = targetClue.contentText,
            imageName = targetClue.imageName,
            questID = targetClue.questID,
            isCorrect = targetClue.isCorrect
        };

        // 5. 수집 목록 추가 및 UI 생성
        collectedClues.Add(collectedClue);

        // 💡 여기서 UI 생성 함수 호출
        CreateClueSlot(collectedClue);
        Debug.Log($"단서 수집 성공: {collectedClue.clueID}");
    }


    public void RemoveClue(string questID, string clueID)
    {
        if (questCollectedClues.ContainsKey(questID) && questCollectedClues[questID].Contains(clueID))
        {
            questCollectedClues[questID].Remove(clueID);
            Object.FindAnyObjectByType<QuestStatusUI>()?.UpdateDisplay();
        }
    }

    public ClueData GetClueData(string clueID)
    {
        if (clueDatabase.TryGetValue(clueID, out ClueData targetClue))
        {
            return targetClue;
        }
        return null;
    }

    private void CreateClueSlot(ClueData clue)
    {
        Debug.Log($"[디버그] 슬롯 생성 시도: {clue.clueID}");
        if (clueSlotPrefab == null || clueContainer == null)
        {
            Debug.LogError("슬롯 프리팹이나 컨테이너가 연결 안 됨!");
            return;
        }

        GameObject slotGo = Instantiate(clueSlotPrefab, clueContainer);
        ClueSlot slotScript = slotGo.GetComponent<ClueSlot>();

        if (slotScript != null)
        {
            slotScript.SetClueUI(clue);
        }
    }

    public void ToggleLogPanel()
    {
        if (logPanelRect == null) return;

        float targetX = 250f; // 💡 채팅창 옆의 정확한 X 위치값

        if (!IsOpen)
        {
            logPanelRect.DOKill();
            logPanelRect.DOAnchorPosX(targetX, duration).SetEase(showEase).SetUpdate(true);

            CanvasGroup cg = logPanelRect.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            isOpen = true;
        }
        else
        {
            HideLogPanel();
        }
    }

    public void HideLogPanel()
    {
        if (logPanelRect == null) return;

        logPanelRect.DOKill();
        logPanelRect.DOAnchorPosX(panelWidth, duration)
            .SetEase(hideEase)
            .SetUpdate(true);

        CanvasGroup cg = logPanelRect.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.DOKill();
            cg.DOFade(0f, duration).SetUpdate(true);
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        isOpen = false;
    }

    /// <summary>
    /// 💡 사이드바의 "단서 수집" 버튼 전용 함수.
    /// 토글이 아니라 "켜는 것"만 담당합니다 - 이미 켜져 있으면 아무 일도 하지 않습니다.
    /// (버튼에 RestoreWindow 등 다른 열기 로직이 같이 연결되어 있어도
    ///  꺼짐 상태와 충돌하지 않도록, 끄는 동작은 이 함수에서 절대 하지 않습니다.)
    /// 끄는 것은 ClueFilterPanelCloser(ESC/우클릭)에서만 처리합니다.
    /// </summary>
    public void OpenClueSearchMode()
    {
        if (IsClueSearchModeActive)
        {
            // 이미 켜져 있으면 완전히 무시 (버튼이 막힌 것처럼 동작)
            return;
        }

        IsClueSearchModeActive = true;

        if (clueFilterPanel != null)
        {
            clueFilterPanel.SetActive(true);
        }

        Debug.Log("[시스템] 단서 수집 모드: True");
    }

    public void ToggleClueSearchMode()
    {
        IsClueSearchModeActive = !IsClueSearchModeActive;

        if (clueFilterPanel != null)
        {
            clueFilterPanel.SetActive(IsClueSearchModeActive);
        }

        Debug.Log($"[시스템] 단서 수집 모드: {IsClueSearchModeActive}");
    }

    /// <summary>
    /// 현재 진행 중인 퀘스트(가장 최근 시작된 퀘스트) 기준으로:
    /// 1) 정답으로 표시된(isCorrect=true) 단서를 하나도 빠짐없이 전부 수집했고
    /// 2) 오답 단서는 하나도 수집하지 않았을 때만 true를 반환합니다.
    /// (이전에는 "수집한 것 중에 오답이 없는가"만 확인해서,
    ///  정답 단서를 일부만 모아도 정답 처리되는 버그가 있었습니다.)
    /// </summary>
    public bool CheckIfAllCluesAreCorrect()
    {
        if (activeQuestIDs.Count == 0) return false;

        // 가장 최근에 시작된 퀘스트를 "현재 판정 대상 퀘스트"로 취급
        string currentQuestID = activeQuestIDs[activeQuestIDs.Count - 1];

        // 1. 마스터 데이터베이스에서 이 퀘스트에 속한 "정답" 단서 ID 목록을 모두 뽑음
        List<string> requiredCorrectClueIDs = new List<string>();
        foreach (var kvp in clueDatabase)
        {
            ClueData dbClue = kvp.Value;
            if (dbClue.questID == currentQuestID && dbClue.isCorrect)
            {
                requiredCorrectClueIDs.Add(dbClue.clueID);
            }
        }

        // 정답으로 지정된 단서가 하나도 없다면(데이터 미설정) 실패로 처리
        if (requiredCorrectClueIDs.Count == 0) return false;

        // 2. 이 퀘스트에 해당하는, 실제로 수집한 단서들만 추림
        List<ClueData> collectedForThisQuest = collectedClues.FindAll(c => c.questID == currentQuestID);

        // 3. 오답을 하나라도 수집했으면 실패
        foreach (var clue in collectedForThisQuest)
        {
            if (!clue.isCorrect) return false;
        }

        // 4. 정답으로 지정된 단서를 전부 수집했는지 확인 (하나라도 빠지면 실패)
        foreach (var requiredID in requiredCorrectClueIDs)
        {
            bool isCollected = collectedForThisQuest.Exists(c => c.clueID == requiredID);
            if (!isCollected) return false;
        }

        return true;
    }

    // ============================================================
    // 💡 파일탐색기 방식 다중 선택 삭제
    // ============================================================

    /// <summary>
    /// ClueSlot의 체크박스(Toggle) 상태가 바뀔 때마다 호출됩니다.
    /// </summary>
    public void SetClueSelected(ClueSlot slot, bool isSelected)
    {
        if (slot == null || slot.clueData == null) return;

        if (isSelected)
        {
            if (!selectedForDeletion.Contains(slot.clueData))
            {
                selectedForDeletion.Add(slot.clueData);
            }
        }
        else
        {
            selectedForDeletion.Remove(slot.clueData);
        }
    }

    /// <summary>
    /// "데이터 삭제" 버튼의 OnClick에 연결하는 함수.
    /// 체크된 단서가 있을 때만 확인 팝업을 띄우고, 확인 시 전부 삭제합니다.
    /// </summary>
    public void OnClickDeleteSelectedClues()
    {
        if (selectedForDeletion.Count == 0)
        {
            Debug.Log("삭제할 단서를 먼저 체크해주세요.");
            return;
        }

        List<ClueData> targets = new List<ClueData>(selectedForDeletion); // 클릭 시점 스냅샷
        int count = targets.Count;

        uiManager.ShowConfirmPopup(
            $"선택한 {count}개의 단서를 삭제하시겠습니까?",
            () =>
            {
                RemoveCluesAndRefreshUI(targets);
                selectedForDeletion.Clear();
            },
            () =>
            {
                Debug.Log("삭제 취소");
            }
        );
    }

    /// <summary>
    /// 여러 단서를 한 번에 삭제하고 UI는 마지막에 한 번만 새로고침합니다.
    /// </summary>
    public void RemoveCluesAndRefreshUI(List<ClueData> cluesToRemove)
    {
        foreach (var clue in cluesToRemove)
        {
            if (collectedClues.Contains(clue))
            {
                collectedClues.Remove(clue);
                RemoveClue(clue.questID, clue.clueID); // 퀘스트 진행도 카운트도 함께 감소
            }
        }

        RefreshClueUI();
    }

    private void RefreshClueUI()
    {
        // 1. 기존 슬롯 싹 다 삭제
        foreach (Transform child in clueContainer) Destroy(child.gameObject);

        // 2. 남은 리스트로 다시 생성
        foreach (var clue in collectedClues) CreateClueSlot(clue);

        // 3. 퀘스트 상태 UI 갱신
        questStatusUI?.UpdateDisplay();
    }

    // ============================================================
    // 수집된 단서를 다시 클릭하면 원본 위치(뉴스/SNS/커뮤니티)를 열어주는 기능
    // 각 매니저(NewsListManager, SNSManager, CommunityManager)의 싱글톤(Instance)을
    // 통해 접근하므로 이 스크립트에서 별도로 인스펙터 연결할 필요가 없습니다.
    // ============================================================
    public void OpenClueSource(ClueData clue)
    {
        if (clue == null) return;

        bool found = false;

        // 💡 대소문자/공백 차이로 놓치는 일이 없도록 정규화해서 비교
        string normalizedType = clue.sourceType?.Trim().ToUpper();

        switch (normalizedType)
        {
            case "NEWS":
            case "뉴스":
                if (NewsListManager.Instance != null)
                {
                    found = NewsListManager.Instance.TryOpenClueSource(clue.clueID);
                }
                break;

            case "SNS":
                if (SNSManager.Instance != null)
                {
                    found = SNSManager.Instance.TryOpenClueSource(clue.clueID);
                }
                break;

            case "COMMUNITY":
            case "COMMENT":
            case "커뮤니티":
            case "댓글":
                if (CommunityManager.Instance != null)
                {
                    found = CommunityManager.Instance.TryOpenClueSource(clue.clueID);
                }
                break;

            case "ARCHIVE":
            case "아카이브":
                if (ArchiveManager.Instance != null)
                {
                    found = ArchiveManager.Instance.TryOpenClueSource(clue.clueID);
                }
                break;
        }

        if (!found)
        {
            Debug.LogWarning($"[원본 열기 실패] sourceType: {clue.sourceType}, clueID: {clue.clueID} 에 해당하는 원본을 찾지 못했습니다.");
        }
    }

    /// <summary>
    /// 💡 정답/오답 대화 시작 ID는 대화 CSV의 isTrigger 행에서 함께 전달받아
    /// StartQuest() 시점에 이미 등록되어 있습니다 (QuestConfigData 등 별도 시트 불필요).
    /// </summary>
    public void OnClickGenerateAnswer()
    {
        bool isSuccess = DataLogManager.Instance.CheckIfAllCluesAreCorrect();

        string currentQuestID = activeQuestIDs.Count > 0 ? activeQuestIDs[activeQuestIDs.Count - 1] : null;

        if (isSuccess)
        {
            Debug.Log("정답입니다!");
        }
        else
        {
            Debug.Log("오답입니다.");
        }

        // 💡 답변 생성 버튼을 누르면 판정 결과와 상관없이
        // DataLog 패널이 오른쪽으로 슬라이드되며 닫히고, 수집했던 단서도 전부 비웁니다.
        collectedClues.Clear();
        selectedForDeletion.Clear();
        RefreshClueUI();
        HideLogPanel();

        if (string.IsNullOrEmpty(currentQuestID) || !questDialogueConfigs.TryGetValue(currentQuestID, out QuestDialogueConfig config))
        {
            Debug.LogWarning($"[DataLogManager] 퀘스트 '{currentQuestID}'에 대한 정답/오답 대화 설정을 찾지 못했습니다. (StartQuest 호출 시 correctDialogueID/incorrectDialogueID가 전달됐는지 확인하세요)");
            return;
        }

        int targetDialogueID = isSuccess ? config.correctDialogueID : config.incorrectDialogueID;

        // 💡 [디버그용] 실제 판정 결과와 선택된 대화 ID 확인
        Debug.Log($"[디버그] isSuccess:{isSuccess}, questID:{currentQuestID}, config.correct:{config.correctDialogueID}, config.incorrect:{config.incorrectDialogueID}, 선택된targetDialogueID:{targetDialogueID}");

        if (ChatDialogueManager.Instance != null)
        {
            ChatDialogueManager.Instance.JumpToDialogue(targetDialogueID);
        }
    }

    // ============================================================
    //  [추가 기능] 문서 패널(DocuGame_panel_1 등) 클릭 이벤트 매핑용 함수
    // ============================================================
    public void OnClickDocumentPanel(DocumentQuestManager targetQuestManager)
    {
        if (targetQuestManager == null) return;

        // 1. 단서 수집 모드가 true인 상태인지 검사
        if (!IsClueSearchModeActive)
        {
            Debug.Log("[시스템] 단서 수집 모드가 비활성화 상태이므로 문서를 분석할 수 없습니다.");
            return;
        }

        // 2. 이미 게이지가 차오르고 있거나(IsScanning), 연출이 끝나서 분석 창이 활성화(IsAnalysisActive)된 상태라면 완벽 차단
        if (targetQuestManager.IsScanning || targetQuestManager.IsAnalysisActive)
        {
            Debug.LogWarning("[스캔 차단] 이미 분석이 진행 중이거나 분석 패널이 활성화되어 있습니다.");
            return;
        }

        // 3. 이미 해당 문서가 성공적으로 요약 실행이 완료되었는지 검사
        if (targetQuestManager.IsCompleted)
        {
            Debug.LogWarning("[스캔 차단] 이 문서는 이미 요약 분석이 완료되어 재실행할 수 없습니다.");
            return;
        }

        // 4. 모든 조건 통과 시에만 최초 1회 연출 시동
        Debug.Log($"[시스템] 단서 수집 조건 충족. '{targetQuestManager.name}' 분석 연출을 시작합니다.");
        targetQuestManager.TriggerScanComplete();
    }
}