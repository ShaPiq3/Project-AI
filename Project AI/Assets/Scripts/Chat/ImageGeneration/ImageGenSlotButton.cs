using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ImageGenerationManager 가 생성/갱신하는 슬롯 하나의 UI.
///
/// 상태:
///  1) 잠김(빈 슬롯) - 자물쇠 아이콘 + 키워드 텍스트(어두운/기본 색), Toggle 클릭 불가
///  2) 채워짐 - 채워짐 아이콘 + 키워드 텍스트 색이 바뀜(단서 수집 인식) + 등록된 이미지가 항상 표시됨
///
/// Toggle 은 "삭제하고 싶은 슬롯을 선택"하는 용도일 뿐이고,
/// 실제 삭제는 패널에 항상 떠있는 별도의 삭제 버튼(ImageGenerationManager.deleteSelectedButton)이
/// 클릭됐을 때, 선택(Toggle On)된 슬롯들만 모아서 한 번에 처리합니다.
/// (삭제되면 슬롯 자체는 사라지지 않고 "잠긴 키워드 슬롯" 상태로 되돌아감)
/// </summary>
public class ImageGenSlotButton : MonoBehaviour
{
    [Header("텍스트 / 아이콘")]
    [SerializeField] private TMP_Text keywordText;
    [SerializeField] private Image stateIcon;       // 왼쪽 작은 아이콘 (자물쇠 / 채워짐)
    [SerializeField] private Sprite lockedIconSprite;
    [SerializeField] private Sprite filledIconSprite;

    [Header("단서 수집 여부에 따른 텍스트 색")]
    [SerializeField] private Color lockedTextColor = new Color(0.55f, 0.55f, 0.55f); // 회색 (미수집)
    [SerializeField] private Color filledTextColor = Color.black;                     // 수집됨

    [Header("등록된 이미지 (채워지면 항상 표시)")]
    [SerializeField] private Image previewImage;
    [SerializeField] private GameObject previewRoot;    // previewImage 를 감싸는 오브젝트 (없으면 previewImage.gameObject 사용)

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
            // 프로젝트의 리소스 로딩 방식에 맞게 교체하세요 (Resources.Load / Addressables 등)
            Sprite sprite = Resources.Load<Sprite>(data.filledDisplayImagePath);
            if (sprite != null) previewImage.sprite = sprite;
        }

        if (selectToggle != null)
        {
            selectToggle.onValueChanged.RemoveAllListeners();
            selectToggle.interactable = filled;      // 빈 슬롯은 선택(체크) 불가
            selectToggle.SetIsOnWithoutNotify(false); // 매번 다시 그릴 때는 선택 해제된 상태로 시작
        }
    }
}