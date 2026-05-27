using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine.UI;

public class NewChatSystem : MonoBehaviour
{
    [System.Serializable]
    public class ChatEntity
    {
        public int id;
        public string context;
        public string sender;
        public string message;
        public string linkPanel;
        public float delay;
    }

    [System.Serializable]
    public class PanelMapping
    {
        public string panelName;
        public GameObject parentPanel;
        public GameObject panelObject;
        public GameObject entryButton;
    }

    [Header("UI 설정")]
    public GameObject popupChatPanel;
    public Transform chatContent;
    public GameObject npcMessagePrefab;
    public GameObject userMessagePrefab;

    [Header("UI 밀어올리기 설정")]
    public RectTransform scrollViewRect;
    [Tooltip("Selection_Panel이 켜질 때 위로 밀어올릴 세로 높이(크기)입니다.")]
    public float selectionPanelHeight = 220f;
    private Vector2 originalOffsetMin;

    [Header("최종 엔딩 선택지 오브젝트 (CSV 제어)")]
    public GameObject finalSelectionPanel;
    public Button finalButtonA;
    public Button finalButtonB;

    [Header("대화 종료 후 팝업 설정")]
    public GameObject updateNotificationPanel;
    public Button updatePanelButton;

    [Header("이동 가능한 패널 및 진입 버튼 세트")]
    public List<PanelMapping> moveablePanels = new List<PanelMapping>();
    private Dictionary<string, PanelMapping> panelDic = new Dictionary<string, PanelMapping>();

    [Header("상단 버튼 해금 설정")]
    public Button targetHeaderButton;
    public Image targetHeaderButtonImage;
    public Sprite lockedSprite;
    public Sprite unlockedSprite;

    [Header("순서대로 등록하는 퀘스트 버튼 리스트")]
    public List<QuestButtonRef> questButtonList = new List<QuestButtonRef>();

    private int currentPlayingQuestIndex = -1;

    [Header("해금된 단서 목록 (실시간 기록)")]
    public List<string> unlockedClues = new List<string>();

    [Header("[기획] 현재 단서 진행 단계")]
    public int currentClueLevel = 1;

    private List<ChatEntity> masterChatDataList = new List<ChatEntity>();
    private Dictionary<string, List<ChatEntity>> dialogueDic = new Dictionary<string, List<ChatEntity>>();
    private Queue<ChatEntity> currentDialogueQueue = new Queue<ChatEntity>();

    private Coroutine chatRoutineHandle = null;
    private readonly string csvParserPattern = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";
    private HashSet<string> startedQuestContexts = new HashSet<string>();

    private void Awake()
    {
        LoadDataFromCSV();
        InitDialogueDictionary();
        InitPanelDictionary();

        if (finalSelectionPanel != null) finalSelectionPanel.SetActive(false);
        if (updateNotificationPanel != null) updateNotificationPanel.SetActive(false);

        InitHeaderButton();
        InitAllQuestButtons();

        if (scrollViewRect != null)
        {
            originalOffsetMin = scrollViewRect.offsetMin;
        }
    }

    private void InitPanelDictionary()
    {
        panelDic.Clear();
        foreach (var mapping in moveablePanels)
        {
            if (!string.IsNullOrEmpty(mapping.panelName))
            {
                panelDic[mapping.panelName.Trim()] = mapping;
                if (mapping.panelObject != null) mapping.panelObject.SetActive(false);
                if (mapping.entryButton != null) mapping.entryButton.SetActive(false);
            }
        }
    }

    private void InitHeaderButton()
    {
        if (targetHeaderButton != null) targetHeaderButton.interactable = false;
        if (targetHeaderButtonImage != null && lockedSprite != null) targetHeaderButtonImage.sprite = lockedSprite;
    }

    private void InitAllQuestButtons()
    {
        for (int i = 0; i < questButtonList.Count; i++)
        {
            if (questButtonList[i] == null) continue;
            if (i == 0) questButtonList[i].SetActive();
            else questButtonList[i].SetLocked();
        }
    }

    public void OnClickQuestButton(string startContextName, QuestButtonRef clickedQuestButton)
    {
        if (popupChatPanel != null)
        {
            popupChatPanel.transform.SetAsLastSibling();
            currentPlayingQuestIndex = questButtonList.IndexOf(clickedQuestButton);

            if (startedQuestContexts.Contains(startContextName))
            {
                popupChatPanel.SetActive(true);
                return;
            }

            startedQuestContexts.Add(startContextName);
            popupChatPanel.SetActive(true);
        }

        PlayDialogueGroup(startContextName);
    }

