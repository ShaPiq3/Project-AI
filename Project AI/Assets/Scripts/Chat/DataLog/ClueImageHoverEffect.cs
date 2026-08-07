using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 뉴스/SNS/커뮤니티/아카이브의 이미지에 붙는 상호작용 컴포넌트.
/// 💡 [변경] targetClueID가 비어있어도(=단서가 아닌 일반 이미지) 동작합니다.
/// 호버/클릭 반응 자체는 단서 수집 모드에서 모든 이미지에 동일하게 일어나고,
/// 실제로 수집되는지는 클릭 시 스캔 판정으로만 구분됩니다.
/// </summary>
public class ClueImageHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Image imageComponent;
    private Color originalColor;

    [Header("설정")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.6f, 1f);
    [SerializeField] private string targetClueID; // 단서 ID (비어있으면 단서 아닌 일반 이미지)
    [SerializeField] private string questID;       // 퀘스트 ID

    // 💡 실제 뉴스 기사/게시글 제목. NewsCard 등이 동적으로 값을 넣어주면
    // 엑셀 SourceTitle 대신 이 값을 그대로 DataLog에 표시합니다.
    [SerializeField] private string sourceTitleOverride;

    // 💡 [추가] 스캔 연출이 재생되는 동안 같은 요소를 연타해서 중복 판정/수집되는 것을 막는 락
    private bool isScanLocked = false;

    void Awake()
    {
        imageComponent = GetComponent<Image>();
        if (imageComponent != null)
        {
            originalColor = imageComponent.color;
        }
    }

    void OnEnable()
    {
        TryRegisterAndResolveQuest();
    }

    /// <summary>
    /// 💡 [추가] News/Community/SNS 등이 이미지를 동적으로 세팅한 직후 호출하는 설정 함수.
    /// 기존에 이 값들을 리플렉션으로 private 필드에 직접 꽂아넣던 방식을 대체합니다.
    /// clueID가 비어있어도(단서가 아닌 일반 이미지여도) 정상 동작합니다.
    /// </summary>
    public void Configure(string clueID, string quest, string sourceTitle)
    {
        targetClueID = clueID ?? "";
        questID = quest ?? "";
        sourceTitleOverride = sourceTitle ?? "";

        TryRegisterAndResolveQuest();
    }

    private void TryRegisterAndResolveQuest()
    {
        // 단서 이미지인 경우에만 아카이브 위치 등록 + questID 자동 해석
        if (string.IsNullOrEmpty(targetClueID)) return;

        if (ArchiveManager.Instance != null)
        {
            ArchiveManager.Instance.RegisterClueLocation(targetClueID, GetComponent<RectTransform>());
        }

        if (string.IsNullOrEmpty(questID) && DataLogManager.Instance != null)
        {
            ClueData masterClue = DataLogManager.Instance.GetClueData(targetClueID.Trim());
            if (masterClue != null && !string.IsNullOrEmpty(masterClue.questID))
            {
                questID = masterClue.questID;
            }
        }
    }

    /// <summary>
    /// 💡 [변경] 호버 반응 여부는 이제 "단서 수집 모드가 켜져 있는지"만 봅니다.
    /// 진짜 단서 이미지인지 여부와 무관하게 모든 이미지가 동일하게 반응해야
    /// 플레이어가 호버만으로 정답을 알아채지 못합니다.
    /// </summary>
    private bool IsHoverable()
    {
        return DataLogManager.Instance != null && DataLogManager.Instance.IsClueSearchModeActive;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsHoverable()) return;
        if (imageComponent != null)
        {
            imageComponent.color = highlightColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (imageComponent != null)
        {
            imageComponent.color = originalColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsHoverable() || isScanLocked) return;
        if (DataLogManager.Instance == null) return;

        ClueIdentifyResult result = DataLogManager.Instance.IdentifyClue(questID, targetClueID);
        ClueScanEffectController.Instance?.PlayScanEffect(GetComponent<RectTransform>(), result);

        if (result == ClueIdentifyResult.Collectible)
        {
            DataLogManager.Instance.AcquireClue(this.questID, this.targetClueID, this.sourceTitleOverride);
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

    void OnDisable()
    {
        if (imageComponent != null)
        {
            imageComponent.color = originalColor;
        }
    }

    private void OnDestroy()
    {
        // ✅ 오브젝트가 완전히 파괴될 때만 등록 해제
        if (ArchiveManager.Instance != null && !string.IsNullOrEmpty(targetClueID))
        {
            ArchiveManager.Instance.UnregisterClueLocation(targetClueID);
        }
    }
}
