using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PostDetailPageUI : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]
    public TMP_Text detailTitleText;
    public TMP_Text detailAuthorText;
    public TMP_Text detailDateText;
    public TMP_Text detailContentText;
    public TMP_Text detailLikeText;
    public TMP_Text detailDislikeText;

    public Button closeButton; // 닫기 버튼

    void Start()
    {
        closeButton.onClick.AddListener(ClosePage);
    }

    // 매니저가 이 함수를 호출하면서 데이터를 던져줄 겁니다.
    public void DisplayPost(PostData data)
    {
        gameObject.SetActive(true); // 창 띄우기

        // 데이터 갈아 끼우기 ⭐⭐⭐
        detailTitleText.text = data.title;
        detailAuthorText.text = data.author;
        detailDateText.text = data.date;
        detailContentText.text = data.content.Replace("\\n", "\n"); // 엑셀에서 줄바꿈 처리용
        detailLikeText.text = data.likes.ToString();
        detailDislikeText.text = data.dislikes.ToString();
    }

    public void ClosePage()
    {
        gameObject.SetActive(false); // 창 닫기
    }
}