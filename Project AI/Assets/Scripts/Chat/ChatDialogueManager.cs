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

    private List<DialogueData> dialogueList = new List<DialogueData>();

    // 상태 체크 변수들
    private bool isChatWindowOpened = false;
    private bool isTimerFinished = false;
    private bool isDialogueStarted = false;
    private bool isClosedByPlayer = false;

    private CanvasGroup dialogueCanvasGroup;

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

        // 3초 절대 타이머 시작
        StartCoroutine(StartAbsoluteTimer());
    }

    void ParseCSV()
    {
        if (csvFile == null) return;
        string[] rows = csvFile.text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = rows[i].Split(',');

            if (columns.Length < 8) continue;
            DialogueData data = new DialogueData();
            data.id = int.Parse(columns[0].Trim());
            data.speakerType = columns[1].Trim();
            data.speakerName = columns[2].Trim();
            data.dialogueText = columns[3].Trim().Replace("\"", "");
            data.hasImage = bool.Parse(columns[4].Trim().ToUpper());
            data.imagePath = columns[5].Trim();
            data.delayTime = float.Parse(columns[6].Trim());
            data.ipAddress = columns[7].Trim();
            
            dialogueList.Add(data);

        }
    }

    IEnumerator StartAbsoluteTimer()
    {
        yield return new WaitForSeconds(3f);
        isTimerFinished = true;

        if (isClosedByPlayer || isChatWindowOpened) yield break;

        TriggerOpenChat();
    }

    // 시스템에 의해 자동으로 창이 열릴 때
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

    // 플레이어가 다시 켜기 버튼을 눌렀을 때
    public void OpenChatWindowByPlayer()
    {
        isClosedByPlayer = false; // ⭐ 일시정지 해제!
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

        // 대화 코루틴이 이미 실행 중일 테니 중복 실행하지 않고 냅둡니다.
        TryStartDialogue();
    }

    // 우측 퇴장 버튼을 누르면 호출되는 함수
    public void CloseChatWindow()
    {
        if (dialoguePanelRect == null) return;

        isClosedByPlayer = true; // ⭐ 일시정지 활성화!
        isChatWindowOpened = false;

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
        // 이제 닫혀있어도 이미 시작된 적이 있다면 코루틴을 중복 생성하지 않습니다.
        if (isDialogueStarted) return;
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
        foreach (DialogueData data in dialogueList)
        {
            // ⭐ [핵심 추가] 플레이어가 수동으로 창을 닫았다면, 다시 열릴 때까지 이 자리에서 멈춰 대기합니다!
            while (isClosedByPlayer)
            {
                yield return null; // 다음 프레임까지 대기 (무한 루프 방지 및 일시정지 구현)
            }

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

                yield return new WaitForSeconds(data.delayTime);
            }
        }
    }
}