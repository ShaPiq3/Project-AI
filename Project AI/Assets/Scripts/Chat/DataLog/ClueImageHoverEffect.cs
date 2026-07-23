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

    // 💡 [추가] 실제 뉴스 기사/게시글 제목. NewsCard 등이 동적으로 값을 넣어주면
    // 엑셀 SourceTitle 대신 이 값을 그대로 DataLog에 표시합니다.
    [SerializeField] private string sourceTitleOverride;

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
        // 아카이브 매니저에 "이 단서는 여기 있다"고 스스로 등록
        if (ArchiveManager.Instance != null && !string.IsNullOrEmpty(targetClueID))
        {
            ArchiveManager.Instance.RegisterClueLocation(targetClueID, GetComponent<RectTransform>());
        }
    }

    /// <summary>
    /// 이 단서가 지금 상호작용 가능한 상태인지 공통으로 체크합니다.
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
        // 💡 [변경] 실제 제목(sourceTitleOverride)이 세팅되어 있으면 그걸 같이 전달
        DataLogManager.Instance.AcquireClue(this.questID, this.targetClueID, this.sourceTitleOverride);
    }

    void OnDisable()
    {
        if (imageComponent != null)
        {
            imageComponent.color = originalColor;
        }
        // 오브젝트가 비활성화될 때 아카이브 위치 등록도 함께 해제
        if (ArchiveManager.Instance != null && !string.IsNullOrEmpty(targetClueID))
        {
            ArchiveManager.Instance.UnregisterClueLocation(targetClueID);
        }
    }
}