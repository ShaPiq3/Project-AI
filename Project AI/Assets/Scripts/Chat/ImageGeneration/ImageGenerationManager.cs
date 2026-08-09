using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// ImageGenSlotItems.csv 한 줄(이미지 하나)의 데이터
/// </summary>
[System.Serializable]
public class ImageGenSlotItemData
{
    public string imageID;
    public string uniqueID;
    public string keyword;
    public string slotDisplayImagePath; // Resources 경로 or Addressable key (프로젝트 로딩 방식에 맞게 사용)
}

/// <summary>
/// 퀘스트 하나가 요구하는 슬롯 배열(ImageGenQuestSlots.csv) 한 줄
/// </summary>
[System.Serializable]
public class ImageGenSlotLayout
{
    public string questID;
    public int slotIndex;
    public string keyword;
}

/// <summary>
/// 퀘스트 판정 결과 설정(ImageGenQuestResults.csv) 한 줄
/// </summary>
[System.Serializable]
public class ImageGenQuestResultConfig
{
    public string questID;
    public string truthCombo;   // "UID1|UID2|UID3" (SlotIndex 순서). 여러 정답 조합이면 ";"로 구분: "UID1|UID2|UID3;UID4|UID5|UID6"
    public string falseCombo;   // 형식은 truthCombo와 동일
    public int truthDialogueID;
    public int falseDialogueID;
    public int malfunctionDialogueID;
}

/// <summary>
/// 런타임에 실제로 채워진 슬롯 상태 (퀘스트ID별로 보관 -> 재시도해도 유지됨)
/// </summary>
public class ImageGenSlotRuntime
{
    public int slotIndex;
    public string keyword;
    public bool isFilled;
    public string filledUniqueID;
    public string filledDisplayImagePath;
}

public class ImageGenerationManager : MonoBehaviour
{
    public static ImageGenerationManager Instance { get; private set; }

    [Header("CSV 데이터")]
    public TextAsset slotItemCsv;      // ImageGenSlotItems.csv
    public TextAsset questSlotCsv;     // ImageGenQuestSlots.csv
    public TextAsset questResultCsv;   // ImageGenQuestResults.csv

