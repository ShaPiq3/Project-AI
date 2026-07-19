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

    public void StartQuest(string questID, int targetCount)
    {
        Debug.Log("StartQuest 호출됨!");
        questTargetCounts[questID] = targetCount;
        Debug.Log($"퀘스트 추가됨! 현재 퀘스트 개수: {questTargetCounts.Count}");
        questCollectedClues[questID] = new List<string>();
        if (!activeQuestIDs.Contains(questID)) activeQuestIDs.Add(questID);
        questStatusUI?.UpdateDisplay();
    }

    /// <summary>
    /// 플레이어가 단서를 발견했을 때 퀘스트ID와 단서ID를 넘겨서 수집
    /// </summary>
    public void AcquireClue(string questID, string clueID)
    {
        Debug.Log($"[디버그] 클릭 감지됨! 퀘스트ID: {questID}, 단서ID: {clueID}");
        Debug.Log($"[디버그] 수집 시도 -> 입력된 퀘스트ID: '{questID}', 수집된 퀘스트 목록: {string.Join(", ", questCollectedClues.Keys)}");
        if (string.IsNullOrEmpty(clueID) || string.IsNullOrEmpty(questID)) return;
        string cleanClueID = clueID.Trim();

        // 1. 이미 수집한 단서인지 체크
        if (collectedClues.Exists(c => c.clueID == cleanClueID)) return;

        // 2. 타이밍 체크
        if (ChatDialogueManager.Instance == null) return;

        // 💡 [추가] 이 단서가 속한 퀘스트가 실제로 시작(isTrigger 발동)됐는지 확인
        //     아직 StartQuest가 호출되지 않은 questID라면, 단서 수집 모드가 켜져 있어도
        //     수집되지 않도록 막습니다. (isTrigger가 발동됐을 때만 수집 가능해야 함)
        if (!questCollectedClues.ContainsKey(questID))
        {
            Debug.LogWarning($"[수집 거부] 퀘스트 '{questID}'가 아직 시작되지 않아 수집할 수 없습니다.");
            return;
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

        // 5. 수집 목록 추가 및 UI 생성
        collectedClues.Add(targetClue);

        // 💡 여기서 UI 생성 함수 호출
        CreateClueSlot(targetClue);
        Debug.Log($"단서 수집 성공: {targetClue.clueID}");
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

    public void ToggleClueSearchMode()
    {
        IsClueSearchModeActive = !IsClueSearchModeActive;

        if (clueFilterPanel != null)
        {
            clueFilterPanel.SetActive(IsClueSearchModeActive);
        }

        Debug.Log($"[시스템] 단서 수집 모드: {IsClueSearchModeActive}");
    }

    public bool CheckIfAllCluesAreCorrect()
    {
        if (collectedClues.Count == 0) return false;
        foreach (var clue in collectedClues)
        {
            if (!clue.isCorrect) return false;
        }
        return true;
    }

    // ============================================================
    // 💡 [변경] 파일탐색기 방식 다중 선택 삭제
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

    public void OnClickGenerateAnswer()
    {
        bool isSuccess = DataLogManager.Instance.CheckIfAllCluesAreCorrect();
        if (isSuccess)
        {
            Debug.Log("정답입니다!");
        }
        else
        {
            Debug.Log("오답입니다.");
        }
    }
}