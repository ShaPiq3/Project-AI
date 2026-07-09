using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



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

    private List<DialogueData> dialogueList = new List<DialogueData>();



    void Start()
    {
        ParseCSV();
        if (topBarToggle != null) topBarToggle.gameObject.SetActive(false);
        StartCoroutine(StartTestRoutine());
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



    IEnumerator StartTestRoutine()
    {
        yield return new WaitForSeconds(5f);
        if (topBarToggle != null)
        {
            topBarToggle.gameObject.SetActive(true);
            topBarToggle.isOn = true;
        }
        yield return new WaitForSeconds(0.5f);
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
                // [중요] targetParent를 container 분리 없이 chatContent로 통일합니다.
                // 모든 대화가 하나의 줄에 시간순으로 쌓입니다.
                GameObject go = Instantiate(selectedPrefab, chatContent);

                // 말풍선 데이터 셋업
                ChatBubbleController controller = go.GetComponent<ChatBubbleController>();
                if (controller != null) controller.SetupBubble(data);

                // [핵심] 전체 레이아웃 갱신
                // 1. 말풍선 자체가 스스로 크기를 잡게 둡니다 (Content Size Fitter 활용)
                // 2. 전체 부모만 갱신하면 순서대로 아래로 쌓입니다.
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

