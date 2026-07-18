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
    public bool isDeleteMode = false;
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
    /// 플레이어가 단서를 발견했을 때 "ID만" 넘겨서 수집하는 함수
    /// </summary>
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

    // DataLogManager.cs 수정
    public void ToggleLogPanel()
    {
        if (logPanelRect == null) return;

        // 타겟 위치를 '0'이 아니라 '채팅창 옆의 정확한 X 좌표'로 지정하세요.
        // 채팅창 옆이 X=500인 지점이라면, 0f 대신 500f를 넣어야 합니다.
        float targetX = 250f; // 💡 채팅창 옆의 정확한 X 위치값을 직접 입력하세요.

        if (!IsOpen)
        {
            logPanelRect.DOKill();
            // 0f 대신 targetX를 사용하세요.
            logPanelRect.DOAnchorPosX(targetX, duration).SetEase(showEase).SetUpdate(true);
            isOpen = true;
        }
        else
        {
            HideLogPanel();
        }
    }

    // 무조건 우측 바깥으로 미끄러지며 숨겨지는 강제 닫기 함수
    public void HideLogPanel()
    {
        if (logPanelRect == null) return;

        logPanelRect.DOKill();
        logPanelRect.DOAnchorPosX(panelWidth, duration)
            .SetEase(hideEase)
            .SetUpdate(true);

        isOpen = false;
    }

    /// <summary>
    /// 💡 [핵심 수정] 기존에 수집 모드용으로 이미 만들어 두신 버튼 클릭 이벤트(OnClick)에 
    /// 이 함수를 한 줄 추가해서 같이 실행되게 묶어주시면 끝납니다!
    /// </summary>
    public void ToggleClueSearchMode()
    {
        IsClueSearchModeActive = !IsClueSearchModeActive;

        // 💡 수집 모드 상태에 따라 필터 패널을 켜거나 끕니다.
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
            // 하나라도 오답(false)이면 실패
            if (!clue.isCorrect) return false;
        }
        return true;
    }

    // 2. 삭제 모드 토글
    public void ToggleDeleteMode()
    {
        isDeleteMode = !isDeleteMode;
        Debug.Log("삭제 모드: " + isDeleteMode);
        // UI에 '삭제 모드' 표시를 하고 싶다면 여기서 호출
    }

    // 3. 실제 삭제 실행 함수 (팝업에서 '예' 버튼 연결용)
    public void RemoveClueAndRefreshUI(ClueData clue)
    {
        if (collectedClues.Contains(clue))
        {
            collectedClues.Remove(clue);

            // UI를 싹 지우고 다시 그리는 함수(필요시 구현)
            RefreshClueUI();
        }
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

    public void OnClickGenerateAnswer()
    {
        bool isSuccess = DataLogManager.Instance.CheckIfAllCluesAreCorrect();
        if (isSuccess)
        {
            // 정답 대화 호출 로직
            Debug.Log("정답입니다!");
        }
        else
        {
            // 오답 대화 호출 로직
            Debug.Log("오답입니다.");
        }
    }
}