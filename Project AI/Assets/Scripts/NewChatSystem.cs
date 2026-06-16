using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static PopupChatData;

public class NewChatSystem : MonoBehaviour
{
    // 완료된 단서들을 기억하는 변수 (이건 창을 닫아도 데이터가 유지되어야 함)
    public HashSet<string> completedClues = new HashSet<string>();

    [System.Serializable]
    public class ChatEntity { public int id; public string context; public string sender; public string message; public string linkPanel; public float delay; public string imagePath; public string triggerAction; }
    [System.Serializable]
    public class PanelMapping { public string panelName; public GameObject parentPanel; public GameObject panelObject; public GameObject entryButton; }

    [Header("UI 설정")]
    public GameObject popupChatPanel;
    public Transform chatContent;
    public GameObject npcMessagePrefab;
    public GameObject userMessagePrefab;

    [Header("효과음 설정")]
    public AudioSource uiAudioSource;
    public AudioClip popupAppearSound;
    public AudioClip messageAppearSound;
    public AudioClip buttonClickSound;

    [Header("이미지 교체 설정")]
    public Image targetButtonImage;
    public Sprite replacedSprite;

    [Header("UI 밀어올리기 설정")]
    public RectTransform scrollViewRect;
    public float selectionPanelHeight = 220f;
    private Vector2 originalOffsetMin;

    [Header("최종 엔딩 선택지")]
    public GameObject finalSelectionPanel;
    public Button finalButtonA, finalButtonB;
    public Image finalButtonImageA, finalButtonImageB;
    public Sprite selectionLockedSpriteA, selectionUnlockedSpriteA;
    public Sprite selectionLockedSpriteB, selectionUnlockedSpriteB;

    [Header("대화 종료 후 팝업")]
    public GameObject updateNotificationPanel;
    public Button updatePanelButton;

    [Header("미니게임 버튼")]
    public Button ImageButton;
    public Image ImageButtonImage;

    public Sprite CloseSprite;
    public Sprite OpenSprite;


    [Header("패널 및 퀘스트 설정")]
    public List<PanelMapping> moveablePanels = new List<PanelMapping>();
    private Dictionary<string, PanelMapping> panelDic = new Dictionary<string, PanelMapping>();
    public Button targetHeaderButton;
    public Image targetHeaderButtonImage;
    public Sprite lockedSprite, unlockedSprite;
    public List<QuestButtonRef> questButtonList = new List<QuestButtonRef>();
    private int currentPlayingQuestIndex = -1;
    public List<string> unlockedClues = new List<string>();
    public int currentClueLevel = 1;

    private GameObject currentActivePrefab;
    private List<ChatEntity> masterChatDataList = new List<ChatEntity>();
    private Dictionary<string, List<ChatEntity>> dialogueDic = new Dictionary<string, List<ChatEntity>>();
    private Queue<ChatEntity> currentDialogueQueue = new Queue<ChatEntity>();
    private Coroutine chatRoutineHandle = null;
    private readonly string csvParserPattern = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";
    private HashSet<string> startedQuestContexts = new HashSet<string>();
    public GameObject Setting_Panel;
    public GameObject imageGame;

    public void OpenSettingsPanel()
    {
        Setting_Panel.SetActive(true);
        Setting_Panel.transform.SetAsLastSibling();
    }

    public void OpenTargetPanel(string panelName)
    {
        MoveToTargetPanel(panelName);
    }

    public void CloseImageButton()
    {
        ImageButton.interactable = false;
        ImageButtonImage.sprite = CloseSprite;
    }

    public void OnClickTriggerButton(string triggerId)
    {
        OnTextLinkClick(triggerId, "");
    }

    private void Awake()
    {
        CloseImageButton();
        LoadDataFromCSV();
        InitDialogueDictionary();
        InitPanelDictionary();
        if (updateNotificationPanel != null) updateNotificationPanel.SetActive(false);
        LockFinalSelectionPanel();
        InitHeaderButton();
        InitAllQuestButtons();
        if (scrollViewRect != null)
        {
            originalOffsetMin = scrollViewRect.offsetMin;
            scrollViewRect.offsetMin = new Vector2(originalOffsetMin.x, originalOffsetMin.y + selectionPanelHeight);
        }
    }

    private bool isDialoguePaused = false;

