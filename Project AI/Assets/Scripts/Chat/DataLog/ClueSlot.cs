using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClueSlot : MonoBehaviour
{
    [Header("단서 슬롯 UI 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI sourceText;
    [SerializeField] private TextMeshProUGUI contentText; // 수집된 단서 본문 내용
    [SerializeField] private Image clueImage;             // 단서 이미지 (있을 경우)

    [Header("다중 선택용 체크박스")]
    [Tooltip("파일 탐색기처럼 개별 체크 후 삭제할 때 쓰는 체크박스입니다.")]
    [SerializeField] private Toggle selectionToggle;

    public ClueData clueData;

    private void Awake()
    {
        // 체크박스 리스너는 한 번만 등록 (Awake에서), SetClueUI가 여러 번 호출돼도 중복 등록 방지
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
            sourceText.text = string.IsNullOrEmpty(data.sourceType) ?
                data.sourceTitle : $"[{data.sourceType}] {data.sourceTitle}";
        }

        if (contentText != null)
        {
            contentText.text = data.contentText;
        }

        if (clueImage != null)
        {
            if (!string.IsNullOrEmpty(data.imageName))
            {
                Sprite loadedSprite = Resources.Load<Sprite>($"NewsImages/{data.imageName}");
                if (loadedSprite != null)
                {
                    clueImage.sprite = loadedSprite;
                    clueImage.gameObject.SetActive(true);
                }
                else
                {
                    clueImage.gameObject.SetActive(false);
                }
            }
            else
            {
                clueImage.gameObject.SetActive(false);
            }
        }

        // 새로 생성/갱신될 때는 체크 해제 상태로 시작
        // (SetIsOnWithoutNotify: 리스너를 다시 트리거하지 않고 값만 초기화)
        if (selectionToggle != null)
        {
            selectionToggle.SetIsOnWithoutNotify(false);
        }
    }

    /// <summary>
    /// 체크박스 상태가 바뀔 때마다 DataLogManager에 알립니다 (다중 선택 목록 갱신용).
    /// </summary>
    private void OnToggleChanged(bool isOn)
    {
        DataLogManager.Instance.SetClueSelected(this, isOn);
    }

    /// <summary>
    /// 슬롯의 "본문" 영역(체크박스 제외) 클릭 시 호출됩니다.
    /// 체크박스를 직접 클릭한 경우에는 체크박스가 이벤트를 먼저 소비하므로
    /// 이 함수는 실행되지 않습니다.
    /// 이 단서가 원래 있던 원본 위치(뉴스 기사 등)를 열어 보여주는 용도로 사용하세요.
    /// </summary>
    public void OnClickSlot()
    {
        DataLogManager.Instance.OpenClueSource(clueData);
    }
}