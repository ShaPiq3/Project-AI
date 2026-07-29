using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 단서(Clue) 시스템과 무관하게, 문단(Paragraph) 텍스트 안에서
/// 특정 구간만 링크처럼 클릭 가능하게 만드는 컴포넌트.
///
/// 사용법:
/// 1. TextMeshProUGUI를 쓰는 문단 오브젝트에 이 컴포넌트를 붙입니다.
///    (TMP_Text의 "Raycast Target"이 켜져 있어야 함)
/// 2. 텍스트 내용 중 링크로 만들고 싶은 부분을 rich text 태그로 감쌉니다.
///    예) "이 사건은 1998년 <link=\"doc_A\">계약서 원본</link>에서 확인된다."
///    -> <link="..."> 태그는 화면에는 보이지 않고, 그 안쪽 글자만 클릭 판정 대상이 됩니다.
/// 3. 인스펙터의 Link Targets 리스트에 linkID("doc_A")와 그에 대응하는
///    targetLocation(RectTransform)을 등록합니다. 한 문단에 링크가 여러 개면
///    리스트에 여러 개 추가하면 됩니다.
/// 4. 문단에서 링크 태그가 아닌 부분을 클릭하면 아무 반응도 하지 않고,
///    태그로 감싼 구간을 클릭했을 때만 해당 패널이 열립니다.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class ArchiveTextLink : MonoBehaviour, IPointerClickHandler
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

    private TMP_Text tmpText;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (tmpText == null) return;

        // ScreenSpace-Overlay 캔버스면 카메라가 필요 없고, 그 외에는 클릭한 이벤트의 카메라를 사용합니다.
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
            // ArchiveManager가 씬에 없는 경우를 대비한 최소한의 폴백
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