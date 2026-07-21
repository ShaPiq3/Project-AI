using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 아카이브, 뉴스 등 어느 창이든 상관없이 "수집 가능한 이미지" 오브젝트에 붙이세요.
/// 데이터 수집 모드(ImageGenerationManager.Instance.IsCollectingMode)가 켜져 있을 때만 동작합니다.
/// 기존에 그 이미지에 다른 클릭 동작(확대보기 등)이 있어도 상관없도록 별도 리스너로 추가하세요.
/// </summary>
public class CollectibleImageIcon : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("ImageGenSlotItems.csv 의 ImageID 와 동일하게 입력")]
    public string imageID;

    /// <summary>CSV로 동적 생성되는 아이템(예: 뉴스)에서 Instantiate 직후 호출해서 자동 연결할 때 사용</summary>
    public void Init(string id)
    {
        imageID = id;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ImageGenerationManager.Instance == null) return;
        if (!ImageGenerationManager.Instance.IsCollectingMode) return;
        if (string.IsNullOrEmpty(imageID)) return;

        ImageGenerationManager.Instance.RegisterImageToSlot(imageID);
    }
}