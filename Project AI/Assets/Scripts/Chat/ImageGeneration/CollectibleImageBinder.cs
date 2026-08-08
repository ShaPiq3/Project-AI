using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CSV + 프리팹으로 동적 생성되는 각종 창(뉴스, 커뮤니티, SNS 등)에서 공통으로 사용하는
/// "이미지 생성 수집 이미지 자동 등록" 헬퍼. 각 매니저가 Instantiate 직후에 한 줄만 호출하면 됩니다.
///
/// 예)
///   GameObject go = Instantiate(itemPrefab, content);
///   var controller = go.GetComponent<NewsItemController>();
///   controller.Setup(data);
///   CollectibleImageBinder.Bind(controller.thumbnailImage, data.imageGenID);
/// </summary>
public static class CollectibleImageBinder
{
    /// <param name="targetImage">클릭 대상이 될 실제 이미지 UI (Image 컴포넌트)</param>
    /// <param name="imageGenID">
    /// 이 칸의 CSV에 있는 ImageGenID 컬럼 값. ImageGenSlotItems.csv의 ImageID와 정확히 같아야 함.
    /// 💡 [변경] 비어있어도(수집 대상이 아닌 일반 이미지여도) 항상 CollectibleImageIcon을 붙입니다.
    /// 단서 수집 모드에서 모든 이미지가 동일하게 반응하고, 실제 등록 가능 여부는 클릭 시 스캔 판정으로 구분됩니다.
    /// </param>
    public static void Bind(Image targetImage, string imageGenID)
    {
        if (targetImage == null) return;

        GameObject go = targetImage.gameObject;
        CollectibleImageIcon icon = go.GetComponent<CollectibleImageIcon>();
        if (icon == null) icon = go.AddComponent<CollectibleImageIcon>();
        icon.Configure(imageGenID);
    }
}
