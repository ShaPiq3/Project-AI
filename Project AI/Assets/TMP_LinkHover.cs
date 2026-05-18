using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// 새로운 인풋 시스템을 사용하는 프로젝트용 스크립트
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TMP_LinkHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private TextMeshProUGUI m_TextMeshPro;
    private Canvas m_Canvas;
    private Camera m_Camera;
    private bool isMouseOver = false;
    private string originalText;

    [Header("색상 설정")]
    public Color hoverColor = Color.yellow;

    [Header("채팅 매니저 연동")]
    public ChatManager chatManager;

    void Awake()
    {
        m_TextMeshPro = GetComponent<TextMeshProUGUI>();
        m_Canvas = GetComponentInParent<Canvas>();

        if (m_Canvas != null)
        {
            if (m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                m_Camera = null;
            else
                m_Camera = m_Canvas.worldCamera != null ? m_Canvas.worldCamera : Camera.main;
        }

        originalText = m_TextMeshPro.text;
    }

    // 현재 마우스 위치를 안전하게 가져오는 함수
    private Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        // 새로운 인풋 시스템 방식의 마우스 위치 가져오기
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif
        // 백업용 옛날 방식 (혹시 모를 충돌 방지)
        return Input.mousePosition;
    }

    void Update()
    {
        if (!isMouseOver) return;

        // 에러가 나던 Input.mousePosition 대신 GetMousePosition() 함수를 사용합니다.
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, GetMousePosition(), m_Camera);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];
            if (linkInfo.GetLinkID() == "q1_trigger")
            {
                string colorHex = ColorUtility.ToHtmlStringRGB(hoverColor);
                m_TextMeshPro.text = originalText.Replace("<link=\"q1_trigger\">1+1=2</link>", $"<link=\"q1_trigger\"><color=#{colorHex}>1+1=2</color></link>");
                return;
            }
        }
        m_TextMeshPro.text = originalText;
    }

    // 마우스로 노란 글자를 콕 클릭했을 때 실행
    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, GetMousePosition(), m_Camera);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];

            if (chatManager != null)
            {
                // 짜두신 ChatManager의 OnTextLinkClick 함수를 호출하며 ID를 넘겨줍니다.
                chatManager.OnTextLinkClick(linkInfo.GetLinkID());
                Debug.Log($"ChatManager로 링크 ID 전송 성공: {linkInfo.GetLinkID()}");
            }
            else
            {
                Debug.LogWarning("ChatManager가 연결되어 있지 않습니다!");
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData) { isMouseOver = true; }
    public void OnPointerExit(PointerEventData eventData) { isMouseOver = false; m_TextMeshPro.text = originalText; }
    void OnDisable() { if (m_TextMeshPro != null) m_TextMeshPro.text = originalText; }
}