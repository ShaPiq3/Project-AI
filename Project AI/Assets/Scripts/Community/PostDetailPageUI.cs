using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
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

    [Tooltip("기존 방식(호환용). 아래 Text Template + Text Container를 연결하면 이 필드는 더 이상 쓰이지 않습니다.")]
    public TMP_Text contentText;

    // 💡 [추가] NewsCard와 동일한 방식: 본문을 '|' 기호로 문단을 나눠서
    // 문단마다 별도의 TMP 오브젝트로 생성합니다. (줄바꿈 안정성 + 문단별 단서 수집을 위함)
    [Header("③번 구역 - 동적 문단 생성 설정 (권장)")]
    [Tooltip("복사 원본이 될 TMP 템플릿 오브젝트 (비활성화 상태로 두세요, Rich Text 체크 필요)")]
    [SerializeField] private TextMeshProUGUI textTemplate;
    [Tooltip("문단들이 생성되어 쌓일 부모 (Vertical Layout Group 필요)")]
    [SerializeField] private Transform textContainer;

    private List<TextMeshProUGUI> spawnedTexts = new List<TextMeshProUGUI>();

    // 💡 [추가] 본문 전체 영역을 클릭했을 때 단서를 수집할 수 있도록 버튼 컴포넌트 연결
    // 만약 이미 본문 오브젝트에 버튼이 붙어있다면 인스펙터에서 드래그하여 연결해 주시면 됩니다.
    [Header("③번 구역 단서 수집용")]
    public Button postContentButton;
    [SerializeField] private string questID;

    [Header("④번 구역 (댓글 리스트 영역)")]
    public Transform commentListTransform;
    public GameObject commentPrefab;

    private int currentPostID = -1;

    public void DisplayPost(PostData data)
    {
        transform.SetAsLastSibling();
        currentPostID = data.postID;
        // 1. ①번 구역 세팅
        titleText.text = data.title;
        authorText.text = data.author;
        dateText.text = data.date;

        // 💡 [추가] 이 창의 실제 게시글 제목을 태스크바에도 반영
        TaskbarWindowTrigger taskbarTrigger = GetComponent<TaskbarWindowTrigger>();
        if (taskbarTrigger != null)
        {
            taskbarTrigger.SetWindowTitle(data.title);
        }

        // 💡 [변경] 제목이 단서인지 여부와 상관없이 항상 ClueTextHoverEffect를 붙입니다.
        Button titleBtn = titleText.gameObject.GetComponent<Button>();
        if (titleBtn != null) Destroy(titleBtn); // 기존 구식 버튼은 충돌 방지를 위해 제거

        ClueTextHoverEffect titleHoverEffect = titleText.gameObject.GetComponent<ClueTextHoverEffect>();
        if (titleHoverEffect == null) titleHoverEffect = titleText.gameObject.AddComponent<ClueTextHoverEffect>();
        titleText.raycastTarget = true;
        titleHoverEffect.Configure(data.titleClueID, questID, data.title);

        PanelLinkParagraphEffect titleLinkEffect = titleText.gameObject.GetComponent<PanelLinkParagraphEffect>();
        if (data.title.Contains("<link=\""))
        {
            if (titleLinkEffect == null)
                titleLinkEffect = titleText.gameObject.AddComponent<PanelLinkParagraphEffect>();
            titleLinkEffect.Setup(data.title);
        }
        else
        {
            if (titleLinkEffect != null) Destroy(titleLinkEffect);
        }

        // 상단 헤더 배지 동기화
        headerLikeText.text = $"추천 {data.likes}";
        headerDislikeText.text = $"비추 {data.dislikes}"; // ⭐ 세팅

        // ③번 구역 하단 버튼 텍스트 동기화
        buttonLikeText.text = $"추천 ▲ {data.likes}";
        buttonDislikeText.text = $"비추 ▼ {data.dislikes}"; // ⭐ 세팅

        // 2. ②번 구역 세팅 - 본문 렌더링
        ClearSpawnedTexts();

        if (textTemplate != null && textContainer != null)
        {
            // 💡 [신규 방식] NewsCard와 동일하게 '|' 기호로 문단을 나눠서 각각 별도 TMP로 생성
            if (textTemplate.gameObject.activeSelf) textTemplate.gameObject.SetActive(false);

            string[] paragraphs = (data.content ?? "").Split('|');

            for (int i = 0; i < paragraphs.Length; i++)
            {
                string rawParagraph = paragraphs[i].Trim();
                if (string.IsNullOrEmpty(rawParagraph)) continue;

                // 💡 [추가] 문단이 여러 개 단서일 수 있으므로, "[CLUE:아이디]" 태그로 시작하는지 확인
                string paragraphText = rawParagraph;
                string taggedClueID = null;

                if (rawParagraph.StartsWith("[CLUE:"))
                {
                    int closeBracketIndex = rawParagraph.IndexOf(']');
                    if (closeBracketIndex > 6)
                    {
                        taggedClueID = rawParagraph.Substring(6, closeBracketIndex - 6);
                        paragraphText = rawParagraph.Substring(closeBracketIndex + 1).TrimStart();
                    }
                }

                TextMeshProUGUI newText = Instantiate(textTemplate, textContainer);
                newText.richText = true;
                newText.text = paragraphText;

                int currentParagraphNum = i + 1;

                // 💡 [변경] 기존 방식(문단 1개만 지정)도 계속 지원 - 하위 호환
                bool isLegacySingleClueParagraph =
                    data.clueParagraphIndex > 0 &&
                    currentParagraphNum == data.clueParagraphIndex &&
                    !string.IsNullOrEmpty(data.bodyClueID);

                string finalClueID = !string.IsNullOrEmpty(taggedClueID)
                    ? taggedClueID
                    : (isLegacySingleClueParagraph ? data.bodyClueID : null);

                // 💡 [변경] 단서 문단인지 여부와 상관없이 모든 문단에 항상 ClueTextHoverEffect를 붙입니다.
                ClueTextHoverEffect hoverEffect = newText.gameObject.GetComponent<ClueTextHoverEffect>();
                if (hoverEffect == null)
                {
                    hoverEffect = newText.gameObject.AddComponent<ClueTextHoverEffect>();
                }

                newText.raycastTarget = true;
                hoverEffect.Configure(finalClueID, questID, data.title);

                if (paragraphText.Contains("<link=\""))
                {
                    PanelLinkParagraphEffect linkEffect = newText.gameObject.AddComponent<PanelLinkParagraphEffect>();
                    linkEffect.Setup(paragraphText);
                }

                newText.gameObject.SetActive(true);
                spawnedTexts.Add(newText);
            }
        }
        else if (contentText != null)
        {
            // 💡 [기존 방식, 호환용] Text Template/Container가 아직 설정 안 되어 있으면
            // 예전처럼 본문 전체를 하나의 텍스트 박스에 넣습니다.
            contentText.text = data.content;
        }

        // PostDetailPageUI.cs 내부의 이미지 렌더링 함수 예시
        if (!string.IsNullOrEmpty(data.imageName))
        {
            // 💡 [변경] 실제 이미지가 저장된 폴더명(PostImages)에 맞게 수정
            Sprite loadedSprite = Resources.Load<Sprite>($"PostImages/{data.imageName}");
            if (loadedSprite != null)
            {
                postImage.sprite = loadedSprite;
                postImage.gameObject.SetActive(true);

                // 💡 [변경] 단서인지 여부와 상관없이 항상 ClueImageHoverEffect를 붙입니다.
                Button imgBtn = postImage.gameObject.GetComponent<Button>();
                if (imgBtn != null) Destroy(imgBtn); // 기존 구식 버튼은 충돌 방지를 위해 제거

                ClueImageHoverEffect imgHover = postImage.gameObject.GetComponent<ClueImageHoverEffect>();
                if (imgHover == null) imgHover = postImage.gameObject.AddComponent<ClueImageHoverEffect>();

                string resolvedImageClueID = (!string.IsNullOrEmpty(data.imageClueID) && data.imageClueID.ToLower() != "none")
                    ? data.imageClueID : "";
                imgHover.Configure(resolvedImageClueID, questID, data.title);

                // 💡 이미지 생성 퀘스트 수집 대상으로 자동 등록.
                // 이 패널은 게시글마다 새로 생성되지 않고 재사용되므로,
                // DisplayPost() 호출될 때마다 Bind()가 imageID를 새로 갱신해줌.
                CollectibleImageBinder.Bind(postImage, data.collectibleImageID);
            }
        }

        // 💡 게시글 본문 자체(전체)에 단서 ID(clueID)가 들어있는지 검사하고 클릭 이벤트 연결
        if (postContentButton != null)
        {
            postContentButton.onClick.RemoveAllListeners();
            if (!string.IsNullOrEmpty(data.clueID) && DataLogManager.Instance != null)
            {
                postContentButton.onClick.AddListener(() => {
                    // 💡 [수정] 예전엔 본문 clueID가 아니라 이미지용 ID를 잘못 넘기고 있었습니다.
                    // 이제 본문 자체의 clueID를 쓰고, questID도 마스터 데이터에서 자동으로 찾습니다.
                    string resolvedQuestID = DataLogManager.Instance.ResolveQuestID(data.clueID, questID);
                    DataLogManager.Instance.AcquireClue(resolvedQuestID, data.clueID, data.title);
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
                // 💡 [변경] 댓글도 텍스트/이모티콘 단위로 항상 반응하도록 clueID/questID를 그대로 넘기고,
                // 실제 부착은 CommentItemUI.Setup 내부(ClueTextHoverEffect/ClueImageHoverEffect)에서 처리합니다.
                cItem.GetComponent<CommentItemUI>().Setup(cData, cData.clueID, questID, $"{data.title} ({cData.author})");
            }
        }

        gameObject.SetActive(true);
    }

    private void ClearSpawnedTexts()
    {
        foreach (var txt in spawnedTexts)
        {
            if (txt != null) Destroy(txt.gameObject);
        }
        spawnedTexts.Clear();
    }

    private void CollectClue(string targetClueID)
    {
        if (string.IsNullOrEmpty(targetClueID) || targetClueID.ToLower() == "none") return;

        // 💡 수집 모드가 꺼져 있다면 클릭해도 단서를 수집하지 않고 리턴시킵니다!
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
        // 💡 [추가] CommunityManager가 들고 있는 참조도 같이 정리
        if (CommunityManager.Instance != null)
        {
            CommunityManager.Instance.NotifyWindowClosed(currentPostID);
        }

        Destroy(gameObject);
    }
}