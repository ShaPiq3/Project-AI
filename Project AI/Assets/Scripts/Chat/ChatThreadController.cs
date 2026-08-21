using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// NPC 한 명(연락처 하나)과의 채팅 스레드. ChatDialogueManager의 대화 재생 로직(CSV 파싱,
/// 말풍선 생성, 분기, 트리거/이미지젠/문서버블/패널트리거)을 그대로 가져오되,
/// 공용 채팅 패널의 슬라이드 애니메이션/열림-닫힘 상태는 ChatCoordinator가 전담한다.
/// 여러 개의 ChatThreadController가 같은 씬에 동시에 존재하며(NPC 수만큼),
/// ChatCoordinator가 어떤 스레드를 화면에 보여줄지 SetActiveThread로 전환한다.
/// </summary>
public class ChatThreadController : MonoBehaviour
{
    [Header("연락처 식별")]
    [Tooltip("NPCContactData.contactID와 일치해야 함")]
    public string contactID;

    [Header("데이터")]
    public TextAsset csvFile;
    public GameObject npcPrefab;
    public GameObject userPrefab;
    public Transform chatContent;
    public TMP_Text topIPText;

    [Header("스크롤 설정")]
    public ScrollRect chatScrollRect;

    [Header("선택지 프리팹 설정")]
    public GameObject branchGroupPrefab;
    [SerializeField] private AudioSource branchClickAudioSource;
    [Tooltip("선택지를 고르면 그 문장이 Player 말풍선으로 echo될 때 쓰는 화자 이름")]
    [SerializeField] private string branchEchoSpeakerName = "AI assistant";

    [Header("문서 요약 버블 프리팹 설정")]
    [SerializeField] private GameObject documentBubblePrefab;

    private CanvasGroup threadCanvasGroup;
    private bool isThreadActive = false;

    private Dictionary<int, DialogueData> dialogueDictionary = new Dictionary<int, DialogueData>();

    private bool isDialogueStarted = false;
    private bool isDialoguePaused = false;
    public bool IsDialoguePaused
    {
        get => isDialoguePaused;
        set => isDialoguePaused = value;
    }

    private bool isTriggerActive = false;
    public bool IsTriggerActive
    {
        get => isTriggerActive;
        private set
        {
            if (isTriggerActive == value) return;
            isTriggerActive = value;
            OnTriggerActiveChanged?.Invoke(isTriggerActive);
        }
    }

    /// <summary> IsTriggerActive 값이 바뀔 때마다 발생 (true = isTrigger 진행 중 / false = 아님) </summary>
    public event Action<bool> OnTriggerActiveChanged;

    /// <summary> 이 스레드가 비활성(화면에 안 보이는) 상태에서 새 말풍선이 그려질 때 contactID와 함께 발생 </summary>
    public event Action<string> OnNewMessageWhileInactive;

    private GameObject activeBranchInstance;
    private Button[] activeBranchButtons;
    private bool isWaitingForBranchSelection = false;
    private int selectedNextId = -1;
    private string selectedUserText = "";

    private Coroutine mainDialogueCoroutine;

    void Awake()
    {
        threadCanvasGroup = GetComponent<CanvasGroup>();
        if (threadCanvasGroup == null) threadCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        SetActiveThread(false); // 코디네이터가 포커스를 줄 때까지는 숨겨둔다
    }

    void Start()
    {
        ParseCSV();
    }

    /// <summary>
    /// 이 스레드를 화면에 보이게/안 보이게 전환한다. GameObject를 끄지 않고 CanvasGroup만
    /// 토글하는 이유: 스크롤 위치/레이아웃을 유지한 채로 탭을 오갈 수 있어야 하고,
    /// 비활성 상태에서도 재생 코루틴은 계속 진행되어야 하기 때문(백그라운드에서 계속 말 걸어옴).
    /// </summary>
    public void SetActiveThread(bool active)
    {
        isThreadActive = active;
        if (threadCanvasGroup == null) return;
        threadCanvasGroup.alpha = active ? 1f : 0f;
        threadCanvasGroup.interactable = active;
        threadCanvasGroup.blocksRaycasts = active;
    }

