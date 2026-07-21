using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CSV + 프리팹으로 동적 생성되는 모든 창(뉴스, 커뮤니티, SNS 등)에서 공통으로 사용하는
/// "수집 가능 이미지 자동 등록" 헬퍼. 각 매니저의 Instantiate 루프에서 한 줄만 호출하면 됩니다.
///
/// 예)
///   GameObject go = Instantiate(itemPrefab, content);
///   var controller = go.GetComponent<NewsItemController>();
///   controller.Setup(data);
///   CollectibleImageBinder.Bind(controller.thumbnailImage, data.imageGenID);
///
///   GameObject go2 = Instantiate(communityPostPrefab, content);
///   var post = go2.GetComponent<CommunityPostController>();
///   post.Setup(data2);
///   CollectibleImageBinder.Bind(post.postImage, data2.imageGenID);
///
///   GameObject go3 = Instantiate(snsPostPrefab, content);
///   var sns = go3.GetComponent<SnsPostController>();
///   sns.Setup(data3);
///   CollectibleImageBinder.Bind(sns.attachedImage, data3.imageGenID);
/// </summary>
public static class CollectibleImageBinder
{
    /// <param name="targetImage">클릭 대상이 될 실제 이미지 UI (Image 컴포넌트)</param>
    /// <param name="imageGenID">
    /// 그 창의 CSV에 있는 ImageGenID 컬럼 값.
    /// ImageGenSlotItems.csv 의 ImageID 와 정확히 같아야 함.
    /// 비어있으면(수집 대상이 아닌 일반 게시물/기사) 아무 것도 붙이지 않고 조용히 넘어감.
    /// </param>
    public static void Bind(Image targetImage, string imageGenID)
    {
        if (targetImage == null) return;
        if (string.IsNullOrEmpty(imageGenID)) return; // 수집 대상 아님 -> 스킵 (정상)

        GameObject go = targetImage.gameObject;
        CollectibleImageIcon icon = go.GetComponent<CollectibleImageIcon>();
        if (icon == null) icon = go.AddComponent<CollectibleImageIcon>();
        icon.Init(imageGenID);
    }
}