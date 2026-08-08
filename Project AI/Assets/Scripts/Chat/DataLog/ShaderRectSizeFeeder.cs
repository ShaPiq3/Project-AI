using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 💡 Custom/ClueScanFrame처럼 자기 크기(_RectSize, 픽셀 단위)를 알아야 하는 커스텀 UI 쉐이더 Material을
/// 쓰는 "정적인" UI 요소(예: 화면 전체를 덮는 ClueFilterPanel)에 붙이는 컴포넌트입니다.
/// ClueHoverFilterOverlay는 호버할 때마다 스스로 크기를 계산해서 넘기지만, 이 컴포넌트는
/// 씬에 고정 배치된 Image에 붙여서 자기 RectTransform 크기를 자동으로 Material에 넘겨줍니다.
///
/// 사용법: Image가 붙은 오브젝트에 이 컴포넌트를 추가하고, Image의 Material에
/// Custom/ClueScanFrame 쉐이더를 쓰는 Material을 연결하면 됩니다.
/// (Material은 공유 에셋이므로 이 컴포넌트가 자동으로 인스턴스화해서 Image.material에 다시 꽂아줍니다.)
/// </summary>
[RequireComponent(typeof(Image))]
public class ShaderRectSizeFeeder : MonoBehaviour
{
    private static readonly int RectSizeID = Shader.PropertyToID("_RectSize");

    private RectTransform rectTransform;
    private Image image;
    private Material materialInstance;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        image = GetComponent<Image>();

        if (image.material != null)
        {
            materialInstance = Instantiate(image.material);
            image.material = materialInstance;
        }
    }

    private void OnEnable() => UpdateRectSize();

    // 💡 캔버스 스케일러 등으로 실제 화면 크기가 바뀌어도 _RectSize가 계속 맞도록 자동 갱신
    private void OnRectTransformDimensionsChange() => UpdateRectSize();

    // 💡 [추가] OnEnable 시점엔 아직 Canvas 레이아웃이 확정되기 전이라 rect가 (0,0)으로
    // 읽히는 경우가 있어서, 활성화된 동안은 매 프레임 갱신해 항상 정확한 크기를 유지합니다.
    // 화면을 통째로 덮는 패널 하나 정도에만 붙는 컴포넌트라 비용은 무시할 수준입니다.
    private void LateUpdate() => UpdateRectSize();

    private void UpdateRectSize()
    {
        if (materialInstance == null || rectTransform == null) return;
        materialInstance.SetVector(RectSizeID, new Vector4(rectTransform.rect.width, rectTransform.rect.height, 0f, 0f));
    }

    private void OnDestroy()
    {
        if (materialInstance != null) Destroy(materialInstance);
    }
}
