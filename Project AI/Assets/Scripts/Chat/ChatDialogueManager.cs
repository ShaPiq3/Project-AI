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

    // ChatDialogueManager.cs의 CloseChatWindow() 함수를 아래처럼 수정하세요.

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

        // 💡 [추가] 채팅창이 실제로 닫힐 때 WindowManager의 벽(오른쪽 제한)도 함께 해제
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

    IEnumerator StartChatGenerationWithDelay()
    {
        yield return new WaitForSeconds(tweenDuration + 0.1f);
        yield return StartCoroutine(GenerateChatWithExcelDelay());
    }

    IEnumerator GenerateChatWithExcelDelay()
    {
        int currentId = 1;

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
            // ChatDialogueManager.cs의 223번 줄 근처
            if (data.isTrigger)
            {
                IsTriggerActive = true;
                IsDialoguePaused = true;

                if (DataLogManager.Instance != null)
                {
                    // 💡 하드코딩된 ID 대신, 파싱해온 data.questID를 사용합니다.
                    string targetQuestID = data.questID;

                    Debug.Log($"퀘스트 시작! 매니저로 전달할 ID: {targetQuestID}");

                    // 여기에 정확한 ID가 들어가는지 콘솔을 통해 확인하세요!
                    DataLogManager.Instance.StartQuest(data.questID, data.targetCount);

                    if (DataLogManager.Instance.questStatusUI != null)
                    {
                        DataLogManager.Instance.questStatusUI.UpdateDisplay();
                    }

                    if (!DataLogManager.Instance.IsOpen)
                    {
                        DataLogManager.Instance.ToggleLogPanel();
                    }
                }
                StartCoroutine(ResumeDialogueAfterDelay(2.5f));
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
                currentId++;
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