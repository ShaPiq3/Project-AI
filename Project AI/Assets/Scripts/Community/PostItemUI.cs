using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class PostItemUI : MonoBehaviour, IPointerClickHandler
{
    public TMP_Text titleText;
    public TMP_Text authorText;
    public TMP_Text dateText;
    public TMP_Text likeText;

    private PostData _data;
    private CommunityManager _manager;

    public void Setup(PostData data, CommunityManager manager)
    {
        _data = data;
        _manager = manager; // 매니저 주소 기억

        titleText.text = data.title;
        authorText.text = data.author;
        dateText.text = data.date;
        likeText.text = data.likes.ToString();
    }

    // 마우스 클릭 시 호출됨
    public void OnPointerClick(PointerEventData eventData)
    {
        // 매니저에게 내 데이터를 넘겨주면서 상세 페이지를 열어달라고 요청
        _manager.OpenDetailPage(_data);
    }
}