    /// <summary> 아직 시작 안 했으면 재생을 시작하고, 이미 분기 선택 대기 중이었으면 선택 UI를 다시 보여준다. </summary>
    public void BeginPlayback(int startId = 1, float startDelay = 0f)
    {
        if (isDialogueStarted)
        {
            if (isWaitingForBranchSelection && activeBranchInstance != null && !activeBranchInstance.Equals(null))
            {
                activeBranchInstance.SetActive(true);
            }
            return;
        }
        isDialogueStarted = true;
        if (startDelay > 0f)
        {
            StartCoroutine(BeginPlaybackDelayed(startId, startDelay));
        }
        else
        {
            mainDialogueCoroutine = StartCoroutine(GenerateChatWithExcelDelay(startId));
        }
    }

    private IEnumerator BeginPlaybackDelayed(int startId, float delay)
    {
        yield return new WaitForSeconds(delay);
        mainDialogueCoroutine = StartCoroutine(GenerateChatWithExcelDelay(startId));
    }

    /// <summary> 패널이 닫힐 때 코디네이터가 호출: 열려 있던 분기 선택 UI를 감춘다. </summary>
    public void HideActiveBranchUI()
    {
        if (activeBranchInstance != null && !activeBranchInstance.Equals(null))
        {
            activeBranchInstance.SetActive(false);
        }
    }

    private string[] SplitCsvLine(string line)
    {
        return Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
    }

