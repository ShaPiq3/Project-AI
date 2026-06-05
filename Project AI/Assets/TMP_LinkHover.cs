using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Unity.VisualScripting;

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

    // 현재 클릭되어 하이라이트가 유지되어야 하는 링크 ID를 저장합니다.
    private string clickedActiveLinkId = null;

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

    void Start()
    {
        if (m_TextMeshPro != null)
        {
            originalText = m_TextMeshPro.text;
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
        if (m_TextMeshPro == null || string.IsNullOrEmpty(originalText)) return;

        // 💡 [핵심 추가] 고정된 단서가 있는데, 채팅 시스템의 ClueLevel이 증가했다면(다음 단서 대화가 떴다면) 고정을 해제하고 불을 끕니다.
        if (!string.IsNullOrEmpty(clickedActiveLinkId) && chatSystem != null)
        {
            System.Text.RegularExpressions.Match numMatch = System.Text.RegularExpressions.Regex.Match(clickedActiveLinkId, @"\d+");
            if (numMatch.Success)
            {
                int linkClueNumber = int.Parse(numMatch.Value);
                // 만약 시스템의 현재 레벨이 이 링크의 레벨보다 높아졌다면 = 다음 단계로 넘어갔다는 뜻
                if (linkClueNumber < chatSystem.currentClueLevel)
                {
                    clickedActiveLinkId = null;
                    m_TextMeshPro.text = originalText;
                    Debug.Log($"[TMP_LinkHover] 다음 단서 활성화 대화 감지: '{clickedActiveLinkId}' 하이라이트 자동 종료.");
                    return;
                }
            }
        }

        // 1. 이미 클릭되어 고정된 링크가 있다면 Update의 호버 처리를 건너뜁니다.
        if (!string.IsNullOrEmpty(clickedActiveLinkId)) return;

        // 2. 마우스가 위에 있을 때만 충돌하는 링크를 찾아서 호버 하이라이트 처리
        int linkIndex = isMouseOver ? TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, GetMousePosition(), m_Camera) : -1;

        if (linkIndex != -1)
        {
            string hoverLinkId = m_TextMeshPro.textInfo.linkInfo[linkIndex].GetLinkID();

            // 해금된 단서 목록에 포함되어 있을 때만 작동
            if (chatSystem != null && chatSystem.unlockedClues.Contains(hoverLinkId))
            {
                ApplyHighlight(hoverLinkId);
                return;
            }
        }

        // 3. 아무것도 해당하지 않으면 원본으로 복구
        if (m_TextMeshPro.text != originalText)
        {
            m_TextMeshPro.text = originalText;
        }
    }

    // 공용 하이라이트 적용 함수 (originalText를 복사하여 태그를 삽입)
    private void ApplyHighlight(string linkId)
    {
        int findIndex = GetLinkIndexById(linkId);
        if (findIndex != -1)
        {
            string colorHex = string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", hoverColor.r, hoverColor.g, hoverColor.b, hoverColor.a);
            TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[findIndex];
            string linkText = linkInfo.GetLinkText();

            string targetSource = $"<link=\"{linkId}\">{linkText}</link>";
            string replaceTarget = $"<link=\"{linkId}\"><mark={colorHex}>{linkText}</mark></link>";

            m_TextMeshPro.text = originalText.Replace(targetSource, replaceTarget);
        }
    }

    private int GetLinkIndexById(string id)
    {
        for (int i = 0; i < m_TextMeshPro.textInfo.linkCount; i++)
        {
            if (m_TextMeshPro.textInfo.linkInfo[i].GetLinkID() == id)
                return i;
        }
        return -1;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (m_TextMeshPro == null) return;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, eventData.position, m_Camera);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];
            string clickedLinkId = linkInfo.GetLinkID();

            if (chatSystem != null)
            {

                if (chatSystem.completedClues.Contains(clickedLinkId))
                {
                    Debug.Log($"[TMP_LinkHover] 이미 완료된 단서입니다: {clickedLinkId}");
                    return;
                }

                // 이미 대화 단계가 지나간 과거 단계의 링크라면 클릭 방지 (원하는 경우 제거 가능)
                System.Text.RegularExpressions.Match numMatch = System.Text.RegularExpressions.Regex.Match(clickedLinkId, @"\d+");
                if (numMatch.Success && int.Parse(numMatch.Value) < chatSystem.currentClueLevel)
                {
                    return;
                }

                if (chatSystem.unlockedClues.Contains(clickedLinkId))
                {
                    chatSystem.completedClues.Add(clickedLinkId);
                    // 클릭 성공 시 즉시 ID를 등록하고 하이라이트 텍스트를 고정
                    clickedActiveLinkId = clickedLinkId;
                    ApplyHighlight(clickedLinkId);

                    chatSystem.OnTextLinkClick(clickedLinkId, "");
                    Debug.Log($"[TMP_LinkHover] 단서 클릭 성공, 다음 단계 대화 전까지 고정: {clickedLinkId}");
                }
            }
        }
    }

    private bool isAlreadyPlayingHover = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;

        if (string.IsNullOrEmpty(originalText))
        {
            originalText = m_TextMeshPro.text;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOver = false;
        isAlreadyPlayingHover = false;
    }

    void OnDisable()
    {
        clickedActiveLinkId = null;
        if (m_TextMeshPro != null && !string.IsNullOrEmpty(originalText))
            m_TextMeshPro.text = originalText;
    }
}