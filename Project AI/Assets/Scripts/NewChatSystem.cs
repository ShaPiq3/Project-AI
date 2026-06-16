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
        // 이제 Setting_Panel을 제어합니다.
        Setting_Panel.SetActive(true);
        Setting_Panel.transform.SetAsLastSibling();
    }

    public void OpenTargetPanel(string panelName)
    {
        MoveToTargetPanel(panelName);
    }



    public void CloseImageButton()
    {
        ImageButton.interactable = false;   // 클릭 불가
        ImageButtonImage.sprite = CloseSprite;
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
        if (popupChatPanel.activeSelf && popupChatPanel != null)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) {
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
            // 클릭한 UI 객체가 chatContent이거나, chatContent의 자식(말풍선들)인지 확인
            if (result.gameObject.transform.IsChildOf(targetObject.transform) || result.gameObject == targetObject)
            {
                return true;
            }
        }
        return false;
    }

    public void RegisterSoundToAllButtons()
    {
        // 씬 전체를 뒤지는 대신, 계층 구조상 하위의 버튼만 찾거나 
        // 혹은 개별 등록을 추천합니다.
        Button[] allButtons = GetComponentsInChildren<Button>(true); // true 옵션을 넣으면 비활성 버튼도 찾음
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

        if (linkId == "ImageButton")
        {
            Debug.Log("이미지게임 버튼 활성화");
            OpenImageButton();
            return;
        }

        if (linkId == "q1_trigger")
        {
            Debug.Log("[시스템] Q2 단서 트리거 클릭됨! Q1_ClueClick 대화 시작.");
            PlayDialogueGroup("Q1_ClueClick");
            OpenImageButton();

            return;
        }

        if (linkId == "q2_trigger")
        {
            Debug.Log("[시스템] Q2 단서 트리거 클릭됨! Q2_ClueClick 대화 시작.");
            PlayDialogueGroup("Q2_ClueClick"); // 바로 Q2 대화로 진입
            return; // 아래의 기본 로직을 타지 않도록 종료
        }

        if (linkId == "q3_trigger")
        {
            Debug.Log("[시스템] Q3 단서 트리거 클릭됨! Q3_ClueClick 대화 시작.");
            PlayDialogueGroup("Q3_ClueClick"); // 바로 Q3 대화로 진입
            return; // 아래의 기본 로직을 타지 않도록 종료
        }

        if (linkId == "q4_trigger")
        {
            Debug.Log("[시스템] Q4 단서 트리거 클릭됨! Q4_ClueClick 대화 시작.");
            PlayDialogueGroup("Q4_ClueClick"); // 바로 Q4 대화로 진입
            return; // 아래의 기본 로직을 타지 않도록 종료
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
            // 팝업창 활성화
            if (popupChatPanel != null)
            {
                popupChatPanel.SetActive(true);
                popupChatPanel.transform.SetAsLastSibling();
            }

            // 코루틴이 멈춰있다면 (null) 다시 시작
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

    // 팝업을 닫을 때 호출
    public void CloseChatPopup()
    {
        if (chatRoutineHandle != null)
        {
            StopCoroutine(chatRoutineHandle);
            chatRoutineHandle = null;
        }
        popupChatPanel.SetActive(false);
    }

    // 팝업을 열 때 호출
    public void OpenChatPopup(string contextName = "")
    {
        if (popupChatPanel.activeSelf) return;

        popupChatPanel.SetActive(true);
        popupChatPanel.transform.SetAsLastSibling();

        // 1. 이미 시작한 퀘스트 컨텍스트라면 새로 시작하지 않음
        if (!string.IsNullOrEmpty(contextName) && startedQuestContexts.Contains(contextName))
        {
            // 이미 본 대화라면 그냥 대화 이어가기(재개)
            if (chatRoutineHandle == null && currentDialogueQueue.Count > 0)
            {
                chatRoutineHandle = StartCoroutine(GenerateChatRoutine());
            }
            return;
        }

        // 2. 처음 보는 컨텍스트거나 대화가 없으면 시작
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
        // 대화 진행 중이었다면 멈추고 새 대화 시작 (강제 교체)
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

            // 딜레이 안전 처리
            else if (entity.delay > 0) {
                yield return WaitForSecondsWithSkip(entity.delay);
            }

            if (typingIndicator != null)
            {
                Destroy(typingIndicator);
                typingIndicator = null; // 초기화
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
                        else {
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

                    // raycastTarget이 켜져 있어야 클릭이 감지됩니다.
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
                // 세미콜론(;)을 기준으로 명령어들을 나눕니다.
                string[] commands = entity.linkPanel.Split(';');
  

                foreach (string rawCommand in commands)
                {
                    string command = rawCommand.Trim();
                    if (string.IsNullOrEmpty(command)) continue;

                    else if (command == "Next_Clue")
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
                    else if (command == "Trigger_Selection") TriggerFinalSelection();
                    else if (command.StartsWith(""))
                    {
                        string[] tokens = command.Split(':');
                    }



                    else if (command.StartsWith("Show_UpdatePanel:"))
                    {
                        // 예: "Show_UpdatePanel:EthicalPanel:0"
                        string[] tokens = command.Split(':');

                        if (tokens.Length >= 4)
                        {
                            string panelName = tokens[1].Trim();
                            string prefabName = tokens[3].Trim();

                            // 딜레이 값 파싱 (혹시 숫자가 아닌 경우를 대비해 try-catch 또는 float.TryParse 권장)
                            if (float.TryParse(tokens[2].Trim(), out float delaySeconds))
                            {
                                StartCoroutine(ShowUpdatePanelDelayed(panelName, prefabName, delaySeconds));
                            }
                            else
                            {
                                Debug.LogWarning($"[오류] 딜레이 값이 숫자가 아닙니다: {tokens[2]}");
                                StartCoroutine(ShowUpdatePanelDelayed(panelName, prefabName, 0f)); // 기본값 0으로 실행
                            }

                        }
                        

                    else if (command.StartsWith("ActiveButton:"))
                    {
                        // 명령어 예: "ActiveButton:PanelName1"
                        string targetButtonName = command.Replace("ActiveButton:", "").Trim();

                        // 씬에 있는 모든 퀘스트 버튼 중 이름이 일치하는 것을 찾아 활성화
                        var targetBtn = questButtonList.Find(x => x.gameObject.name == targetButtonName);
                        if (targetBtn != null)
                        {
                            targetBtn.gameObject.SetActive(true);
                            targetBtn.SetActive(); // 기존에 정의된 활성화 로직
                            Debug.Log($"[시스템] 버튼 활성화 성공: {targetButtonName}");
                        }
                        else
                        {
                            Debug.LogError($"[오류] 활성화하려는 버튼을 찾을 수 없습니다: {targetButtonName}");
                        }
                    }

                    else
                    {
                        Debug.LogError($"[오류] 명령어 형식이 잘못되었습니다: {command}");
                    }
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
                isSkipping = false; // 클릭 소모
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


    private IEnumerator ShowUpdatePanelDelayed(string targetPanelName, string prefabName, float delaySeconds)
    {
        if (chatRoutineHandle != null) isDialoguePaused = true;
        yield return new WaitForEndOfFrame(); // 화면 갱신 대기
        Debug.Log("[디버그] 클릭 충돌 방지 완료, 로직 시작");

        Debug.Log($"[디버그] 함수 진입! 패널이름: {targetPanelName}, 프리팹이름: {prefabName}");
        if (delaySeconds > 0f) yield return new WaitForSeconds(delaySeconds);

        // 1. 애니메이션 부분 (기존 유지)
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

        // 2. 버튼 매핑 로직 (기존 유지)
        if (!string.IsNullOrEmpty(targetPanelName) && panelDic.ContainsKey(targetPanelName))
        {
            var mapping = panelDic[targetPanelName];
            if (mapping.entryButton != null)
            {
                mapping.entryButton.SetActive(true);
                mapping.entryButton.transform.SetAsFirstSibling();
            }
        }

        // 3. 프리팹 로드 및 생성
        GameObject prefabToLoad = Resources.Load<GameObject>(prefabName);
        if (prefabToLoad != null)
        {
            if (currentActivePrefab != null) Destroy(currentActivePrefab);

            currentActivePrefab = Instantiate(prefabToLoad);

            // [수정 핵심] 부모 결정 로직: 
            // 1순위: targetPanelName 오브젝트, 2순위: 캔버스
            GameObject parentPanel = GameObject.Find(targetPanelName);
            var mapping = panelDic.ContainsKey(targetPanelName) ? panelDic[targetPanelName] : null;
            Transform targetParent = (mapping != null && mapping.panelObject != null) ? mapping.panelObject.transform : FindObjectOfType<Canvas>()?.transform;
                         
                         

            if (targetParent != null)
            {
                currentActivePrefab.transform.SetParent(targetParent, false);
                Debug.Log($"[성공] '{targetParent.name}' 아래에 생성 완료");
            }

            // 4. 위치 초기화 (중앙 정렬)
            RectTransform rect = currentActivePrefab.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            currentActivePrefab.SetActive(true);
            currentActivePrefab.transform.SetAsLastSibling();
            Debug.Log($"[시스템] 성공적으로 생성함: {prefabName}");
        }
        else
        {
            Debug.LogError($"[오류] 프리팹을 찾을 수 없습니다: Resources/{prefabName}.prefab 경로를 확인하세요.");
        }

        // 5. 버튼 이벤트 리스너 (기존 유지)
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
            // 필요하다면 parentPanel도 여기서 닫습니다.
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
            // 1. 기존에 걸려있던 모든 클릭 이벤트 제거
            targetHeaderButton.onClick.RemoveAllListeners();

            // 2. 버튼 활성화
            targetHeaderButton.interactable = true;

            // 3. 해금 시 클릭 동작 정의 (대화 재생이 아닌, 필요한 패널 열기 등)
            targetHeaderButton.onClick.AddListener(() => {
                Debug.Log("[시스템] 헤더 버튼 클릭됨: 대화 루프 없음");
                if (uiAudioSource != null && buttonClickSound != null)
                {
                    uiAudioSource.PlayOneShot(buttonClickSound);
                }
                // 여기서 대화를 다시 시작하는 대신, 
                // 원하는 동작(예: 패널 열기)만 수행하세요.
                // 예: MoveToTargetPanel("적절한패널이름");
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
        Match match = Regex.Match(currentContext, @"\d+");
        if (match.Success)
        {
            int currentNum = int.Parse(match.Value);
            int nextNum = currentNum + 1;
            currentClueLevel = nextNum;
            string nextContextName = currentContext.Replace(currentNum.ToString(), nextNum.ToString());
            if (nextContextName.Contains("_ClueClick")) nextContextName = nextContextName.Replace("_ClueClick", "_Start");
            return nextContextName;
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


    private void OnClickFinalChoice(string choiceType) { LockFinalSelectionPanel(); if (choiceType == "A") PlayDialogueGroup("Logical_Btn"); else PlayDialogueGroup("Ethical_Btn"); }
    private void LoadDataFromCSV()
    {
        // 1. CSV 파일을 TextAsset으로 로드 (Assets/Resources 폴더에 있어야 합니다)
        // 파일 확장자(.csv)는 빼고 파일명만 넣으세요.
        TextAsset csvData = Resources.Load<TextAsset>("PopupChatData");
  

        if (csvData == null)
        {
            Debug.LogError("[오류] CSV 파일을 찾을 수 없습니다! Resources 폴더에 있는지 확인하세요.");
            return;
        }

        string[] lines = csvData.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        masterChatDataList.Clear();


        // 2. 첫 줄(헤더)을 건너뛰고 1부터 시작
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            // 정규식으로 콤마 분리 (큰따옴표 안의 콤마 무시)
            string[] fields = Regex.Split(lines[i], csvParserPattern);
            if (fields.Length < 7) continue;



            if (!int.TryParse(fields[0].Trim(), out int parsedId))
            {
                Debug.LogWarning($"[경고] {i}번 줄의 ID('{fields[0]}')를 숫자로 변환할 수 없습니다.");
                continue; // ID가 숫자가 아니면 이 줄은 건너뜁니다.
            }

            if (!float.TryParse(fields[5].Trim(), out float parsedDelay))
            {
                Debug.LogWarning($"[경고] {i}번 줄의 Delay('{fields[5]}')를 숫자로 변환할 수 없습니다. 0으로 설정합니다.");
                parsedDelay = 0f; // 실패 시 기본값 0 사용
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