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
            topBarToggle.isOn = true; // 버튼 클릭 트리거
        }

        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(GenerateChatWithExcelDelay());
    }

    IEnumerator GenerateChatWithExcelDelay()
    {
        foreach (DialogueData data in dialogueList)
        {
            GameObject selectedPrefab = (data.speakerType == "NPC") ? npcPrefab : userPrefab;
            if (selectedPrefab != null)
            {
                GameObject go = Instantiate(selectedPrefab, chatContent);
                ChatBubbleController controller = go.GetComponent<ChatBubbleController>();
                if (controller != null) controller.SetupBubble(data);
                yield return new WaitForSeconds(data.delayTime);
            }
        }
        yield break; // 코루틴 명확한 종료
    }
}