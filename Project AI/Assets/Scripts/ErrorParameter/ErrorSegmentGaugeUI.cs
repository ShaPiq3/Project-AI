using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 오류 파라미터를 Custom/ErrorSegmentGauge 셰이더로 그리는 UI 위젯.
/// 실제 오브젝트를 100개 쪼개지 않고, Image 1개 + 셰이더가 프로시저럴로 칸을 그린다.
/// 켜진 칸 수는 항상 (현재값-5)~현재값 사이에서 계속 흔들리고(값을 절대 넘지 않음),
/// 색상은 구간(0~50/50~70/70~100)에 따라 바뀌며, 값이 100이 되면 흔들림이 멈춘다.
/// </summary>
[RequireComponent(typeof(Image))]
public class ErrorSegmentGaugeUI : MonoBehaviour
{
    [Header("표시할 텍스트")]
    [Tooltip("\"MEMORY LEAK...N%\" 형태로 표시할 텍스트. 흔들리는 표시값(currentLitCount)을 그대로 따라감.")]
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private string labelPrefix = "MEMORY LEAK...";

    [Header("흔들림 애니메이션")]
    [Tooltip("현재 값에서 최대 몇 칸까지 아래로 내려가며 흔들릴지")]
    [SerializeField] private float oscillationRange = 5f;
    [Tooltip("몇 초마다 새로운 랜덤 값으로 전환할지")]
    [SerializeField] private float randomStepInterval = 1.5f;

    [Header("구간별 색상 (0~50 / 50~70 / 70~100)")]
    [SerializeField] private Color lowColor = new Color(0f / 255f, 188f / 255f, 212f / 255f, 1f);   // #00BCD4
    [SerializeField] private Color midColor = new Color(255f / 255f, 212f / 255f, 4f / 255f, 1f);    // #FFD404
    [SerializeField] private Color highColor = new Color(245f / 255f, 0f / 255f, 0f / 255f, 1f);     // #F50000

    private Image image;
    private Material materialInstance;
    private RectTransform rectTransform;

    private Tween oscillationTween;
    private float currentLitCount;

    private static readonly int LitCountID = Shader.PropertyToID("_LitCount");
    private static readonly int LitColorID = Shader.PropertyToID("_LitColor");
    private static readonly int RectSizeID = Shader.PropertyToID("_RectSize");

    private void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        if (image.material == null || image.material == Image.defaultGraphicMaterial)
        {
            Shader shader = Shader.Find("Custom/ErrorSegmentGauge");
            if (shader == null)
            {
                Debug.LogError("[ErrorSegmentGaugeUI] Custom/ErrorSegmentGauge 셰이더를 찾지 못했습니다.");
                return;
            }
            materialInstance = new Material(shader);
        }
        else
        {
            // 💡 인스펙터에 이미 이 셰이더 머티리얼이 연결돼있다면, 다른 곳과 값이 서로 덮어써지지
            // 않도록 여기서도 인스턴스화해서 씀 (ChatCoordinator의 VHS 글리치와 동일한 이유).
            materialInstance = new Material(image.material);
        }

        image.material = materialInstance;
    }

    private void Start()
    {
        UpdateRectSize();

        if (ErrorParameterManager.Instance != null)
        {
            ApplyValue(ErrorParameterManager.Instance.CurrentValue, ErrorParameterManager.Instance.MaxValue);
        }
        else
        {
            ApplyValue(0f, 100f);
        }
    }

    private void OnEnable()
    {
        ErrorParameterManager.OnValueChanged += HandleValueChanged;
    }

    private void OnDisable()
    {
        ErrorParameterManager.OnValueChanged -= HandleValueChanged;
        oscillationTween?.Kill();
    }

    private void OnRectTransformDimensionsChange()
    {
        // 💡 Awake보다 먼저 호출될 수 있어서(레이아웃 초기화 등) materialInstance가 아직 없을 수 있음
        if (materialInstance != null) UpdateRectSize();
    }

    private void UpdateRectSize()
    {
        if (materialInstance == null || rectTransform == null) return;
        Rect rect = rectTransform.rect;
        materialInstance.SetVector(RectSizeID, new Vector4(rect.width, rect.height, 0f, 0f));
    }

    private void HandleValueChanged(float current, float max)
    {
        ApplyValue(current, max);
    }

    private void ApplyValue(float current, float max)
    {
        float percent = max > 0f ? Mathf.Clamp(current / max * 100f, 0f, 100f) : 0f;

        if (materialInstance != null)
        {
            materialInstance.SetColor(LitColorID, GetColorForPercent(percent));
        }

        oscillationTween?.Kill();

        if (percent >= 100f)
        {
            // 💡 [요청사항] 100에 도달하면 흔들리는 애니메이션을 멈춘다.
            currentLitCount = 100f;
            if (materialInstance != null) materialInstance.SetFloat(LitCountID, currentLitCount);
            UpdatePercentText(currentLitCount);
            return;
        }

        currentLitCount = percent;
        if (materialInstance != null) materialInstance.SetFloat(LitCountID, currentLitCount);
        UpdatePercentText(currentLitCount);

        ScheduleNextRandomStep(percent);
    }

    /// <summary>
    /// 💡 규칙적으로 왔다갔다 하는 대신, basePercent를 기준으로 (basePercent-oscillationRange)~basePercent
    /// 사이의 랜덤한 값을 randomStepInterval마다 새로 뽑아서 그쪽으로 흘러가는 걸 반복한다.
    /// </summary>
    private void ScheduleNextRandomStep(float basePercent)
    {
        float low = Mathf.Max(0f, basePercent - oscillationRange);
        float target = Random.Range(low, basePercent);

        oscillationTween = DOTween.To(
                () => currentLitCount,
                x =>
                {
                    currentLitCount = x;
                    UpdatePercentText(x);
                    if (materialInstance != null) materialInstance.SetFloat(LitCountID, x);
                },
                target, randomStepInterval)
            .SetEase(Ease.InOutSine)
            .OnComplete(() => ScheduleNextRandomStep(basePercent));
    }

    private void UpdatePercentText(float displayedLitCount)
    {
        if (percentText == null) return;
        percentText.text = $"{labelPrefix}{Mathf.RoundToInt(displayedLitCount)}%";
    }

    private Color GetColorForPercent(float percent)
    {
        if (percent < 50f) return lowColor;
        if (percent < 70f) return midColor;
        return highColor;
    }
}
