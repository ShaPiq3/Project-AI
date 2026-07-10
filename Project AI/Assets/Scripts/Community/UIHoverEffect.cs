using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 필수!

// 이 스크립트가 붙은 오브젝트는 Image 컴포넌트를 필수 요구함
[RequireComponent(typeof(Image))]
public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image _backgroundImage;

    // 호버 시 변경할 배경 색상 (인스펙터에서 설정)
    // 예: 아주 연한 민트색이나 회색
    public Color hoverColor = new Color(0.9f, 0.95f, 1f, 1f);

    // 원래 기본 배경 색상
    private Color _normalColor;

    void Awake()
    {
        // 내 Image 컴포넌트 가져오기
        _backgroundImage = GetComponent<Image>();
        // 시작 시 원래 색상 저장
        _normalColor = _backgroundImage.color;
    }

    // 마우스가 영역 안으로 들어왔을 때 호출됨 (Hover On)
    public void OnPointerEnter(PointerEventData eventData)
    {
        _backgroundImage.color = hoverColor;
    }

    // 마우스가 영역 밖으로 나갔을 때 호출됨 (Hover Off)
    public void OnPointerExit(PointerEventData eventData)
    {
        _backgroundImage.color = _normalColor;
    }
}