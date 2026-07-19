using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 아카이브(텍스트/이미지가 제각각 섞여있고, 스크롤뷰가 있는 문서도 없는 문서도 있는 구조)를 위한 매니저.
/// 엑셀/CSV로 자동 생성되지 않고 수동으로 배치된 콘텐츠이므로,
/// 각 단서 태그 컴포넌트(ClueTextHoverEffect, ArchiveClueImage)가
/// 활성화될 때 스스로 자기 위치를 이 매니저에 등록합니다.
/// </summary>
public class ArchiveManager : MonoBehaviour
{
    public static ArchiveManager Instance { get; private set; }

    [Header("이 아카이브 창 자체를 여닫는 InGameWindowManager (선택 사항)")]
    [SerializeField] private InGameWindowManager archiveWindowManager;

    // clueID -> 그 단서가 붙어있는 UI 오브젝트의 RectTransform
    private Dictionary<string, RectTransform> clueLocations = new Dictionary<string, RectTransform>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// 단서 태그 컴포넌트가 자기 자신의 위치를 등록할 때 호출합니다.
    /// </summary>
    public void RegisterClueLocation(string clueID, RectTransform location)
    {
        if (string.IsNullOrEmpty(clueID) || location == null) return;
        clueLocations[clueID] = location;
    }

    public void UnregisterClueLocation(string clueID)
    {
        if (string.IsNullOrEmpty(clueID)) return;
        clueLocations.Remove(clueID);
    }

    /// <summary>
    /// DataLogManager가 "원본 보기"를 요청할 때 호출.
    /// 등록된 위치를 찾아 보여줍니다.
    /// 그 위치가 스크롤뷰 안에 있으면 스크롤을 이동시키고,
    /// 스크롤뷰가 없는 문서라면 스크롤 없이 창을 열어주는 것만으로 처리합니다.
    /// </summary>
    public bool TryOpenClueSource(string clueID)
    {
        if (string.IsNullOrEmpty(clueID)) return false;
        if (!clueLocations.TryGetValue(clueID, out RectTransform target) || target == null) return false;

        if (archiveWindowManager != null)
        {
            archiveWindowManager.RestoreWindow();
        }

        // 💡 [변경] 단서마다 스크롤뷰가 있는지 없는지 다를 수 있으므로,
        // 매번 자동으로 부모 계층에서 ScrollRect를 찾아봅니다.
        // 없으면(스크롤뷰 없는 문서) 스크롤 없이 창만 열어주는 걸로 충분합니다.
        ScrollRect parentScrollRect = target.GetComponentInParent<ScrollRect>();
        if (parentScrollRect != null)
        {
            ScrollToTarget(parentScrollRect, target);
        }

        return true;
    }

    /// <summary>
    /// 스크롤뷰 안의 특정 자식(target)이 보이도록 Content 위치를 이동시킵니다.
    /// </summary>
    private void ScrollToTarget(ScrollRect scrollRect, RectTransform target)
    {
        if (scrollRect == null || target == null || scrollRect.viewport == null || scrollRect.content == null) return;

        Canvas.ForceUpdateCanvases();

        Vector2 viewportLocalPosition = scrollRect.viewport.localPosition;
        Vector2 childLocalPosition = target.localPosition;

        Vector2 result = new Vector2(
            0f,
            0f - (viewportLocalPosition.y + childLocalPosition.y)
        );

        float offsetY = target.rect.height * 0.5f;

        scrollRect.content.anchoredPosition = new Vector2(
            scrollRect.content.anchoredPosition.x,
            result.y - offsetY
        );
    }
}