using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Text.RegularExpressions;

/// <summary>
/// 뉴스/SNS/커뮤니티/아카이브의 텍스트 블록에 붙는 상호작용 컴포넌트.
/// 💡 [변경] 이제 targetClueID가 비어있어도(=단서가 아닌 일반 문단) 동작합니다.
/// 호버/클릭 반응 자체는 단서 수집 모드에서 모든 텍스트 블록에 동일하게 일어나고,
/// 실제로 수집되는지(=DataLog에 추가되는지)는 클릭 시 스캔 판정으로만 구분됩니다.
/// </summary>
public class ClueTextHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private TMP_Text textComponent;
    private string cleanText;
    private bool isInitialized = false;
    [SerializeField] private string targetClueID;
    [SerializeField] private string questID;

    // 💡 실제 뉴스 기사/게시글 제목. NewsCard 등이 동적으로 값을 넣어주면
    // 엑셀 SourceTitle 대신 이 값을 그대로 DataLog에 표시합니다.
    [SerializeField] private string sourceTitleOverride;

    // 💡 [변경] 텍스트 색을 바꾸는 대신, 이 텍스트 블록 크기에 맞는 필터 오버레이를 키우고/줄이는 방식으로 호버를 표시합니다.
    private ClueHoverFilterOverlay hoverFilter;

    // 💡 스캔 연출이 재생되는 동안 같은 요소를 연타해서 중복 판정/수집되는 것을 막는 락
    private bool isScanLocked = false;

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        hoverFilter = GetComponent<ClueHoverFilterOverlay>();
        if (hoverFilter == null) hoverFilter = gameObject.AddComponent<ClueHoverFilterOverlay>();
    }

    void OnEnable() => StartCoroutine(DelayedInitialize());

    /// <summary>
    /// 💡 [추가] News/Community 등이 문단을 동적으로 생성한 직후 호출하는 설정 함수.
    /// 기존에 이 값들을 리플렉션으로 private 필드에 직접 꽂아넣던 방식을 대체합니다.
    /// clueID가 비어있어도(단서가 아닌 일반 문단이어도) 정상 동작합니다.
    /// </summary>
    public void Configure(string clueID, string quest, string sourceTitle)
    {
        targetClueID = clueID ?? "";
        questID = quest ?? "";
        sourceTitleOverride = sourceTitle ?? "";

        isInitialized = false;
        TryInitializeFromExcel();
    }

    private void OnDestroy()
    {
        // 오브젝트가 사라질 때 아카이브 위치 등록도 함께 해제
        if (ArchiveManager.Instance != null && !string.IsNullOrEmpty(targetClueID))
        {
            ArchiveManager.Instance.UnregisterClueLocation(targetClueID);
        }
    }

    private System.Collections.IEnumerator DelayedInitialize()
    {
        yield return new WaitForEndOfFrame();
        if (textComponent != null)
        {
            cleanText = Regex.Replace(textComponent.text, @"<[^>]*>", "");
            textComponent.text = cleanText;
            TryInitializeFromExcel();
        }
    }

    private void TryInitializeFromExcel()
    {
        // 💡 targetClueID가 비어있는 일반(단서 아닌) 텍스트는 여기서 더 할 일이 없습니다.
        if (isInitialized || string.IsNullOrEmpty(targetClueID)) return;
        if (DataLogManager.Instance == null) return;
        ClueData clueData = DataLogManager.Instance.GetClueData(targetClueID.Trim());
        if (clueData != null && !string.IsNullOrEmpty(clueData.contentText))
        {
            isInitialized = true;

            // 💡 questID가 비어있다면(뉴스/커뮤니티에서 동적으로 붙었는데 questID를
            // 못 받은 경우 등), 마스터 데이터(ClueExcelData)에서 자동으로 찾아 채웁니다.
            // Inspector에서 직접 지정해둔 경우(아카이브 등)는 그 값을 그대로 존중합니다.
            if (string.IsNullOrEmpty(questID) && !string.IsNullOrEmpty(clueData.questID))
            {
                questID = clueData.questID;
            }
            // 아카이브 매니저에 "이 단서는 여기 있다"고 스스로 등록
            if (ArchiveManager.Instance != null)
            {
                ArchiveManager.Instance.RegisterClueLocation(targetClueID, GetComponent<RectTransform>());
            }
        }
    }

    /// <summary>
    /// 💡 [변경] 호버 반응 여부는 이제 "단서 수집 모드가 켜져 있는지"만 봅니다.
    /// 진짜 단서인지 여부와 무관하게 모든 텍스트 블록이 동일하게 반응해야
    /// 플레이어가 호버만으로 정답을 알아채지 못합니다.
    /// 단, 이미지 생성 퀘스트가 활성화된 동안에는 이미지만 반응해야 하므로 텍스트는 제외합니다.
    /// </summary>
    private bool IsHoverable()
    {
        if (DataLogManager.Instance == null || !DataLogManager.Instance.IsClueSearchModeActive) return false;
        if (ImageGenerationManager.Instance != null && ImageGenerationManager.Instance.IsUnlocked) return false;
        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsHoverable()) return;
        hoverFilter.Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoverFilter.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsHoverable() || isScanLocked) return;
        if (DataLogManager.Instance == null) return;

        eventData.Use();

        ClueIdentifyResult result = DataLogManager.Instance.IdentifyClue(questID, targetClueID);
        ClueScanEffectController.Instance?.PlayScanEffect(GetComponent<RectTransform>(), result);

        // 💡 이미 수집한 단서를 다른 출처(예: 다른 기사)에서 다시 클릭한 경우에도,
        // 기존 항목을 최신 클릭 정보로 갱신하기 위해 AcquireClue를 그대로 호출합니다.
        if (result == ClueIdentifyResult.Collectible || result == ClueIdentifyResult.AlreadyCollected)
        {
            DataLogManager.Instance.AcquireClue(this.questID, this.targetClueID, this.sourceTitleOverride);

            // 💡 [변경] 실제로 단서를 수집했을 때만 단서 수집 모드를 끈다. 단서가 아닌 문단을
            // 잘못 클릭했을 때도 무조건 꺼버리면, 긴 기사에서 원하는 단서를 못 맞히는 클릭 한
            // 번마다 모드가 꺼져서 다시 켜야 하는 문제가 있었다.
            DataLogManager.Instance.CloseClueSearchMode();
        }

        StartCoroutine(ScanLockRoutine());
    }

    private System.Collections.IEnumerator ScanLockRoutine()
    {
        isScanLocked = true;
        float lockDuration = ClueScanEffectController.Instance != null
            ? ClueScanEffectController.Instance.TotalEffectDuration
            : 0.9f;
        yield return new WaitForSeconds(lockDuration);
        isScanLocked = false;
    }
}
