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

        // 💡 [추가] questID가 비어있다면(뉴스/커뮤니티에서 동적으로 붙었는데 questID를
        // 못 받은 경우 등), 마스터 데이터(ClueExcelData)에서 자동으로 찾아 채웁니다.
        if (string.IsNullOrEmpty(questID) && DataLogManager.Instance != null && !string.IsNullOrEmpty(targetClueID))
        {
            ClueData masterClue = DataLogManager.Instance.GetClueData(targetClueID.Trim());
            if (masterClue != null && !string.IsNullOrEmpty(masterClue.questID))
            {
                questID = masterClue.questID;
            }
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

        // 💡 이미 수집한 단서라면 더 이상 호버/클릭 반응이 없도록 막습니다.
        if (DataLogManager.Instance.IsClueAlreadyCollected(targetClueID)) return false;

        // 💡 [추가] 이 단서 자체는 안 모았어도, 퀘스트가 이미 목표 개수를 다 채웠다면
        // 더 이상 아무것도 못 모으는 상태이므로 마찬가지로 막습니다.
        if (DataLogManager.Instance.IsQuestCapReached(questID)) return false;

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