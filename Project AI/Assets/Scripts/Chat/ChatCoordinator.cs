using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// 멀티 NPC 연락처 채팅의 조율자. 공용 채팅 패널(슬라이드 애니메이션 + WindowManager 연동)을
/// 소유하고, 지금 "열려있는" 대화방이 몇 개인지 + 어느 방이 포커스되어 보이는지 관리한다.
///
/// 대화방(연락처)은 고정 목록이 아니라 CSV 트리거(isOpenNextRoomTrigger/isCloseRoomTrigger)로
/// 동적으로 열리고 닫힌다. 열린 방마다 ChatTopBar에 탭 버튼이 하나씩 생겼다가, 방이 닫히면
/// 그 탭도 같이 사라진다(단, 그 방의 대화 기록/스레드 자체는 지워지지 않고 백그라운드에 남는다 -
/// 나중에 "이전 대화 열기" 기능으로 다시 불러올 수 있게 하기 위함).
///
/// 탭 슬롯은 세 종류로 나뉜다:
/// - "진행중" 슬롯 (최대 2개) : 스토리 진행에 따라 자동으로 열리는 NPC 대화방들
/// - "시스템" 슬롯 (1개) : 게임오버 등 시스템 메시지 전용 연락처
/// - "이전대화" 슬롯 (1개) : 사이드바에서 과거 대화를 불러올 때 쓰는 슬롯. 이미 뭔가 열려있으면
///   새로 여는 것으로 교체(기존 것은 자동으로 닫힘)
///
/// 이제 챕터1(MainScene)도 연락처 1개짜리 구성으로 이 클래스를 그대로 쓴다.
/// 공유 코드(DataLogManager 등)에서의 호출부는 JumpToDialogueSafe를 통해 ChatCoordinator로 라우팅된다.
/// </summary>
public class ChatCoordinator : MonoBehaviour
{
    public static ChatCoordinator Instance { get; private set; }

    private const int MaxStorySlots = 2;

    [Header("연락처 목록 (인스펙터에서 구성)")]
    public List<NPCContactData> contacts = new List<NPCContactData>();

    [Header("연락처별 스레드 컨트롤러")]
    [SerializeField] private List<ChatThreadController> threadControllers = new List<ChatThreadController>();

    [Header("대화방 탭 (ChatTopBar)")]
    [Tooltip("탭 버튼들이 생성될 부모 (ChatTopBar). HorizontalLayoutGroup이 붙어있으면 자동 정렬됨")]
    [SerializeField] private Transform chatTopBarContainer;
    [Tooltip("탭 하나의 프리팹 (Chat_Button_1을 프리팹 에셋으로 만든 것)")]
    [SerializeField] private GameObject chatTabButtonPrefab;
    [Tooltip("탭들끼리 하나만 선택되게 묶는 ToggleGroup (ChatTopBar에 붙어있는 것)")]
    [SerializeField] private ToggleGroup chatTabToggleGroup;

    [Header("대화창 애니메이션 설정 (화면 밖 -> 안 구조)")]
    [SerializeField] private RectTransform dialoguePanelRect;
    [SerializeField] private float tweenDuration = 0.5f;
    [SerializeField] private float targetPositionX = 0f;
    [SerializeField] private float hidePositionX = 600f;
    [SerializeField] private AudioSource chatPanelAudioSource;

    [Header("버튼 References (WindowManager 기능 통합)")]
    public Button closeButton;
    public Button showButton;

    [Header("WindowManager 연동")]
    [SerializeField] private WindowManager windowManager;

    [Header("자동 열림 설정")]
    [Tooltip("씬 시작 후 이 시간이 지나면 자동으로 첫 대화방을 연다. 0 이하면 자동으로 열지 않음")]
    [SerializeField] private float autoOpenDelay = 3f;
    [Tooltip("자동으로 열 연락처 ID. 비어있으면 자동으로 열지 않음")]
    [SerializeField] private string autoOpenContactID = "";

    [Header("방 전환 설정")]
    [Tooltip("한 NPC와의 대화방이 닫히고 다음 대화방이 열리기까지 쉬는 시간(초)")]
    [SerializeField] private float roomTransitionDelay = 3f;
    public float RoomTransitionDelay => roomTransitionDelay;

