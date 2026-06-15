using UnityEngine;
using UnityEngine.EventSystems;

// 유니티 공식 이벤트 인터페이스를 사용합니다.
public class UIDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // 💡 중요: UI가 속한 최상위 Canvas를 찾아냅니다.
        canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("UIDragHandler: 부모 오브젝트 중 Canvas를 찾을 수 없습니다!");
        }
    }

    // 1. 패널을 마우스로 딱 클릭한 순간
    public void OnPointerDown(PointerEventData eventData)
    {
        // 클릭한 패널을 UI 레이어 상에서 가장 앞으로 오게 만듭니다. (선택 창 맨 앞으로 띄우기)
        rectTransform.SetAsLastSibling();
    }

    // 2. 마우스를 누른 채로 움직이는 동안 (드래그 전체 과정 추적)
    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // 💡 핵심: 마우스가 패널을 벗어나도 캔버스 전체 화면 기준 좌표로 계산하여 절대 멈추지 않습니다.
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
}