using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClueImageHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Image imageComponent;
    private Color originalColor;
    [Header("설정")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.6f, 1f);
    [SerializeField] private string targetClueID; // 단서 ID
    [SerializeField] private string questID;       // 퀘스트 ID

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
        // 💡 아카이브 매니저에 "이 단서는 여기 있다"고 스스로 등록
        // (뉴스 등 다른 곳에서 동적으로 붙는 경우에도 무해합니다.
        //  ArchiveManager는 sourceType이 "아카이브"일 때만 조회되기 때문입니다.)
        if (ArchiveManager.Instance != null && !string.IsNullOrEmpty(targetClueID))
        {
            ArchiveManager.Instance.RegisterClueLocation(targetClueID, GetComponent<RectTransform>());
        }
    }

    /// <summary>
    /// 💡 [추가] 이 단서가 지금 상호작용 가능한 상태인지 공통으로 체크합니다.
    /// (수집 모드가 켜져 있고, 이 단서가 속한 퀘스트가 실제로 시작된 상태여야 함)
    /// </summary>
    private bool IsInteractable()
    {
        if (DataLogManager.Instance == null) return false;
        if (!DataLogManager.Instance.IsClueSearchModeActive) return false;
        if (!DataLogManager.Instance.IsQuestActive(questID)) return false;
        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
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
        if (!IsInteractable()) return;
        DataLogManager.Instance.AcquireClue(this.questID, this.targetClueID);
    }

    void OnDisable()
    {
        if (imageComponent != null)
        {
            imageComponent.color = originalColor;
        }
        // 💡 오브젝트가 비활성화될 때 아카이브 위치 등록도 함께 해제
        if (ArchiveManager.Instance != null && !string.IsNullOrEmpty(targetClueID))
        {
            ArchiveManager.Instance.UnregisterClueLocation(targetClueID);
        }
    }
}