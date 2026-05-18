using UnityEngine;
using UnityEngine.EventSystems; // 마우스 이벤트를 받기 위해 필수

// 드래그 관련 유니티 인터페이스(IDragHandler, IPointerDownHandler)를 구현합니다.
public class UIDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    // 이동시킬 진짜 목표 오브젝트 (PopupPanel)
    [SerializeField] private RectTransform targetRectTransform;

    private Canvas canvas;

    private void Awake()
    {
        // 팝업이 속한 최상위 Canvas를 자동으로 찾습니다.
        canvas = GetComponentInParent<Canvas>();

        // 만약 타겟을 지정하지 않았다면, 이 스크립트가 붙은 오브젝트 자신을 타겟으로 설정합니다.
        if (targetRectTransform == null)
        {
            targetRectTransform = GetComponent<RectTransform>();
        }
    }

    // 마우스 드래그 중일 때 매 프레임 실행되는 함수
    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null || targetRectTransform == null) return;

        // 마우스의 움직임 양(delta)을 캔버스 스케일에 맞춰 계산하여 팝업 위치를 이동시킵니다.
        targetRectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    // 팝업을 클릭했을 때 화면 맨 앞으로 오게 만드는 기능 (선택 사항)
    public void OnPointerDown(PointerEventData eventData)
    {
        // 하이어라키 상에서 맨 아래로 내리면 화면 최상단에 그려집니다.
        targetRectTransform.SetAsLastSibling();
    }
}
