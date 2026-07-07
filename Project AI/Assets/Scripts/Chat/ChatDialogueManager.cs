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
            bool isUser = (data.speakerType == "USER"); // USER면 왼쪽, 아니면 오른쪽
            GameObject selectedPrefab = isUser ? userPrefab : npcPrefab;

            if (selectedPrefab != null)
            {
                GameObject go = Instantiate(selectedPrefab, chatContent);

                // --- 위치 정렬 로직 추가 시작 ---
                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    if (isUser) // USER: 왼쪽 정렬
                    {
                        rt.anchorMin = new Vector2(0, 0.5f);
                        rt.anchorMax = new Vector2(0, 0.5f);
                        rt.pivot = new Vector2(0, 0.5f);
                    }
                    else // NPC: 오른쪽 정렬
                    {
                        rt.anchorMin = new Vector2(1, 0.5f);
                        rt.anchorMax = new Vector2(1, 0.5f);
                        rt.pivot = new Vector2(1, 0.5f);
                    }
                    rt.anchoredPosition = Vector2.zero;
                }
                // --- 위치 정렬 로직 추가 끝 ---

                ChatBubbleController controller = go.GetComponent<ChatBubbleController>();
                if (controller != null) controller.SetupBubble(data);

                // 레이아웃이 꼬이는 것을 방지하기 위해 매번 갱신 요청
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());

                yield return new WaitForSeconds(data.delayTime);
            }
        }
        yield break;
    }
}