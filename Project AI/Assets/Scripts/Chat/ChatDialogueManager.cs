using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ChatDialogueManager : MonoBehaviour
{
    public TextAsset csvFile;
    public GameObject npcPrefab;
    public GameObject userPrefab;
    public Transform chatContent;
    public TMP_Text topIPText;

    [Header("스크롤 설정")]
    public ScrollRect chatScrollRect;

    [Header("버튼 References (WindowManager 기능 통합)")]
    public Button closeButton; // 창 끄기 (Hide)
    public Button showButton;  // 다시 켜기 (Show)

    [Header("대화창 애니메이션 설정 (화면 밖 -> 안 구조)")]
    [SerializeField] private RectTransform dialoguePanelRect;
    [SerializeField] private float tweenDuration = 0.5f;

    // 대화창이 화면 안으로 들어왔을 때 안착할 최종 X 좌표 (기본값 0)
    [SerializeField] private float targetPositionX = 0f;

    // 시작할 때 또는 숨길 때 나갈 오른쪽 화면 밖 X 좌표 (예: 500 또는 800)
    [SerializeField] private float hidePositionX = 600f;

    // --- 선택지 동적 생성용 프리팹 설정 ---
    [Header("선택지 프리팹 설정")]
    public GameObject branchGroupPrefab; // Project 뷰에 있는 선택지 패널 프리팹 할당

    // 순차 리스트 대신 ID 분기(Jump) 탐색을 위해 딕셔너리로 대화 데이터를 관리합니다.
    private Dictionary<int, DialogueData> dialogueDictionary = new Dictionary<int, DialogueData>();

    // 상태 체크 변수들
    private bool isChatWindowOpened = false;
    private bool isTimerFinished = false;
    private bool isDialogueStarted = false;
    private bool isClosedByPlayer = false;

    // --- 동적 선택지 인스턴스 제어용 변수 ---
    private GameObject activeBranchInstance; // 현재 대화창에 생성된 선택지 오브젝트
    private Button[] activeBranchButtons;     // 생성된 오브젝트 내부에 바인딩할 버튼 배열
    private bool isWaitingForBranchSelection = false;
    private int selectedNextId = -1;
    private string selectedUserText = "";

    private CanvasGroup dialogueCanvasGroup;

    void Start()
    {
        // 1. CSV 파싱 및 딕셔너리 구축
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

        // 3초 절대 타이머 시작
        StartCoroutine(StartAbsoluteTimer());
    }

    void ParseCSV()
    {
        if (csvFile == null) return;

        // 줄바꿈 단위로 파싱 (맥/윈도우 호환을 위해 \n과 \r 제거 고려)
        string[] rows = csvFile.text.Replace("\r", "").Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = rows[i].Split(',');

            // 새로 확장된 CSV는 최소 8개 이상의 열을 가집니다.
            if (columns.Length < 8) continue;

            DialogueData data = new DialogueData();

            // TryParse를 사용하여 파싱 에러(FormatException)를 원천 차단합니다.
            int.TryParse(columns[0].Trim(), out data.id);
            data.speakerType = columns[1].Trim();
            data.speakerName = columns[2].Trim();
            data.dialogueText = columns[3].Trim().Replace("\"", "");

            bool.TryParse(columns[4].Trim(), out data.hasImage);
            data.imagePath = columns[5].Trim();
            float.TryParse(columns[6].Trim(), out data.delayTime);
            data.ipAddress = columns[7].Trim();

            // --- 선택지 및 분기 데이터 파싱 ---
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

            if (!dialogueDictionary.ContainsKey(data.id))
            {
                dialogueDictionary.Add(data.id, data);
            }
            else
            {
                Debug.LogWarning($"중복된 ID 발견: {data.id}. 덮어씁니다.");
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
        if (isClosedByPlayer) return;
        if (isChatWindowOpened) return;
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
        }

        if (isTimerFinished)
        {
            TryStartDialogue();
        }
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
        }

        TryStartDialogue();
    }

    public void CloseChatWindow()
    {
        if (dialoguePanelRect == null) return;

        isClosedByPlayer = true;
        isChatWindowOpened = false;

        // 파괴되지 않고 유효한 객체일 때만 SetActive 제어
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
            // 복구 시 MissingReferenceException 방지 필터링
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
            while (isClosedByPlayer)
            {
                yield return null;
            }

            DialogueData data = dialogueDictionary[currentId];

            if (topIPText != null)
            {
                if (!string.IsNullOrEmpty(data.ipAddress))
                {
                    topIPText.text = $"IP : {data.ipAddress}";
                }
                else
                {
                    topIPText.text = "IP : -";
                }
            }

            // 만약 현재 ID 대사가 '선택지 분기 전용 껍데기 대사'가 아니고 출력할 내용이 있다면 말풍선 생성
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

                    if (chatScrollRect != null)
                    {
                        chatScrollRect.verticalNormalizedPosition = 0f;
                    }
                }

                // 대사가 출력되었다면 기획된 딜레이만큼 대기
                yield return new WaitForSeconds(data.delayTime);
            }

            // --- 선택지 분기 판정 로직 ---
            if (data.isBranch)
            {
                ShowBranchUI(data);

                isWaitingForBranchSelection = true;
                while (isWaitingForBranchSelection)
                {
                    yield return null;
                }

                // 플레이어가 선택지를 누르면, 플레이어가 고른 대사를 채팅창에 말풍선으로 직접 띄워줍니다.
                if (userPrefab != null && !string.IsNullOrEmpty(selectedUserText))
                {
                    GameObject userSpeechGo = Instantiate(userPrefab, chatContent);
                    ChatBubbleController controller = userSpeechGo.GetComponent<ChatBubbleController>();
                    if (controller != null)
                    {
                        DialogueData userSelectionData = new DialogueData();
                        userSelectionData.speakerType = "USER";
                        userSelectionData.speakerName = "AI assistant"; // "USER" 대신 화자명 매핑
                        userSelectionData.dialogueText = selectedUserText;
                        userSelectionData.hasImage = false;
                        controller.SetupBubble(userSelectionData);
                    }

                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());
                    if (chatScrollRect != null)
                    {
                        chatScrollRect.verticalNormalizedPosition = 0f;
                    }

                    // 전송 연출 딜레이
                    yield return new WaitForSeconds(0.8f);
                }

                // 선택된 분기 ID로 다이렉트 점프합니다.
                currentId = selectedNextId;
            }
            else
            {
                // 일반 대사 흐름일 때만 루프 ID 1 증가
                currentId++;
            }
        }
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
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
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
            if (buttonText != null)
            {
                buttonText.text = text;
            }

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