    [Header("패널/버튼")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Button generateAnswerButton; // 답변 생성 버튼
    [SerializeField] private TMP_Text progressText; // 💡 "2/3" 형태로 진행도를 보여줄 텍스트 (DataLogManager의 questStatusUI와 동일한 역할)
    [SerializeField] private Button deleteSelectedButton;  // 항상 떠있는 삭제 버튼 (체크된 슬롯을 지움)
    [SerializeField] private AudioSource panelAudioSource;
    [SerializeField] private AudioSource imageRegisteredAudioSource; // 💡 추가

    [Header("여닫기 탭 버튼 (<< / >>) - Hover Preview + Pin")]
    [SerializeField] private Button edgeToggleButton;
    [SerializeField] private GameObject edgeToggleButtonRoot;   // 여러 요소 묶음이면 상위 오브젝트, 아니면 비워둬도 됨
    [SerializeField] private Image edgeToggleButtonImage;
    [SerializeField] private Sprite edgeToggleClosedSprite;      // ">>" 펼치기
    [SerializeField] private Sprite edgeToggleOpenSprite;        // "<<" 접기
    [SerializeField] private float hoverPreviewCloseDelay = 0.15f;


    [Header("애니메이션 설정")]
    [SerializeField] private float tweenDuration = 0.4f;
    [SerializeField] private float shownPositionX = 0f;
    [SerializeField] private float hiddenPositionX = 600f;

    [Header("슬롯 UI")]
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotButtonPrefab; // ImageGenSlotButton 붙은 프리팹

    [Header("UI Manager (확인 팝업용)")]
    public UIManager uiManager;

    [Tooltip("숨겨질 때 버튼이 원래 위치에서 X축으로 얼마나 밀려나는지.")]
    [SerializeField] private float edgeToggleHiddenOffsetX = 100f; // 💡 추가

    private RectTransform edgeToggleRect;      // 💡 추가
    private Vector2 edgeToggleShownPos;        // 💡 추가
    private CanvasGroup edgeToggleCanvasGroup; // 💡 추가

    private Coroutine hoverCloseCoroutine;
    private bool isPinnedOpen = false;

    // ---- 파싱된 정적 데이터 ----
    private Dictionary<string, ImageGenSlotItemData> itemsByImageID = new Dictionary<string, ImageGenSlotItemData>();
    private Dictionary<string, List<ImageGenSlotLayout>> layoutByQuestID = new Dictionary<string, List<ImageGenSlotLayout>>();
    private Dictionary<string, ImageGenQuestResultConfig> resultByQuestID = new Dictionary<string, ImageGenQuestResultConfig>();

    // ---- 퀘스트별 런타임 상태 (재시도해도 유지) ----
    private Dictionary<string, List<ImageGenSlotRuntime>> runtimeByQuestID = new Dictionary<string, List<ImageGenSlotRuntime>>();

    private List<ImageGenSlotButton> activeSlotButtons = new List<ImageGenSlotButton>();

    private string currentQuestID = null;
    private bool isPanelOpen = false;
    private bool isUnlocked = false;

    /// <summary>
    /// 💡 [추가] 지금 이미지 생성 퀘스트가 실제로 열려있는(잠금 해제된) 상태인지 외부에서 확인할 수 있게 합니다.
    /// CollectibleImageIcon이 이걸로 "아직 발동 안 한 단서인데 호버가 뜨는" 문제를 막습니다.
    /// </summary>
    public bool IsUnlocked => isUnlocked;
    public bool IsPanelOpen => isPanelOpen;

    /// <summary> 데이터 수집 모드 (기존 "데이터 수집" 버튼에서 같이 켜/꺼주세요) </summary>
    public bool IsCollectingMode { get; private set; } = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        ParseAllCsv();
    }

    void Start()
    {
        if (panelRect != null)
        {
            panelRect.anchoredPosition = new Vector2(hiddenPositionX, panelRect.anchoredPosition.y);
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f; // 💡 추가
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        if (edgeToggleButton != null)
        {
            edgeToggleButton.onClick.AddListener(OnEdgeToggleButtonClicked);
            edgeToggleButton.interactable = false; // 언락 전까지 잠금
        }

        ShowEdgeToggleButton(false);

        var edgeTarget = edgeToggleButtonRoot != null ? edgeToggleButtonRoot
                : (edgeToggleButton != null ? edgeToggleButton.gameObject : null);
        if (edgeTarget != null)
        {
            edgeToggleRect = edgeTarget.GetComponent<RectTransform>();
            if (edgeToggleRect != null) edgeToggleShownPos = edgeToggleRect.anchoredPosition;

            edgeToggleCanvasGroup = edgeTarget.GetComponent<CanvasGroup>();
            if (edgeToggleCanvasGroup == null) edgeToggleCanvasGroup = edgeTarget.AddComponent<CanvasGroup>();
        }

        if (generateAnswerButton != null)
        {
            generateAnswerButton.onClick.AddListener(OnGenerateAnswerClicked);
            generateAnswerButton.interactable = false;
        }

        if (deleteSelectedButton != null)
        {
            deleteSelectedButton.onClick.AddListener(OnDeleteSelectedClicked);
            // 항상 떠있고 항상 눌러볼 수 있음 (선택된 게 없으면 아무 일도 안 일어남)
            deleteSelectedButton.interactable = false;
        }

        UpdateEdgeToggleButtonSprite();
    }

    /// <summary>
    /// 💡 [추가] 다른 트리거(단서 수집 등)와 상호 배타적으로 동작하도록,
    /// toggleButton의 interactable을 강제로 설정합니다.
    /// </summary>
    public void SetToggleButtonInteractable(bool value)
    {
        if (edgeToggleButton != null)
        {
            edgeToggleButton.interactable = value;
        }
    }


    private void UpdateEdgeToggleButtonSprite()
    {
        if (edgeToggleButtonImage == null) return;
        edgeToggleButtonImage.sprite = isPanelOpen ? edgeToggleOpenSprite : edgeToggleClosedSprite;
    }

    // =========================================================
    // CSV 파싱
    // =========================================================
    private void ParseAllCsv()
    {
        ParseSlotItemCsv();
        ParseQuestSlotCsv();
        ParseQuestResultCsv();
    }

    private void ParseSlotItemCsv()
    {
        if (slotItemCsv == null)
        {
            Debug.LogWarning("[ImageGenerationManager] slotItemCsv(ImageGenSlotItems.csv)가 인스펙터에 연결되지 않았습니다!");
            return;
        }
        string[] rows = slotItemCsv.text.Replace("\r", "").Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < rows.Length; i++)
        {
            string[] c = rows[i].Split(',');
            if (c.Length < 4) continue;

            var data = new ImageGenSlotItemData
            {
                imageID = c[0].Trim(),
                uniqueID = c[1].Trim(),
                keyword = c[2].Trim(),
                slotDisplayImagePath = c[3].Trim()
            };

            if (!itemsByImageID.ContainsKey(data.imageID))
                itemsByImageID.Add(data.imageID, data);
            else
                itemsByImageID[data.imageID] = data;
        }
    }

