using UnityEngine;
using UnityEngine.EventSystems;

// 유니티 공식 이벤트 인터페이스를 사용합니다.
public class UIDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private WindowManager windowManager; // 🌟 1. WindowManager 참조 변수 추가

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // 💡 중요: UI가 속한 최상위 Canvas를 찾아냅니다.
        canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("UIDragHandler: 부모 오브젝트 중 Canvas를 찾을 수 없습니다!");
        }

        // 🌟 2. 씬에 배치된 WindowManager를 자동으로 찾아 연결합니다.
        windowManager = FindAnyObjectByType<WindowManager>();
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

        // 🌟 3. 마우스 움직임에 따른 '다음 이동 예정 좌표'를 먼저 계산합니다.
        Vector2 nextPosition = rectTransform.anchoredPosition + (eventData.delta / canvas.scaleFactor);

        // 🌟 4. WindowManager가 존재한다면, 계산된 좌표를 '벽 영역 제한' 함수에 통과시킵니다.
        if (windowManager != null)
        {
            nextPosition = windowManager.ClampWindowPosition(rectTransform, nextPosition);
        }

        // 🌟 5. 최종적으로 제한된 안전한 좌표만 반영합니다.
        rectTransform.anchoredPosition = nextPosition;
    }
}