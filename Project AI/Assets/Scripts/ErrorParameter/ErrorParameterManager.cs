using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 플레이어에게 보이는 "오류 파라미터". 씬이 바뀌면 자연히 초기화되도록 DontDestroyOnLoad를
/// 쓰지 않는 씬 로컬 싱글톤이다. DataLogManager(단서조사)/DocumentQuestManager(문서분석)/
/// ImageGenerationManager(이미지생성) 세 퀘스트 시스템의 실패 신호를 구독해서 값을 올리고,
/// 임계치에 도달하면 게임오버 시스템 연락처(GameOverDialogue_C2.csv)를 연다.
/// </summary>
public class ErrorParameterManager : MonoBehaviour
{
    public static ErrorParameterManager Instance { get; private set; }

    [Header("파라미터 설정")]
    [SerializeField] private float maxValue = 100f;
    public float CurrentValue { get; private set; } = 0f;
    public float MaxValue => maxValue;

    [Header("게이지 UI (임시 - 값만 오르게 해둠, 추후 UI 교체 예정)")]
    [Tooltip("빈 배경(트랙) 쪽 RectTransform. 비워두면 게이지 표시 없이 값만 누적됩니다.")]
    [SerializeField] private RectTransform gaugeTrackRect;
    [Tooltip("채워지는 쪽 Image. Type=Simple(스프라이트 없음)이어야 합니다 - 폭 자체를 늘렸다 줄였다 하는 방식입니다.")]
    [SerializeField] private Image gaugeFillImage;
    [SerializeField] private float gaugeTweenDuration = 0.3f;
    private Tween gaugeTween;

    [Header("게임오버 연동")]
    [Tooltip("게임오버 시스템 메시지를 재생할 연락처 ID (NPCContactData.contactID, isSystemContact=true, csvFile=GameOverDialogue_C2.csv)")]
    [SerializeField] private string gameOverContactID = "C2_SYS";

    private bool hasTriggeredGameOver = false;

    /// <summary> 값이 바뀔 때마다 (현재값, 최대값)과 함께 발생. 다른 UI에서 구독해서 쓸 수 있음. </summary>
    public static event Action<float, float> OnValueChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        DocumentQuestManager.OnQuestComplete += HandleDocumentQuestComplete;
        DataLogManager.OnQuestJudged += HandleDataLogQuestJudged;
        ImageGenerationManager.OnQuestMalfunction += HandleImageGenMalfunction;
    }

    private void OnDisable()
    {
        DocumentQuestManager.OnQuestComplete -= HandleDocumentQuestComplete;
        DataLogManager.OnQuestJudged -= HandleDataLogQuestJudged;
        ImageGenerationManager.OnQuestMalfunction -= HandleImageGenMalfunction;
    }

    private void HandleDocumentQuestComplete(DocumentQuestManager.QuestResult result)
    {
        if (!result.isSuccess) AddError(result.errorWeight);
    }

    private void HandleDataLogQuestJudged(string questID, bool isSuccess, float weight)
    {
        if (!isSuccess) AddError(weight);
    }

    private void HandleImageGenMalfunction(string questID, float weight)
    {
        AddError(weight);
    }

    public void AddError(float amount)
    {
        if (hasTriggeredGameOver || amount <= 0f) return;

        CurrentValue = Mathf.Min(CurrentValue + amount, maxValue);
        OnValueChanged?.Invoke(CurrentValue, maxValue);
        UpdateGaugeVisual();

        if (CurrentValue >= maxValue)
        {
            TriggerGameOver();
        }
    }

    private void UpdateGaugeVisual()
    {
        if (gaugeFillImage == null || gaugeTrackRect == null) return;

        float ratio = maxValue > 0f ? Mathf.Clamp01(CurrentValue / maxValue) : 0f;
        RectTransform fillRect = gaugeFillImage.rectTransform;
        float targetWidth = gaugeTrackRect.rect.width * ratio;

        gaugeTween?.Kill();
        gaugeTween = fillRect.DOSizeDelta(new Vector2(targetWidth, fillRect.sizeDelta.y), gaugeTweenDuration)
            .SetEase(Ease.OutQuad);
    }

    private void TriggerGameOver()
    {
        hasTriggeredGameOver = true;
        Debug.Log("[ErrorParameterManager] 오류 파라미터가 임계치에 도달했습니다. 게임오버 연출을 시작합니다.");

        if (ChatCoordinator.Instance != null)
        {
            ChatCoordinator.Instance.LockToGameOverRoom(gameOverContactID);
        }
        else
        {
            Debug.LogWarning("[ErrorParameterManager] ChatCoordinator.Instance가 없어 게임오버 연출을 재생하지 못했습니다.");
        }
    }
}
