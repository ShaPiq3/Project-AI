using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;

[RequireComponent(typeof(TMP_Text))]
public class SimpleLinkClickHandler : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text m_TextMeshPro;

    // 💡 유니티 인스펙터창에서 대화창을 담당하는 ChatManager를 바로 연결할 구멍입니다!
    public ChatManager chatManager;

    void Awake()
    {
        m_TextMeshPro = GetComponent<TMP_Text>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (m_TextMeshPro == null || chatManager == null) return;

        // 최신 인풋 시스템 방식으로 마우스 좌표를 안전하게 가져옵니다.
        Vector2 mousePosition = Vector2.zero;
        if (Pointer.current != null)
        {
            mousePosition = Pointer.current.position.ReadValue();
        }
        else
        {
            mousePosition = eventData.position;
        }

        // 마우스 클릭 위치에 TMP 링크가 있는지 확인합니다.
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, mousePosition, eventData.pressEventCamera);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];

            // 찾은 링크 ID(예: q1_trigger)를 ChatManager의 함수로 직접 다이렉트 배달합니다!
            chatManager.OnTextLinkClick(linkInfo.GetLinkID());
        }
    }
}