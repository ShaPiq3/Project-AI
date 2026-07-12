using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PostDetailPageUI : MonoBehaviour
{
    [Header("①번 구역 (상단 헤더)")]
    public TMP_Text titleText;
    public TMP_Text authorText;
    public TMP_Text dateText;
    public TMP_Text headerLikeText;    // LikeBadge 텍스트 (추천)
    public TMP_Text headerDislikeText; // ⭐ [추가] DislikeBadge 텍스트 (비추천)

    [Header("②번 구역 (추천/비추천 버튼)")]
    public TMP_Text buttonLikeText;    // LikeButton 안의 텍스트
    public TMP_Text buttonDislikeText; // ⭐ [추가] DislikeButton 안의 텍스트

    [Header("③번 구역 (본문)")]
    public Image postImage;
    public TMP_Text contentText;

    [Header("④번 구역 (댓글 리스트 영역)")]
    public Transform commentListTransform;
    public GameObject commentPrefab;

    public void DisplayPost(PostData data)
    {
        transform.SetAsLastSibling();
        // 1. ①번 구역 세팅
        titleText.text = data.title;
        authorText.text = data.author;
        dateText.text = data.date;

        // 상단 헤더 배지 동기화
        headerLikeText.text = $"추천 {data.likes}";
        headerDislikeText.text = $"비추 {data.dislikes}"; // ⭐ 세팅

        // ③번 구역 하단 버튼 텍스트 동기화
        buttonLikeText.text = $"추천 ▲ {data.likes}";
        buttonDislikeText.text = $"비추 ▼ {data.dislikes}"; // ⭐ 세팅

        // 2. ②번 구역 세팅
        contentText.text = data.content;
        if (!string.IsNullOrEmpty(data.imageName))
        {
            Sprite loadedSprite = Resources.Load<Sprite>($"PostImages/{data.imageName}");
            if (loadedSprite != null)
            {
                postImage.gameObject.SetActive(true);
                postImage.sprite = loadedSprite;
            }
            else
            {
                postImage.gameObject.SetActive(false);
            }
        }
        else
        {
            postImage.gameObject.SetActive(false);
        }

        // 3. ⑤번 구역 댓글 생성 처리
        foreach (Transform child in commentListTransform)
        {
            Destroy(child.gameObject);
        }

        if (data.comments != null)
        {
            foreach (CommentData cData in data.comments)
            {
                GameObject cItem = Instantiate(commentPrefab, commentListTransform);
                cItem.GetComponent<CommentItemUI>().Setup(cData);
            }
        }

        gameObject.SetActive(true);
    }

    public void ClosePage()
    {
        gameObject.SetActive(false);
    }
}