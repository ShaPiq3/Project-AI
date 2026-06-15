using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class SimpleLinkClickHandler : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text m_TextMeshPro;
    public NewChatSystem chatManager;

    void Awake()
    {
        m_TextMeshPro = GetComponent<TMP_Text>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, eventData.position, eventData.pressEventCamera);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];

            if (chatManager != null)
            {
                string linkId = linkInfo.GetLinkID();
                string linkText = linkInfo.GetLinkText();

                // NewChatSystem에 복구된 매개변수 2개짜리 함수를 올바르게 호출합니다.
                chatManager.OnTextLinkClick(linkId, linkText);
            }
        }
    }
}