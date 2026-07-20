using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ChatDialogueManager : MonoBehaviour
{
    public static ChatDialogueManager Instance { get; private set; } // 💡 외부 접근을 위한 싱글톤 인스턴스 추가

    public TextAsset csvFile;
    public GameObject npcPrefab;
    public GameObject userPrefab;
    public Transform chatContent;
    public TMP_Text topIPText;

    [Header("스크롤 설정")]
    public ScrollRect chatScrollRect;

    [Header("버튼 References (WindowManager 기능 통합)")]
    public Button closeButton;
    public Button showButton;

    [Header("대화창 애니메이션 설정 (화면 밖 -> 안 구조)")]
    [SerializeField] private RectTransform dialoguePanelRect;
    [SerializeField] private float tweenDuration = 0.5f;
    [SerializeField] private float targetPositionX = 0f;
    [SerializeField] private float hidePositionX = 600f;

    [Header("선택지 프리팹 설정")]
    public GameObject branchGroupPrefab;

    [Header("문서 요약 버블 프리팹 설정")]
    [Tooltip("DocumentBubbleController가 붙어있는 프리팹 (로딩바 + 문서 열기 버튼)")]
    [SerializeField] private GameObject documentBubblePrefab;

    [Header("WindowManager 연동")]
    [SerializeField] private WindowManager windowManager;

    private Dictionary<int, DialogueData> dialogueDictionary = new Dictionary<int, DialogueData>();

    private bool isChatWindowOpened = false;
    private bool isTimerFinished = false;
    private bool isDialogueStarted = false;
    private bool isClosedByPlayer = false;
    private bool isDialoguePaused = false;

    public bool IsDialoguePaused
    {
        get => isDialoguePaused;
        set => isDialoguePaused = value;
    }

    // 💡 이제 이 값이 말풍선 타이밍에 맞춰 실시간으로 true / false 제어됩니다!
    public bool IsTriggerActive { get; private set; } = false;

    private GameObject activeBranchInstance;
    private Button[] activeBranchButtons;
    private bool isWaitingForBranchSelection = false;
    private int selectedNextId = -1;
    private string selectedUserText = "";

    private CanvasGroup dialogueCanvasGroup;

    // 💡 [추가] 현재 진행 중인 메인 대화 코루틴 참조 (정답/오답 분기 등으로 점프할 때 정지시키기 위함)
    private Coroutine mainDialogueCoroutine;

    void Awake()
    {
        // 💡 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ParseCSV();

        if (dialoguePanelRect != null)
        {
            dialoguePanelRect.anchoredPosition = new Vector2(hidePositionX, dialoguePanelRect.anchoredPosition.y);
            dialoguePanelRect.gameObject.SetActive(true);

            dialogueCanvasGroup = dialoguePanelRect.GetComponent<CanvasGroup>();
            if (dialogueCanvasGroup == null)
            {
                dialogueCanvasGroup = dialoguePanelRect.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseChatWindow);
            closeButton.interactable = true;
        }

        if (showButton != null)
        {
            showButton.onClick.AddListener(OpenChatWindowByPlayer);
            showButton.gameObject.SetActive(false);
        }

        StartCoroutine(StartAbsoluteTimer());
    }

    void ParseCSV()
    {
        if (csvFile == null) return;

        string[] rows = csvFile.text.Replace("\r", "").Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = rows[i].Split(',');
            if (columns.Length < 8) continue;

            DialogueData data = new DialogueData();
            int.TryParse(columns[0].Trim(), out data.id);
            data.speakerType = columns[1].Trim();
            data.speakerName = columns[2].Trim();
            data.dialogueText = columns[3].Trim().Replace("\"", "");
            bool.TryParse(columns[4].Trim(), out data.hasImage);
            data.imagePath = columns[5].Trim();
            float.TryParse(columns[6].Trim(), out data.delayTime);
            data.ipAddress = columns[7].Trim();

            if (columns.Length >= 15)
            {
                bool.TryParse(columns[8].Trim(), out data.isBranch);
                data.branchText1 = columns[9].Trim().Replace("\"", "");
                int.TryParse(columns[10].Trim(), out data.nextId1);
                data.branchText2 = columns[11].Trim().Replace("\"", "");
                int.TryParse(columns[12].Trim(), out data.nextId2);
                data.branchText3 = columns[13].Trim().Replace("\"", "");
                int.TryParse(columns[14].Trim(), out data.nextId3);
            }
            else
            {
                data.isBranch = false;
                data.branchText1 = ""; data.nextId1 = 0;
                data.branchText2 = ""; data.nextId2 = 0;
                data.branchText3 = ""; data.nextId3 = 0;
            }

            if (columns.Length >= 16)
            {
                bool.TryParse(columns[15].Trim(), out data.isTrigger);
            }
            else
            {
                data.isTrigger = false;
            }

            if (columns.Length >= 17)
            {
                data.questID = columns[16].Trim(); // 실제 CSV 순서에 맞게 인덱스를 조정하세요!
            }
            else
            {
                data.questID = "Q1"; // 혹시 비어있을 경우를 대비한 기본값 설정 (테스트용)
            }

            if (columns.Length >= 18)
            {
                int.TryParse(columns[17].Trim(), out data.targetCount);
            }
            else
            {
                data.targetCount = 5; // 데이터가 없을 경우 기본값(예: 5)
            }

            // 💡 [추가] 문서 요약 버블 관련 컬럼 (18, 19, 20번째 컬럼)
            if (columns.Length >= 19)
            {
                bool.TryParse(columns[18].Trim(), out data.isDocumentBubble);
            }
            else
            {
                data.isDocumentBubble = false;
            }

            if (columns.Length >= 20)
            {
                data.documentID = columns[19].Trim();
            }
            else
            {
                data.documentID = "";
            }

            if (columns.Length >= 21)
            {
                float.TryParse(columns[20].Trim(), out data.bubbleLoadingDuration);
            }
            else
            {
                data.bubbleLoadingDuration = 0f; // 0이면 DocumentBubbleController의 기본값 사용
            }

            // 💡 [추가] 강제 점프 ID (22번째 컬럼)
            if (columns.Length >= 22)
            {
                int.TryParse(columns[21].Trim(), out data.overrideNextId);
            }
            else
            {
                data.overrideNextId = 0;
            }

            // 💡 [추가] 정답/오답 대화 ID (23, 24번째 컬럼) - isTrigger 행에만 채우면 됨
            if (columns.Length >= 23)
            {
                int.TryParse(columns[22].Trim(), out data.correctDialogueID);
            }
            else
            {
                data.correctDialogueID = 0;
            }

            if (columns.Length >= 24)
            {
                int.TryParse(columns[23].Trim(), out data.incorrectDialogueID);
            }
            else
            {
                data.incorrectDialogueID = 0;
            }

            if (!dialogueDictionary.ContainsKey(data.id))
            {
                dialogueDictionary.Add(data.id, data);
            }
            else
            {
                dialogueDictionary[data.id] = data;
            }
        }
    }

    IEnumerator StartAbsoluteTimer()
    {
        yield return new WaitForSeconds(3f);
        isTimerFinished = true;
        if (isClosedByPlayer || isChatWindowOpened) yield break;
        TriggerOpenChat();
    }

    public void TriggerOpenChat()
    {
        if (isClosedByPlayer || isChatWindowOpened) return;
        isChatWindowOpened = true;

        if (showButton != null) showButton.gameObject.SetActive(false);

        if (dialoguePanelRect != null)
        {
            dialoguePanelRect.gameObject.SetActive(true);
            dialoguePanelRect.DOKill();

            if (dialogueCanvasGroup != null)
            {
                dialogueCanvasGroup.interactable = true;
                dialogueCanvasGroup.blocksRaycasts = true;
            }

            if (closeButton != null)
            {
                closeButton.interactable = true;
                closeButton.transform.SetAsLastSibling();
            }

            dialoguePanelRect.DOAnchorPosX(targetPositionX, tweenDuration).SetEase(Ease.OutQuad);

            if (windowManager != null)
            {
                WindowManager.Instance.isChatOpen = true;
                WindowManager.Instance.RefreshAllWindows();
            }
        }

        if (isTimerFinished) TryStartDialogue();
    }

    public void OpenChatWindowByPlayer()
    {
        isClosedByPlayer = false;
        isChatWindowOpened = true;

        if (showButton != null) showButton.gameObject.SetActive(false);

        if (dialoguePanelRect != null)
        {
            dialoguePanelRect.DOKill();
            if (dialogueCanvasGroup != null)
            {
                dialogueCanvasGroup.interactable = true;
                dialogueCanvasGroup.blocksRaycasts = true;
            }
            if (closeButton != null) closeButton.interactable = true;

            dialoguePanelRect.DOAnchorPosX(targetPositionX, tweenDuration).SetEase(Ease.OutQuad);

            if (windowManager != null)
            {
                WindowManager.Instance.isChatOpen = true;
                WindowManager.Instance.RefreshAllWindows();
            }
        }
        TryStartDialogue();
    }

    public void CloseChatWindow()
    {
        if (dialoguePanelRect == null) return;

        isClosedByPlayer = true;
        isChatWindowOpened = false;

        if (activeBranchInstance != null && !activeBranchInstance.Equals(null))
        {
            activeBranchInstance.SetActive(false);
        }

        if (closeButton != null) closeButton.interactable = false;

        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.interactable = false;
            dialogueCanvasGroup.blocksRaycasts = false;
        }

        // 💡 채팅창이 실제로 닫힐 때 WindowManager의 벽(오른쪽 제한)도 함께 해제
        if (windowManager != null)
        {
            windowManager.isChatOpen = false;
        }

        dialoguePanelRect.DOKill();
        dialoguePanelRect.DOAnchorPosX(hidePositionX, tweenDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (showButton != null)
                {
                    showButton.gameObject.SetActive(true);
                    showButton.transform.SetAsLastSibling();
                }
            });
    }

    private void TryStartDialogue()
    {
        if (isDialogueStarted)
        {
            if (isWaitingForBranchSelection && activeBranchInstance != null && !activeBranchInstance.Equals(null))
            {
                activeBranchInstance.SetActive(true);
            }
            return;
        }
        isDialogueStarted = true;
        StartCoroutine(StartChatGenerationWithDelay());
    }

    IEnumerator StartChatGenerationWithDelay(int startId = 1)
    {
        yield return new WaitForSeconds(tweenDuration + 0.1f);
        mainDialogueCoroutine = StartCoroutine(GenerateChatWithExcelDelay(startId));
    }

    /// <summary>
    /// 💡 [추가] 외부(DataLogManager, DocumentQuestManager 등)에서
    /// "정답/오답" 또는 "성공/실패"에 따라 지정된 대화 ID로 즉시 점프해서
    /// 그 이후 대화를 CSV에 작성된 그대로 이어서 재생합니다.
    /// </summary>
    public void JumpToDialogue(int targetId)
    {
        if (!dialogueDictionary.ContainsKey(targetId))
        {
            Debug.LogWarning($"[ChatDialogueManager] 대화 ID {targetId} 를 CSV에서 찾을 수 없습니다.");
            return;
        }

        // 기존에 진행 중이던 대화/분기 UI를 정리합니다.
        if (mainDialogueCoroutine != null)
        {
            StopCoroutine(mainDialogueCoroutine);
            mainDialogueCoroutine = null;
        }

        if (activeBranchInstance != null && !activeBranchInstance.Equals(null))
        {
            Destroy(activeBranchInstance);
        }
        isWaitingForBranchSelection = false;
        isDialoguePaused = false;
        isClosedByPlayer = false;

        // 💡 [추가] 트리거로 인해 멈춰있던 상태였다면 여기서 완전히 해제
        // (이전에는 2.5초 타이머가 이 역할을 했지만, 이제는 JumpToDialogue가 담당)
        IsTriggerActive = false;

        // 채팅창이 닫혀있다면 강제로 열어서 점프한 대화가 바로 보이게 합니다.
        if (!isChatWindowOpened)
        {
            isChatWindowOpened = true;

            if (showButton != null) showButton.gameObject.SetActive(false);

            if (dialoguePanelRect != null)
            {
                dialoguePanelRect.DOKill();

                if (dialogueCanvasGroup != null)
                {
                    dialogueCanvasGroup.interactable = true;
                    dialogueCanvasGroup.blocksRaycasts = true;
                }
                if (closeButton != null) closeButton.interactable = true;

                dialoguePanelRect.DOAnchorPosX(targetPositionX, tweenDuration).SetEase(Ease.OutQuad);

                if (windowManager != null)
                {
                    WindowManager.Instance.isChatOpen = true;
                    WindowManager.Instance.RefreshAllWindows();
                }
            }
        }

        isDialogueStarted = true; // 이미 시작된 것으로 표시 (TryStartDialogue의 초기 진입 로직과 충돌 방지)
        mainDialogueCoroutine = StartCoroutine(GenerateChatWithExcelDelay(targetId));
    }

    IEnumerator GenerateChatWithExcelDelay(int startId = 1)
    {
        int currentId = startId;

        while (dialogueDictionary.ContainsKey(currentId))
        {
            while (isClosedByPlayer || isDialoguePaused)
            {
                yield return null;
            }

            DialogueData data = dialogueDictionary[currentId];

            if (topIPText != null)
            {
                topIPText.text = !string.IsNullOrEmpty(data.ipAddress) ? $"IP : {data.ipAddress}" : "IP : -";
            }

            // 🌟 [핵심 수정 위치] 말풍선이 출력되기 전 트리거 세팅
            if (data.isTrigger)
            {
                IsTriggerActive = true;
                IsDialoguePaused = true;

                if (DataLogManager.Instance != null)
                {
                    string targetQuestID = data.questID;
                    Debug.Log($"퀘스트 시작! 매니저로 전달할 ID: {targetQuestID}");

                    DataLogManager.Instance.StartQuest(data.questID, data.targetCount, data.correctDialogueID, data.incorrectDialogueID);

                    if (DataLogManager.Instance.questStatusUI != null)
                    {
                        DataLogManager.Instance.questStatusUI.UpdateDisplay();
                    }

                    if (!DataLogManager.Instance.IsOpen)
                    {
                        DataLogManager.Instance.ToggleLogPanel();
                    }
                }
                // 💡 [변경] 이전에는 여기서 2.5초 뒤 자동으로 대화가 재개됐지만,
                // 이제는 플레이어가 실제로 "답변 생성"을 눌러 JumpToDialogue()가
                // 호출될 때까지 대화가 계속 멈춰있어야 하므로 자동 타이머를 제거합니다.
                // (JumpToDialogue 안에서 IsDialoguePaused = false / IsTriggerActive = false 처리)
            }

            if (!data.isBranch || !string.IsNullOrEmpty(data.dialogueText))
            {
                bool isUser = (data.speakerType == "USER");
                GameObject selectedPrefab = isUser ? userPrefab : npcPrefab;

                if (selectedPrefab != null)
                {
                    GameObject go = Instantiate(selectedPrefab, chatContent);
                    ChatBubbleController controller = go.GetComponent<ChatBubbleController>();
                    if (controller != null) controller.SetupBubble(data);

                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());

                    if (chatScrollRect != null) chatScrollRect.verticalNormalizedPosition = 0f;
                }

                yield return new WaitForSeconds(data.delayTime);
            }

            // 💡 [추가] 문서 요약 버블 생성 (로딩바 → 문서 열기 버튼)
            // 일반 텍스트 말풍선 뒤에 이어서 표시됩니다.
            if (data.isDocumentBubble)
            {
                if (documentBubblePrefab != null)
                {
                    GameObject bubbleGo = Instantiate(documentBubblePrefab, chatContent);
                    DocumentBubbleController bubbleController = bubbleGo.GetComponent<DocumentBubbleController>();
                    if (bubbleController != null)
                    {
                        bubbleController.Setup(data);
                    }
                    else
                    {
                        Debug.LogWarning("[ChatDialogueManager] documentBubblePrefab에 DocumentBubbleController가 없습니다!");
                    }

                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());
                    if (chatScrollRect != null) chatScrollRect.verticalNormalizedPosition = 0f;
                }
                else
                {
                    Debug.LogWarning("[ChatDialogueManager] documentBubblePrefab이 인스펙터에 연결되지 않았습니다!");
                }

                // 💡 [추가] 문서 버블이 뜬 뒤에는 대화가 자동으로 계속 진행되면 안 됩니다.
                // 플레이어가 실제로 문서(요약) 퀘스트를 끝내고 DocumentQuestManager.ExecuteSummary()가
                // JumpToDialogue()를 호출할 때까지 여기서 멈춰있어야, 9/10번처럼 정답/오답 대화가
                // 순서대로 다 재생되어버리는 문제가 생기지 않습니다.
                IsDialoguePaused = true;
            }

            if (data.isBranch)
            {
                ShowBranchUI(data);
                isWaitingForBranchSelection = true;
                while (isWaitingForBranchSelection)
                {
                    while (isDialoguePaused) yield return null;
                    yield return null;
                }

                if (userPrefab != null && !string.IsNullOrEmpty(selectedUserText))
                {
                    GameObject userSpeechGo = Instantiate(userPrefab, chatContent);
                    ChatBubbleController controller = userSpeechGo.GetComponent<ChatBubbleController>();
                    if (controller != null)
                    {
                        DialogueData userSelectionData = new DialogueData();
                        userSelectionData.speakerType = "USER";
                        userSelectionData.speakerName = "AI assistant";
                        userSelectionData.dialogueText = selectedUserText;
                        userSelectionData.hasImage = false;
                        controller.SetupBubble(userSelectionData);
                    }

                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());
                    if (chatScrollRect != null) chatScrollRect.verticalNormalizedPosition = 0f;

                    yield return new WaitForSeconds(0.8f);
                }

                currentId = selectedNextId;
            }
            else
            {
                // 💡 [변경] overrideNextId가 지정되어 있으면(0이 아니면) 그 ID로 강제 점프,
                // 아니면 기존처럼 다음 순번으로 진행
                currentId = data.overrideNextId != 0 ? data.overrideNextId : currentId + 1;
            }
        }
    }

    private IEnumerator ResumeDialogueAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        IsDialoguePaused = false;
        IsTriggerActive = false; // ⭕ 대화가 다음으로 넘어가므로 단서 수집 가능 상태를 꺼줍니다!
    }

    private void ShowBranchUI(DialogueData data)
    {
        if (branchGroupPrefab == null) return;

        if (activeBranchInstance != null && !activeBranchInstance.Equals(null))
        {
            Destroy(activeBranchInstance);
        }

        activeBranchInstance = Instantiate(branchGroupPrefab, chatContent);
        activeBranchButtons = activeBranchInstance.GetComponentsInChildren<Button>(true);

        SetBranchButton(0, data.branchText1, data.nextId1);
        SetBranchButton(1, data.branchText2, data.nextId2);
        SetBranchButton(2, data.branchText3, data.nextId3);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());
        if (chatScrollRect != null) chatScrollRect.verticalNormalizedPosition = 0f;
    }

    private void SetBranchButton(int index, string text, int nextId)
    {
        if (activeBranchButtons == null || index >= activeBranchButtons.Length) return;
        Button targetButton = activeBranchButtons[index];
        if (targetButton == null) return;

        if (!string.IsNullOrEmpty(text) && nextId != 0)
        {
            targetButton.gameObject.SetActive(true);
            TMP_Text buttonText = targetButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null) buttonText.text = text;

            targetButton.onClick.RemoveAllListeners();
            targetButton.onClick.AddListener(() => OnBranchSelected(nextId, text));
        }
        else
        {
            targetButton.gameObject.SetActive(false);
        }
    }

    private void OnBranchSelected(int nextId, string text)
    {
        selectedNextId = nextId;
        selectedUserText = text;

        if (activeBranchInstance != null && !activeBranchInstance.Equals(null))
        {
            Destroy(activeBranchInstance);
        }
        isWaitingForBranchSelection = false;
    }
}