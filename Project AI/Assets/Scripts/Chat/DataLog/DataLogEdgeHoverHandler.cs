using UnityEngine;
using UnityEngine.EventSystems;

public class DataLogEdgeHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        DataLogManager.Instance?.OnEdgeHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DataLogManager.Instance?.OnEdgeHoverExit();
    }
}