using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ImageGenSlotButton : MonoBehaviour
{
    [Header("텍스트 / 아이콘")]
    [SerializeField] private TMP_Text keywordText;
    [SerializeField] private Image stateIcon;
    [SerializeField] private Sprite lockedIconSprite;
    [SerializeField] private Sprite filledIconSprite;

    [Header("단서 수집 여부에 따른 텍스트 색")]
    [SerializeField] private Color lockedTextColor = new Color(0.55f, 0.55f, 0.55f);
    [SerializeField] private Color filledTextColor = Color.black;

    [Header("등록된 이미지 (채워지면 항상 표시)")]
    [SerializeField] private Image previewImage;
    [SerializeField] private GameObject previewRoot;
    [SerializeField] private AspectRatioFitter previewAspectRatioFitter; // 💡 추가: previewRoot에 붙은 컴포넌트

    [Header("삭제 대상 선택용 체크박스")]
    [Tooltip("삭제하고 싶을 때 체크하는 용도. 빈 슬롯은 체크 불가")]
    [SerializeField] private Toggle selectToggle;

    public int SlotIndex { get; private set; }
    public bool IsSelectedForDelete => selectToggle != null && selectToggle.isOn;

    public void Setup(ImageGenSlotRuntime data)
    {
        SlotIndex = data.slotIndex;
        bool filled = data.isFilled;

        if (keywordText != null)
        {
            keywordText.text = data.keyword;
            keywordText.color = filled ? filledTextColor : lockedTextColor;
        }

        if (stateIcon != null)
        {
            stateIcon.sprite = filled ? filledIconSprite : lockedIconSprite;
        }

        GameObject previewGo = previewRoot != null ? previewRoot : (previewImage != null ? previewImage.gameObject : null);
        if (previewGo != null) previewGo.SetActive(filled);

        if (previewImage != null && filled && !string.IsNullOrEmpty(data.filledDisplayImagePath))
        {
            Sprite sprite = Resources.Load<Sprite>(data.filledDisplayImagePath);
            Debug.Log($"[디버그] 슬롯 이미지 로드 시도: path='{data.filledDisplayImagePath}', 성공 여부: {sprite != null}");
            if (sprite != null)
                if (sprite != null)
            {
                previewImage.sprite = sprite;

                // 💡 이미지 비율에 맞춰 AspectRatioFitter 값 갱신
                if (previewAspectRatioFitter != null)
                {
                    float ratio = sprite.rect.width / sprite.rect.height;
                    previewAspectRatioFitter.aspectRatio = ratio;
                }
            }
        }

        if (selectToggle != null)
        {
            selectToggle.onValueChanged.RemoveAllListeners();
            selectToggle.interactable = filled;
            selectToggle.SetIsOnWithoutNotify(false);
        }

        // 💡 다음 프레임에 레이아웃 재계산
        StartCoroutine(RebuildLayoutNextFrame());
    }

    private IEnumerator RebuildLayoutNextFrame()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}