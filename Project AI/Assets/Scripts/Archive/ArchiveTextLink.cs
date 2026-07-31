using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// 단서(Clue) 시스템과 무관하게, 문단(Paragraph) 텍스트 안에서
/// 특정 구간만 링크처럼 클릭 가능하게 만드는 컴포넌트.
///
/// 사용법:
/// 1. TextMeshProUGUI를 쓰는 문단 오브젝트에 이 컴포넌트를 붙입니다.
///    (TMP_Text의 "Raycast Target"이 켜져 있어야 함)
/// 2. 텍스트 내용 중 링크로 만들고 싶은 부분을 rich text 태그로 감쌉니다.
///    예) "이 사건은 1998년 <link=\"doc_A\">계약서 원본</link>에서 확인된다."
/// 3. 인스펙터의 Link Targets 리스트에 linkID("doc_A")와 그에 대응하는
///    targetLocation(RectTransform)을 등록합니다.
/// 4. 태그로 감싼 구간에 마우스를 올리면 그 부분에만 밑줄이 생기고,
///    클릭하면 해당 패널이 열립니다. 태그 바깥을 클릭/호버하면 아무 반응 없음.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class ArchiveTextLink : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Serializable]
    public class LinkTarget
    {
        [Tooltip("텍스트 안 <link=\"...\"> 태그에 적은 ID와 동일해야 합니다.")]
        public string linkID;
        [Tooltip("이 링크를 클릭했을 때 열릴 패널/이미지 등의 RectTransform")]
        public RectTransform targetLocation;
    }

    [Header("linkID <-> 열릴 패널 매핑 (문단 하나에 링크가 여러 개일 수 있음)")]
    [SerializeField] private List<LinkTarget> linkTargets = new List<LinkTarget>();

    private static readonly Regex LinkTagRegex = new Regex(
        "<link=\"(?<id>[^\"]*)\">(?<content>.*?)</link>",
        RegexOptions.Singleline);

    private TMP_Text tmpText;

    private string rawText;
    private readonly Dictionary<string, (int start, int length)> linkContentRanges = new Dictionary<string, (int, int)>();

    private string currentHoveredLinkID = null;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        CacheRawTextAndRanges();
    }

    /// <summary>
    /// 문단 텍스트를 동적으로 새로 세팅할 때(예: NewsCard/PostDetailPageUI에서 Instantiate 직후)
    /// 이 함수를 호출해서 링크 구간을 다시 파싱하도록 해주세요.
    /// </summary>
    public void RefreshFromCurrentText()
    {
        CacheRawTextAndRanges();
    }

    private void CacheRawTextAndRanges()
    {
        if (tmpText == null) tmpText = GetComponent<TMP_Text>();

        rawText = tmpText.text;
        linkContentRanges.Clear();

        foreach (Match m in LinkTagRegex.Matches(rawText))
        {
            string id = m.Groups["id"].Value;
            Group content = m.Groups["content"];
            if (!linkContentRanges.ContainsKey(id))
            {
                linkContentRanges.Add(id, (content.Index, content.Length));
            }
        }
    }

    // 💡 [변경] Update()+Input.mousePosition 폴링 대신,
    // EventSystem이 직접 넘겨주는 OnPointerMove를 사용합니다.
    public void OnPointerMove(PointerEventData eventData)
    {
        if (tmpText == null) return;

        Canvas canvas = tmpText.canvas;
        Camera eventCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? eventData.pressEventCamera ?? canvas.worldCamera
            : null;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmpText, eventData.position, eventCamera);
        string hoveredID = (linkIndex != -1) ? tmpText.textInfo.linkInfo[linkIndex].GetLinkID() : null;

        if (hoveredID != currentHoveredLinkID)
        {
            currentHoveredLinkID = hoveredID;
            ApplyUnderline(hoveredID);
        }
    }

    private void ApplyUnderline(string linkID)
    {
        if (string.IsNullOrEmpty(linkID) || !linkContentRanges.TryGetValue(linkID, out var range))
        {
            tmpText.text = rawText;
            return;
        }

        string before = rawText.Substring(0, range.start);
        string content = rawText.Substring(range.start, range.length);
        string after = rawText.Substring(range.start + range.length);

        tmpText.text = before + "<u>" + content + "</u>" + after;
    }

    public void OnPointerEnter(PointerEventData eventData) { }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentHoveredLinkID != null)
        {
            currentHoveredLinkID = null;
            tmpText.text = rawText;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (tmpText == null) return;

        Canvas canvas = tmpText.canvas;
        Camera eventCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? eventData.pressEventCamera
            : null;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmpText, eventData.position, eventCamera);
        if (linkIndex == -1) return; // 링크 태그 바깥을 클릭 -> 무시

        string linkID = tmpText.textInfo.linkInfo[linkIndex].GetLinkID();
        OpenByLinkID(linkID);
    }

    private void OpenByLinkID(string linkID)
    {
        LinkTarget match = linkTargets.Find(l => l.linkID == linkID);
        if (match == null || match.targetLocation == null)
        {
            Debug.LogWarning($"[ArchiveTextLink] linkID '{linkID}'에 매칭되는 targetLocation이 등록되어 있지 않습니다. ({gameObject.name})");
            return;
        }

        if (ArchiveManager.Instance != null)
        {
            ArchiveManager.Instance.OpenPanel(match.targetLocation);
        }
        else
        {
            Transform current = match.targetLocation;
            while (current != null)
            {
                if (!current.gameObject.activeSelf) current.gameObject.SetActive(true);
                current = current.parent;
            }
            match.targetLocation.SetAsLastSibling();
        }
    }
}