using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClueSlot : MonoBehaviour
{
    [Header("단서 슬롯 UI 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI sourceText;
    [SerializeField] private Image sourceIcon;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Image clueImage;
    [Tooltip("clueImage의 부모(이미지가 없을 때 통째로 숨겨서, 텍스트만 있는 단서에서 빈 여백이 남지 않도록 함)")]
    [SerializeField] private GameObject imageContainer;

    [Header("다중 선택용 체크박스")]
    [SerializeField] private Toggle selectionToggle;

    [SerializeField] private AspectRatioFitter containerAspectRatioFitter;

    public ClueData clueData;

    private void Awake()
    {
        if (selectionToggle != null)
        {
            selectionToggle.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    public void SetClueUI(ClueData data)
    {
        this.clueData = data;

        if (sourceText != null)
        {
            sourceText.text = string.IsNullOrEmpty(data.sourceType) ? "" : $"[{data.sourceType}]";
        }

        if (sourceIcon != null)
        {
            Sprite icon = DataLogManager.Instance != null ? DataLogManager.Instance.GetSourceIcon(data.sourceType) : null;
            sourceIcon.sprite = icon;
            sourceIcon.gameObject.SetActive(icon != null);
        }

        if (contentText != null)
        {
            bool hasText = !string.IsNullOrEmpty(data.contentText);
            contentText.gameObject.SetActive(hasText);
            if (hasText) contentText.text = data.contentText;
        }

        if (clueImage != null)
        {
            bool hasImage = !string.IsNullOrEmpty(data.imageName);

            if (hasImage)
            {
                Sprite loadedSprite = LoadClueSprite(data.imageName);

                if (loadedSprite != null)
                {
                    clueImage.sprite = loadedSprite;
                    clueImage.gameObject.SetActive(true);
                    if (imageContainer != null) imageContainer.SetActive(true);

                    // 💡 이미지 비율에 맞춰 AspectRatioFitter 값 갱신
                    if (containerAspectRatioFitter != null)
                    {
                        float ratio = loadedSprite.rect.width / loadedSprite.rect.height;
                        containerAspectRatioFitter.aspectRatio = ratio;
                    }
                }
                else
                {
                    clueImage.gameObject.SetActive(false);
                    if (imageContainer != null) imageContainer.SetActive(false);
                    Debug.LogWarning($"[ClueSlot] 이미지 로드 실패: {data.imageName} (모든 폴더에서 찾지 못함)");
                }
            }
            else
            {
                clueImage.gameObject.SetActive(false);
                if (imageContainer != null) imageContainer.SetActive(false);
            }
        }

        if (selectionToggle != null)
        {
            selectionToggle.SetIsOnWithoutNotify(false);
        }

        // 💡 다음 프레임에 레이아웃 재계산
        StartCoroutine(RebuildLayoutNextFrame());
    }

    private Sprite LoadClueSprite(string imageName)
    {
        Sprite directSprite = Resources.Load<Sprite>(imageName);
        if (directSprite != null)
        {
            return directSprite;
        }

        string[] candidateFolders = { "NewsImages", "SNSImages", "PostImages", "ArchiveImages" };

        foreach (string folder in candidateFolders)
        {
            Sprite sprite = Resources.Load<Sprite>($"{folder}/{imageName}");
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private void OnToggleChanged(bool isOn)
    {
        DataLogManager.Instance.SetClueSelected(this, isOn);
    }

    public void OnClickSlot()
    {
        DataLogManager.Instance.OpenClueSource(clueData);
    }

    // 💡 [추가] 레이아웃 재계산용 코루틴
    private IEnumerator RebuildLayoutNextFrame()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }
}