using UnityEngine;
using DG.Tweening;

public class Windows11Notification : MonoBehaviour
{
    public RectTransform panelRect;
    private Vector2 hiddenPos = new Vector2(-400, 227);
    private Vector2 visiblePos = new Vector2(186, 227);

    // Start 대신 Awake 사용 권장 (오브젝트가 활성화될 때 위치 초기화)
    private void Awake()
    {
        if (panelRect != null)
            panelRect.anchoredPosition = hiddenPos;
    }

    public void Show()
    {
        if (panelRect != null) panelRect.anchoredPosition = hiddenPos;
        // 1. 여기서 활성화
        gameObject.SetActive(true);

        if (panelRect == null) return;

        panelRect.DOKill();
        panelRect.DOAnchorPos(visiblePos, 0.5f).SetEase(Ease.OutBack);
    }

    public void Hide()
    {
        if (panelRect == null) return;

        panelRect.DOKill();
        panelRect.DOAnchorPos(hiddenPos, 0.3f).SetEase(Ease.InSine)
                 .OnComplete(() => gameObject.SetActive(false)); // 사라진 후 비활성화
    }
}