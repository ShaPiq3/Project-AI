using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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

    private bool isTriggerActive = false;
    public bool IsTriggerActive
    {
        get => isTriggerActive;
        private set
        {
            if (isTriggerActive == value) return;
            isTriggerActive = value;
            OnTriggerActiveChanged?.Invoke(isTriggerActive);
        }
    }

    /// <summary> IsTriggerActive 값이 바뀔 때마다 발생 (true = isTrigger 진행 중 / false = 아님) </summary>
    public event System.Action<bool> OnTriggerActiveChanged;
    private GameObject activeBranchInstance;
    private Button[] activeBranchButtons;
    private bool isWaitingForBranchSelection = false;
    private int selectedNextId = -1;
    private string selectedUserText = "";

    private CanvasGroup dialogueCanvasGroup;

    // 💡 현재 진행 중인 메인 대화 코루틴 참조 (정답/오답 분기 등으로 점프할 때 정지시키기 위함)
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

    /// <summary>
    /// 💡 [추가] 한 줄(row)을 콤마로 나누되, 큰따옴표(")로 감싼 구간 안의 콤마는
    /// 구분자로 취급하지 않습니다. 예: "안녕, 반가워요" -> 하나의 컬럼으로 유지됨.
    /// </summary>
    private string[] SplitCsvLine(string line)
    {
        return Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
    }

    /// <summary>
    /// 💡 [추가] CSV 셀 값에서 "바깥을 감싸는 큰따옴표 한 쌍"만 벗겨내고,
    /// 셀 안에 실제 글자로 들어있는 큰따옴표(CSV 규칙상 ""로 이스케이프된 것)는
    /// 하나의 " 로 되돌려줍니다. (인터뷰 인용문 등에서 실제 큰따옴표를 쓰고 싶을 때 사용)
    /// 예) 셀 원본: "그는 ""정말 놀랍다""라고 말했다"
    ///     변환 후: 그는 "정말 놀랍다"라고 말했다
    /// </summary>
    private string UnwrapCsvQuotes(string raw)
    {
        string s = raw.Trim();
        if (s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\""))
        {
            s = s.Substring(1, s.Length - 2);
        }
        return s.Replace("\"\"", "\"");
    }

    void ParseCSV()
    {
        if (csvFile == null) return;

        string[] rows = csvFile.text.Replace("\r", "").Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < rows.Length; i++)
        {
            // 💡 [변경] 단순 Split(',') 대신, "큰따옴표로 감싼 구간의 콤마는 무시"하는 파서 사용.
            string[] columns = SplitCsvLine(rows[i]);
            if (columns.Length < 8) continue;

            DialogueData data = new DialogueData();
            int.TryParse(columns[0].Trim(), out data.id);
            data.speakerType = columns[1].Trim();
            data.speakerName = columns[2].Trim();
            // 💡 [변경] .Replace("\"","") 대신 UnwrapCsvQuotes 사용 -> 인터뷰 인용문 등 실제 큰따옴표는 보존됨
            data.dialogueText = UnwrapCsvQuotes(columns[3]).Replace("\\n", "\n").Replace("|", "\n");
            bool.TryParse(columns[4].Trim(), out data.hasImage);
            data.imagePath = columns[5].Trim();
            float.TryParse(columns[6].Trim(), out data.delayTime);
            data.ipAddress = columns[7].Trim();

            if (columns.Length >= 15)
            {
                bool.TryParse(columns[8].Trim(), out data.isBranch);
                data.branchText1 = UnwrapCsvQuotes(columns[9]).Replace("\\n", "\n").Replace("|", "\n");
                int.TryParse(columns[10].Trim(), out data.nextId1);
                data.branchText2 = UnwrapCsvQuotes(columns[11]).Replace("\\n", "\n").Replace("|", "\n");
                int.TryParse(columns[12].Trim(), out data.nextId2);
                data.branchText3 = UnwrapCsvQuotes(columns[13]).Replace("\\n", "\n").Replace("|", "\n");
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
                data.questID = columns[16].Trim();
            }
            else
            {
                data.questID = "Q1";
            }

            if (columns.Length >= 18)
            {
                int.TryParse(columns[17].Trim(), out data.targetCount);
            }
            else
            {
                data.targetCount = 5;
            }

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
                data.bubbleLoadingDuration = 0f;
            }

            if (columns.Length >= 22)
            {
                int.TryParse(columns[21].Trim(), out data.overrideNextId);
            }
            else
            {
                data.overrideNextId = 0;
            }

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

            if (columns.Length >= 25)
            {
                bool.TryParse(columns[24].Trim(), out data.isImageGenTrigger);
            }
            else
            {
                data.isImageGenTrigger = false;
            }

            if (columns.Length >= 26)
            {
                data.imageGenQuestID = columns[25].Trim();
            }
            else
            {
                data.imageGenQuestID = "";
            }

            if (columns.Length >= 27)
            {
                int.TryParse(columns[26].Trim(), out data.imageGenTruthDialogueID);
            }
            else
            {
                data.imageGenTruthDialogueID = 0;
            }

            if (columns.Length >= 28)
            {
                int.TryParse(columns[27].Trim(), out data.imageGenFalseDialogueID);
            }
            else
            {
                data.imageGenFalseDialogueID = 0;
            }

            if (columns.Length >= 29)
            {
                int.TryParse(columns[28].Trim(), out data.imageGenMalfunctionDialogueID);
            }
            else
            {
                data.imageGenMalfunctionDialogueID = 0;
            }

            // 💡 오작동 결과 시퀀스의 마지막 줄 표시 (30번째 컬럼)
            if (columns.Length >= 30)
            {
                bool.TryParse(columns[29].Trim(), out data.isImageGenMalfunctionEnd);
            }
            else
            {
                data.isImageGenMalfunctionEnd = false;
            }

            // 💡 [추가] 씬 전환 (31, 32번째 컬럼)
            if (columns.Length >= 31)
            {
                bool.TryParse(columns[30].Trim(), out data.isSceneTransition);
            }
            else
            {
                data.isSceneTransition = false;
            }

            if (columns.Length >= 32)
            {
                data.nextSceneName = columns[31].Trim();
            }
            else
            {
                data.nextSceneName = "";
            }

            // 💡 [추가] 타이핑 속도 (33번째 컬럼)
            if (columns.Length >= 33)
            {
                float.TryParse(columns[32].Trim(), out data.typingSpeed);
            }
            else
            {
                data.typingSpeed = 0f;
            }

            if (!dialogueDictionary.ContainsKey(data.id))
            {
                dialogueDictionary.Add(data.id, data);
            }
            else
            {
                dialogueDictionary[data.id] = data;
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
                WindowManager.Instance.PushWindowsLeft(WindowManager.Instance.GetChatPanelWidth(), tweenDuration); // 💡 추가
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
                WindowManager.Instance.PushWindowsLeft(WindowManager.Instance.GetChatPanelWidth(), tweenDuration); // 💡 추가
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

        if (windowManager != null)
        {
            windowManager.PullWindowsRight(windowManager.GetChatPanelWidth(), tweenDuration); // 💡 추가 (isChatOpen=false 처리도 이 안에서 함)
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
    /// 💡 특정 대화ID의 delayTime(그 줄이 화면에 머무는 시간)을 조회합니다.
    /// </summary>
    public float GetDialogueDelay(int id)
    {
        if (dialogueDictionary.TryGetValue(id, out var data))
        {
            return data.delayTime;
        }
        return 0f;
    }

    public void JumpToDialogue(int targetId)
    {
        if (!dialogueDictionary.ContainsKey(targetId))
        {
            Debug.LogWarning($"[ChatDialogueManager] 대화 ID {targetId} 를 CSV에서 찾을 수 없습니다.");
            return;
        }

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

        IsTriggerActive = false;

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
                    WindowManager.Instance.PushWindowsLeft(WindowManager.Instance.GetChatPanelWidth(), tweenDuration); // 💡 추가
                }
            }
        }

        isDialogueStarted = true;
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
            }

            // 💡 isTrigger 블록과 완전히 별개(형제 관계)로 분리.
            if (data.isImageGenTrigger)
            {
                IsDialoguePaused = true;

                // 💡 [디버그용] 이 분기까지 실제로 도달하는지, Instance가 살아있는지 확인
                Debug.Log($"[디버그] isImageGenTrigger 발동! questID:{data.imageGenQuestID}, ImageGenerationManager.Instance == null: {ImageGenerationManager.Instance == null}");

                if (ImageGenerationManager.Instance != null)
                {
                    ImageGenerationManager.Instance.UnlockAndOpen(
                        data.imageGenQuestID,
                        data.imageGenTruthDialogueID,
                        data.imageGenFalseDialogueID,
                        data.imageGenMalfunctionDialogueID
                    );
                }
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

                    if (controller != null)
                    {
                        while (!controller.IsTypingComplete)
                        {
                            if (chatScrollRect != null) chatScrollRect.verticalNormalizedPosition = 0f;
                            yield return null;
                        }
                    }
                }

                yield return new WaitForSeconds(data.delayTime);
            }

            // 💡 이 줄까지가 오작동 결과 시퀀스의 끝이라고 표시된 경우
            if (data.isImageGenMalfunctionEnd)
            {
                if (ImageGenerationManager.Instance != null)
                {
                    ImageGenerationManager.Instance.OnMalfunctionDialogueFinished();
                }
            }

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
                currentId = data.overrideNextId != 0 ? data.overrideNextId : currentId + 1;
            }

            if (data.isSceneTransition)
            {
                TriggerSceneTransition(data.nextSceneName);
                yield break;
            }
        }
    }

    private IEnumerator ResumeDialogueAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        IsDialoguePaused = false;
        IsTriggerActive = false;
    }

    // <summary>
    /// 💡 [추가] 대화 CSV에서 isSceneTransition=TRUE로 표시된 줄까지 재생을 마치면 호출됩니다.
    /// </summary>
    private void TriggerSceneTransition(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[ChatDialogueManager] isSceneTransition이 TRUE인데 nextSceneName이 비어있습니다! CSV를 확인하세요.");
            return;
        }

        Debug.Log($"[시스템] 대화 종료 -> 씬 전환: {sceneName}");
        SceneManager.LoadScene(sceneName);
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