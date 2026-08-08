using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 뉴스/SNS/커뮤니티의 이미지에 붙는 이미지 생성 퀘스트용 상호작용 컴포넌트.
/// 💡 [변경] imageID가 비어있어도(=수집 대상 아닌 일반 이미지) 동작합니다.
/// 호버/클릭 반응 자체는 단서 수집 모드에서 모든 이미지에 동일하게 일어나고,
/// 실제로 슬롯에 등록되는지는 클릭 시 스캔 판정(ClueTextHoverEffect/ClueImageHoverEffect와 동일한 방식)으로만 구분됩니다.
/// </summary>
public class CollectibleImageIcon : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("ImageGenSlotItems.csv 의 ImageID 와 동일하게 입력 (비어있으면 수집 대상 아닌 일반 이미지)")]
    [SerializeField] private string imageID;

    // 💡 [변경] 색상 하이라이트 대신, 이 이미지 크기에 맞는 필터 오버레이(ClueHoverFilterOverlay)를 공용으로 재사용
    private ClueHoverFilterOverlay hoverFilter;

    // 💡 스캔 연출이 재생되는 동안 같은 요소를 연타해서 중복 판정/등록되는 것을 막는 락
    private bool isScanLocked = false;

    private void Awake()
    {
        hoverFilter = GetComponent<ClueHoverFilterOverlay>();
        if (hoverFilter == null) hoverFilter = gameObject.AddComponent<ClueHoverFilterOverlay>();
    }

    /// <summary>
    /// 💡 [추가] CollectibleImageBinder/ArchiveCollectibleAutoBinder 등이 호출하는 설정 함수.
    /// imageID가 비어있어도(단서 아닌 일반 이미지여도) 정상 동작합니다.
    /// </summary>
    public void Configure(string id)
    {
        imageID = id ?? "";
    }

    /// <summary>기존 호출부(리플렉션 아님, 직접 호출)와의 호환을 위해 유지하는 별칭.</summary>
    public void Init(string id) => Configure(id);

    /// <summary>
    /// 💡 [변경] 호버 반응 여부는 이제 "단서 수집 모드가 켜져 있는지"만 봅니다.
    /// 진짜 수집 대상 이미지인지 여부와 무관하게 모든 이미지가 동일하게 반응해야
    /// 플레이어가 호버만으로 정답을 알아채지 못합니다.
    /// </summary>
    private bool IsHoverable()
    {
        return DataLogManager.Instance != null && DataLogManager.Instance.IsClueSearchModeActive;
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
        if (ImageGenerationManager.Instance == null) return;

        ClueIdentifyResult result = ImageGenerationManager.Instance.IdentifyImage(imageID);
        ClueScanEffectController.Instance?.PlayScanEffect(GetComponent<RectTransform>(), result);

        if (result == ClueIdentifyResult.Collectible)
        {
            ImageGenerationManager.Instance.RegisterImageToSlot(imageID);
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