    private void Start()
    {
        ImageButton.onClick.AddListener(OpenImagePanel);
        RegisterSoundToAllButtons();
        StartCoroutine(AppearFirstQuestButtonAfter5Seconds());
    }

    private void OpenImageButton()
    {
        ImageButton.interactable = true;
        ImageButtonImage.sprite = OpenSprite;
        Debug.Log("[시스템] ImageButton 해금");
    }

    private void OpenImagePanel()
    {
        Debug.Log("이미지 게임 열기");
        MoveToTargetPanel("imageGame_Panel");
    }

    private bool isSkipping = false;

    private void Update()
    {
        if (popupChatPanel != null && popupChatPanel.activeSelf)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (IsPointerOverUI(chatContent.gameObject))
                {
                    isSkipping = true;
                }
            }
        }
    }

    private bool IsPointerOverUI(GameObject targetObject)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.transform.IsChildOf(targetObject.transform) || result.gameObject == targetObject)
            {
                return true;
            }
        }
        return false;
    }

    public void RegisterSoundToAllButtons()
    {
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in allButtons)
        {
            btn.onClick.RemoveListener(() => uiAudioSource.PlayOneShot(buttonClickSound));
            btn.onClick.AddListener(() => uiAudioSource.PlayOneShot(buttonClickSound));
        }
    }

    public void PlayPopupSound()
    {
        if (uiAudioSource != null && popupAppearSound != null) uiAudioSource.PlayOneShot(popupAppearSound);
    }

    private void LockFinalSelectionPanel()
    {
        if (finalSelectionPanel != null)
        {
            finalSelectionPanel.SetActive(true);
            if (finalButtonA != null) finalButtonA.interactable = false;
            if (finalButtonB != null) finalButtonB.interactable = false;
            if (finalButtonImageA != null && selectionLockedSpriteA != null) finalButtonImageA.sprite = selectionLockedSpriteA;
            if (finalButtonImageB != null && selectionLockedSpriteB != null) finalButtonImageB.sprite = selectionLockedSpriteB;
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
        foreach (var btn in questButtonList) { if (btn != null) { btn.gameObject.SetActive(false); btn.SetLocked(); } }
    }

    private IEnumerator AppearFirstQuestButtonAfter5Seconds()
    {
        yield return new WaitForSeconds(3.0f);
        if (questButtonList.Count > 0 && questButtonList[0] != null)
        {
            questButtonList[0].gameObject.SetActive(true);
            questButtonList[0].SetActive();
            PlayPopupSound();
        }
    }

    public void OnTextLinkClick(string linkId, string targetPanelName)
    {
        EventSystem.current.SetSelectedGameObject(null);
        Match numMatch = Regex.Match(linkId, @"\d+");
        if (numMatch.Success)
        {
            int clickedClueNumber = int.Parse(numMatch.Value);
            if (clickedClueNumber < currentClueLevel) return;
        }

        if (linkId == "OpenImageButton")
        {
            Debug.Log("이미지게임 버튼 활성화");
            OpenImageButton();
            return;
        }

        if (linkId == "q1_trigger")
        {
            Debug.Log("[시스템] Q2 단서 트리거 클릭됨! Q1_ClueClick 대화 시작.");
            PlayDialogueGroup("Q1_ClueClick");
            return;
        }

        if (linkId == "q2_trigger")
        {
            Debug.Log("[시스템] Q2 단서 트리거 클릭됨! Q2_ClueClick 대화 시작.");
            PlayDialogueGroup("Q2_ClueClick");
            return;
        }

        if (linkId == "q3_trigger")
        {
            Debug.Log("[시스템] Q3 단서 트리거 클릭됨! Q3_ClueClick 대화 시작.");
            PlayDialogueGroup("Q3_ClueClick");
            return;
        }

        if (linkId == "q4_trigger")
        {
            Debug.Log("[시스템] Q4 단서 트리거 클릭됨! Q4_ClueClick 대화 시작.");
            PlayDialogueGroup("Q4_ClueClick");
            return;
        }

        if (completedClues.Contains(linkId))
        {
            Debug.Log($"[시스템] 이미 완료된 단서입니다: {linkId}");
            return;
        }

        bool isClueUnlocked = unlockedClues.Contains(linkId);
        string contextKey = "";
        if (linkId.StartsWith("q"))
        {
            string qNum = linkId.Substring(0, 2).ToUpper();
            contextKey = isClueUnlocked ? $"{qNum}_ClueClick" : $"{qNum}_NotFound";
        }

        if (!string.IsNullOrEmpty(contextKey) && dialogueDic.ContainsKey(contextKey))
        {
            PlayDialogueGroup(contextKey);
        }
    }

    public void OnClickQuestButton(string startContextName)
    {
        Debug.Log($"[디버그] 버튼 눌림! 전달받은 context: '{startContextName}'");
        var questBtn = GetComponent<QuestButtonRef>();

        if (currentDialogueQueue.Count > 0)
        {
            if (popupChatPanel != null)
            {
                popupChatPanel.SetActive(true);
                popupChatPanel.transform.SetAsLastSibling();
            }

            if (chatRoutineHandle == null)
            {
                chatRoutineHandle = StartCoroutine(GenerateChatRoutine());
            }
            return;
        }

        if (popupChatPanel != null)
        {
            popupChatPanel.SetActive(true);
            popupChatPanel.transform.SetAsLastSibling();
            if (questBtn != null) currentPlayingQuestIndex = questButtonList.IndexOf(questBtn);
            if (!startedQuestContexts.Contains(startContextName)) startedQuestContexts.Add(startContextName);
        }

        PlayDialogueGroup(startContextName);
    }

    public void CloseChatPopup()
    {
        if (chatRoutineHandle != null)
        {
            StopCoroutine(chatRoutineHandle);
            chatRoutineHandle = null;
        }
        popupChatPanel.SetActive(false);
    }

    public void OpenChatPopup(string contextName = "")
    {
        if (popupChatPanel.activeSelf) return;

        popupChatPanel.SetActive(true);
        popupChatPanel.transform.SetAsLastSibling();

        if (!string.IsNullOrEmpty(contextName) && startedQuestContexts.Contains(contextName))
        {
            if (chatRoutineHandle == null && currentDialogueQueue.Count > 0)
            {
                chatRoutineHandle = StartCoroutine(GenerateChatRoutine());
            }
            return;
        }

        if (!string.IsNullOrEmpty(contextName))
        {
            startedQuestContexts.Add(contextName);
            PlayDialogueGroup(contextName);
        }
        else if (currentDialogueQueue != null && currentDialogueQueue.Count > 0 && chatRoutineHandle == null)
        {
            chatRoutineHandle = StartCoroutine(GenerateChatRoutine());
        }
    }

    public void PlayDialogueGroup(string groupName, bool forceRestart = false)
    {
        if (chatRoutineHandle != null)
        {
            StopCoroutine(chatRoutineHandle);
            chatRoutineHandle = null;
        }

        if (dialogueDic.ContainsKey(groupName))
        {
            currentDialogueQueue = new Queue<ChatEntity>(dialogueDic[groupName]);
            chatRoutineHandle = StartCoroutine(GenerateChatRoutine());
        }
    }

    private IEnumerator GenerateChatRoutine()
    {
        while (currentDialogueQueue.Count > 0)
        {
            while (isDialoguePaused)
            {
                yield return null;
            }

            var entity = currentDialogueQueue.Dequeue();
            GameObject typingIndicator = null;
            float preDelay = 2.0f;
            yield return WaitForSecondsWithSkip(preDelay);

            if (!isSkipping && entity.delay > 0.5f)
            {
                typingIndicator = Instantiate(npcMessagePrefab, chatContent);
                var textComp = typingIndicator.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null) textComp.text = "...";
                Canvas.ForceUpdateCanvases();

                ScrollRect scroll = chatContent.parent.parent.GetComponent<ScrollRect>();
                scroll.verticalNormalizedPosition = 0f;

                float minTypingTime = 2.0f;
                float waitTime = Mathf.Max(minTypingTime, entity.delay);
                yield return WaitForSecondsWithSkip(waitTime);
            }
            else if (entity.delay > 0)
            {
                yield return WaitForSecondsWithSkip(entity.delay);
            }

            if (typingIndicator != null)
            {
                Destroy(typingIndicator);
                typingIndicator = null;
            }

            isSkipping = false;

            GameObject prefab = (entity.sender == "USER" || entity.sender.Contains("USER")) ? userMessagePrefab : npcMessagePrefab;
            GameObject spawned = Instantiate(prefab, chatContent);

            if (spawned != null)
            {
                var imageComp = spawned.transform.Find("ContentImage")?.GetComponent<Image>();
                if (imageComp != null)
                {
                    if (entity.imagePath.ToLower() != "none" && !string.IsNullOrEmpty(entity.imagePath))
                    {
                        Sprite loadedSprite = Resources.Load<Sprite>(entity.imagePath);
                        if (loadedSprite != null)
                        {
                            imageComp.sprite = loadedSprite;
                            imageComp.gameObject.SetActive(true);
                        }
                        else
                        {
                            Debug.LogWarning($"[오류] 이미지를 찾을 수 없음: {entity.imagePath}");
                            imageComp.gameObject.SetActive(false);
                        }
                    }
                    else { imageComp.gameObject.SetActive(false); }
                }
            }

            if (!string.IsNullOrEmpty(entity.triggerAction))
            {
                HandleTriggerAction(entity.triggerAction);
            }

            if (prefab != null && chatContent != null)
            {
                if (uiAudioSource != null && messageAppearSound != null)
                {
                    uiAudioSource.PlayOneShot(messageAppearSound);
                }
                var textComp = spawned.GetComponentInChildren<TextMeshProUGUI>();

                if (textComp != null)
                {
                    textComp.text = entity.message;
                    var linkHandler = spawned.AddComponent<TMPLinkHandler>();
                    linkHandler.Setup(this);
                    textComp.raycastTarget = true;
                }

                if (!string.IsNullOrEmpty(entity.linkPanel))
                {
                    var clickable = spawned.AddComponent<MessageClickable>();
                    clickable.Setup(entity.linkPanel, this);
                }
            }

            Canvas.ForceUpdateCanvases();
            if (chatContent.parent != null && chatContent.parent.parent != null)
                chatContent.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 0f;

            if (!string.IsNullOrEmpty(entity.linkPanel))
            {
                Debug.Log($"[디버그] 현재 처리 중인 메시지: {entity.message}, linkPanel 내용: {entity.linkPanel}");
                string[] commands = entity.linkPanel.Split(';');

                foreach (string rawCommand in commands)
                {
                    string command = rawCommand.Trim();
                    if (string.IsNullOrEmpty(command)) continue;

                    if (command == "Next_Clue")
                    {
                        string nextGroup = DetermineNextContext(entity.context);
                        PlayDialogueGroup(nextGroup);
                        break;
                    }
                    else if (command.StartsWith("Unlock:"))
                    {
                        string clueIdToUnlock = command.Replace("Unlock:", "").Trim();
                        if (!unlockedClues.Contains(clueIdToUnlock))
                        {
                            unlockedClues.Add(clueIdToUnlock);
                            if (clueIdToUnlock == "q1_trigger" && targetButtonImage != null && replacedSprite != null)
                                targetButtonImage.sprite = replacedSprite;
                        }
                    }
                    else if (command == "OpenImageButton")
                    {
                        OpenImageButton();
                    }
                    else if (command.StartsWith("Unlock_Quest_"))
                    {
                        string questIndexStr = command.Replace("Unlock_Quest_", "").Trim();
                        if (int.TryParse(questIndexStr, out int questIdx) && questIdx >= 0 && questIdx < questButtonList.Count && questButtonList[questIdx] != null)
                        {
                            questButtonList[questIdx].gameObject.SetActive(true);
                            questButtonList[questIdx].SetActive();

                            if (questIdx != 0)
                            {
                                PlayPopupSound();
                            }

                            if (questIdx == 0 && targetButtonImage != null && replacedSprite != null)
                            {
                                targetButtonImage.sprite = replacedSprite;
                                Debug.Log("[시스템] 퀘스트 0번 해금: 이미지 교체 완료");
                            }
                        }
                    }
                    else if (command == "Trigger_Selection")
                    {
                        TriggerFinalSelection();
                    }
                    else if (command.StartsWith("Show_UpdatePanel:"))
                    {
                        string[] tokens = command.Split(':');

                        // 최소 3개 인자만 있어도(Show_UpdatePanel:패널명:딜레이) 프리팹 생성 없이 정상 작동하도록 완화
                        if (tokens.Length >= 3)
                        {
                            string panelName = tokens[1].Trim();

                            if (float.TryParse(tokens[2].Trim(), out float delaySeconds))
                            {
                                StartCoroutine(ShowUpdatePanelDelayed(panelName, delaySeconds));
                            }
                            else
                            {
                                Debug.LogWarning($"[오류] 딜레이 값이 숫자가 아닙니다: {tokens[2]}");
                                StartCoroutine(ShowUpdatePanelDelayed(panelName, 0f));
                            }
                        }
                        else
                        {
                            Debug.LogError($"[오류] Show_UpdatePanel 명령어의 인자 개수가 부족합니다: {command}");
                        }
                    }
                    else if (command.StartsWith("ActiveButton:"))
                    {
                        string targetButtonName = command.Replace("ActiveButton:", "").Trim();
                        var targetBtn = questButtonList.Find(x => x.gameObject.name == targetButtonName);
                        if (targetBtn != null)
                        {
                            targetBtn.gameObject.SetActive(true);
                            targetBtn.SetActive();
                            Debug.Log($"[시스템] 버튼 활성화 성공: {targetButtonName}");
                        }
                        else
                        {
                            Debug.LogError($"[오류] 활성화하려는 버튼을 찾을 수 없습니다: {targetButtonName}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[오류] 알 수 없거나 형식이 잘못된 명령어입니다: {command}");
                    }
                }
            }
        }
        chatRoutineHandle = null;
    }

    private IEnumerator WaitForSecondsWithSkip(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (isSkipping)
            {
                isSkipping = false;
                break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void FinishCurrentQuest()
    {
        Debug.Log($"[시스템] 대화 종료 확인: 인덱스 {currentPlayingQuestIndex}");

        if (currentPlayingQuestIndex != -1 && currentPlayingQuestIndex < questButtonList.Count)
        {
            questButtonList[currentPlayingQuestIndex].gameObject.SetActive(false);
            Debug.Log($"[시스템] {currentPlayingQuestIndex}번 버튼을 성공적으로 숨겼습니다.");
            currentPlayingQuestIndex = -1;
        }
    }

    private void HandleTriggerAction(string action)
    {
        if (action == "ImageGame")
        {
            Debug.Log("[트리거] ImageGame 오픈");
            OpenTargetPanel("ImageGame");
        }
    }

    // [수정] 파라미터에서 프리팹 관련 인자를 완전히 제외
    private IEnumerator ShowUpdatePanelDelayed(string targetPanelName, float delaySeconds)
    {
        if (chatRoutineHandle != null) isDialoguePaused = true;
        yield return new WaitForEndOfFrame();
        Debug.Log("[디버그] 클릭 충돌 방지 완료, 로직 시작");

        Debug.Log($"[디버그] 함수 진입! 패널이름: {targetPanelName}");
        if (delaySeconds > 0f) yield return new WaitForSeconds(delaySeconds);

        if (updateNotificationPanel != null)
        {
            RectTransform rect = updateNotificationPanel.GetComponent<RectTransform>();
            CanvasGroup cg = updateNotificationPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = updateNotificationPanel.AddComponent<CanvasGroup>();

            updateNotificationPanel.SetActive(true);
            Vector2 targetPos = rect.anchoredPosition;
            rect.anchoredPosition = new Vector2(-1000f, targetPos.y);
            cg.alpha = 0f;

            rect.DOAnchorPos(targetPos, 0.5f).SetEase(Ease.OutCubic);
            cg.DOFade(1f, 0.5f);
            updateNotificationPanel.transform.SetAsLastSibling();
            PlayPopupSound();
        }

        UnlockHeaderButton();

        if (!string.IsNullOrEmpty(targetPanelName) && panelDic.ContainsKey(targetPanelName))
        {
            var mapping = panelDic[targetPanelName];
            if (mapping.entryButton != null)
            {
                mapping.entryButton.SetActive(true);
                mapping.entryButton.transform.SetAsFirstSibling();
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
                    CloseAllMoveablePanels();
                    MoveToTargetPanel(targetPanelName);
                }
                isDialoguePaused = false;
            });
        }
    }

    private void CloseAllMoveablePanels()
    {
        foreach (var mapping in moveablePanels)
        {
            if (mapping.panelObject != null) mapping.panelObject.SetActive(false);
        }
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

    private void UnlockHeaderButton()
    {
        if (targetHeaderButton != null)
        {
            targetHeaderButton.onClick.RemoveAllListeners();
            targetHeaderButton.interactable = true;
            targetHeaderButton.onClick.AddListener(() => {
                Debug.Log("[시스템] 헤더 버튼 클릭됨: 대화 루프 없음");
                if (uiAudioSource != null && buttonClickSound != null)
                {
                    uiAudioSource.PlayOneShot(buttonClickSound);
                }
            });
        }
        if (targetHeaderButtonImage != null && unlockedSprite != null) targetHeaderButtonImage.sprite = unlockedSprite;

        if (questButtonList.Count > 0 && questButtonList[0] != null)
        {
            questButtonList[0].gameObject.SetActive(false);
            Debug.Log("[시스템] 헤더 버튼이 해금되어 Q_1 버튼을 숨겼습니다.");
        }
    }

    private string DetermineNextContext(string currentContext)
    {
        Match match = Regex.Match(currentContext, @"Q(\d+)");

        if (match.Success)
        {
            int currentNum = int.Parse(match.Groups[1].Value);
            int nextNum = currentNum + 1;
            currentClueLevel = nextNum;
            return $"Q{nextNum}_Start";
        }

        return "Q1_Start";
    }

    private void TriggerFinalSelection()
    {
        if (finalSelectionPanel != null)
        {
            finalSelectionPanel.SetActive(true);
            if (finalButtonA != null) { finalButtonA.interactable = true; finalButtonA.onClick.AddListener(() => OnClickFinalChoice("A")); if (finalButtonImageA != null) finalButtonImageA.sprite = selectionUnlockedSpriteA; }
            if (finalButtonB != null) { finalButtonB.interactable = true; finalButtonB.onClick.AddListener(() => OnClickFinalChoice("B")); if (finalButtonImageB != null) finalButtonImageB.sprite = selectionUnlockedSpriteB; }
            PlayPopupSound();
        }
    }

    private void OnClickFinalChoice(string choiceType)
    {
        LockFinalSelectionPanel();
        if (choiceType == "A") PlayDialogueGroup("Logical_Btn");
        else PlayDialogueGroup("Ethical_Btn");
    }

    private void LoadDataFromCSV()
    {
        TextAsset csvData = Resources.Load<TextAsset>("PopupChatData");

        if (csvData == null)
        {
            Debug.LogError("[오류] CSV 파일을 찾을 수 없습니다! Resources 폴더에 있는지 확인하세요.");
            return;
        }

        string[] lines = csvData.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        masterChatDataList.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] fields = Regex.Split(lines[i], csvParserPattern);
            if (fields.Length < 7) continue;

            if (!int.TryParse(fields[0].Trim(), out int parsedId))
            {
                Debug.LogWarning($"[경고] {i}번 줄의 ID('{fields[0]}')를 숫자로 변환할 수 없습니다.");
                continue;
            }

            if (!float.TryParse(fields[5].Trim(), out float parsedDelay))
            {
                Debug.LogWarning($"[경고] {i}번 줄의 Delay('{fields[5]}')를 숫자로 변환할 수 없습니다. 0으로 설정합니다.");
                parsedDelay = 0f;
            }

            string rawMessage = fields[3].Trim().Replace("\"", "");
            rawMessage = rawMessage.Replace("[br]", "\n");

            ChatEntity entity = new ChatEntity
            {
                id = parsedId,
                context = fields[1].Trim(),
                sender = fields[2].Trim(),
                message = rawMessage,
                linkPanel = fields[4].Trim(),
                delay = parsedDelay,
                imagePath = (fields.Length > 6) ? fields[6].Trim() : "None",
                triggerAction = (fields.Length > 7) ? fields[7].Trim() : ""
            };
            masterChatDataList.Add(entity);
        }
    }

    private void InitDialogueDictionary()
    {
        dialogueDic.Clear();
        foreach (var entity in masterChatDataList)
        {
            if (!dialogueDic.ContainsKey(entity.context))
            {
                dialogueDic[entity.context] = new List<ChatEntity>();
            }
            dialogueDic[entity.context].Add(entity);
        }
        Debug.Log($"[데이터] {dialogueDic.Count}개의 대화 그룹 로드 완료.");
    }
}