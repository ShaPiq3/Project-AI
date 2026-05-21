using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class NewChatSystem : MonoBehaviour
{
    [Header("UI 설정")]
    public GameObject popupChatPanel;
    public Transform chatContent;
    public GameObject npcMessagePrefab;
    public GameObject userMessagePrefab;

    [Header("연동 패널")]
    public GameObject dbWikiClue1Panel;

    private PopupChatData chatData;
    private Dictionary<string, List<PopupChatData.Entity>> dialogueDic = new Dictionary<string, List<PopupChatData.Entity>>();
    private Queue<PopupChatData.Entity> currentDialogueQueue = new Queue<PopupChatData.Entity>();

    private void Awake()
    {
        LoadDataFromCSV();
        InitDialogueDictionary();
    }

    private void LoadDataFromCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("PopupChatData");
        if (csvFile == null) return;

        chatData = ScriptableObject.CreateInstance<PopupChatData>();
        chatData.Entities = new List<PopupChatData.Entity>();

        string[] lines = csvFile.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] row = lines[i].Split(',');
            if (row.Length < 5) continue;

            chatData.Entities.Add(new PopupChatData.Entity
            {
                id = int.Parse(row[0]),
                context = row[1].Trim(),
                sender = row[2].Trim(),
                message = row[3].Trim(),
                linkPanel = row[4].Trim()
            });
        }
    }

    private void InitDialogueDictionary()
    {
        if (chatData?.Entities == null) return;
        dialogueDic = chatData.Entities.GroupBy(e => e.context)
                                      .ToDictionary(g => g.Key, g => g.OrderBy(e => e.id).ToList());
    }

    // 버튼 클릭 시 실행
    public void OnClickQ1Button()
    {
        if (popupChatPanel != null) popupChatPanel.SetActive(true);
        PlayDialogueGroup("Q1_Start");
    }

    // 텍스트 링크 클릭 시 실행
    public void OnTextLinkClick(string linkId, string targetPanelName)
    {
        if (linkId == "q1_trigger") PlayDialogueGroup("Q1_ClueClick");
    }

    public void PlayDialogueGroup(string groupName)
    {
        if (!dialogueDic.ContainsKey(groupName)) return;
        currentDialogueQueue = new Queue<PopupChatData.Entity>(dialogueDic[groupName]);
        StopAllCoroutines();
        StartCoroutine(GenerateChatRoutine());
    }

    private IEnumerator GenerateChatRoutine()
    {
        while (currentDialogueQueue.Count > 0)
        {
            yield return new WaitForSeconds(1.0f);
            var entity = currentDialogueQueue.Dequeue();

            GameObject prefab = (entity.sender == "USER") ? userMessagePrefab : npcMessagePrefab;
            GameObject spawned = Instantiate(prefab, chatContent);
            spawned.GetComponentInChildren<TextMeshProUGUI>().text = entity.message;

            if (entity.linkPanel == "DB_WIKI_Clue_1" && dbWikiClue1Panel != null)
            {
                dbWikiClue1Panel.SetActive(true);
                yield break;
            }
        }
    }
}