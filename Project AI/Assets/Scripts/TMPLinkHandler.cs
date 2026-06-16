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
        if (tmpText == null)
            return;

        if (tmpText.textInfo.linkCount == 0)
            return;

        Camera cam = eventData.pressEventCamera;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            tmpText,
            eventData.position,
            cam);

        if (linkIndex == -1)
            return;

        TMP_LinkInfo linkInfo = tmpText.textInfo.linkInfo[linkIndex];
        string linkId = linkInfo.GetLinkID();

        if (chatSystem != null)
            chatSystem.OnTextLinkClick(linkId, "");
    }
}