    public void OnTextLinkClick(string linkId, string targetPanelName)
    {
        Match numMatch = Regex.Match(linkId, @"\d+");
        if (numMatch.Success)
        {
            int clickedClueNumber = int.Parse(numMatch.Value);
            if (clickedClueNumber < currentClueLevel) return;
        }
        bool isClueUnlocked = unlockedClues.Contains(linkId);
        if (isClueUnlocked) unlockedClues.Remove(linkId);

        string contextKey = "";
        if (linkId.StartsWith("q"))
        {
            string qNum = linkId.Substring(0, 2).ToUpper();
            contextKey = isClueUnlocked ? $"{qNum}_ClueClick" : $"{qNum}_NotFound";
        }
        if (!string.IsNullOrEmpty(contextKey) && dialogueDic.ContainsKey(contextKey)) PlayDialogueGroup(contextKey);
    }

    public void PlayDialogueGroup(string groupName)
    {
        if (!dialogueDic.ContainsKey(groupName)) return;
        currentDialogueQueue = new Queue<ChatEntity>(dialogueDic[groupName]);
        if (chatRoutineHandle != null) StopCoroutine(chatRoutineHandle);
        chatRoutineHandle = StartCoroutine(GenerateChatRoutine());
    }

    private IEnumerator GenerateChatRoutine()
    {
        while (currentDialogueQueue.Count > 0)
        {
            var entity = currentDialogueQueue.Dequeue();
            yield return new WaitForSeconds(entity.delay);

            GameObject prefab = (entity.sender == "USER" || entity.sender.Contains("USER")) ? userMessagePrefab : npcMessagePrefab;
            GameObject spawned = Instantiate(prefab, chatContent);
            spawned.GetComponentInChildren<TextMeshProUGUI>().text = entity.message;

            Canvas.ForceUpdateCanvases();
            if (chatContent.parent != null && chatContent.parent.parent != null)
            {
                ScrollRect scrollRect = chatContent.parent.parent.GetComponent<ScrollRect>();
                if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
            }

            if (!string.IsNullOrEmpty(entity.linkPanel))
            {
                string command = entity.linkPanel.Trim();

                if (command.StartsWith("Unlock:"))
                {
                    string clueIdToUnlock = command.Replace("Unlock:", "").Trim();
                    if (!unlockedClues.Contains(clueIdToUnlock)) unlockedClues.Add(clueIdToUnlock);
                }

                if (command == "Trigger_Selection") TriggerFinalSelection();

                if (command.StartsWith("Show_UpdatePanel"))
                {
                    // 1. 💡 [수정] 알림창 패널 활성화 및 '화면 최상단(맨 앞)' 강제 배치
                    if (updateNotificationPanel != null)
                    {
                        updateNotificationPanel.SetActive(true);
                        updateNotificationPanel.transform.SetAsLastSibling();
                    }

                    UnlockHeaderButton();

                    string targetPanelName = "";
                    if (command.Contains(":"))
                    {
                        targetPanelName = command.Split(':')[1].Trim();
                    }

                    // 2. 💡 [수정] 알림창 뜨는 이 타이밍에 즉시 기사 진입 버튼을 활성화하고 리스트 '최상단(위)'으로 정렬
                    if (!string.IsNullOrEmpty(targetPanelName) && panelDic.ContainsKey(targetPanelName))
                    {
                        var mapping = panelDic[targetPanelName];
                        if (mapping.entryButton != null)
                        {
                            mapping.entryButton.SetActive(true);
                            mapping.entryButton.transform.SetAsFirstSibling();
                            Debug.Log($"<color=cyan>[실시간 업데이트]</color> {targetPanelName} 진입 버튼이 목록 최상단에 배치되었습니다.");
                        }
                    }

                    if (updatePanelButton != null)
                    {
                        updatePanelButton.onClick.RemoveAllListeners();
                        updatePanelButton.onClick.AddListener(() => {

                            updateNotificationPanel.SetActive(false);
                            if (popupChatPanel != null) popupChatPanel.SetActive(false);

                            if (!string.IsNullOrEmpty(targetPanelName))
                            {
                                MoveToTargetPanel(targetPanelName);
                            }
                        });
                    }

                    if (currentPlayingQuestIndex != -1 && currentPlayingQuestIndex < questButtonList.Count)
                    {
                        questButtonList[currentPlayingQuestIndex].gameObject.SetActive(false);
                        int nextQuestIndex = currentPlayingQuestIndex + 1;
                        if (nextQuestIndex < questButtonList.Count && questButtonList[nextQuestIndex] != null)
                        {
                            questButtonList[nextQuestIndex].SetActive();
                        }
                    }
                }

                if (command == "Next_Clue")
                {
                    string currentContext = entity.context;
                    string nextGroup = DetermineNextContext(currentContext);
                    PlayDialogueGroup(nextGroup);
                    yield break;
                }
            }
        }
        chatRoutineHandle = null;
    }

