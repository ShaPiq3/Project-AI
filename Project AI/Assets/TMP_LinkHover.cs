using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

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
    [Tooltip("알파값(A)을 120 정도로 낮추면 글자가 비치는 부드러운 형광펜이 됩니다.")]
    public Color32 hoverColor = new Color32(255, 255, 0, 128); // 기본값: 반투명 노란색

    [Header("채팅 매니저 연동")]
    public NewChatSystem chatSystem;

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
    }

    private Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif
        return Input.mousePosition;
    }

    void Update()
    {
        if (!isMouseOver || m_TextMeshPro == null) return;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, GetMousePosition(), m_Camera);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];
            string linkId = linkInfo.GetLinkID();
            string linkText = linkInfo.GetLinkText();

            if (chatSystem != null)
            {
                System.Text.RegularExpressions.Match numMatch = System.Text.RegularExpressions.Regex.Match(linkId, @"\d+");
                if (numMatch.Success)
                {
                    int clueNumber = int.Parse(numMatch.Value);

                    if (clueNumber < chatSystem.currentClueLevel)
                    {
                        m_TextMeshPro.text = originalText;
                        return;
                    }
                }

                if (chatSystem.unlockedClues.Contains(linkId))
                {
                    // 💡 [변경] Color32 구조체를 활용해 알파값(RGBA)을 포함한 HEX 코드를 만듭니다.
                    string colorHex = string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", hoverColor.r, hoverColor.g, hoverColor.b, hoverColor.a);

                    string targetSource = $"<link=\"{linkId}\">{linkText}</link>";

                    // 💡 [변경] <color> 대신 배경을 칠해주는 <mark> 태그로 교체했습니다.
                    string replaceTarget = $"<link=\"{linkId}\"><mark={colorHex}>{linkText}</mark></link>";

                    m_TextMeshPro.text = originalText.Replace(targetSource, replaceTarget);
                    return;
                }
            }
        }

        m_TextMeshPro.text = originalText;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (m_TextMeshPro == null) return;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, GetMousePosition(), m_Camera);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];
            string clickedLinkId = linkInfo.GetLinkID();

            if (chatSystem != null)
            {
                System.Text.RegularExpressions.Match numMatch = System.Text.RegularExpressions.Regex.Match(clickedLinkId, @"\d+");
                if (numMatch.Success)
                {
                    int clueNumber = int.Parse(numMatch.Value);
                    if (clueNumber < chatSystem.currentClueLevel)
                    {
                        Debug.Log($"[TMP_LinkHover] 클릭 실패: '{clickedLinkId}'는 이미 잠긴 과거의 단서입니다.");
                        return;
                    }
                }

                if (chatSystem.unlockedClues.Contains(clickedLinkId))
                {
                    chatSystem.OnTextLinkClick(clickedLinkId, "");
                    Debug.Log($"[TMP_LinkHover] 해금된 단서 클릭 성공, 채팅창으로 ID 전송: {clickedLinkId}");
                }
                else
                {
                    Debug.Log($"[TMP_LinkHover] 클릭 차단: '{clickedLinkId}' 단서는 아직 채팅창에서 언급(해금)되지 않았습니다.");
                }
            }
            else
            {
                Debug.LogWarning("[TMP_LinkHover] 연동된 NewChatSystem 컴포넌트가 없습니다!");
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_TextMeshPro == null) return;
        isMouseOver = true;
        originalText = m_TextMeshPro.text;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOver = false;
        if (m_TextMeshPro != null) m_TextMeshPro.text = originalText;
    }

    void OnDisable()
    {
        if (m_TextMeshPro != null && !string.IsNullOrEmpty(originalText))
            m_TextMeshPro.text = originalText;
    }
}