    [Header("메시지 도착 알림")]
    [Tooltip("연락처를 열기 전에 '메시지 도착' 알림+확인을 먼저 보여줄 팝업. 비워두면 알림 없이 바로 열림")]
    [SerializeField] private IncomingMessagePopup incomingMessagePopup;

    [Header("Screen Shatter Glitch (공용 - 쉐이더/오버레이 설정)")]
    [Tooltip("Custom/ScreenShatterGlitch 쉐이더로 만든 머티리얼. 비워두면 글리치 없이 기존 방식(즉시 전환/즉시 다음 대사)으로 동작합니다.")]
    [SerializeField] private Material vhsNoiseMaterial;
    [Tooltip("비워두면 이 오브젝트에 자동으로 AudioSource를 하나 추가해서 사용합니다.")]
    [SerializeField] private AudioSource vhsSfxSource;
    [Tooltip("화면이 깨지기 시작하는 순간 재생할 효과음")]
    [SerializeField] private AudioClip vhsGlitchSfxClip;
    [Range(0f, 1f)][SerializeField] private float vhsGlitchSfxVolume = 1f;

    [Tooltip("화면을 나누는 블록 크기 (화면 비율 기준). 작을수록 조각이 잘게 쪼개짐")]
    [Range(0.005f, 0.2f)][SerializeField] private float vhsBlockSize = 0.045f;
    [Tooltip("블록이 어긋나는(밀리는) 최대 거리 (화면 비율 기준)")]
    [Range(0f, 0.3f)][SerializeField] private float vhsMaxBlockOffset = 0.07f;
    [Tooltip("색수차(RGB 채널 분리) 강도")]
    [Range(0f, 0.05f)][SerializeField] private float vhsChromaticAberration = 0.012f;
    [Tooltip("세로로 길게 번지는 스트릭(스미어) 강도")]
    [Range(0f, 1f)][SerializeField] private float vhsStreakAmount = 0.55f;