    private void MoveToTargetPanel(string panelName)
    {
        if (panelDic.ContainsKey(panelName))
        {
            var mapping = panelDic[panelName];

            if (mapping.panelObject != null)
            {
                Transform currentParent = mapping.panelObject.transform.parent;
                while (currentParent != null && currentParent.GetComponent<Canvas>() == null)
                {
                    currentParent.gameObject.SetActive(true);
                    currentParent.SetAsLastSibling();
                    currentParent = currentParent.parent;
                }
            }

            if (mapping.parentPanel != null)
            {
                mapping.parentPanel.SetActive(true);
                mapping.parentPanel.transform.SetAsLastSibling();
            }

            if (mapping.panelObject != null)
            {
                mapping.panelObject.SetActive(true);
                mapping.panelObject.transform.SetAsLastSibling();
            }

            Debug.Log($"<color=lime>[화면 이동 완료]</color> {panelName} 기사 패널을 열었습니다.");
        }
    }

    private void UnlockHeaderButton() { if (targetHeaderButton != null) targetHeaderButton.interactable = true; if (targetHeaderButtonImage != null && unlockedSprite != null) targetHeaderButtonImage.sprite = unlockedSprite; }
    private string DetermineNextContext(string currentContext) { Match match = Regex.Match(currentContext, @"\d+"); if (match.Success) { int currentNum = int.Parse(match.Value); int nextNum = currentNum + 1; currentClueLevel = nextNum; string nextContextName = currentContext.Replace(currentNum.ToString(), nextNum.ToString()); if (nextContextName.Contains("_ClueClick")) nextContextName = nextContextName.Replace("_ClueClick", "_Start"); return nextContextName; } return "Q1_Start"; }

    private void TriggerFinalSelection()
    {
        if (finalSelectionPanel != null)
        {
            finalSelectionPanel.SetActive(true);

            // 💡 [수정] 스크롤 뷰의 하단 단을 들어 올려 틀 전체(스크롤바, 화살표 포함)를 위로 밀어냅니다.
            if (scrollViewRect != null)
            {
                scrollViewRect.offsetMin = new Vector2(originalOffsetMin.x, originalOffsetMin.y + selectionPanelHeight);
            }

            finalButtonA.onClick.RemoveAllListeners();
            finalButtonA.onClick.AddListener(() => OnClickFinalChoice("A"));
            finalButtonB.onClick.RemoveAllListeners();
            finalButtonB.onClick.AddListener(() => OnClickFinalChoice("B"));

            StartCoroutine(ForceScrollBottomRoutine());
        }
    }

    private void OnClickFinalChoice(string choiceType)
    {
        if (finalSelectionPanel != null) finalSelectionPanel.SetActive(false);

        // 💡 [수정] 선택지가 끝나면 하단 단 마진을 원래 크기로 원상복구 시킵니다.
        if (scrollViewRect != null)
        {
            scrollViewRect.offsetMin = originalOffsetMin;
        }

        if (choiceType == "A") PlayDialogueGroup("Logical_Btn");
        else if (choiceType == "B") PlayDialogueGroup("Ethical_Btn");
    }

    private IEnumerator ForceScrollBottomRoutine()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        if (chatContent != null && chatContent.parent != null && chatContent.parent.parent != null)
        {
            ScrollRect scrollRect = chatContent.parent.parent.GetComponent<ScrollRect>();
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void LoadDataFromCSV() { TextAsset csvFile = Resources.Load<TextAsset>("PopupChatData"); if (csvFile == null) return; masterChatDataList.Clear(); string[] lines = csvFile.text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries); for (int i = 1; i < lines.Length; i++) { if (string.IsNullOrWhiteSpace(lines[i])) continue; string[] row = Regex.Split(lines[i], csvParserPattern); if (row.Length < 2 || string.IsNullOrEmpty(row[0])) continue; string cleanId = row[0].Replace("\"", "").Trim(); string cleanContext = row[1].Replace("\"", "").Trim(); string cleanSender = (row.Length > 2) ? row[2].Replace("\"", "").Trim() : ""; string cleanMessage = (row.Length > 3) ? row[3].Replace("\"", "").Trim() : ""; string cleanLinkPanel = (row.Length > 4) ? row[4].Replace("\"", "").Trim() : ""; float parsedDelay = 1.0f; if (row.Length >= 6 && !string.IsNullOrWhiteSpace(row[5])) { string cleanDelay = row[5].Replace("\"", "").Trim(); float.TryParse(cleanDelay, out parsedDelay); } if (int.TryParse(cleanId, out int idResult)) { masterChatDataList.Add(new ChatEntity { id = idResult, context = cleanContext, sender = cleanSender, message = cleanMessage, linkPanel = cleanLinkPanel, delay = parsedDelay <= 0 ? 0.1f : parsedDelay }); } } }
    private void InitDialogueDictionary() { if (masterChatDataList == null || masterChatDataList.Count == 0) return; dialogueDic = masterChatDataList.GroupBy(e => e.context).ToDictionary(g => g.Key, g => g.OrderBy(e => e.id).ToList()); }
}