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
    [SerializeField] private string questID;

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

        // 💡 [추가] 제목(title) 자체가 단서인 경우, 제목 텍스트에 클릭 시 수집 기능을 붙입니다.
        Button titleBtn = titleText.gameObject.GetComponent<Button>();
        if (!string.IsNullOrEmpty(data.titleClueID) && DataLogManager.Instance != null)
        {
            if (titleBtn == null) titleBtn = titleText.gameObject.AddComponent<Button>();

            titleBtn.transition = Selectable.Transition.ColorTint;
            titleBtn.onClick.RemoveAllListeners();
            titleBtn.onClick.AddListener(() => {
                Debug.Log($"커뮤니티 제목 클릭으로 단서 수집: {data.titleClueID}");
                DataLogManager.Instance.AcquireClue(questID, data.titleClueID, data.title);
            });
        }
        else
        {
            // 제목이 단서가 아닌 게시글로 다시 세팅될 수도 있으므로, 이전에 붙어있던 버튼은 제거
            if (titleBtn != null) Destroy(titleBtn);
        }

        // 상단 헤더 배지 동기화
        headerLikeText.text = $"추천 {data.likes}";
        headerDislikeText.text = $"비추 {data.dislikes}"; // ⭐ 세팅

        // ③번 구역 하단 버튼 텍스트 동기화
        buttonLikeText.text = $"추천 ▲ {data.likes}";
        buttonDislikeText.text = $"비추 ▼ {data.dislikes}"; // ⭐ 세팅

        // 2. ②번 구역 세팅
        contentText.text = data.content;
        // PostDetailPageUI.cs 내부의 이미지 렌더링 함수 예시
        if (!string.IsNullOrEmpty(data.imageName))
        {
            // 💡 [변경] 실제 이미지가 저장된 폴더명(PostImages)에 맞게 수정
            Sprite loadedSprite = Resources.Load<Sprite>($"PostImages/{data.imageName}");
            if (loadedSprite != null)
            {
                postImage.sprite = loadedSprite;
                postImage.gameObject.SetActive(true);

                // 💡 이미지 클릭 단서 수집 세팅
                Button imgBtn = postImage.gameObject.GetComponent<Button>();

                if (!string.IsNullOrEmpty(data.imageClueID) && data.imageClueID.ToLower() != "none")
                {
                    if (imgBtn == null) imgBtn = postImage.gameObject.AddComponent<Button>();

                    imgBtn.transition = Selectable.Transition.ColorTint;
                    imgBtn.onClick.RemoveAllListeners();
                    imgBtn.onClick.AddListener(() => {
                        Debug.Log($"커뮤니티 이미지 클릭으로 단서 수집: {data.imageClueID}");
                        // 💡 [추가] 실제 게시글 제목을 같이 전달
                        DataLogManager.Instance.AcquireClue(data.imageQuestID, data.imageClueID, data.title);
                    });
                }
                else
                {
                    if (imgBtn != null) Destroy(imgBtn); // 단서가 없는 이미지는 버튼 기능 제거
                }

                // 💡 이미지 생성 퀘스트 수집 대상으로 자동 등록.
                // 이 패널은 게시글마다 새로 생성되지 않고 재사용되므로,
                // DisplayPost() 호출될 때마다 Bind()가 imageID를 새로 갱신해줌.
                CollectibleImageBinder.Bind(postImage, data.collectibleImageID);
            }
        }

        // 💡 [추가] 게시글 본문 자체에 단서 ID(clueID)가 들어있는지 검사하고 클릭 이벤트 연결
        if (postContentButton != null)
        {
            postContentButton.onClick.RemoveAllListeners();
            if (!string.IsNullOrEmpty(data.clueID) && DataLogManager.Instance != null)
            {
                postContentButton.onClick.AddListener(() => {
                    // ⚠️ [기존 버그, 손대지 않음] 아래 AcquireClue가 본문의 clueID가 아니라
                    // 이미지용 ID(imageQuestID/imageClueID)를 넘기고 있습니다. 의도하신 게 맞는지 확인 필요.
                    // 💡 [추가] 실제 게시글 제목을 같이 전달
                    DataLogManager.Instance.AcquireClue(data.imageQuestID, data.imageClueID, data.title);
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
                        // ⚠️ [기존 버그, 손대지 않음] 아래 AcquireClue가 댓글의 clueID가 아니라
                        // 이미지용 ID(imageQuestID/imageClueID)를 넘기고 있습니다. 의도하신 게 맞는지 확인 필요.
                        // 💡 [추가] "게시글 제목 (댓글 작성자)" 형태로 제목 전달
                        DataLogManager.Instance.AcquireClue(data.imageQuestID, data.imageClueID, $"{data.title} ({cData.author})");
                    });
                }
            }
        }

        gameObject.SetActive(true);
    }

    private void CollectClue(string targetClueID)
    {
        if (string.IsNullOrEmpty(targetClueID) || targetClueID.ToLower() == "none") return;

        // 💡 [추가] 수집 모드가 꺼져 있다면 클릭해도 단서를 수집하지 않고 리턴시킵니다!
        if (DataLogManager.Instance != null && !DataLogManager.Instance.IsClueSearchModeActive)
        {
            Debug.Log("현재 단서 수집 모드가 비활성화되어 있어 수집할 수 없습니다.");
            return;
        }

        Debug.Log($"[SNS] 단서 수집 요청: {targetClueID}");

        if (DataLogManager.Instance != null)
        {
            DataLogManager.Instance.AcquireClue(questID, targetClueID);
        }
    }

    public void ClosePage()
    {
        gameObject.SetActive(false);
    }
}