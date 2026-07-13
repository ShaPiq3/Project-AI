using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions; // 💡 태그 제거(Regex)를 위해 반드시 필요합니다.
using TMPro;

public class NewsButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI previewText;

    [Header("Category Tag (기존 구조 유지)")]
    public string category;

    public NewsData MyData { get; private set; }
    private NewsListManager listManager;

    public void SetButton(NewsData data, NewsListManager manager)
    {
        MyData = data;
        listManager = manager;

        // 카테고리 설정 및 오브젝트 이름 변경
        category = data.category;
        gameObject.name = $"{data.category}_{data.id}";

        // 텍스트 세팅
        titleText.text = data.title;
        infoText.text = data.info;

        // 💡 본문 텍스트가 비어있지 않은지 먼저 확인합니다.
        if (!string.IsNullOrEmpty(data.body))
        {
            // '|' 기호 기준으로 문단을 쪼갭니다.
            string[] paragraphs = data.body.Split('|');

            if (paragraphs.Length > 0)
            {
                // 1. 첫 번째 문단을 가져옵니다. (여기서 변수가 확실하게 선언됩니다)
                string firstParagraph = paragraphs[0].Trim();

                // 2. 프리뷰 창에서는 <b>, <color> 같은 태그들을 싹 지우고 순수 글자만 남깁니다.
                string cleanText = Regex.Replace(firstParagraph, "<[^>]*>", string.Empty);

                // 3. 최종 가공된 텍스트를 할당합니다.
                previewText.text = cleanText;
            }
            else
            {
                previewText.text = "";
            }
        }
        else
        {
            previewText.text = "";
        }

        // 이미지 로드 (Resources/NewsImages/ 경로)
        Sprite loadedSprite = Resources.Load<Sprite>($"NewsImages/{data.imageName}");
        if (loadedSprite != null)
        {
            thumbnailImage.sprite = loadedSprite;
        }

        // 버튼 클릭 이벤트 바인딩 (기존 리스너가 중복 등록되지 않도록 깔끔하게 처리)
        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        // 클릭 시 매니저를 통해 상세 팝업창을 띄움
        listManager.OpenDetailPopup(MyData);
    }
}