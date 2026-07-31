using UnityEngine;
using UnityEngine.EventSystems;

public class ImageGenEdgeHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        ImageGenerationManager.Instance?.OnEdgeHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ImageGenerationManager.Instance?.OnEdgeHoverExit();
    }
}