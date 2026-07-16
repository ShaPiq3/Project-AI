using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // TextMeshPro 사용을 위해 추가

public class SNSPost : MonoBehaviour
{
    [Header("Layout References")]
    [SerializeField] private RectTransform contentGroupRect; // Content_Group의 RectTransform
    [SerializeField] private RectTransform profileRect;      // 본문_프로필
    [SerializeField] private RectTransform textRect;         // 본문_글
    [SerializeField] private RectTransform imageRect;        // 본문_이미지
    [SerializeField] private RectTransform commentRect;      // 댓글창

    [Header("UI Component References (실제 데이터가 주입될 컴포넌트들)")]
    [SerializeField] private TMP_Text authorText;            // 본문 작성자 텍스트
    [SerializeField] private TMP_Text contentText;           // 본문 글 텍스트
    [SerializeField] private Image profileImage;             // 프로필 이미지 컴포넌트
    [SerializeField] private Image postImage;                // 본문 이미지 컴포넌트

    [Header("Comment Spawner Settings")]
    [SerializeField] private GameObject commentPrefab;       // 👈 새로 만든 댓글 프리팹(SNSCommentItem 부착된 것)
    [SerializeField] private Transform commentContentParent; // 👈 댓글들이 생성되어 담길 부모 (commentRect 내부의 Content)

    /// <summary>
    /// [🌟 핵심 추가] SNSManager가 생성 직후 엑셀 데이터를 밀어 넣어주는 함수
    /// </summary>
    public void Setup(SNSPostData data)
    {
        // 1. 본문 텍스트 데이터 주입
        if (authorText != null) authorText.text = data.author;
        if (contentText != null) contentText.text = data.content;

        // 2. 프로필 이미지 로드
        if (profileImage != null && !string.IsNullOrEmpty(data.profileImageName))
        {
            Sprite profSprite = Resources.Load<Sprite>("SNSImages/" + data.profileImageName);
            if (profSprite != null) profileImage.sprite = profSprite;
        }

        // 3. 본문 첨부 이미지 가공 (기획자가 빈칸으로 두었을 때의 방어 코드)
        if (postImage != null && imageRect != null)
        {
            if (!string.IsNullOrEmpty(data.postImageName))
            {
                imageRect.gameObject.SetActive(true); // 이미지 구역 켜기
                Sprite loadedSprite = Resources.Load<Sprite>("SNSImages/" + data.postImageName);
                if (loadedSprite != null) postImage.sprite = loadedSprite;
            }
            else
            {
                // 엑셀 칸이 비어있었다면 이미지 구역을 완전히 꺼서 여백 감옥 방지!
                imageRect.gameObject.SetActive(false);
            }
        }

        // 4. 댓글 동적 생성 구역
        if (commentPrefab != null && commentContentParent != null)
        {
            // 혹시 기존 찌꺼기가 있다면 청소
            foreach (Transform child in commentContentParent) Destroy(child.gameObject);

            // 엑셀에서 이 게시글(postID) 앞으로 차곡차곡 누적된 댓글 개수만큼 프리팹 찍어내기
            foreach (SNSCommentData commentData in data.comments)
            {
                GameObject newComment = Instantiate(commentPrefab, commentContentParent);
                SNSCommentItem commentScript = newComment.GetComponent<SNSCommentItem>();
                if (commentScript != null)
                {
                    commentScript.SetComment(commentData);
                }
            }
        }

        // 5. 텍스트 주입과 댓글 생성이 "완전히 끝난 최종 크기"를 기준으로 레이아웃 높이 강제 갱신!!
        RefreshLayoutForce();
    }

    /// <summary>
    /// 엑셀에서 받아온 텍스트/이미지 크기에 맞추어 UI 박스 높이를 강제로 재계산하는 핵심 최적화 함수
    /// </summary>
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
