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
        // 💡 [추가] 아카이브 매니저에 "이 단서는 여기 있다"고 스스로 등록
        // (뉴스 등 다른 곳에서 동적으로 붙는 경우에도 무해합니다.
        //  ArchiveManager는 sourceType이 "아카이브"일 때만 조회되기 때문입니다.)
        if (ArchiveManager.Instance != null && !string.IsNullOrEmpty(targetClueID))
        {
            ArchiveManager.Instance.RegisterClueLocation(targetClueID, GetComponent<RectTransform>());
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DataLogManager.Instance == null || !DataLogManager.Instance.IsClueSearchModeActive) return;
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
        if (DataLogManager.Instance == null || !DataLogManager.Instance.IsClueSearchModeActive) return;
        DataLogManager.Instance.AcquireClue(this.questID, this.targetClueID);
    }

    void OnDisable()
    {
        if (imageComponent != null)
        {
            imageComponent.color = originalColor;
        }

        // 💡 [추가] 오브젝트가 비활성화될 때 아카이브 위치 등록도 함께 해제
        if (ArchiveManager.Instance != null && !string.IsNullOrEmpty(targetClueID))
        {
            ArchiveManager.Instance.UnregisterClueLocation(targetClueID);
        }
    }
}