    private string UnwrapCsvQuotes(string raw)
    {
        string s = raw.Trim();
        if (s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\""))
        {
            s = s.Substring(1, s.Length - 2);
        }
        return s.Replace("\"\"", "\"");
    }

    void ParseCSV()
    {
        if (csvFile == null) return;

        string[] rows = csvFile.text.Replace("\r", "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = SplitCsvLine(rows[i]);
            if (columns.Length < 8) continue;

            DialogueData data = new DialogueData();
            int.TryParse(columns[0].Trim(), out data.id);
            data.speakerType = columns[1].Trim();
            data.speakerName = columns[2].Trim();
            data.dialogueText = UnwrapCsvQuotes(columns[3]).Replace("\\n", "\n").Replace("|", "\n").Replace("`", ",");
            bool.TryParse(columns[4].Trim(), out data.hasImage);
            data.imagePath = columns[5].Trim();
            float.TryParse(columns[6].Trim(), out data.delayTime);
            data.ipAddress = columns[7].Trim();

            if (columns.Length >= 15)
            {
                bool.TryParse(columns[8].Trim(), out data.isBranch);
                data.branchText1 = UnwrapCsvQuotes(columns[9]).Replace("\\n", "\n").Replace("|", "\n").Replace("`", ",");
                int.TryParse(columns[10].Trim(), out data.nextId1);
                data.branchText2 = UnwrapCsvQuotes(columns[11]).Replace("\\n", "\n").Replace("|", "\n").Replace("`", ",");
                int.TryParse(columns[12].Trim(), out data.nextId2);
                data.branchText3 = UnwrapCsvQuotes(columns[13]).Replace("\\n", "\n").Replace("|", "\n").Replace("`", ",");
                int.TryParse(columns[14].Trim(), out data.nextId3);
            }
            else
            {
                data.isBranch = false;
                data.branchText1 = ""; data.nextId1 = 0;
                data.branchText2 = ""; data.nextId2 = 0;
                data.branchText3 = ""; data.nextId3 = 0;
            }

            data.isTrigger = columns.Length >= 16 && bool.TryParse(columns[15].Trim(), out var isTrig) && isTrig;
            data.questID = columns.Length >= 17 ? columns[16].Trim() : "Q1";
            data.targetCount = columns.Length >= 18 && int.TryParse(columns[17].Trim(), out var tc) ? tc : 5;
            data.isDocumentBubble = columns.Length >= 19 && bool.TryParse(columns[18].Trim(), out var isDoc) && isDoc;
            data.documentID = columns.Length >= 20 ? columns[19].Trim() : "";
            data.bubbleLoadingDuration = columns.Length >= 21 && float.TryParse(columns[20].Trim(), out var bld) ? bld : 0f;
            data.overrideNextId = columns.Length >= 22 && int.TryParse(columns[21].Trim(), out var oni) ? oni : 0;
            data.correctDialogueID = columns.Length >= 23 && int.TryParse(columns[22].Trim(), out var cdi) ? cdi : 0;
            data.incorrectDialogueID = columns.Length >= 24 && int.TryParse(columns[23].Trim(), out var idi) ? idi : 0;
            data.isImageGenTrigger = columns.Length >= 25 && bool.TryParse(columns[24].Trim(), out var igt) && igt;
            data.imageGenQuestID = columns.Length >= 26 ? columns[25].Trim() : "";
            data.imageGenTruthDialogueID = columns.Length >= 27 && int.TryParse(columns[26].Trim(), out var igtd) ? igtd : 0;
            data.imageGenFalseDialogueID = columns.Length >= 28 && int.TryParse(columns[27].Trim(), out var igfd) ? igfd : 0;
            data.imageGenMalfunctionDialogueID = columns.Length >= 29 && int.TryParse(columns[28].Trim(), out var igmd) ? igmd : 0;
            data.isImageGenMalfunctionEnd = columns.Length >= 30 && bool.TryParse(columns[29].Trim(), out var igme) && igme;
            data.isSceneTransition = columns.Length >= 31 && bool.TryParse(columns[30].Trim(), out var ist) && ist;
            data.nextSceneName = columns.Length >= 32 ? columns[31].Trim() : "";
            data.typingSpeed = columns.Length >= 33 && float.TryParse(columns[32].Trim(), out var tsp) ? tsp : 0f;
            data.isPanelTrigger = columns.Length >= 34 && bool.TryParse(columns[33].Trim(), out var ipt) && ipt;
            data.panelID = columns.Length >= 35 ? columns[34].Trim() : "";
            data.isOpenNextRoomTrigger = columns.Length >= 36 && bool.TryParse(columns[35].Trim(), out var ionr) && ionr;
            data.nextRoomContactID = columns.Length >= 37 ? columns[36].Trim() : "";
            data.isCloseRoomTrigger = columns.Length >= 38 && bool.TryParse(columns[37].Trim(), out var icrt) && icrt;
            data.glitchEffect = columns.Length >= 39 ? columns[38].Trim() : "";
            data.errorWeight = columns.Length >= 40 && float.TryParse(columns[39].Trim(), out var ew) ? ew : 10f;

            if (!dialogueDictionary.ContainsKey(data.id))
            {
                dialogueDictionary.Add(data.id, data);
            }
            else
            {
                dialogueDictionary[data.id] = data;
            }
        }
    }

    public void JumpToDialogue(int targetId)
    {
        if (!dialogueDictionary.ContainsKey(targetId))
        {
            Debug.LogWarning($"[ChatThreadController:{contactID}] 대화 ID {targetId} 를 CSV에서 찾을 수 없습니다.");
            return;
        }

        if (mainDialogueCoroutine != null)
        {
            StopCoroutine(mainDialogueCoroutine);
            mainDialogueCoroutine = null;
        }

        if (activeBranchInstance != null && !activeBranchInstance.Equals(null))
        {
            Destroy(activeBranchInstance);
        }
        isWaitingForBranchSelection = false;
        isDialoguePaused = false;
        IsTriggerActive = false;

        isDialogueStarted = true;
        mainDialogueCoroutine = StartCoroutine(GenerateChatWithExcelDelay(targetId));
    }

    IEnumerator GenerateChatWithExcelDelay(int startId = 1)
    {
        int currentId = startId;

        while (dialogueDictionary.ContainsKey(currentId))
        {
            while (isDialoguePaused)
            {
                yield return null;
            }

            DialogueData data = dialogueDictionary[currentId];

            if (topIPText != null && isThreadActive)
            {
                topIPText.text = !string.IsNullOrEmpty(data.ipAddress) ? $"IP : {data.ipAddress}" : "IP : -";
            }

            // 💡 [오류 파라미터 - 자동세이브] 퀘스트가 시작되는 행(단서조사/문서분석/이미지생성)마다
            // "이 지점"을 체크포인트로 저장한다. 게임오버 후 "이어하기"를 누르면 여기로 되돌아온다.
            if (data.isTrigger || data.isDocumentBubble || data.isImageGenTrigger)
            {
                CheckpointManager.SaveCheckpoint(SceneManager.GetActiveScene().name, contactID, data.id);
            }

            if (data.isTrigger)
            {
                IsTriggerActive = true;
                IsDialoguePaused = true;

                if (DataLogManager.Instance != null)
                {
                    DataLogManager.Instance.StartQuest(data.questID, data.targetCount, data.correctDialogueID, data.incorrectDialogueID, contactID, data.errorWeight);

                    if (DataLogManager.Instance.questStatusUI != null)
                    {
                        DataLogManager.Instance.questStatusUI.UpdateDisplay();
                    }

                    if (!DataLogManager.Instance.IsOpen)
                    {
                        DataLogManager.Instance.ToggleLogPanel();
                    }

                    DataLogManager.Instance.NotifyTriggerStarted();

                    if (ImageGenerationManager.Instance != null)
                    {
                        ImageGenerationManager.Instance.SetToggleButtonInteractable(false);
                    }
                }
            }

            if (data.isImageGenTrigger)
            {
                IsDialoguePaused = true;

                if (ImageGenerationManager.Instance != null)
                {
                    ImageGenerationManager.Instance.UnlockAndOpen(
                        data.imageGenQuestID,
                        data.imageGenTruthDialogueID,
                        data.imageGenFalseDialogueID,
                        data.imageGenMalfunctionDialogueID,
                        contactID
                    );

                    if (DataLogManager.Instance != null)
                    {
                        DataLogManager.Instance.NotifyTriggerStarted();
                    }
                }
            }

            if (!data.isBranch || !string.IsNullOrEmpty(data.dialogueText))
            {
                bool isUser = (data.speakerType == "USER");
                GameObject selectedPrefab = isUser ? userPrefab : npcPrefab;

                if (selectedPrefab != null)
                {
                    GameObject go = Instantiate(selectedPrefab, chatContent);
                    ChatBubbleController controller = go.GetComponent<ChatBubbleController>();
                    if (controller != null) controller.SetupBubble(data);

                    if (!isThreadActive) OnNewMessageWhileInactive?.Invoke(contactID);

                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());

                    if (chatScrollRect != null && isThreadActive) chatScrollRect.verticalNormalizedPosition = 0f;

                    if (controller != null && isUser)
                    {
                        while (!controller.IsTypingComplete)
                        {
                            if (chatScrollRect != null && isThreadActive) chatScrollRect.verticalNormalizedPosition = 0f;
                            yield return null;
                        }
                    }
                }

                yield return new WaitForSeconds(data.delayTime);
            }

            // 💡 [추가] 이 줄에서 화면이 잠깐 깨졌다가 복구되는 글리치 연출 (씬 전환 없음, ChatCoordinator가 소유한 공용 오버레이 사용)
            if (!string.IsNullOrEmpty(data.glitchEffect) && data.glitchEffect.Equals("Glitch", StringComparison.OrdinalIgnoreCase))
            {
                yield return ChatCoordinator.Instance?.PlayMidGlitch();
            }

            if (data.isImageGenMalfunctionEnd)
            {
                ImageGenerationManager.Instance?.OnMalfunctionDialogueFinished();
            }

            if (data.isDocumentBubble)
            {
                if (documentBubblePrefab != null)
                {
                    GameObject bubbleGo = Instantiate(documentBubblePrefab, chatContent);
                    DocumentBubbleController bubbleController = bubbleGo.GetComponent<DocumentBubbleController>();
                    if (bubbleController != null)
                    {
                        bubbleController.Setup(data);
                    }
                    else
                    {
                        Debug.LogWarning("[ChatThreadController] documentBubblePrefab에 DocumentBubbleController가 없습니다!");
                    }

                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());
                    if (chatScrollRect != null && isThreadActive) chatScrollRect.verticalNormalizedPosition = 0f;
                }
                else
                {
                    Debug.LogWarning("[ChatThreadController] documentBubblePrefab이 인스펙터에 연결되지 않았습니다!");
                }

                DocumentQuestManager targetDoc = DocumentQuestManager.GetByID(data.documentID);
                if (targetDoc != null)
                {
                    targetDoc.NotifyBubbleShown();
                }
                else
                {
                    Debug.LogWarning($"[ChatThreadController] documentID '{data.documentID}'에 해당하는 DocumentQuestManager를 찾지 못했습니다.");
                }

                IsDialoguePaused = true;
            }

            if (data.isPanelTrigger && !string.IsNullOrEmpty(data.panelID))
            {
                PopupPanelController targetPanel = PopupPanelController.GetByID(data.panelID);
                if (targetPanel != null)
                {
                    targetPanel.OpenPanel();
                }
                else
                {
                    Debug.LogWarning($"[ChatThreadController] panelID '{data.panelID}'에 해당하는 PopupPanelController를 찾을 수 없습니다.");
                }
            }

            // 💡 [변경] 방을 닫기 전에 먼저 텀을 준다 - 그래야 마지막 대사가 뜨자마자 창이
            // 바로 꺼지지 않고, 플레이어가 읽을 시간을 번 뒤에 닫힌다. 방 닫기가 먼저, 다음 방
            // 열기가 나중이라는 순서 자체는 그대로 유지한다("진행중" 슬롯을 비워야 다음 방을
            // 열 수 있는 자동 진행 체이닝 때문).
            if (data.isCloseRoomTrigger || (data.isOpenNextRoomTrigger && !string.IsNullOrEmpty(data.nextRoomContactID)))
            {
                float transitionDelay = ChatCoordinator.Instance != null ? ChatCoordinator.Instance.RoomTransitionDelay : 0f;
                if (transitionDelay > 0f) yield return new WaitForSeconds(transitionDelay);
            }

            if (data.isCloseRoomTrigger)
            {
                ChatCoordinator.Instance?.CloseRoom(contactID);
            }

            if (data.isOpenNextRoomTrigger && !string.IsNullOrEmpty(data.nextRoomContactID))
            {
                ChatCoordinator.Instance?.NotifyIncomingMessage(data.nextRoomContactID);
            }

            if (data.isBranch)
            {
                ShowBranchUI(data);
                isWaitingForBranchSelection = true;
                while (isWaitingForBranchSelection)
                {
                    while (isDialoguePaused) yield return null;
                    yield return null;
                }

                if (userPrefab != null && !string.IsNullOrEmpty(selectedUserText))
                {
                    GameObject userSpeechGo = Instantiate(userPrefab, chatContent);
                    ChatBubbleController controller = userSpeechGo.GetComponent<ChatBubbleController>();
                    if (controller != null)
                    {
                        DialogueData userSelectionData = new DialogueData();
                        userSelectionData.speakerType = "USER";
                        userSelectionData.speakerName = branchEchoSpeakerName;
                        userSelectionData.dialogueText = selectedUserText;
                        userSelectionData.hasImage = false;
                        controller.SetupBubble(userSelectionData);
                    }

                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());
                    if (chatScrollRect != null && isThreadActive) chatScrollRect.verticalNormalizedPosition = 0f;

                    yield return new WaitForSeconds(0.8f);
                }

                currentId = selectedNextId;
            }
            else
            {
                currentId = data.overrideNextId != 0 ? data.overrideNextId : currentId + 1;
            }

            if (data.isSceneTransition)
            {
                if (!string.IsNullOrEmpty(data.nextSceneName))
                {
                    bool useGlitch = !string.IsNullOrEmpty(data.glitchEffect) && data.glitchEffect.Equals("Glitch_Scene", StringComparison.OrdinalIgnoreCase);
                    Debug.Log($"[ChatThreadController:{contactID}] 대화 종료 -> 씬 전환: {data.nextSceneName} (Glitch_Scene: {useGlitch})");

                    if (ChatCoordinator.Instance != null)
                    {
                        ChatCoordinator.Instance.TriggerSceneTransition(data.nextSceneName, useGlitch);
                    }
                    else
                    {
                        SceneManager.LoadScene(data.nextSceneName);
                    }
                }
                else
                {
                    Debug.LogError("[ChatThreadController] isSceneTransition이 TRUE인데 nextSceneName이 비어있습니다! CSV를 확인하세요.");
                }
                yield break;
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

        StartCoroutine(ScrollToBottomAfterLayout());
    }

    private IEnumerator ScrollToBottomAfterLayout()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());

        yield return null;

        if (chatScrollRect != null && isThreadActive)
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
            if (buttonText != null) buttonText.text = text;

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

        if (branchClickAudioSource != null) branchClickAudioSource.Play();
    }
}