    private void ParseQuestSlotCsv()
    {
        if (questSlotCsv == null)
        {
            Debug.LogWarning("[ImageGenerationManager] questSlotCsv(ImageGenQuestSlots.csv)가 인스펙터에 연결되지 않았습니다!");
            return;
        }
        string[] rows = questSlotCsv.text.Replace("\r", "").Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < rows.Length; i++)
        {
            string[] c = rows[i].Split(',');
            if (c.Length < 3) continue;

            var layout = new ImageGenSlotLayout
            {
                questID = c[0].Trim(),
                keyword = c[2].Trim()
            };
            int.TryParse(c[1].Trim(), out layout.slotIndex);

            if (!layoutByQuestID.ContainsKey(layout.questID))
                layoutByQuestID[layout.questID] = new List<ImageGenSlotLayout>();
            layoutByQuestID[layout.questID].Add(layout);
        }

        foreach (var kv in layoutByQuestID)
        {
            kv.Value.Sort((a, b) => a.slotIndex.CompareTo(b.slotIndex));
        }
    }

    private void ParseQuestResultCsv()
    {
        if (questResultCsv == null)
        {
            Debug.LogWarning("[ImageGenerationManager] questResultCsv(ImageGenQuestResults.csv)가 인스펙터에 연결되지 않았습니다!");
            return;
        }
        string[] rows = questResultCsv.text.Replace("\r", "").Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < rows.Length; i++)
        {
            string[] c = rows[i].Split(',');
            if (c.Length < 6) continue;

            var cfg = new ImageGenQuestResultConfig
            {
                questID = c[0].Trim(),
                truthCombo = c[1].Trim(),
                falseCombo = c[2].Trim()
            };
            int.TryParse(c[3].Trim(), out cfg.truthDialogueID);
            int.TryParse(c[4].Trim(), out cfg.falseDialogueID);
            int.TryParse(c[5].Trim(), out cfg.malfunctionDialogueID);

            resultByQuestID[cfg.questID] = cfg;
        }
    }

    // =========================================================
    // 외부(ChatDialogueManager)에서 호출: 특정 말풍선 도달 시 잠금 해제 + 자동 오픈
    // =========================================================
    /// <param name="truthDialogueID">correctDialogueID 재사용</param>
    /// <param name="falseDialogueID">incorrectDialogueID 재사용</param>
    /// <param name="malfunctionDialogueID">신규 컬럼</param>
    public void UnlockAndOpen(string questID, int truthDialogueID, int falseDialogueID, int malfunctionDialogueID)
    {
        if (string.IsNullOrEmpty(questID) || !layoutByQuestID.ContainsKey(questID))
        {
            Debug.LogWarning($"[ImageGenerationManager] 퀘스트ID '{questID}' 의 슬롯 배열을 찾을 수 없습니다.");
            return;
        }

        if (!resultByQuestID.TryGetValue(questID, out var cfg))
        {
            cfg = new ImageGenQuestResultConfig { questID = questID };
            resultByQuestID[questID] = cfg;
        }
        cfg.truthDialogueID = truthDialogueID;
        cfg.falseDialogueID = falseDialogueID;
        cfg.malfunctionDialogueID = malfunctionDialogueID;

        currentQuestID = questID;
        isUnlocked = true;

        if (edgeToggleButton != null) edgeToggleButton.interactable = true;

        EnsureRuntimeForQuest(questID);
        RebuildSlotUI(questID);

        ShowEdgeToggleButton(true);
        isPinnedOpen = true;

        OpenPanel();
    }

    private void EnsureRuntimeForQuest(string questID)
    {
        if (runtimeByQuestID.ContainsKey(questID)) return; // 이미 있으면(재시도) 그대로 유지

        var layouts = layoutByQuestID[questID];
        var list = new List<ImageGenSlotRuntime>();
        foreach (var l in layouts)
        {
            list.Add(new ImageGenSlotRuntime
            {
                slotIndex = l.slotIndex,
                keyword = l.keyword,
                isFilled = false,
                filledUniqueID = null,
                filledDisplayImagePath = null
            });
        }
        runtimeByQuestID[questID] = list;
    }

    // =========================================================
    // 슬롯 UI
    // =========================================================
    private void RebuildSlotUI(string questID)
    {
        if (slotContainer == null)
        {
            Debug.LogWarning("[ImageGenerationManager] slotContainer가 인스펙터에 연결되지 않았습니다!");
            return;
        }
        if (slotButtonPrefab == null)
        {
            Debug.LogWarning("[ImageGenerationManager] slotButtonPrefab이 인스펙터에 연결되지 않았습니다!");
            return;
        }

        foreach (Transform child in slotContainer) Destroy(child.gameObject);
        activeSlotButtons.Clear();

        var runtimeSlots = runtimeByQuestID[questID];
        foreach (var slot in runtimeSlots)
        {
            GameObject go = Instantiate(slotButtonPrefab, slotContainer);
            ImageGenSlotButton ui = go.GetComponent<ImageGenSlotButton>();
            if (ui != null)
            {
                ui.Setup(slot);
                activeSlotButtons.Add(ui);
            }
        }

        Canvas.ForceUpdateCanvases();
        if (slotContainer is RectTransform rt)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        RefreshGenerateButtonState();
        RefreshDeleteButtonInteractable();
    }

    /// <summary>슬롯 UI를 하나만 다시 그림 (등록/해제 시 호출)</summary>
    private void RefreshSlotUI(int slotIndex)
    {
        var runtimeSlots = runtimeByQuestID[currentQuestID];
        var slot = runtimeSlots.Find(s => s.slotIndex == slotIndex);
        var ui = activeSlotButtons.Find(b => b.SlotIndex == slotIndex);
        if (slot != null && ui != null)
        {
            ui.Setup(slot);
        }
        RefreshGenerateButtonState();
        RefreshDeleteButtonInteractable();
    }

    private void RefreshGenerateButtonState()
    {
        if (currentQuestID == null || !runtimeByQuestID.TryGetValue(currentQuestID, out var runtimeSlots)) return;

        int filledCount = runtimeSlots.Count(s => s.isFilled);
        int totalCount = runtimeSlots.Count;
        bool allFilled = filledCount == totalCount;

        if (generateAnswerButton != null) generateAnswerButton.interactable = allFilled;

        // 💡 DataLogManager의 진행도 표시와 동일한 역할
        if (progressText != null) progressText.text = $"{filledCount}/{totalCount}";
    }

    // =========================================================
    // 데이터 수집: 아카이브/뉴스 등에서 이미지를 클릭했을 때 호출
    // =========================================================
    public void SetCollectingMode(bool active)
    {
        IsCollectingMode = active;
    }

    /// <summary>CollectibleImageIcon에서 호출</summary>
    public void RegisterImageToSlot(string imageID)
    {
        if (!isUnlocked || currentQuestID == null) return;
        if (!itemsByImageID.TryGetValue(imageID, out var itemData))
        {
            Debug.LogWarning($"[ImageGenerationManager] ImageID '{imageID}' 가 ImageGenSlotItems.csv 에 없습니다.");
            return;
        }

        var runtimeSlots = runtimeByQuestID[currentQuestID];
        // 같은 키워드의 슬롯을 찾는다. 이미 채워져 있어도 "교체" 로 덮어씀 (재시도 대응)
        var targetSlot = runtimeSlots.Find(s => s.keyword == itemData.keyword && !s.isFilled)
                          ?? runtimeSlots.Find(s => s.keyword == itemData.keyword);

        if (targetSlot == null)
        {
            // 이 퀘스트에는 해당 키워드 슬롯이 아예 없음
            return;
        }

        targetSlot.isFilled = true;
        targetSlot.filledUniqueID = itemData.uniqueID;
        targetSlot.filledDisplayImagePath = itemData.slotDisplayImagePath;

        RefreshSlotUI(targetSlot.slotIndex);
        if (imageRegisteredAudioSource != null) imageRegisteredAudioSource.Play();
    }

    /// <summary>슬롯 버튼 자체를 클릭해서 등록 취소하고 싶을 때 (선택 기능)</summary>
    public void ClearSlot(int slotIndex)
    {
        if (currentQuestID == null) return;
        var runtimeSlots = runtimeByQuestID[currentQuestID];
        var slot = runtimeSlots.Find(s => s.slotIndex == slotIndex);
        if (slot == null) return;

        slot.isFilled = false;
        slot.filledUniqueID = null;
        slot.filledDisplayImagePath = null;

        RefreshSlotUI(slotIndex);
    }

    /// <summary>
    /// 항상 떠있는 삭제 버튼 클릭 시 호출.
    /// 체크박스(Toggle)가 켜져있는 슬롯들만 모아서 한 번에 ClearSlot 처리합니다.
    /// 체크된 게 없으면 아무 일도 일어나지 않습니다.
    /// </summary>
    private void OnDeleteSelectedClicked()
    {
        if (currentQuestID == null) return;

        var targetIndexes = new List<int>();
        foreach (var ui in activeSlotButtons)
        {
            if (ui.IsSelectedForDelete)
            {
                targetIndexes.Add(ui.SlotIndex);
            }
        }

        if (targetIndexes.Count == 0) return; // 안전장치 (버튼이 잠겨있으니 사실상 도달 안 함)

        int count = targetIndexes.Count;

        if (uiManager != null)
        {
            uiManager.ShowConfirmPopup(
                $"선택한 {count}개의 이미지를 삭제하시겠습니까?",
                () =>
                {
                    // 예 -> 실제 삭제 진행
                    foreach (int slotIndex in targetIndexes)
                    {
                        ClearSlot(slotIndex);
                    }
                    RefreshDeleteButtonInteractable();
                },
                () =>
                {
                    // 아니오 -> 아무 것도 안 함
                }
            );
        }
        else
        {
            // uiManager 미연결 시 기존 방식(즉시 삭제)으로 폴백
            Debug.LogWarning("[ImageGenerationManager] uiManager가 인스펙터에 연결되지 않아 확인 팝업 없이 바로 삭제합니다.");
            foreach (int slotIndex in targetIndexes)
            {
                ClearSlot(slotIndex);
            }
            RefreshDeleteButtonInteractable();
        }
    }

    // =========================================================
    // 답변 생성 -> 판정
    // =========================================================
    private void OnGenerateAnswerClicked()
    {
        if (currentQuestID == null) return;
        if (!resultByQuestID.TryGetValue(currentQuestID, out var cfg)) return;

        var runtimeSlots = runtimeByQuestID[currentQuestID];
        if (!runtimeSlots.TrueForAll(s => s.isFilled)) return; // 안전장치

        // 💡 판정 시작하는 순간 바로 두 버튼을 잠급니다.
        if (generateAnswerButton != null) generateAnswerButton.interactable = false;
        if (deleteSelectedButton != null) deleteSelectedButton.interactable = false;

        // SlotIndex 순서대로 UniqueID 를 이어붙여 조합 문자열 생성
        var ordered = new List<ImageGenSlotRuntime>(runtimeSlots);
        ordered.Sort((a, b) => a.slotIndex.CompareTo(b.slotIndex));

        var uids = new List<string>();
        foreach (var s in ordered) uids.Add(s.filledUniqueID);
        string combo = string.Join("|", uids);

        int targetDialogueID;
        if (ComboMatchesAny(combo, cfg.truthCombo))
        {
            targetDialogueID = cfg.truthDialogueID;
        }
        else if (ComboMatchesAny(combo, cfg.falseCombo))
        {
            targetDialogueID = cfg.falseDialogueID;
        }
        else
        {
            targetDialogueID = cfg.malfunctionDialogueID; // 오작동 -> 대화에서 텍스트+이미지 출력 후 재시도 가능
        }

        if (ChatDialogueManager.Instance != null)
        {
            ChatDialogueManager.Instance.JumpToDialogue(targetDialogueID);
        }

        // 오작동이어도 슬롯 데이터는 절대 초기화하지 않음 (요구사항)
        bool isResolved = (targetDialogueID == cfg.truthDialogueID || targetDialogueID == cfg.falseDialogueID);
        if (isResolved)
        {
            // 💡 퀘스트가 "확정"(진실 또는 거짓)되면 버튼/패널을 자동으로 다시 잠급니다.
            LockAndClose();
        }
    }

    /// <summary>
    /// 💡 ChatDialogueManager가 오작동 결과 시퀀스의 마지막 줄
    /// (isImageGenMalfunctionEnd=TRUE)까지 다 재생했을 때 호출합니다.
    /// </summary>
    public void OnMalfunctionDialogueFinished()
    {
        if (!isUnlocked) return; // 그 사이 퀘스트가 종료/변경됐으면 건드리지 않음

        if (generateAnswerButton != null) generateAnswerButton.interactable = true;
        if (deleteSelectedButton != null) deleteSelectedButton.interactable = true;
    }
    private void LockAndClose()
    {
        isUnlocked = false;
        currentQuestID = null;

        if (edgeToggleButton != null) edgeToggleButton.interactable = false;
        if (generateAnswerButton != null) generateAnswerButton.interactable = false;

        isPinnedOpen = false;
        if (hoverCloseCoroutine != null)
        {
            StopCoroutine(hoverCloseCoroutine);
            hoverCloseCoroutine = null;
        }
        ShowEdgeToggleButton(false);

        ClosePanel();

        if (DataLogManager.Instance != null)
        {
            DataLogManager.Instance.NotifyTriggerEnded();
        }
    }

    /// <summary>
    /// 플레이어가 만든 조합(combo)이, CSV에 적힌 조합 목록(comboListString) 중
    /// 하나와 일치하는지 확인합니다.
    /// </summary>
    private bool ComboMatchesAny(string combo, string comboListString)
    {
        if (string.IsNullOrEmpty(comboListString)) return false;

        string[] alternatives = comboListString.Split(';');
        foreach (var alt in alternatives)
        {
            if (combo == alt.Trim()) return true;
        }
        return false;
    }

    private void OnEdgeToggleButtonClicked()
    {
        if (!isUnlocked) return;

        if (isPanelOpen && !isPinnedOpen)
        {
            // 미리보기 중 클릭 → 고정만 시킴
            isPinnedOpen = true;
            UpdateEdgeToggleButtonSprite();
        }
        else if (isPanelOpen && isPinnedOpen)
        {
            // 고정 열림 → 클릭하면 닫음
            isPinnedOpen = false;
            ClosePanel();
        }
        else
        {
            // 닫혀있음 → 열고 고정
            isPinnedOpen = true;
            OpenPanel();
        }
    }

    public void OnEdgeHoverEnter()
    {
        if (!isUnlocked) return;

        if (hoverCloseCoroutine != null)
        {
            StopCoroutine(hoverCloseCoroutine);
            hoverCloseCoroutine = null;
        }

        if (!isPanelOpen)
        {
            OpenPanel();
        }
    }

    public void OnEdgeHoverExit()
    {
        if (!isUnlocked) return;

        if (hoverCloseCoroutine != null) StopCoroutine(hoverCloseCoroutine);
        hoverCloseCoroutine = StartCoroutine(DelayedPreviewClose());
    }

    private System.Collections.IEnumerator DelayedPreviewClose()
    {
        yield return new WaitForSeconds(hoverPreviewCloseDelay);

        if (!isPinnedOpen && isPanelOpen)
        {
            ClosePanel();
        }
        hoverCloseCoroutine = null;
    }

    private void ShowEdgeToggleButton(bool show)
    {
        var target = edgeToggleButtonRoot != null ? edgeToggleButtonRoot
                    : (edgeToggleButton != null ? edgeToggleButton.gameObject : null);
        if (target == null) return;

        edgeToggleRect?.DOKill();
        if (edgeToggleRect != null)
        {
            edgeToggleRect.anchoredPosition = show
                ? edgeToggleShownPos
                : new Vector2(edgeToggleShownPos.x + edgeToggleHiddenOffsetX, edgeToggleShownPos.y);
        }
        if (edgeToggleCanvasGroup != null) edgeToggleCanvasGroup.blocksRaycasts = show;

        target.SetActive(show);
    }

    public void OpenPanel()
    {
        if (panelRect == null) return;

        // 💡 panelRect 자기 자신뿐 아니라, 상위 조상 오브젝트들까지 전부 비활성화 상태였을 수 있으므로,
        // 부모 체인을 타고 올라가며 꺼져있는 오브젝트를 전부 강제로 켭니다.
        Transform current = panelRect.transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }
            current = current.parent;
        }

        isPanelOpen = true;

        panelRect.DOKill();
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.DOKill();
            panelCanvasGroup.DOFade(1f, tweenDuration);
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }
        panelRect.DOAnchorPosX(shownPositionX, tweenDuration).SetEase(Ease.OutQuad);

        if (panelAudioSource != null) panelAudioSource.Play();

        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.NotifyImageGenOpenedExternally();
        }

        UpdateEdgeToggleButtonSprite();
    }

    public void ClosePanel()
    {
        if (panelRect == null) return;
        isPanelOpen = false;

        panelRect.DOKill();
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.DOKill();
            panelCanvasGroup.DOFade(0f, tweenDuration);
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }
        panelRect.DOAnchorPosX(hiddenPositionX, tweenDuration).SetEase(Ease.InQuad);

        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.NotifyImageGenClosedExternally();
        }

        UpdateEdgeToggleButtonSprite();
    }

    /// <summary>
    /// 💡 [추가] 이 imageID가 "현재 활성화된 퀘스트(currentQuestID)"에 실제로 속하는 키워드인지 확인합니다.
    /// </summary>
    public bool IsImageValidForCurrentQuest(string imageID)
    {
        if (!isUnlocked || currentQuestID == null) return false;
        if (string.IsNullOrEmpty(imageID)) return false;

        if (!itemsByImageID.TryGetValue(imageID, out var itemData)) return false;

        if (!layoutByQuestID.TryGetValue(currentQuestID, out var layouts)) return false;

        return layouts.Exists(l => l.keyword == itemData.keyword);
    }

    /// <summary>
    /// 💡 [추가] 이 imageID에 해당하는 슬롯이 현재 활성 퀘스트에서 이미 채워졌는지(등록 완료됐는지) 확인합니다.
    /// </summary>
    public bool IsImageAlreadyRegistered(string imageID)
    {
        if (!isUnlocked || currentQuestID == null) return false;
        if (string.IsNullOrEmpty(imageID)) return false;
        if (!itemsByImageID.TryGetValue(imageID, out var itemData)) return false;
        if (!runtimeByQuestID.TryGetValue(currentQuestID, out var runtimeSlots)) return false;

        var slot = runtimeSlots.Find(s => s.keyword == itemData.keyword);
        return slot != null && slot.isFilled && slot.filledUniqueID == itemData.uniqueID;
    }

    /// <summary>
    /// 💡 [추가] 클릭한 이미지가 무엇인지 "판정"만 합니다 (등록/상태 변경 없음).
    /// 뉴스/SNS/커뮤니티의 모든 이미지가 이제 클릭에 반응하므로, 실제로 RegisterImageToSlot을
    /// 부를지 말지, 어떤 스캔 결과 연출을 보여줄지를 CollectibleImageIcon이 먼저 판단할 때 사용합니다.
    /// DataLogManager.IdentifyClue와 동일한 판정 결과(ClueIdentifyResult)를 그대로 재사용합니다.
    /// </summary>
    public ClueIdentifyResult IdentifyImage(string imageID)
    {
        if (string.IsNullOrEmpty(imageID)) return ClueIdentifyResult.NotCollectible;
        if (!itemsByImageID.ContainsKey(imageID)) return ClueIdentifyResult.NotCollectible;
        if (IsImageAlreadyRegistered(imageID)) return ClueIdentifyResult.AlreadyCollected;
        if (!IsImageValidForCurrentQuest(imageID)) return ClueIdentifyResult.NotCollectible;

        return ClueIdentifyResult.Collectible;
    }

    public void SetSlotSelected(ImageGenSlotButton slotButton, bool isSelected)
    {
        RefreshDeleteButtonInteractable();
    }

    private void RefreshDeleteButtonInteractable()
    {
        bool anySelected = activeSlotButtons.Exists(b => b.IsSelectedForDelete);
        if (deleteSelectedButton != null)
        {
            deleteSelectedButton.interactable = anySelected; // ✅ 항상 보이되 잠금/해제만
        }
    }

    /// <summary>
    /// 💡 [변경] offsetX를 파라미터로 받아, 채팅 패널과 정확히 같은 이동 거리로 슬라이드합니다.
    /// </summary>
    public void SetEdgeToggleButtonForceHidden(bool hidden, float duration, float offsetX)
    {
        bool shouldShow = hidden ? false : isUnlocked; // 💡 activeDatalogTriggerCount → isUnlocked로 수정

        var target = edgeToggleButtonRoot != null ? edgeToggleButtonRoot
                    : (edgeToggleButton != null ? edgeToggleButton.gameObject : null);
        if (target == null || edgeToggleRect == null) return;

        Vector2 hiddenPos = new Vector2(edgeToggleShownPos.x + offsetX, edgeToggleShownPos.y);

        if (shouldShow && !target.activeSelf)
        {
            target.SetActive(true);
            edgeToggleRect.anchoredPosition = hiddenPos;
        }

        if (edgeToggleCanvasGroup != null) edgeToggleCanvasGroup.blocksRaycasts = shouldShow;

        edgeToggleRect.DOKill();
        Vector2 targetPos = shouldShow ? edgeToggleShownPos : hiddenPos;
        edgeToggleRect.DOAnchorPosX(targetPos.x, duration)
            .SetEase(shouldShow ? Ease.OutQuad : Ease.InQuad)
            .OnComplete(() =>
            {
                if (!shouldShow) target.SetActive(false);
            });
    }
}