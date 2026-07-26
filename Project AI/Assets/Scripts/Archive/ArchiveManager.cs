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

        ActivateHierarchy(target); // 💡 여기선 SetActive + SetAsLastSibling만

        ScrollRect parentScrollRect = target.GetComponentInParent<ScrollRect>();
        StartCoroutine(OpenClueSourceRoutine(target, parentScrollRect));

        return true;
    }

    private IEnumerator OpenClueSourceRoutine(RectTransform target, ScrollRect parentScrollRect)
    {
        ScrollRect[] innerScrollRects = target.GetComponentsInChildren<ScrollRect>(true);

        // 💡 [변경] 팝업 스케일 애니메이션이 끝날 때까지 넉넉히 기다림
        //    (RestoreWindow의 AnimatePopUp이 스케일을 계속 바꾸는 동안은
        //     스크롤/레이아웃 계산이 매 프레임 어긋남)
        yield return new WaitForSeconds(0.5f); // 필요시 값 조절 (팝업 연출 시간보다 조금 더 길게)

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

        // 💡 [변경] Canvas.ForceUpdateCanvases()만으로는 Layout Group이 
        // 방금 활성화된 자식들의 위치를 완전히 재계산했다고 보장할 수 없으므로,
        // Content 전체를 강제로 즉시 리빌드해서 target.localPosition이 최신 값이 되도록 함
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
    /// target부터 최상위까지 올라가며, 비활성화된 부모 오브젝트(개별 문서 창 등)를 전부 활성화합니다.
    /// 문서들이 서로 독립적이라 여러 개 동시에 열려있어도 문제없는 구조에 적합합니다.
    /// </summary>
    private void ActivateHierarchy(RectTransform target)
    {
        Transform current = target;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            current.SetAsLastSibling();
            current = current.parent;
        }
        // 💡 [삭제] 여기서 즉시 리셋하지 않음 - 코루틴에서 애니메이션 끝난 뒤 처리
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