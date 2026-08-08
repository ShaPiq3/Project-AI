using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 💡 단서 수집 모드에서 텍스트/이미지에 마우스를 올렸을 때, 색을 바꾸는 대신
/// 그 오브젝트 크기에 딱 맞는 반투명 "필터" 오버레이가 커지면서(호버 진입) /
/// 작아지면서(호버 이탈) 나타나고 사라지는 연출을 담당합니다.
/// ClueTextHoverEffect / ClueImageHoverEffect가 자기 자신에게 이 컴포넌트를 붙여서 사용합니다.
/// 오버레이는 대상 오브젝트의 자식으로 붙어서 RectTransform에 꽉 차게 앵커되므로, 문장이든 이미지든
/// 그 오브젝트의 실제 크기에 맞춰 자동으로 커지고 작아집니다.
/// </summary>
public class ClueHoverFilterOverlay : MonoBehaviour
{
    [Header("필터 비주얼 (스프라이트를 비워두면 반투명 색상 박스로 대체)")]
    [SerializeField] private Sprite filterSprite;
    [SerializeField] private Image.Type filterImageType = Image.Type.Simple;
    [SerializeField] private Color filterColor = new Color(1f, 1f, 0.4f, 0.35f);

    [Header("셰이더 필터 (선택 - 지정하면 스프라이트/색상 대신 이 Material로 그림)")]
    [Tooltip("Custom/ClueScanFrame 같은 커스텀 UI 쉐이더 Material. 대상 크기(_RectSize)를 매번 자동으로 넘겨줍니다.")]
    [SerializeField] private Material filterMaterial;

    [Header("연출 타이밍")]
    [SerializeField] private float growDuration = 0.15f;
    [SerializeField] private float shrinkDuration = 0.12f;
    [SerializeField] private Ease growEase = Ease.OutBack;
    [SerializeField] private Ease shrinkEase = Ease.InQuad;

    [Header("여백")]
    [Tooltip("대상 크기보다 사방으로 이만큼(px) 더 크게 오버레이를 그려서, 문장/이미지에 너무 딱 붙어 답답해 보이지 않게 합니다.")]
    [SerializeField] private float padding = 6f;

    private static readonly int RectSizeID = Shader.PropertyToID("_RectSize");

    private RectTransform overlayRect;
    private Image overlayImage;
    private Material materialInstance;
    private Tween activeTween;

    private void EnsureCreated()
    {
        if (overlayRect != null) return;

        GameObject go = new GameObject("HoverFilterOverlay");
        go.transform.SetParent(transform, false);

        overlayRect = go.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        // 💡 offsetMin을 음수로, offsetMax를 양수로 주면 부모 rect보다 사방으로 padding만큼 더 크게 퍼집니다.
        overlayRect.offsetMin = new Vector2(-padding, -padding);
        overlayRect.offsetMax = new Vector2(padding, padding);
        overlayRect.pivot = new Vector2(0.5f, 0.5f);

        // 💡 [버그 수정] 대상(부모)이 자기 자신에게 LayoutGroup(Vertical/Horizontal 등)을 갖고 있으면,
        // 그 레이아웃 그룹이 새로 추가된 이 자식의 크기를 자동으로 재계산해서 0으로 뭉개버립니다.
        // LayoutElement.ignoreLayout을 켜서 부모 레이아웃 계산에서 이 오브젝트를 완전히 제외시킵니다.
        LayoutElement layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        overlayImage = go.AddComponent<Image>();
        overlayImage.raycastTarget = false; // 클릭은 원래 대상(텍스트/이미지)에 그대로 전달되어야 함
        overlayImage.sprite = filterSprite;
        overlayImage.type = filterImageType;

        // 💡 이 컴포넌트 자체가 런타임에 자동으로 붙는 방식이라 프리팹에 미리 연결해둘 수 없으므로,
        // 자기 자신의 Material이 비어있으면 ClueScanEffectController에 등록된 기본 Material을 사용합니다.
        Material materialToUse = filterMaterial != null
            ? filterMaterial
            : (ClueScanEffectController.Instance != null ? ClueScanEffectController.Instance.DefaultHoverFilterMaterial : null);

        if (materialToUse != null)
        {
            // 💡 Material은 공유 에셋이라 인스턴스화하지 않으면 모든 오버레이가 같은 _RectSize를 공유하게 됩니다.
            materialInstance = Instantiate(materialToUse);
            overlayImage.material = materialInstance;
            // 💡 [버그 수정] 쉐이더가 자체적으로 색/알파(테두리는 알파 1)를 계산하는데,
            // 여기서 Image.color까지 filterColor(기본 알파 0.35)로 남겨두면 쉐이더 출력에 다시 곱해져서
            // 테두리까지 통째로 흐려져 버립니다. 쉐이더를 쓸 때는 Image 자체는 흰색 불투명으로 둡니다.
            overlayImage.color = Color.white;
        }
        else
        {
            // 쉐이더가 없을 때만 기존 방식(반투명 색상 박스)으로 대체
            overlayImage.color = filterColor;
        }

        go.transform.localScale = Vector3.zero;
        go.SetActive(false);
    }

    /// <summary>오버레이를 대상 크기만큼 확대하며 보여줍니다.</summary>
    public void Show()
    {
        EnsureCreated();

        if (materialInstance != null)
        {
            materialInstance.SetVector(RectSizeID, new Vector4(overlayRect.rect.width, overlayRect.rect.height, 0f, 0f));
        }

        activeTween?.Kill();
        overlayRect.gameObject.SetActive(true);
        activeTween = overlayRect.DOScale(1f, growDuration).SetEase(growEase);
    }

    /// <summary>오버레이를 축소하며 감춥니다.</summary>
    public void Hide()
    {
        if (overlayRect == null || !overlayRect.gameObject.activeSelf) return;

        activeTween?.Kill();
        RectTransform capturedRect = overlayRect;
        activeTween = overlayRect.DOScale(0f, shrinkDuration).SetEase(shrinkEase)
            .OnComplete(() =>
            {
                if (capturedRect != null) capturedRect.gameObject.SetActive(false);
            });
    }

    private void OnDisable()
    {
        activeTween?.Kill();
        if (overlayRect != null) overlayRect.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        activeTween?.Kill();
        if (materialInstance != null) Destroy(materialInstance);
    }
}
