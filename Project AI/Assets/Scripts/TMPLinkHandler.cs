using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TMPLinkHandler : MonoBehaviour, IPointerClickHandler
{
    private TextMeshProUGUI tmpText;
    private NewChatSystem chatSystem;

    public void Setup(NewChatSystem system)
    {
        chatSystem = system;
        tmpText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 텍스트 내의 링크 영역을 찾습니다.
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmpText, eventData.position, null);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = tmpText.textInfo.linkInfo[linkIndex];
            string linkId = linkInfo.GetLinkID();

            // 클릭된 ID를 NewChatSystem의 OnTextLinkClick으로 전달
            chatSystem.OnTextLinkClick(linkId, "");
        }
    }
}