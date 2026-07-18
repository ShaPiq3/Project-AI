using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 💡 IPointerClickHandler 추가
public class ClueImageHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Image imageComponent;
    private Color originalColor;

    [Header("설정")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.6f, 1f);
    [SerializeField] private string targetClueID; // 💡 단서 ID 추가
    [SerializeField] private string questID;       // 💡 퀘스트 ID 추가

    void Awake()
    {
        imageComponent = GetComponent<Image>();
        if (imageComponent != null)
        {
            originalColor = imageComponent.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 💡 트리거 체크 조건 삭제 (전체 통일)
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

    // 💡 클릭 이벤트 추가
    public void OnPointerClick(PointerEventData eventData)
    {
        if (DataLogManager.Instance == null || !DataLogManager.Instance.IsClueSearchModeActive) return;

        // 퀘스트ID와 단서ID를 함께 전달
        DataLogManager.Instance.AcquireClue(this.questID, this.targetClueID);
    }

    void OnDisable()
    {
        if (imageComponent != null)
        {
            imageComponent.color = originalColor;
        }
    }
}