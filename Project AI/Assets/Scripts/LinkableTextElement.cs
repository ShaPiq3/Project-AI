using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 뉴스/커뮤니티에서 "패널 링크"가 걸린 제목/문단에 붙이는 컴포넌트.
/// 마우스를 올리면 밑줄이 생기고, 클릭하면 PanelLinkManager를 통해 지정된 패널을 엽니다.
/// linkID가 없거나 등록 안 된 ID면 그냥 평범한 텍스트로 동작합니다(밑줄/클릭 없음).
/// </summary>
public class LinkableTextElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private TMP_Text targetText;

    private string rawText;
    private string linkID;
    private bool isLinkActive;

    public void SetLink(string displayText, string targetLinkID)
    {
        rawText = displayText;
        linkID = targetLinkID;
        isLinkActive = !string.IsNullOrEmpty(linkID) && PanelLinkManager.Instance != null && PanelLinkManager.Instance.HasLink(linkID);

        ApplyText(false);
    }

    private void ApplyText(bool underline)
    {
        if (targetText == null) return;
        targetText.text = (isLinkActive && underline) ? $"<u>{rawText}</u>" : rawText;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isLinkActive) return;
        ApplyText(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isLinkActive) return;
        ApplyText(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isLinkActive) return;
        PanelLinkManager.Instance?.OpenPanel(linkID);
    }
}