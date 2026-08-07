using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 💡 뉴스/SNS/커뮤니티/아카이브의 모든 텍스트/이미지가 클릭에 반응하게 되면서,
/// "이걸 클릭했더니 실제로 수집 가능한 단서인지 아닌지"를 즉시 알려주는
/// 스캔 판정 팝업을 담당하는 싱글톤입니다.
///
/// 별도 프리팹 없이, 클릭된 지점 근처에 작은 UI 오브젝트를 코드로 생성합니다
/// (CollectibleImageIcon의 하이라이트 오버레이 자동 생성 방식과 동일한 패턴).
/// 최종 아이콘/사운드는 비어있어도(placeholder) 동작하며, 인스펙터에서 교체 가능합니다.
/// </summary>
public class ClueScanEffectController : MonoBehaviour
{
    public static ClueScanEffectController Instance { get; private set; }

    [Header("연출 타이밍")]
    [SerializeField] private float popInDuration = 0.15f;
    [SerializeField] private float scanDuration = 0.35f;
    [SerializeField] private float holdDuration = 0.9f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    [Header("판정 결과별 표시 (비워두면 텍스트/색상 placeholder 사용)")]
    [SerializeField] private Sprite collectibleIconSprite;
    [SerializeField] private Sprite alreadyCollectedIconSprite;
    [SerializeField] private Sprite notCollectibleIconSprite;

    [SerializeField] private Color collectibleColor = new Color(0.4f, 1f, 0.55f, 1f);
    [SerializeField] private Color alreadyCollectedColor = new Color(0.75f, 0.8f, 1f, 1f);
    [SerializeField] private Color notCollectibleColor = new Color(1f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color scanningColor = new Color(1f, 1f, 1f, 0.9f);

    [SerializeField] private string collectibleLabel = "확보";
    [SerializeField] private string alreadyCollectedLabel = "이미 확보됨";
    [SerializeField] private string notCollectibleLabel = "대상 아님";

    [Header("사운드 (선택, 비워두면 재생 안 함)")]
    [SerializeField] private AudioClip collectibleSfx;
    [SerializeField] private AudioClip alreadyCollectedSfx;
    [SerializeField] private AudioClip notCollectibleSfx;
    [SerializeField] private AudioSource sfxSource;

    [Header("팝업을 그릴 Canvas (비워두면 씬에서 자동으로 하나 찾음)")]
    [Tooltip("여러 Canvas가 있는 씬이라면, 항상 최상단에 보여야 하므로 정렬 순서가 가장 높은 Canvas를 직접 연결하는 것을 권장합니다.")]
    [SerializeField] private Canvas overlayCanvasOverride;

    /// <summary>클릭 후 이 시간(초) 동안은 같은 요소를 다시 클릭해도 무시하도록, 호출부에서 참고할 총 연출 길이.</summary>
    public float TotalEffectDuration => popInDuration + scanDuration + holdDuration + fadeOutDuration;

    private Canvas overlayCanvas;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    private Canvas GetOverlayCanvas()
    {
        if (overlayCanvasOverride != null) return overlayCanvasOverride;
        if (overlayCanvas != null) return overlayCanvas;
        overlayCanvas = FindAnyObjectByType<Canvas>();
        return overlayCanvas;
    }

    /// <summary>
    /// anchor 근처에 스캔 판정 팝업을 띄웁니다. anchor는 클릭된 텍스트/이미지의 RectTransform입니다.
    /// </summary>
    public void PlayScanEffect(RectTransform anchor, ClueIdentifyResult result)
    {
        if (anchor == null) return;

        Canvas canvas = GetOverlayCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("[ClueScanEffectController] 오버레이를 그릴 Canvas를 찾지 못했습니다.");
            return;
        }

        GameObject popupGo = new GameObject("ClueScanPopup");
        popupGo.transform.SetParent(canvas.transform, false);
        popupGo.transform.SetAsLastSibling();

        RectTransform rect = popupGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160f, 48f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.position = GetAnchorTopWorldPosition(anchor);

        CanvasGroup canvasGroup = popupGo.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        Image bg = popupGo.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(popupGo.transform, false);
        RectTransform iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(8f, 0f);
        iconRect.sizeDelta = new Vector2(24f, 24f);
        Image icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        iconGo.SetActive(false);

        GameObject labelGo = new GameObject("Label");
        labelGo.transform.SetParent(popupGo.transform, false);
        RectTransform labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 22f;
        label.text = "스캔 중...";
        label.color = scanningColor;

        popupGo.transform.localScale = Vector3.one * 0.5f;

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(canvasGroup.DOFade(1f, popInDuration));
        seq.Join(popupGo.transform.DOScale(1f, popInDuration).SetEase(Ease.OutBack));
        seq.AppendInterval(scanDuration);
        seq.AppendCallback(() => ApplyResultVisual(bg, icon, label, result));
        seq.Append(popupGo.transform.DOPunchScale(Vector3.one * 0.12f, 0.2f, 4, 0.5f));
        seq.AppendInterval(holdDuration);
        seq.Append(canvasGroup.DOFade(0f, fadeOutDuration));
        seq.OnComplete(() =>
        {
            if (popupGo != null) Destroy(popupGo);
        });
    }

    private Vector3 GetAnchorTopWorldPosition(RectTransform anchor)
    {
        Vector3[] corners = new Vector3[4];
        anchor.GetWorldCorners(corners); // [1] = 좌상단
        Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
        return topCenter + new Vector3(0f, 12f, 0f);
    }

    private void ApplyResultVisual(Image bg, Image icon, TextMeshProUGUI label, ClueIdentifyResult result)
    {
        Sprite sprite;
        string glyph;
        string text;
        Color tint;
        AudioClip sfx;

        switch (result)
        {
            case ClueIdentifyResult.Collectible:
                sprite = collectibleIconSprite; glyph = "✓"; text = collectibleLabel;
                tint = collectibleColor; sfx = collectibleSfx;
                break;

            case ClueIdentifyResult.AlreadyCollected:
                sprite = alreadyCollectedIconSprite; glyph = "◎"; text = alreadyCollectedLabel;
                tint = alreadyCollectedColor; sfx = alreadyCollectedSfx;
                break;

            case ClueIdentifyResult.NotCollectible:
            default:
                sprite = notCollectibleIconSprite; glyph = "✕"; text = notCollectibleLabel;
                tint = notCollectibleColor; sfx = notCollectibleSfx;
                break;
        }

        bg.color = new Color(tint.r, tint.g, tint.b, 0.85f);
        label.color = Color.black;

        if (sprite != null)
        {
            icon.sprite = sprite;
            icon.gameObject.SetActive(true);
            label.text = text;
        }
        else
        {
            // 💡 아이콘 스프라이트가 아직 없으면 텍스트 글리프로 대체 (placeholder)
            label.text = $"{glyph} {text}";
        }

        PlaySfx(sfx);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
