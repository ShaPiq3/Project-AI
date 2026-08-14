using UnityEngine;
using TMPro;
using UnityEngine.UI; // 버튼을 제어하기 위해 추가
using DG.Tweening;

public class QuestStatusUI : MonoBehaviour
{
    // 컨테이너/프리팹 대신 고정된 UI를 직접 연결
    public TMP_Text statusText;      // 0/5 등이 표시될 텍스트
    public Button generateButton;    // 답변 생성 버튼

    [Header("진행도 게이지 바")]
    [Tooltip("빈 배경(트랙) 쪽 RectTransform. 이 폭을 기준으로 채움 바의 폭을 계산합니다.")]
    public RectTransform gaugeTrackRect;
    [Tooltip("채워지는 쪽 Image. Type=Simple(스프라이트 없음)이어야 합니다 - 폭 자체를 늘렸다 줄였다 하는 방식이라 fillAmount는 사용하지 않습니다.")]
    public Image gaugeFillImage;
    [Tooltip("게이지 위에 '25%' 형태로 표시할 텍스트 (선택)")]
    public TMP_Text gaugePercentText;
    [Tooltip("게이지가 목표 폭까지 차오르는 데 걸리는 시간(초)")]
    public float gaugeTweenDuration = 0.4f;

    private Tween gaugeTween;

    public void UpdateDisplay()
    {
        if (DataLogManager.Instance == null) return;

        // 💡 [변경] 여러 퀘스트가 등록되어 있어도, DataLogManager의 다른 로직
        // (정답 판정, 대화 분기)과 동일하게 "가장 최근에 시작된 퀘스트"를 기준으로 표시합니다.
        var activeQuestIDs = DataLogManager.Instance.activeQuestIDs;

        if (activeQuestIDs == null || activeQuestIDs.Count == 0)
        {
            if (statusText != null)
            {
                statusText.text = "0 / 0";
            }

            if (generateButton != null)
            {
                generateButton.gameObject.SetActive(true);
                generateButton.interactable = false;
            }

            SetGaugeRatio(0f);

            return;
        }

        string currentQuestID = activeQuestIDs[activeQuestIDs.Count - 1];

        int current = DataLogManager.Instance.questCollectedClues.ContainsKey(currentQuestID)
                      ? DataLogManager.Instance.questCollectedClues[currentQuestID].Count : 0;

        int target = DataLogManager.Instance.questTargetCounts.ContainsKey(currentQuestID)
                     ? DataLogManager.Instance.questTargetCounts[currentQuestID] : 0;

        // 텍스트 업데이트
        if (statusText != null)
        {
            statusText.text = $"{current} / {target}";
        }

        // 💡 [추가] 게이지 바 채움 비율 갱신
        SetGaugeRatio(target > 0 ? (float)current / target : 0f);

        // 💡 [변경] 목표 개수를 다 채웠을 때만 버튼이 눌리도록 잠금.
        // 버튼 자체는 계속 보이게 유지하고(SetActive(true)), interactable만 잠갔다 풉니다.
        if (generateButton != null)
        {
            generateButton.gameObject.SetActive(true);
            generateButton.interactable = (target > 0 && current >= target);
        }
    }

    /// <summary>
    /// 💡 fillAmount 대신 채움 바의 실제 폭(sizeDelta.x)을 트랙 폭 * 비율로 직접 계산합니다.
    /// Image.Type=Filled는 스프라이트가 없으면 fillAmount를 무시하고 항상 꽉 찬 채로 그려지는
    /// 제약이 있어서, 스프라이트 없는 순수 사각형 스타일을 유지하기 위해 이 방식을 씁니다.
    /// </summary>
    private void SetGaugeRatio(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);

        if (gaugePercentText != null)
        {
            gaugePercentText.text = $"{Mathf.RoundToInt(ratio * 100f)}%";
        }

        if (gaugeFillImage == null || gaugeTrackRect == null) return;

        RectTransform fillRect = gaugeFillImage.rectTransform;
        float targetWidth = gaugeTrackRect.rect.width * ratio;

        gaugeTween?.Kill();
        gaugeTween = fillRect.DOSizeDelta(new Vector2(targetWidth, fillRect.sizeDelta.y), gaugeTweenDuration)
            .SetEase(Ease.OutQuad);
    }
}