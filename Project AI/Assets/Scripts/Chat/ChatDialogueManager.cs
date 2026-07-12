using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ChatDialogueManager : MonoBehaviour
{
    public TextAsset csvFile;
    public GameObject npcPrefab;
    public GameObject userPrefab;
    public Transform chatContent;

    [Header("스크롤 설정")]
    public ScrollRect chatScrollRect;

    [Header("상단바 토글 버튼 (Toggle 컴포넌트 필수)")]
    public Toggle topBarToggle;

    [Header("대화창 애니메이션 설정 (화면 밖 -> 안 구조)")]
    [SerializeField] private RectTransform dialoguePanelRect;
    [SerializeField] private float tweenDuration = 0.5f;

    // 대화창이 화면 안으로 들어왔을 때 안착할 최종 X 좌표 (기본값 0)
    [SerializeField] private float targetPositionX = 0f;

    // [오류 해결] 누락되었던 대화 데이터 리스트 변수를 상단에 추가했습니다! ⭐
    private List<DialogueData> dialogueList = new List<DialogueData>();

    // 시작할 때 화면 밖에 배치된 그 위치를 기억할 변수
    private Vector2 hidePosition;

    // 상태 체크 변수들
    private bool isChatWindowOpened = false;
    private bool isTimerFinished = false;
    private bool isDialogueStarted = false;

    void Start()
    {
        ParseCSV();

        if (dialoguePanelRect != null)
        {
            // 시작 시점에 화면 밖에 배치되어 있는 그 좌표를 '숨김 위치'로 기억합니다.
            hidePosition = dialoguePanelRect.anchoredPosition;

            // 대화창 오브젝트 자체는 코루틴이 돌아야 하므로 확실하게 켜둡니다.
            dialoguePanelRect.gameObject.SetActive(true);
        }

        if (topBarToggle != null) topBarToggle.gameObject.SetActive(false);

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

            if (columns.Length < 7) continue;
            DialogueData data = new DialogueData();
            data.id = int.Parse(columns[0].Trim());
            data.speakerType = columns[1].Trim();
            data.speakerName = columns[2].Trim();
            data.dialogueText = columns[3].Trim().Replace("\"", "");
            data.hasImage = bool.Parse(columns[4].Trim().ToUpper());
            data.imagePath = columns[5].Trim();
            data.delayTime = float.Parse(columns[6].Trim());
            dialogueList.Add(data);
        }
    }

    IEnumerator StartAbsoluteTimer()
    {
        // 정확히 3초 동안 대기
        yield return new WaitForSeconds(3f);
        isTimerFinished = true;

        // 3초가 되었을 때 플레이어가 아직 수동으로 버튼을 안 눌렀다면 자동으로 창을 안으로 들여옵니다.
        if (!isChatWindowOpened)
        {
            TriggerOpenChat();
        }
        else
        {
            // 이미 버튼을 눌러서 창을 열어둔 상태라면 대화 내용만 즉시 시작합니다.
            TryStartDialogue();
        }
    }

    // 버튼을 누르거나, 3초 뒤 자동으로 나타날 때 호출되는 함수
    public void TriggerOpenChat()
    {
        if (isChatWindowOpened) return;
        isChatWindowOpened = true;

        if (dialoguePanelRect != null)
        {
            dialoguePanelRect.gameObject.SetActive(true);

            // 화면 밖에 있던 창을 지정해 둔 '화면 안의 좌표(targetPositionX)'로 스르륵 이동시킵니다!
            dialoguePanelRect.DOKill();
            dialoguePanelRect.DOAnchorPosX(targetPositionX, tweenDuration).SetEase(Ease.OutQuad);
        }

        if (topBarToggle != null)
        {
            topBarToggle.gameObject.SetActive(true);
            topBarToggle.isOn = true;
        }

        // 3초 타이머가 끝난 상태에서 열린 거라면 대화 생성 시작
        if (isTimerFinished)
        {
            TryStartDialogue();
        }
    }

    private void TryStartDialogue()
    {
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