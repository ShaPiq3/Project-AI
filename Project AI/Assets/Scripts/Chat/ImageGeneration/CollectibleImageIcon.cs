using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 아카이브, 뉴스 등 어느 창이든 상관없이 "수집 가능한 이미지" 오브젝트에 붙이세요.
/// 다른 단서(ClueTextHoverEffect/ClueImageHoverEffect)와 동일하게
/// DataLogManager.IsClueSearchModeActive("단서 수집 모드")가 켜져 있을 때만 동작합니다.
/// 기존에 그 이미지에 다른 클릭 동작(확대보기 등)이 있어도 상관없도록 별도 리스너로 추가하세요.
/// </summary>
public class CollectibleImageIcon : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("ImageGenSlotItems.csv 의 ImageID 와 동일하게 입력")]
    public string imageID;

    [Header("호버 시 색 표시 (선택 사항)")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.6f, 1f);

    private Image image;
    private Color originalColor;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (image != null)
        {
            originalColor = image.color;
        }
    }

    /// <summary>CSV로 동적 생성되는 아이템(예: 뉴스)에서 Instantiate 직후 호출해서 자동 연결할 때 사용</summary>
    public void Init(string id)
    {
        imageID = id;
    }

    /// <summary>
    /// 💡 [변경] 예전에는 ImageGenerationManager.IsCollectingMode를 확인했는데,
    /// 이 값을 세팅하는 곳이 어디에도 없어서 항상 false로 고정되어 클릭이 막혀있었습니다.
    /// 다른 단서들과 동일하게 DataLogManager의 단서 수집 모드를 기준으로 통일합니다.
    /// </summary>
    private bool IsInteractable()
    {
        if (DataLogManager.Instance == null) return false;
        if (!DataLogManager.Instance.IsClueSearchModeActive) return false;
        if (string.IsNullOrEmpty(imageID)) return false;
        if (ImageGenerationManager.Instance == null || !ImageGenerationManager.Instance.IsImageValidForCurrentQuest(imageID)) return false;

        // 💡 [추가] 이미 이 이미지가 슬롯에 등록된 상태라면 더 이상 호버/클릭 반응하지 않음
        if (ImageGenerationManager.Instance.IsImageAlreadyRegistered(imageID)) return false;

        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        if (image != null)
        {
            image.color = highlightColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (image != null)
        {
            image.color = originalColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        if (ImageGenerationManager.Instance == null) return;

        ImageGenerationManager.Instance.RegisterImageToSlot(imageID);
    }
}