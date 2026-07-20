using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SentenceBlock : MonoBehaviour
{
    private TextMeshProUGUI indexText;  // 자식 중 'Label'을 자동으로 검색
    private TextMeshProUGUI bodyText;   // 자식 중 'Body'를 자동으로 검색
    private Image backgroundImage;      // 자기 자신의 Image 컴포넌트
    private Button blockButton;

    [Header("--- Color Settings ---")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.7f, 0.95f, 0.95f, 1.0f); // 선택 시 민트색 하이라이트

    // 외부(Manager)에서 유저의 정답 여부를 판단하기 위한 프로퍼티
    public int Index { get; private set; }
    public bool IsSelected { get; private set; }

    // 메인 매니저가 고정 버튼들을 순회하며 번호(Index)를 매겨줄 때 호출하는 초기화 함수
    public void Initialize(int index)
    {
        Index = index;
        IsSelected = false;

        // 1) 내부 자식 오브젝트들 이름으로 컴포넌트 자동 검색 (드래그앤드롭 불필요)
        Transform labelTransform = transform.Find("Label");
        if (labelTransform != null) indexText = labelTransform.GetComponent<TextMeshProUGUI>();

        Transform bodyTransform = transform.Find("Body");
        if (bodyTransform != null) bodyText = bodyTransform.GetComponent<TextMeshProUGUI>();

        backgroundImage = GetComponent<Image>();
        blockButton = GetComponent<Button>();

        // 2) 클릭 리스너 중복 방지 및 등록
        if (blockButton != null)
        {
            blockButton.onClick.RemoveAllListeners();
            blockButton.onClick.AddListener(ToggleSelect);
        }

        // 3) 초기 컬러 세팅
        if (backgroundImage != null) backgroundImage.color = defaultColor;
    }

    // 클릭 시 토글 연출
    private void ToggleSelect()
    {
        IsSelected = !IsSelected;
        if (backgroundImage != null)
        {
            backgroundImage.color = IsSelected ? selectedColor : defaultColor;
        }
    }
}