    [Header("Glitch_Scene (씬 전환에 물려서 나오는 연출, 화면이 깨진 채로 씬이 넘어감)")]
    [Tooltip("화면이 블록 단위로 완전히 깨질 때까지 걸리는 시간")]
    [SerializeField] private float vhsTearDuration = 0.28f;
    [Tooltip("완전히 깨진 상태로 유지되는 시간 (다음 씬 로드 직전 잠깐의 정적)")]
    [SerializeField] private float vhsHoldDuration = 0.08f;
    [SerializeField] private AnimationCurve vhsTearCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2f, 0f));

    [Header("Glitch (대화 중간에 잠깐 나오는 연출, 화면이 깨졌다가 다시 복구됨)")]
    [Tooltip("화면이 깨지기 시작해서 완전히 깨질 때까지 걸리는 시간")]
    [SerializeField] private float midGlitchTearInDuration = 0.15f;
    [Tooltip("완전히 깨진 상태로 유지되는 시간")]
    [SerializeField] private float midGlitchHoldDuration = 0.15f;
    [Tooltip("깨진 화면이 원래대로 복구되는 데 걸리는 시간")]
    [SerializeField] private float midGlitchTearOutDuration = 0.25f;

    private Image vhsNoiseOverlayImage;
    private Material vhsNoiseMaterialInstance;
    private static readonly int VhsTearAmountID = Shader.PropertyToID("_TearAmount");
    private static readonly int VhsBlockSizeID = Shader.PropertyToID("_BlockSize");
    private static readonly int VhsMaxBlockOffsetID = Shader.PropertyToID("_MaxBlockOffset");
    private static readonly int VhsChromaticAberrationID = Shader.PropertyToID("_ChromaticAberration");
    private static readonly int VhsStreakAmountID = Shader.PropertyToID("_StreakAmount");
    private static readonly int VhsSnapshotTexID = Shader.PropertyToID("_SnapshotTex");

    private string pendingContactID = "";

    private readonly Dictionary<string, ChatThreadController> threadsByID = new Dictionary<string, ChatThreadController>();
    private readonly Dictionary<string, ChatTabButtonUI> tabButtons = new Dictionary<string, ChatTabButtonUI>();

    private readonly List<string> openStoryContactIDs = new List<string>();
    private string openSystemContactID = "";
    private string openArchiveContactID = "";

    private bool isChatWindowOpened = false;
    private bool isTimerFinished = false;
    private bool isClosedByPlayer = false;
    private CanvasGroup dialogueCanvasGroup;

    private ChatThreadController currentThread;
    public string CurrentContactID { get; private set; } = "";

    /// <summary> 포커스된 연락처가 바뀔 때 contactID와 함께 발생 (ChatProfileBar, 탭 선택 표시용) </summary>
    public event Action<string> OnContactFocused;
    /// <summary> 포커스되어 있던 방이 닫혔는데 넘겨받을 다른 방이 없을 때 발생 (ChatProfileBar를 비워두는 용도) </summary>
    public event Action OnFocusCleared;
    /// <summary> 비활성 상태의 스레드에 새 메시지가 도착했을 때 contactID와 함께 발생 (안읽음 배지용) </summary>
    public event Action<string> OnContactUnreadMessage;
    /// <summary> 현재 포커스된 스레드의 IsTriggerActive 값이 바뀔 때마다 발생 (TriggerLockedButton 연동) </summary>
    public event Action<bool> OnTriggerActiveChanged;

    public bool IsTriggerActive => currentThread != null && currentThread.IsTriggerActive;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CreateVhsNoiseOverlayObject(); // 모든 ChatThreadController가 공유하는 글리치 오버레이 (여러 스레드가 각자 만들면 캔버스에 중복 생성됨)

        foreach (var thread in threadControllers)
        {
            if (thread == null || string.IsNullOrEmpty(thread.contactID)) continue;
            threadsByID[thread.contactID] = thread;
            thread.OnNewMessageWhileInactive += HandleThreadNewMessage;
        }
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        foreach (var thread in threadControllers)
        {
            if (thread != null) thread.OnNewMessageWhileInactive -= HandleThreadNewMessage;
        }
    }

    void Start()
    {
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

        // 💡 [오류 파라미터 - 자동세이브] 게임오버 "이어하기"로 이 씬에 들어온 경우,
        // 저장된 체크포인트의 연락처/대사 지점으로 점프시킨다. 각 ChatThreadController의
        // CSV 파싱(ParseCSV)이 자기 Start()에서 일어나기 때문에, 실행 순서 문제를 피하려고
        // 한 프레임 기다렸다가(다른 모든 Start()가 끝난 뒤) 호출한다.
        if (CheckpointManager.ConsumePendingRestore(out var checkpoint) && !string.IsNullOrEmpty(checkpoint.contactID))
        {
            StartCoroutine(RestoreCheckpointNextFrame(checkpoint));
        }
        else if (autoOpenDelay > 0f && !string.IsNullOrEmpty(autoOpenContactID))
        {
            StartCoroutine(StartAbsoluteTimer());
        }
    }

    private IEnumerator RestoreCheckpointNextFrame(CheckpointData checkpoint)
    {
        yield return null;
        OpenRoom(checkpoint.contactID);
        JumpToDialogueSafe(checkpoint.contactID, checkpoint.dialogueID);
    }

    IEnumerator StartAbsoluteTimer()
    {
        yield return new WaitForSeconds(autoOpenDelay);
        isTimerFinished = true;
        if (isClosedByPlayer || isChatWindowOpened) yield break;
        NotifyIncomingMessage(autoOpenContactID);
    }

    /// <summary>
    /// 연락처의 방을 바로 열지 않고, 먼저 "메시지 도착" 알림을 띄운다. 플레이어가 알림의
    /// 확인 버튼을 눌러야 그때 실제로 OpenRoom이 호출되어 대화가 시작된다.
    /// (OpenArchivedRoom은 플레이어가 이미 명시적으로 요청한 행동이라 이 알림을 거치지 않는다.)
    /// </summary>
    public void NotifyIncomingMessage(string contactID)
    {
        if (!threadsByID.ContainsKey(contactID))
        {
            Debug.LogWarning($"[ChatCoordinator] contactID '{contactID}' 에 해당하는 ChatThreadController를 찾지 못했습니다.");
            return;
        }

        if (!isChatWindowOpened) OpenPanelInternal();

        pendingContactID = contactID;

        if (incomingMessagePopup != null)
        {
            NPCContactData data = GetContactData(contactID);
            string displayName = data != null ? data.displayName : contactID;
            incomingMessagePopup.Show(displayName, HandleIncomingMessageConfirmed);
        }
        else
        {
            Debug.LogWarning("[ChatCoordinator] incomingMessagePopup이 연결되어 있지 않아 알림 없이 바로 방을 엽니다.");
            OpenRoom(contactID);
        }
    }

    private void HandleIncomingMessageConfirmed()
    {
        if (string.IsNullOrEmpty(pendingContactID)) return;
        string contactID = pendingContactID;
        pendingContactID = "";
        OpenRoom(contactID);
    }

    /// <summary> showButton 클릭 등 플레이어가 직접 채팅창을 열 때. 이미 열려있던 방들 중 마지막으로 보던 걸 다시 보여준다. </summary>
    public void OpenChatWindowByPlayer()
    {
        isClosedByPlayer = false;
        OpenPanelInternal();

        string target = CurrentContactID;
        if (string.IsNullOrEmpty(target) && openStoryContactIDs.Count > 0) target = openStoryContactIDs[openStoryContactIDs.Count - 1];
        if (string.IsNullOrEmpty(target)) target = openSystemContactID;
        if (string.IsNullOrEmpty(target)) target = openArchiveContactID;
        if (!string.IsNullOrEmpty(target)) FocusThread(target);
    }

    private void OpenPanelInternal()
    {
        if (isChatWindowOpened) return;
        isChatWindowOpened = true;

        if (showButton != null) showButton.gameObject.SetActive(false);
        if (dialoguePanelRect == null) return;

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

        float slideDist = GetChatSlideDistance();
        DataLogManager.Instance?.SetEdgeToggleButtonForceHidden(false, tweenDuration, slideDist);
        ImageGenerationManager.Instance?.SetEdgeToggleButtonForceHidden(false, tweenDuration, slideDist);

        if (chatPanelAudioSource != null) chatPanelAudioSource.Play();

        if (windowManager != null)
        {
            WindowManager.Instance.isChatOpen = true;
            WindowManager.Instance.PushWindowsLeft(WindowManager.Instance.GetChatPanelWidth(), tweenDuration);
        }
    }

    public void CloseChatWindow()
    {
        if (dialoguePanelRect == null) return;

        isClosedByPlayer = true;
        isChatWindowOpened = false;

        currentThread?.HideActiveBranchUI();

        if (closeButton != null) closeButton.interactable = false;

        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.interactable = false;
            dialogueCanvasGroup.blocksRaycasts = false;
        }

        if (windowManager != null)
        {
            windowManager.PullWindowsRight(windowManager.GetChatPanelWidth(), tweenDuration);
        }

        float slideDist = GetChatSlideDistance();
        DataLogManager.Instance?.SetEdgeToggleButtonForceHidden(true, tweenDuration, slideDist);
        ImageGenerationManager.Instance?.SetEdgeToggleButtonForceHidden(true, tweenDuration, slideDist);

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

    public float GetChatSlideDistance()
    {
        return Mathf.Abs(hidePositionX - targetPositionX);
    }

    private int GetStartDialogueID(string contactID)
    {
        foreach (var contact in contacts)
        {
            if (contact.contactID == contactID) return contact.startDialogueID;
        }
        return 1;
    }

    public NPCContactData GetContactData(string contactID)
    {
        foreach (var contact in contacts)
        {
            if (contact.contactID == contactID) return contact;
        }
        return null;
    }

    // ===================== 대화방 열기/닫기 =====================

    /// <summary>
    /// 연락처의 대화방을 연다(탭 생성 + 포커스). 시스템 연락처는 자동으로 시스템 슬롯으로,
    /// 나머지는 "진행중" 슬롯(최대 2개)으로 들어간다. 이미 열려있는 방이면 그냥 포커스만 이동.
    /// </summary>
    public void OpenRoom(string contactID)
    {
        // 💡 [추가] 게임오버로 잠긴 상태면, 게임오버 방 자신을 여는 호출(LockToGameOverRoom 내부)을
        // 제외한 모든 OpenRoom 요청을 무시한다. JumpToDialogueSafe가 원래 진행 중이던 방으로
        // 다시 OpenRoom을 호출해서 포커스를 되찾아가는 문제를 여기서 막는다.
        if (isGameOverLocked && contactID != gameOverLockedContactID)
        {
            Debug.Log($"[ChatCoordinator] 게임오버 상태라 '{contactID}' 방을 열지 않습니다.");
            return;
        }

        if (!threadsByID.ContainsKey(contactID))
        {
            Debug.LogWarning($"[ChatCoordinator] contactID '{contactID}' 에 해당하는 ChatThreadController를 찾지 못했습니다.");
            return;
        }

        NPCContactData data = GetContactData(contactID);
        bool isSystem = data != null && data.isSystemContact;

        if (isSystem)
        {
            if (!string.IsNullOrEmpty(openSystemContactID) && openSystemContactID != contactID)
            {
                CloseRoom(openSystemContactID);
            }
            if (openSystemContactID != contactID)
            {
                openSystemContactID = contactID;
                CreateTabButton(contactID, data != null ? data.displayName : contactID);
            }
        }
        else
        {
            if (!openStoryContactIDs.Contains(contactID))
            {
                if (openStoryContactIDs.Count >= MaxStorySlots)
                {
                    Debug.LogWarning($"[ChatCoordinator] '진행중' 대화방 슬롯이 이미 {MaxStorySlots}개 차 있어 '{contactID}' 를 열지 못했습니다. CSV에서 동시에 열리는 방 개수를 확인해주세요.");
                    return;
                }
                openStoryContactIDs.Add(contactID);
                CreateTabButton(contactID, data != null ? data.displayName : contactID);
            }
        }

        if (!isChatWindowOpened) OpenPanelInternal();
        FocusThread(contactID);
    }

    /// <summary>
    /// "이전 대화 열기" 전용 슬롯으로 연락처를 연다. 이미 이 슬롯에 다른 방이 열려있으면
    /// 그 방을 먼저 닫고(기록은 유지) 교체한다. (사이드바 UI는 아직 미구현 - API만 미리 준비)
    /// </summary>
    public void OpenArchivedRoom(string contactID)
    {
        if (!threadsByID.ContainsKey(contactID))
        {
            Debug.LogWarning($"[ChatCoordinator] contactID '{contactID}' 에 해당하는 ChatThreadController를 찾지 못했습니다.");
            return;
        }

        if (!string.IsNullOrEmpty(openArchiveContactID) && openArchiveContactID != contactID)
        {
            CloseRoom(openArchiveContactID);
        }

        if (openArchiveContactID != contactID)
        {
            openArchiveContactID = contactID;
            NPCContactData data = GetContactData(contactID);
            CreateTabButton(contactID, data != null ? data.displayName : contactID);
        }

        if (!isChatWindowOpened) OpenPanelInternal();
        FocusThread(contactID);
    }

    /// <summary>
    /// 연락처의 대화방을 닫는다(탭 제거). 대화 기록/스레드 자체는 지워지지 않고
    /// 백그라운드에 그대로 남는다 - 나중에 OpenRoom/OpenArchivedRoom으로 다시 열면
    /// 이어서 볼 수 있다.
    /// </summary>
    public void CloseRoom(string contactID)
    {
        bool wasOpen = openStoryContactIDs.Remove(contactID);
        if (openSystemContactID == contactID) { openSystemContactID = ""; wasOpen = true; }
        if (openArchiveContactID == contactID) { openArchiveContactID = ""; wasOpen = true; }

        if (!wasOpen) return;

        if (tabButtons.TryGetValue(contactID, out var tabButton))
        {
            if (tabButton != null) Destroy(tabButton.gameObject);
            tabButtons.Remove(contactID);
        }

        if (threadsByID.TryGetValue(contactID, out var thread))
        {
            thread.SetActiveThread(false);
            thread.OnTriggerActiveChanged -= HandleCurrentThreadTriggerActiveChanged;
        }

        if (CurrentContactID == contactID)
        {
            currentThread = null;
            CurrentContactID = "";

            string fallback = openStoryContactIDs.Count > 0 ? openStoryContactIDs[openStoryContactIDs.Count - 1]
                : (!string.IsNullOrEmpty(openSystemContactID) ? openSystemContactID
                : openArchiveContactID);
            if (!string.IsNullOrEmpty(fallback))
            {
                FocusThread(fallback);
            }
            else
            {
                OnFocusCleared?.Invoke();
            }
        }
    }

    /// <summary>
    /// 💡 [추가] 현재 열려 있는 대화방을 전부 닫는다("진행중" 슬롯 + 시스템 슬롯 + 아카이브 슬롯).
    /// </summary>
    private void CloseAllRooms()
    {
        foreach (var contactID in new List<string>(openStoryContactIDs))
        {
            CloseRoom(contactID);
        }
        if (!string.IsNullOrEmpty(openSystemContactID)) CloseRoom(openSystemContactID);
        if (!string.IsNullOrEmpty(openArchiveContactID)) CloseRoom(openArchiveContactID);
    }

    private bool isGameOverLocked = false;
    private string gameOverLockedContactID = "";

    /// <summary>
    /// 💡 [추가] 게임오버 전용. 열려 있던 모든 방을 닫고 게임오버 방(보통 시스템 연락처)만 연 뒤,
    /// 이후로는 어떤 경로(JumpToDialogueSafe가 원래 방으로 OpenRoom을 다시 부르는 경우 포함)로도
    /// 다른 방이 열리지 못하도록 잠근다. 판정 함수들(DataLogManager 등)이 OnQuestJudged를 쏜 직후
    /// 자기 방으로 JumpToDialogueSafe를 또 부르면서 포커스를 되찾아가던 문제를 막기 위함.
    /// </summary>
    public void LockToGameOverRoom(string gameOverContactID)
    {
        if (isGameOverLocked) return;

        isGameOverLocked = true;
        gameOverLockedContactID = gameOverContactID;

        CloseAllRooms();
        OpenRoom(gameOverContactID);
    }

    private void CreateTabButton(string contactID, string displayName)
    {
        if (tabButtons.ContainsKey(contactID)) return;
        if (chatTabButtonPrefab == null || chatTopBarContainer == null)
        {
            Debug.LogWarning("[ChatCoordinator] chatTabButtonPrefab 또는 chatTopBarContainer가 연결되지 않아 탭 버튼을 만들지 못했습니다.");
            return;
        }

        GameObject go = Instantiate(chatTabButtonPrefab, chatTopBarContainer);
        ChatTabButtonUI tabButton = go.GetComponent<ChatTabButtonUI>();
        if (tabButton == null)
        {
            Debug.LogWarning("[ChatCoordinator] chatTabButtonPrefab에 ChatTabButtonUI가 없습니다!");
            Destroy(go);
            return;
        }

        tabButton.Setup(contactID, displayName, chatTabToggleGroup, OnTabButtonClicked);
        tabButtons[contactID] = tabButton;
    }

    private void OnTabButtonClicked(string contactID)
    {
        FocusThread(contactID);
    }

    /// <summary> 이미 열려있는 방으로 포커스를 옮긴다(탭 생성/삭제는 하지 않음). </summary>
    private void FocusThread(string contactID)
    {
        if (!threadsByID.TryGetValue(contactID, out var thread)) return;

        if (currentThread == thread)
        {
            thread.BeginPlayback(GetStartDialogueID(contactID), tweenDuration + 0.1f);
            tabButtons.TryGetValue(contactID, out var selfTab);
            selfTab?.SetSelectedWithoutNotify(true);
            return;
        }

        if (currentThread != null)
        {
            currentThread.SetActiveThread(false);
            currentThread.OnTriggerActiveChanged -= HandleCurrentThreadTriggerActiveChanged;
        }

        currentThread = thread;
        CurrentContactID = contactID;
        thread.OnTriggerActiveChanged += HandleCurrentThreadTriggerActiveChanged;
        thread.SetActiveThread(true);

        if (tabButtons.TryGetValue(contactID, out var tabButton))
        {
            tabButton.SetSelectedWithoutNotify(true);
        }

        OnTriggerActiveChanged?.Invoke(thread.IsTriggerActive);
        OnContactFocused?.Invoke(contactID);

        thread.BeginPlayback(GetStartDialogueID(contactID), tweenDuration + 0.1f);
    }

    private void HandleCurrentThreadTriggerActiveChanged(bool active)
    {
        OnTriggerActiveChanged?.Invoke(active);
    }

    private void HandleThreadNewMessage(string contactID)
    {
        OnContactUnreadMessage?.Invoke(contactID);
    }

    /// <summary> 다른 시스템(DataLogManager 등)이 특정 연락처의 특정 대화 ID로 점프시키고 싶을 때. </summary>
    public void JumpToDialogue(string contactID, int targetId)
    {
        if (!threadsByID.TryGetValue(contactID, out var thread))
        {
            Debug.LogWarning($"[ChatCoordinator] contactID '{contactID}' 에 해당하는 ChatThreadController를 찾지 못했습니다.");
            return;
        }

        if (currentThread != thread) OpenRoom(contactID);
        thread.JumpToDialogue(targetId);
    }

    /// <summary>
    /// DataLogManager/ImageGenerationManager/DocumentQuestManager에서 정답/오답 판정 후
    /// 대화를 점프시킬 때 쓰는 헬퍼. contactID가 비어있거나 ChatCoordinator가 없으면
    /// 점프가 불가능하므로, 조용히 무시하지 않고 경고를 남깁니다.
    /// </summary>
    public static void JumpToDialogueSafe(string contactID, int targetId)
    {
        if (Instance != null && !string.IsNullOrEmpty(contactID))
        {
            Instance.JumpToDialogue(contactID, targetId);
        }
        else
        {
            Debug.LogWarning($"[ChatCoordinator] JumpToDialogueSafe 실패: contactID가 비어있거나 ChatCoordinator가 없어서 대화 ID {targetId}로 점프하지 못했습니다.");
        }
    }

    // ===================== Screen Shatter Glitch (모든 ChatThreadController가 공유) =====================

    private void CreateVhsNoiseOverlayObject()
    {
        if (vhsNoiseMaterial == null) return; // 머티리얼 미설정 시 오버레이 생성 안 함 (기존 동작 유지)

        Canvas parentCanvas = dialoguePanelRect != null
            ? dialoguePanelRect.GetComponentInParent<Canvas>()
            : FindAnyObjectByType<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogWarning("[ChatCoordinator] 글리치 전환 오버레이를 붙일 Canvas를 찾지 못했습니다.");
            return;
        }

        GameObject glitchObj = new GameObject("ScreenShatter_Glitch_Overlay_Panel");
        glitchObj.transform.SetParent(parentCanvas.transform, false);
        glitchObj.transform.SetAsLastSibling();

        vhsNoiseOverlayImage = glitchObj.AddComponent<Image>();
        vhsNoiseOverlayImage.color = new Color(1f, 1f, 1f, 0f);
        vhsNoiseOverlayImage.raycastTarget = false;
        vhsNoiseOverlayImage.canvasRenderer.cullTransparentMesh = false;

        RectTransform rt = glitchObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        vhsNoiseMaterialInstance = Instantiate(vhsNoiseMaterial);
        vhsNoiseOverlayImage.material = vhsNoiseMaterialInstance;
        vhsNoiseMaterialInstance.SetFloat(VhsBlockSizeID, vhsBlockSize);
        vhsNoiseMaterialInstance.SetFloat(VhsMaxBlockOffsetID, vhsMaxBlockOffset);
        vhsNoiseMaterialInstance.SetFloat(VhsChromaticAberrationID, vhsChromaticAberration);
        vhsNoiseMaterialInstance.SetFloat(VhsStreakAmountID, vhsStreakAmount);
        vhsNoiseMaterialInstance.SetFloat(VhsTearAmountID, 0f);

        glitchObj.SetActive(false);

        if (vhsSfxSource == null)
        {
            vhsSfxSource = gameObject.AddComponent<AudioSource>();
            vhsSfxSource.playOnAwake = false;
        }
    }

    private void SetVhsTearAmount(float tear)
    {
        if (vhsNoiseMaterialInstance == null) return;
        vhsNoiseMaterialInstance.SetFloat(VhsTearAmountID, tear);
    }

    /// <summary>
    /// ChatThreadController가 glitchEffect="Glitch_Scene"인 줄에서 씬 전환할 때 호출.
    /// useGlitch가 false거나 머티리얼이 없으면 기존처럼 즉시 전환.
    /// </summary>
    public void TriggerSceneTransition(string sceneName, bool useGlitch)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[ChatCoordinator] TriggerSceneTransition에 sceneName이 비어있습니다!");
            return;
        }

        if (useGlitch && vhsNoiseMaterialInstance != null && vhsNoiseOverlayImage != null)
        {
            StartCoroutine(VhsNoiseThenLoadSceneCoroutine(sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator VhsNoiseThenLoadSceneCoroutine(string sceneName)
    {
        vhsNoiseOverlayImage.gameObject.SetActive(true);
        vhsNoiseOverlayImage.raycastTarget = true;
        SetVhsTearAmount(0f); // 스냅샷을 찍는 순간엔 패널이 완전히 투명해야 함

        yield return new WaitForEndOfFrame();
        Texture2D screenSnapshot = ScreenCapture.CaptureScreenshotAsTexture();
        vhsNoiseMaterialInstance.SetTexture(VhsSnapshotTexID, screenSnapshot);

        if (vhsGlitchSfxClip != null && vhsSfxSource != null)
        {
            vhsSfxSource.PlayOneShot(vhsGlitchSfxClip, vhsGlitchSfxVolume);
        }

        float elapsed = 0f;
        while (elapsed < vhsTearDuration)
        {
            elapsed += Time.deltaTime;
            float p = vhsTearCurve.Evaluate(Mathf.Clamp01(elapsed / vhsTearDuration));
            SetVhsTearAmount(p);
            yield return null;
        }
        SetVhsTearAmount(1f);

        yield return new WaitForSeconds(vhsHoldDuration);

        Destroy(screenSnapshot);
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// ChatThreadController가 glitchEffect="Glitch"인 줄에서 호출. 씬 전환 없이 화면이
    /// 잠깐 깨졌다가 복구된 뒤 대화가 계속 진행됩니다. 호출부에서 yield return으로 대기하세요.
    /// </summary>
    public Coroutine PlayMidGlitch()
    {
        return StartCoroutine(PlayMidGlitchCoroutine());
    }

    private IEnumerator PlayMidGlitchCoroutine()
    {
        if (vhsNoiseMaterialInstance == null || vhsNoiseOverlayImage == null) yield break;

        vhsNoiseOverlayImage.gameObject.SetActive(true);
        vhsNoiseOverlayImage.raycastTarget = true;
        SetVhsTearAmount(0f);

        yield return new WaitForEndOfFrame();
        Texture2D screenSnapshot = ScreenCapture.CaptureScreenshotAsTexture();
        vhsNoiseMaterialInstance.SetTexture(VhsSnapshotTexID, screenSnapshot);

        if (vhsGlitchSfxClip != null && vhsSfxSource != null)
        {
            vhsSfxSource.PlayOneShot(vhsGlitchSfxClip, vhsGlitchSfxVolume);
        }

        float elapsed = 0f;
        while (elapsed < midGlitchTearInDuration)
        {
            elapsed += Time.deltaTime;
            SetVhsTearAmount(Mathf.Clamp01(elapsed / midGlitchTearInDuration));
            yield return null;
        }
        SetVhsTearAmount(1f);

        yield return new WaitForSeconds(midGlitchHoldDuration);

        elapsed = 0f;
        while (elapsed < midGlitchTearOutDuration)
        {
            elapsed += Time.deltaTime;
            SetVhsTearAmount(1f - Mathf.Clamp01(elapsed / midGlitchTearOutDuration));
            yield return null;
        }
        SetVhsTearAmount(0f);

        // 연출이 끝나는 시점에 효과음도 같이 끊음 (클립이 지속시간보다 길게 남아 계속 들리는 것 방지)
        if (vhsSfxSource != null) vhsSfxSource.Stop();

        Destroy(screenSnapshot);
        vhsNoiseOverlayImage.raycastTarget = false;
        vhsNoiseOverlayImage.gameObject.SetActive(false);
    }
}
