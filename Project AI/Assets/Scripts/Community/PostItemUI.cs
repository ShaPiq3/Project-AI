using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PostItemUI : MonoBehaviour
{
    // 인스펙터에서 연결할 텍스트 컴포넌트들 (image_737ec0.png 기준)
    public TMP_Text titleText;
    public TMP_Text authorText;
    public TMP_Text dateText;
    public TMP_Text likesText;

    // 클릭 이벤트를 감지할 버튼 컴포넌트
    private Button itemButton;
    private PostData postData;
    private CommunityManager manager;

    void Awake()
    {
        // 프리팹 자신에게 붙어있는 Button 컴포넌트를 자동으로 가져옵니다.
        itemButton = GetComponent<Button>();

        // 버튼이 클릭되면 OnItemClick 함수가 실행되도록 리스너를 등록합니다.
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OnItemClick);
        }
    }

    // CommunityManager가 목록을 생성할 때 데이터를 채워주는 함수
    public void Setup(PostData data, CommunityManager communityManager)
    {
        postData = data;
        manager = communityManager;


        // 화면에 엑셀에서 읽어온 텍스트 뿌려주기
        titleText.text = data.title;
        authorText.text = data.author;
        dateText.text = data.date;
        likesText.text = data.likes.ToString();
    }

    // 버튼이 클릭되었을 때 실행되는 함수 ⭐
    private void OnItemClick()
    {
        if (manager != null && postData != null)
        {
            // 매니저에게 "이 데이터 들고 상세 페이지 열어줘!"라고 요청합니다.
            manager.OpenDetailPage(postData);
        }
    }
}