using UnityEngine;
using UnityEngine.UI;

public class SNSPost : MonoBehaviour
{
    [SerializeField] private RectTransform contentGroupRect; // Content_Group의 RectTransform
    [SerializeField] private RectTransform profileRect;      // 본문_프로필
    [SerializeField] private RectTransform textRect;         // 本문_글
    [SerializeField] private RectTransform imageRect;        // 본문_이미지
    [SerializeField] private RectTransform commentRect;      // 댓글창

    public void RefreshLayoutForce()
    {
        // 1. 유니티가 텍스트나 댓글 생성된 크기를 일단 계산하게 만듦
        Canvas.ForceUpdateCanvases();

        // 2. 이미지가 켜져있을 때만 이미지 높이를 더하기 위한 변수
        float imageHeight = imageRect.gameObject.activeSelf ? imageRect.rect.height : 0f;

        // 3. 자식들의 '진짜 실제 높이'를 수동으로 다 더해버립니다. (여백 20씩 추가)
        float totalHeight = profileRect.rect.height +
                            textRect.rect.height +
                            imageHeight +
                            commentRect.rect.height + 80f; // 간격 여백

        // 4. Content_Group의 높이를 코드로 강제 주입!! (840 감옥 부수기)
        contentGroupRect.sizeDelta = new Vector2(contentGroupRect.sizeDelta.x, totalHeight);

        // 5. 내 부모인 SNS_Template과 최상위 스크롤뷰 Content도 갱신
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        if (transform.parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
        }
    }
}