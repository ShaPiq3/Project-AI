using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Text.RegularExpressions;
using System.Collections.Generic;

[RequireComponent(typeof(TMP_Text))]
public class PanelLinkParagraphEffect : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private static readonly Regex LinkTagRegex = new Regex(
        "<link=\"(?<id>[^\"]*)\">(?<content>.*?)</link>",
        RegexOptions.Singleline);

    private TMP_Text tmpText;
    private string rawText;

    private readonly List<(string id, int start, int length)> linkRanges = new List<(string, int, int)>();
    private string currentHoveredLinkID;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    public void Setup(string textWithTags)
    {
        if (tmpText == null) tmpText = GetComponent<TMP_Text>();

        rawText = textWithTags;
        tmpText.richText = true;
        tmpText.raycastTarget = true;
        tmpText.text = rawText;

        linkRanges.Clear();
        foreach (Match m in LinkTagRegex.Matches(rawText))
        {
            linkRanges.Add((m.Groups["id"].Value, m.Groups["content"].Index, m.Groups["content"].Length));
        }

        // 💡 디버그용: 태그가 실제로 살아서 들어왔는지 확인하고 싶으면 주석 해제
        // Debug.Log($"[PanelLinkParagraphEffect] rawText='{rawText}', linkCount={linkRanges.Count}");
    }

    // 💡 [변경] Update()+Input.mousePosition 폴링 대신, EventSystem이 직접 넘겨주는
    // OnPointerMove를 사용합니다. New Input System / Old Input System 어느 쪽이든 동작합니다.
    public void OnPointerMove(PointerEventData eventData)
    {
        if (tmpText == null || linkRanges.Count == 0) return;

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
        if (string.IsNullOrEmpty(linkID))
        {
            tmpText.text = rawText;
            return;
        }

        int idx = linkRanges.FindIndex(r => r.id == linkID);
        if (idx == -1)
        {
            tmpText.text = rawText;
            return;
        }

        var range = linkRanges[idx];
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
            ? eventData.pressEventCamera : null;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmpText, eventData.position, eventCamera);
        if (linkIndex == -1) return;

        string linkID = tmpText.textInfo.linkInfo[linkIndex].GetLinkID();
        PanelLinkManager.Instance?.OpenPanel(linkID);
    }
}