using System.Collections.Generic;
using UnityEngine;

public class PanelLinkManager : MonoBehaviour
{
    public static PanelLinkManager Instance { get; private set; }

    [System.Serializable]
    public class PanelLinkEntry
    {
        [Tooltip("CSV에 적는 값과 정확히 일치해야 합니다.")]
        public string linkID;

        [Tooltip("열/닫을 패널 오브젝트")]
        public GameObject panelObject;

        [Tooltip("WindowManager가 위치를 재배치해줄 창이면 연결 (고정 UI라면 비워둬도 됨)")]
        public RectTransform panelRect;
    }

    [SerializeField] private List<PanelLinkEntry> panelLinks = new List<PanelLinkEntry>();
    [SerializeField] private WindowManager windowManager;

    private Dictionary<string, PanelLinkEntry> lookup = new Dictionary<string, PanelLinkEntry>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        foreach (var entry in panelLinks)
        {
            if (string.IsNullOrEmpty(entry.linkID)) continue;
            if (!lookup.ContainsKey(entry.linkID))
                lookup.Add(entry.linkID, entry);
            else
                Debug.LogWarning($"[PanelLinkManager] linkID 중복: '{entry.linkID}'");
        }
    }

    /// <summary>CSV에 적힌 linkID가 실제로 등록되어 있는지 확인 (호버 밑줄 표시 여부 판단용)</summary>
    public bool HasLink(string linkID)
    {
        return !string.IsNullOrEmpty(linkID) && lookup.ContainsKey(linkID);
    }

    /// <summary>해당 linkID의 패널을 엽니다. "news:"/"post:" 접두사면 동적 기사/게시글로 라우팅합니다.</summary>
    public bool OpenPanel(string linkID)
    {
        if (string.IsNullOrEmpty(linkID)) return false;

        // 💡 [추가] 동적으로 생성되는 뉴스/커뮤니티 상세 패널 라우팅
        if (linkID.StartsWith("news:"))
        {
            string idPart = linkID.Substring("news:".Length);
            if (int.TryParse(idPart, out int newsID) && NewsListManager.Instance != null)
            {
                return NewsListManager.Instance.OpenNewsByID(newsID);
            }
            Debug.LogWarning($"[PanelLinkManager] 잘못된 news 링크 형식: '{linkID}'");
            return false;
        }

        if (linkID.StartsWith("post:") || linkID.StartsWith("community:"))
        {
            string prefix = linkID.StartsWith("post:") ? "post:" : "community:";
            string idPart = linkID.Substring(prefix.Length);
            if (int.TryParse(idPart, out int postID) && CommunityManager.Instance != null)
            {
                return CommunityManager.Instance.OpenPostByID(postID);
            }
            Debug.LogWarning($"[PanelLinkManager] 잘못된 post 링크 형식: '{linkID}'");
            return false;
        }

        // 기존 방식: 씬에 고정으로 등록해둔 패널
        if (!lookup.TryGetValue(linkID, out var entry) || entry.panelObject == null)
        {
            Debug.LogWarning($"[PanelLinkManager] linkID '{linkID}' 에 해당하는 패널을 찾을 수 없습니다.");
            return false;
        }

        entry.panelObject.SetActive(true);
        entry.panelObject.transform.SetAsLastSibling();

        if (windowManager != null && entry.panelRect != null)
        {
            windowManager.RepositionPopupWindow(entry.panelRect);
        }

        var anim = entry.panelObject.GetComponent<PopupSpawnAnimation>();
        if (anim != null) anim.PlayPopAnimation();

        return true;
    }
}