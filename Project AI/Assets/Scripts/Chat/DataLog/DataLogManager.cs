using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DataLogManager : MonoBehaviour
{
    public static DataLogManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject clueSlotPrefab;
    [SerializeField] private Transform clueContainer;
    [SerializeField] private AudioSource clueCollectedAudioSource;

    [Header("Filter Settings")]
    [SerializeField] private GameObject clueFilterPanel;
    [SerializeField] private CanvasGroup clueFilterPanelCanvasGroup;
    [SerializeField] private float duration = 0.4f;

    [Header("UI Reference")]
    public QuestStatusUI questStatusUI;

    [Header("Trigger-Linked Button")]
    [Tooltip("사이드바의 '단서 수집' 버튼. isTrigger/isImageGenTrigger/문서요약 중 하나라도 진행 중이면 자동으로 활성화되고, 전부 끝나면 비활성화됩니다.")]
    [SerializeField] private Button clueCollectButton;

    [Header("단서 수집 버튼 스프라이트")]
    [SerializeField] private Image clueCollectButtonImage;   // clueCollectButton에 붙은 Image 컴포넌트
    [SerializeField] private Sprite clueCollectNormalSprite;  // 평소(꺼짐) 스프라이트
    [SerializeField] private Sprite clueCollectActiveSprite;  // 단서 수집 모드 켜짐 스프라이트

    [Header("Sidebar MiniIcon 연동")]
    [Tooltip("단서 수집 상태를 사이드바 미니아이콘으로 보여주기 위한 SidebarController 참조")]
    [SerializeField] private SidebarController sidebarController;
    [Tooltip("SidebarController의 Menu Mini Icon Rects 배열에서 '단서 수집' 아이콘의 인덱스")]
    [SerializeField] private int clueCollectTaskbarIndex = -1;

    [Header("DataLog ↔ Chat 사이 여닫기 버튼 (<< / >>)")]
    [Tooltip("트리거가 활성화된 동안에만 보이는, 패널 가장자리에 붙는 여닫기 탭 버튼")]
    [SerializeField] private GameObject edgeToggleButtonRoot;   // 버튼을 담는 오브젝트 (없으면 edgeToggleButton.gameObject 사용)
    [SerializeField] private Button edgeToggleButton;
    [SerializeField] private Image edgeToggleButtonImage;
    [SerializeField] private Sprite edgeToggleClosedSprite;      // 패널 닫힘 상태 (">>" 펼치기)
    [SerializeField] private Sprite edgeToggleOpenSprite;        // 패널 열림 상태 ("<<" 접기)

    [Header("Hover Preview 설정")]
    [SerializeField] private float hoverPreviewCloseDelay = 0.15f;

    private Coroutine hoverCloseCoroutine;


    // 전체 단서 데이터베이스 (엑셀에서 파싱해서 담아둘 사전)
    private Dictionary<string, ClueData> clueDatabase = new Dictionary<string, ClueData>();

    // 플레이어가 실제로 인게임에서 획득/수집한 단서 목록
    private List<ClueData> collectedClues = new List<ClueData>();

    /// <summary>
    /// 💡 [변경] 패널이 열려있는지 여부는 WindowManager가 유일하게 관리하는 상태(IsDatalogOpen)를 그대로 반환합니다.
    /// (이전에는 DataLogManager가 자체 bool(isOpen)을 따로 들고 있어서, WindowManager와 서로 다른
    ///  상태를 관리하게 되어 DataLog_Btn / 좌측 버튼 등 여러 진입점으로 열고 닫을 때 상태가 어긋나는 버그가 있었습니다.)
    /// </summary>
    /// 
    public bool HasActiveTrigger => activeTriggerCount > 0;
    public bool IsOpen => WindowManager.Instance != null && WindowManager.Instance.IsDatalogOpen;
    // 💡 [변경] 파일탐색기 방식 다중 선택 삭제를 위한 선택 목록
    //     (기존 isDeleteMode / selectedSlot 방식은 제거)
    private List<ClueData> selectedForDeletion = new List<ClueData>();

    private int activeTriggerCount = 0;

    public UIManager uiManager;

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

    /// <summary>
    /// 💡 [추가] 이 단서를 이미 수집했는지 확인합니다.
    /// 이미 수집한 단서는 호버 효과/클릭 수집이 다시 반응하지 않게 하는 데 씁니다.
    /// </summary>
    public bool IsClueAlreadyCollected(string clueID)
    {
        if (string.IsNullOrEmpty(clueID)) return false;
        string cleanClueID = clueID.Trim();
        return collectedClues.Exists(c => c.clueID == cleanClueID);
    }

    /// <summary>
    /// 💡 [추가] 이 퀘스트가 이미 목표 개수(targetCount)만큼 다 모아서
    /// 더 이상 아무 단서도 수집할 수 없는 상태인지 확인합니다.
    /// (이 단서 자체를 이미 모았는지와는 별개 - 퀘스트 전체가 꽉 찬 경우)
    /// </summary>
    public bool IsQuestCapReached(string questID)
    {
        if (string.IsNullOrEmpty(questID)) return false;
        if (!questTargetCounts.TryGetValue(questID, out int target)) return false;
        if (!questCollectedClues.TryGetValue(questID, out var collectedList)) return false;
        return collectedList.Count >= target;
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

        LoadClueDatabase();

        if (clueCollectButton != null)
        {
            clueCollectButton.interactable = false;
        }
        UpdateClueCollectButtonSprite(false);

        // 💡 추가: 여닫기 탭 버튼 초기화
        if (edgeToggleButton != null)
        {
            edgeToggleButton.onClick.AddListener(OnEdgeToggleButtonClicked);
        }
        ShowEdgeToggleButton(false);
    }

    private void OnEdgeToggleButtonClicked()
    {
        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.ToggleOrPinDatalog(); // 기존 ToggleDatalogWindow() 대신
        }
        UpdateEdgeToggleButtonSprite();
    }

    private void ShowEdgeToggleButton(bool show)
    {
        var target = edgeToggleButtonRoot != null ? edgeToggleButtonRoot
                    : (edgeToggleButton != null ? edgeToggleButton.gameObject : null);
        if (target != null) target.SetActive(show);
    }

    /// <summary>
    /// 💡 sidebar의 DataLog_Btn 등 다른 경로로 패널이 열리고/닫혀도
    /// WindowManager가 이 함수를 호출해주면 << / >> 아이콘이 항상 정확히 동기화됩니다.
    /// </summary>
    public void UpdateEdgeToggleButtonSprite()
    {
        if (edgeToggleButtonImage == null) return;
        edgeToggleButtonImage.sprite = IsOpen ? edgeToggleOpenSprite : edgeToggleClosedSprite;
    }

    private void UpdateClueCollectButtonSprite(bool isActive)
    {
        if (clueCollectButtonImage == null) return;

        clueCollectButtonImage.sprite = isActive
            ? clueCollectActiveSprite
            : clueCollectNormalSprite;
    }

    public void OnEdgeHoverEnter()
    {
        if (hoverCloseCoroutine != null)
        {
            StopCoroutine(hoverCloseCoroutine);
            hoverCloseCoroutine = null;
        }

        WindowManager.Instance?.PreviewOpenDatalog();
    }

    public void OnEdgeHoverExit()
    {
        if (hoverCloseCoroutine != null) StopCoroutine(hoverCloseCoroutine);
        hoverCloseCoroutine = StartCoroutine(DelayedPreviewClose());
    }

    private System.Collections.IEnumerator DelayedPreviewClose()
    {
        yield return new WaitForSeconds(hoverPreviewCloseDelay);
        WindowManager.Instance?.PreviewCloseDatalog();
        hoverCloseCoroutine = null;
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
        questTargetCounts[questID] = targetCount;
        questCollectedClues[questID] = new List<string>();
        if (!activeQuestIDs.Contains(questID)) activeQuestIDs.Add(questID);

        questDialogueConfigs[questID] = new QuestDialogueConfig
        {
            questID = questID,
            correctDialogueID = correctDialogueID,
            incorrectDialogueID = incorrectDialogueID
        };

        questStatusUI?.UpdateDisplay();
    }

    /// <summary>
    /// 💡 isTrigger, isImageGenTrigger, 문서요약 트리거가 시작될 때 호출합니다.
    /// 여러 개가 동시에 진행 중일 수 있으므로 카운트를 늘립니다.
    /// </summary>
    public void NotifyTriggerStarted()
    {
        activeTriggerCount++;
        UpdateClueCollectButtonState();

        ShowEdgeToggleButton(true);
        UpdateEdgeToggleButtonSprite();
    }

    /// <summary>
    /// 💡 [추가] 다른 트리거(이미지 생성 등)와 상호 배타적으로 동작하도록,
    /// 내부 카운터와 무관하게 clueCollectButton의 interactable을 강제로 설정합니다.
    /// </summary>
    public void SetClueCollectButtonInteractable(bool value)
    {
        if (clueCollectButton != null)
        {
            clueCollectButton.interactable = value;
        }
    }

    /// <summary>
    /// 💡 트리거 하나가 완료(답변 생성/판정 완료)될 때 호출합니다.
    /// 다른 트리거가 아직 진행 중이면 버튼은 계속 켜진 채로 유지됩니다.
    /// </summary>
    public void NotifyTriggerEnded()
    {
        activeTriggerCount = Mathf.Max(0, activeTriggerCount - 1);
        UpdateClueCollectButtonState();
    }

    private void UpdateClueCollectButtonState()
    {
        bool isActive = activeTriggerCount > 0;

        if (clueCollectButton != null)
        {
            clueCollectButton.interactable = isActive;
        }

        UpdateClueCollectButtonSprite(isActive);

        if (!isActive && sidebarController != null && clueCollectTaskbarIndex >= 0)
        {
            sidebarController.UpdateTaskbarStatus(clueCollectTaskbarIndex, 0);
        }

        if (activeTriggerCount == 0 && IsClueSearchModeActive)
        {
            ToggleClueSearchMode();
        }

        // 💡 추가: 트리거가 전부 끝나면 여닫기 버튼도 함께 숨김
        if (!isActive)
        {
            ShowEdgeToggleButton(false);
        }
    }

    /// <summary>
    /// 플레이어가 단서를 발견했을 때 퀘스트ID와 단서ID를 넘겨서 수집
    /// </summary>
    public void AcquireClue(string questID, string clueID, string overrideSourceTitle = null)
    {
        if (string.IsNullOrEmpty(clueID) || string.IsNullOrEmpty(questID)) return;
        string cleanClueID = clueID.Trim();

        if (collectedClues.Exists(c => c.clueID == cleanClueID)) return;
        if (ChatDialogueManager.Instance == null) return;

        if (!questCollectedClues.ContainsKey(questID))
        {
            Debug.LogWarning($"[수집 거부] 퀘스트 '{questID}'가 아직 시작되지 않아 수집할 수 없습니다.");
            return;
        }

        if (questTargetCounts.TryGetValue(questID, out int targetCount))
        {
            int currentCount = questCollectedClues[questID].Count;
            if (currentCount >= targetCount)
            {
                Debug.LogWarning($"[수집 거부] 퀘스트 '{questID}'는 이미 목표 개수({targetCount}개)를 다 모아서 더 이상 수집할 수 없습니다.");
                return;
            }
        }

        if (!clueDatabase.TryGetValue(cleanClueID, out ClueData targetClue))
        {
            Debug.LogWarning($"데이터베이스에 없음: {cleanClueID}");
            return;
        }

        if (questCollectedClues.ContainsKey(questID))
        {
            if (!questCollectedClues[questID].Contains(cleanClueID))
            {
                questCollectedClues[questID].Add(cleanClueID);
                questStatusUI?.UpdateDisplay();
            }
        }

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

        collectedClues.Add(collectedClue);
        CreateClueSlot(collectedClue);

        if (clueCollectedAudioSource != null) clueCollectedAudioSource.Play();
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

    /// <summary>
    /// 💡 [추가] 주어진 clueID의 정확한 questID를 마스터 데이터(ClueExcelData)에서 자동으로 찾습니다.
    /// </summary>
    public string ResolveQuestID(string clueID, string fallbackQuestID = null)
    {
        if (string.IsNullOrEmpty(clueID)) return fallbackQuestID;

        if (clueDatabase.TryGetValue(clueID.Trim(), out ClueData masterClue) && !string.IsNullOrEmpty(masterClue.questID))
        {
            return masterClue.questID;
        }

        return fallbackQuestID;
    }

    private void CreateClueSlot(ClueData clue)
    {
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

    /// <summary>
    /// 💡 [변경] 패널의 실제 열기/닫기는 WindowManager가 전담합니다 (DataLog_Btn과 동일한 진입점).
    /// DataLogManager는 더 이상 자체적으로 패널 위치/CanvasGroup을 건드리지 않습니다.
    /// 기존에 이 함수를 호출하던 코드(ChatDialogueManager 등)는 그대로 써도 됩니다.
    /// </summary>
    public void ToggleLogPanel()
    {
        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.ToggleDatalogWindow();
        }
    }

    /// <summary>
    /// 💡 [변경] 닫기도 WindowManager에 위임합니다.
    /// </summary>
    public void HideLogPanel()
    {
        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.CloseDatalogDirect();
        }
    }

    /// <summary>
    /// 💡 사이드바의 "단서 수집" 버튼 전용 함수.
    /// </summary>
    public void OpenClueSearchMode()
    {
        if (IsClueSearchModeActive)
        {
            return;
        }

        IsClueSearchModeActive = true;

        if (clueFilterPanel != null)
        {
            clueFilterPanel.SetActive(true);
            clueFilterPanel.transform.SetAsLastSibling();

            if (clueFilterPanelCanvasGroup != null)
            {
                clueFilterPanelCanvasGroup.DOKill();
                clueFilterPanelCanvasGroup.alpha = 0f;
                clueFilterPanelCanvasGroup.DOFade(1f, duration).SetUpdate(true);
            }
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayClueSearchModeOnSound();
    }

    public void ToggleClueSearchMode()
    {
        IsClueSearchModeActive = !IsClueSearchModeActive;

        if (clueFilterPanel != null)
        {
            if (IsClueSearchModeActive)
            {
                clueFilterPanel.SetActive(true);
                clueFilterPanel.transform.SetAsLastSibling();

                if (clueFilterPanelCanvasGroup != null)
                {
                    clueFilterPanelCanvasGroup.DOKill();
                    clueFilterPanelCanvasGroup.alpha = 0f;
                    clueFilterPanelCanvasGroup.DOFade(1f, duration).SetUpdate(true);
                }

                if (SoundManager.Instance != null) SoundManager.Instance.PlayClueSearchModeOnSound();
            }
            else
            {
                if (clueFilterPanelCanvasGroup != null)
                {
                    clueFilterPanelCanvasGroup.DOKill();
                    clueFilterPanelCanvasGroup.DOFade(0f, duration).SetUpdate(true)
                        .OnComplete(() => clueFilterPanel.SetActive(false));
                }
                else
                {
                    clueFilterPanel.SetActive(false);
                }
            }
        }

        if (!IsClueSearchModeActive && sidebarController != null && clueCollectTaskbarIndex >= 0)
        {
            sidebarController.UpdateTaskbarStatus(clueCollectTaskbarIndex, 0);
        }
    }

    /// <summary>
    /// 현재 진행 중인 퀘스트(가장 최근 시작된 퀘스트) 기준으로 정답 여부를 판정합니다.
    /// </summary>
    public bool CheckIfAllCluesAreCorrect()
    {
        if (activeQuestIDs.Count == 0) return false;

        string currentQuestID = activeQuestIDs[activeQuestIDs.Count - 1];

        List<string> requiredCorrectClueIDs = new List<string>();
        foreach (var kvp in clueDatabase)
        {
            ClueData dbClue = kvp.Value;
            if (dbClue.questID == currentQuestID && dbClue.isCorrect)
            {
                requiredCorrectClueIDs.Add(dbClue.clueID);
            }
        }

        if (requiredCorrectClueIDs.Count == 0) return false;

        List<ClueData> collectedForThisQuest = collectedClues.FindAll(c => c.questID == currentQuestID);

        foreach (var clue in collectedForThisQuest)
        {
            if (!clue.isCorrect) return false;
        }

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

    public void OnClickDeleteSelectedClues()
    {
        if (selectedForDeletion.Count == 0)
        {
            return;
        }

        List<ClueData> targets = new List<ClueData>(selectedForDeletion);
        int count = targets.Count;

        uiManager.ShowConfirmPopup(
            $"선택한 {count}개의 단서를 삭제하시겠습니까?",
            () =>
            {
                RemoveCluesAndRefreshUI(targets);
                selectedForDeletion.Clear();
            },
            () => { }
        );
    }

    /// <summary>
    /// 💡 [추가] 사이드바 "단서 수집" 버튼의 OnClick()에 연결하는 함수.
    /// </summary>
    public void OnClueCollectButtonClicked()
    {
        if (activeTriggerCount <= 0) return;

        if (sidebarController != null && clueCollectTaskbarIndex >= 0)
        {
            sidebarController.UpdateTaskbarStatus(clueCollectTaskbarIndex, 2);
        }

        OpenClueSearchMode();
    }

    public void RemoveCluesAndRefreshUI(List<ClueData> cluesToRemove)
    {
        foreach (var clue in cluesToRemove)
        {
            if (collectedClues.Contains(clue))
            {
                collectedClues.Remove(clue);
                RemoveClue(clue.questID, clue.clueID);
            }
        }

        RefreshClueUI();
    }

    private void RefreshClueUI()
    {
        foreach (Transform child in clueContainer) Destroy(child.gameObject);
        foreach (var clue in collectedClues) CreateClueSlot(clue);
        questStatusUI?.UpdateDisplay();
    }

    // ============================================================
    // 수집된 단서를 다시 클릭하면 원본 위치를 열어주는 기능
    // ============================================================
    public void OpenClueSource(ClueData clue)
    {
        if (clue == null) return;

        bool found = false;
        string normalizedType = clue.sourceType?.Trim().ToUpper();

        switch (normalizedType)
        {
            case "NEWS":
            case "뉴스":
                if (NewsListManager.Instance != null)
                    found = NewsListManager.Instance.TryOpenClueSource(clue.clueID);
                break;

            case "SNS":
                if (SNSManager.Instance != null)
                    found = SNSManager.Instance.TryOpenClueSource(clue.clueID);
                break;

            case "COMMUNITY":
            case "COMMENT":
            case "커뮤니티":
            case "댓글":
                if (CommunityManager.Instance != null)
                    found = CommunityManager.Instance.TryOpenClueSource(clue.clueID);
                break;

            case "ARCHIVE":
            case "아카이브":
                if (ArchiveManager.Instance != null)
                    found = ArchiveManager.Instance.TryOpenClueSource(clue.clueID);
                break;
        }

        if (!found)
        {
            Debug.LogWarning($"[원본 열기 실패] sourceType: {clue.sourceType}, clueID: {clue.clueID} 에 해당하는 원본을 찾지 못했습니다.");
        }
    }

    /// <summary>
    /// 💡 정답/오답 대화 시작 ID는 대화 CSV의 isTrigger 행에서 함께 전달받아 등록되어 있습니다.
    /// </summary>
    public void OnClickGenerateAnswer()
    {
        bool isSuccess = CheckIfAllCluesAreCorrect();

        string currentQuestID = activeQuestIDs.Count > 0 ? activeQuestIDs[activeQuestIDs.Count - 1] : null;

        // 💡 답변 생성 버튼을 누르면 판정 결과와 상관없이
        // DataLog 패널이 오른쪽으로 슬라이드되며 닫히고, 수집했던 단서도 전부 비웁니다.
        collectedClues.Clear();
        selectedForDeletion.Clear();
        RefreshClueUI();
        HideLogPanel();
        NotifyTriggerEnded();

        if (string.IsNullOrEmpty(currentQuestID) || !questDialogueConfigs.TryGetValue(currentQuestID, out QuestDialogueConfig config))
        {
            Debug.LogWarning($"[DataLogManager] 퀘스트 '{currentQuestID}'에 대한 정답/오답 대화 설정을 찾지 못했습니다.");
            return;
        }

        int targetDialogueID = isSuccess ? config.correctDialogueID : config.incorrectDialogueID;

        if (ChatDialogueManager.Instance != null)
        {
            ChatDialogueManager.Instance.JumpToDialogue(targetDialogueID);
        }
    }

    // ============================================================
    //  문서 패널 클릭 이벤트 매핑용 함수
    // ============================================================
    public void OnClickDocumentPanel(DocumentQuestManager targetQuestManager)
    {
        if (targetQuestManager == null) return;

        if (!IsClueSearchModeActive)
        {
            return;
        }

        if (targetQuestManager.IsScanning || targetQuestManager.IsAnalysisActive)
        {
            return;
        }

        if (targetQuestManager.IsCompleted)
        {
            return;
        }

        targetQuestManager.TriggerScanComplete();
    }
}