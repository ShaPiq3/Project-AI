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
    public Color hoverColor = Color.yellow;

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
                // 💡 [추가] 링크 ID(예: q1_trigger)에서 단서 번호 숫자를 쏙 뽑아냅니다.
                System.Text.RegularExpressions.Match numMatch = System.Text.RegularExpressions.Regex.Match(linkId, @"\d+");
                if (numMatch.Success)
                {
                    int clueNumber = int.Parse(numMatch.Value);

                    // 만약 마우스를 올린 단서 번호가 현재 대화 진행 레벨(currentClueLevel)보다 낮다면?
                    if (clueNumber < chatSystem.currentClueLevel)
                    {
                        // ➔ 이미 잠긴 과거 단서이므로 원본 텍스트로 고정시키고 노란 불빛 연출을 차단합니다!
                        m_TextMeshPro.text = originalText;
                        return;
                    }
                }

                // 💡 [기존 유지] 해금 목록에 있고 현재 진행 레벨과 맞을 때만 불이 켜집니다.
                if (chatSystem.unlockedClues.Contains(linkId))
                {
                    string colorHex = ColorUtility.ToHtmlStringRGB(hoverColor);
                    string targetSource = $"<link=\"{linkId}\">{linkText}</link>";
                    string replaceTarget = $"<link=\"{linkId}\"><color=#{colorHex}>{linkText}</color></link>";

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
                // 💡 [추가] 클릭 순간에도 더블 체크! 이미 지나간 레벨의 단서라면 클릭 반응도 완전 차단합니다.
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