using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

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

        ActivateHierarchy(target); // 💡 계층 활성화 및 최상위 창만 맨 앞으로 이동

        ScrollRect parentScrollRect = target.GetComponentInParent<ScrollRect>();
        StartCoroutine(OpenClueSourceRoutine(target, parentScrollRect));

        return true;
    }

    private IEnumerator OpenClueSourceRoutine(RectTransform target, ScrollRect parentScrollRect)
    {
        ScrollRect[] innerScrollRects = target.GetComponentsInChildren<ScrollRect>(true);

        // 💡 팝업 스케일 애니메이션이 끝날 때까지 넉넉히 기다림
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < 3; i++)
        {
            yield return null;

            foreach (var sr in innerScrollRects)
            {
                if (sr.content != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(sr.content);
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        foreach (var sr in innerScrollRects)
        {
            sr.verticalNormalizedPosition = 1f;
            sr.horizontalNormalizedPosition = 0f;
        }

        if (parentScrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentScrollRect.content);
            Canvas.ForceUpdateCanvases();
            ScrollToTarget(parentScrollRect, target);
        }
    }

    private System.Collections.IEnumerator ScrollToTargetNextFrame(ScrollRect scrollRect, RectTransform target)
    {
        yield return null; // 한 프레임 대기
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        Canvas.ForceUpdateCanvases();
        ScrollToTarget(scrollRect, target);
    }

    /// <summary>
    /// 스크롤뷰 안의 특정 자식(target)이 보이도록 Content 위치를 이동시킵니다.
    /// </summary>
    private void ScrollToTarget(ScrollRect scrollRect, RectTransform target)
    {
        if (scrollRect == null || target == null || scrollRect.viewport == null || scrollRect.content == null) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
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

    /// <summary>
    /// target부터 최상위까지 올라가며, 비활성화된 부모 오브젝트를 전부 활성화하고
    /// 단서가 포함된 최상위 창(Window)만 화면 맨 앞으로 가져옵니다.
    /// </summary>
    private void ActivateHierarchy(RectTransform target)
    {
        Transform current = target;
        Transform topWindow = null;

        while (current != null)
        {
            // 1. 비활성화된 부모 패널들은 전부 켜줍니다.
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            // 2. InGameWindowManager가 붙어있거나 Canvas 바로 아래에 있는 최상위 창 패널을 탐색합니다.
            if (current.GetComponent<InGameWindowManager>() != null || (current.parent != null && current.parent.GetComponent<Canvas>() != null))
            {
                if (topWindow == null)
                {
                    topWindow = current;
                }
            }

            current = current.parent;
        }

        // 3. 내부 자식 요소(Row_Logo 등)의 순서는 건드리지 않고, '최상위 창'만 맨 앞으로 올립니다.
        if (topWindow != null)
        {
            topWindow.SetAsLastSibling();
        }
        else if (target != null)
        {
            target.root.SetAsLastSibling();
        }
    }

    private void ResetInternalScrollViews(RectTransform target)
    {
        ScrollRect[] innerScrollRects = target.GetComponentsInChildren<ScrollRect>(true);
        foreach (var sr in innerScrollRects)
        {
            if (sr.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(sr.content);
            }
            sr.verticalNormalizedPosition = 1f;   // 맨 위
            sr.horizontalNormalizedPosition = 0f; // 맨 왼쪽
        }
    }
}