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

    // 💡 [추가] 본문 전체 영역을 클릭했을 때 단서를 수집할 수 있도록 버튼 컴포넌트 연결
    // 만약 이미 본문 오브젝트에 버튼이 붙어있다면 인스펙터에서 드래그하여 연결해 주시면 됩니다.
    [Header("③번 구역 단서 수집용")]
    public Button postContentButton;

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

        // 💡 [추가] 게시글 본문 자체에 단서 ID(clueID)가 들어있는지 검사하고 클릭 이벤트 연결
        if (postContentButton != null)
        {
            postContentButton.onClick.RemoveAllListeners();
            if (!string.IsNullOrEmpty(data.clueID) && DataLogManager.Instance != null)
            {
                postContentButton.onClick.AddListener(() => {
                    ClueData clue = new ClueData
                    {
                        clueID = data.clueID,
                        sourceType = "커뮤니티",
                        sourceTitle = data.title,
                        contentText = data.content,
                        imageName = data.imageName
                    };
                    DataLogManager.Instance.AddClue(clue);
                });
            }
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

                // 💡 [추가] 해당 댓글에 단서 ID(clueID)가 매핑되어 있다면 클릭 시 단서 수집 처리
                // 댓글 프리팹 자체에 Button 컴포넌트가 부착되어 있거나 동적으로 추가하여 연동합니다.
                if (!string.IsNullOrEmpty(cData.clueID) && DataLogManager.Instance != null)
                {
                    Button commentBtn = cItem.GetComponent<Button>();
                    if (commentBtn == null) commentBtn = cItem.AddComponent<Button>();

                    commentBtn.onClick.RemoveAllListeners();
                    commentBtn.onClick.AddListener(() => {
                        ClueData clue = new ClueData
                        {
                            clueID = cData.clueID,
                            sourceType = "댓글",
                            sourceTitle = $"{data.title} ({cData.author})",
                            contentText = cData.content,
                            imageName = ""
                        };
                        DataLogManager.Instance.AddClue(clue);
                    });
                }
            }
        }

        gameObject.SetActive(true);
    }

    public void ClosePage()
    {
        gameObject.SetActive(false);
    }
}
