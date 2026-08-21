using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Menu_Item_ClueSearch(사이드바 "단서 수집" 버튼)에 붙여서, 마우스가 버튼 위에
/// 있는 동안 DataLogManager에 알려 강조 애니메이션(흔들림)을 잠깐 멈추게 한다.
/// </summary>
public class ClueCollectButtonHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        DataLogManager.Instance?.SetClueButtonHovered(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DataLogManager.Instance?.SetClueButtonHovered(false);
    }
}
