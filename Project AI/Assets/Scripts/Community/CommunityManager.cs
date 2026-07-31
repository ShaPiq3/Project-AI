using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class CommunityManager : MonoBehaviour
{
    // 💡 싱글톤 인스턴스
    public static CommunityManager Instance { get; private set; }

    public GameObject postItemPrefab;
    public Transform contentTransform;

    [Header("상세 페이지 UI 스크립트 연결")]
    public PostDetailPageUI detailPageUI;

    [Header("WindowManager 연동")]
    [SerializeField] private WindowManager windowManager;

    [Header("이 커뮤니티 창 자체를 여닫는 InGameWindowManager (선택 사항)")]
    [Tooltip("사이드바 등에서 이 커뮤니티 창을 최소화/복원하는 InGameWindowManager가 따로 있다면 연결하세요.")]
    [SerializeField] private InGameWindowManager communityWindowManager;

    private List<PostData> postList = new List<PostData>();

    // 💡 [추가] 여러 창을 동시에 띄우기 위해, 게시글 postID별로 "지금 열려있는 창"을 추적합니다.
    // 같은 게시글을 또 열려고 하면 새로 만들지 않고 이 창을 맨 앞으로만 가져옵니다.
    private Dictionary<int, PostDetailPageUI> openPostWindows = new Dictionary<int, PostDetailPageUI>();

    // CSV 필드 하나 안에 콤마/줄바꿈이 포함될 수 있으므로(따옴표로 감싼 셀) 정규식으로 셀 분리
    private const string CsvParserPattern = ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)";

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // 💡 [추가] 상세 페이지 템플릿은 시작할 때 꺼둡니다 (여러 창 복제 방식이므로
        // 원본 템플릿 자체는 화면에 보이면 안 됨). NewsListManager와 동일한 패턴.
        if (detailPageUI != null) detailPageUI.gameObject.SetActive(false);

        // 💡 포스트 CSV(CommunityData)와 댓글 CSV(CommentExcelData)를 각각 따로 불러와서 병합
        TextAsset postCsv = Resources.Load<TextAsset>("CommunityExcelData");
        TextAsset commentCsv = Resources.Load<TextAsset>("CommentExcelData");

        if (postCsv != null)
        {
            ParsePostCSV(postCsv.text);

            if (commentCsv != null)
            {
                ParseCommentCSV(commentCsv.text);
            }
            else
            {
                Debug.LogWarning("[CommunityManager] Resources 폴더에서 'CommentExcelData' 를 찾을 수 없습니다. 댓글 없이 진행합니다.");
            }

            GenerateUI();
        }
        else
        {
            Debug.LogError("[CommunityManager] Resources 폴더에서 'PostData' 를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 포스트 전용 CSV 파싱.
    /// 예상 컬럼 순서: postID,title,author,date,likes,dislikes,content,imageName,clueID,imageClueID,collectibleImageID,titleClueID,clueParagraphIndex,bodyClueID
    /// (실제 CommunityData.csv 컬럼 순서가 다르면 이 함수만 맞춰서 수정하면 됩니다)
    /// </summary>
    private void ParsePostCSV(string csvText)
    {
        string[] rows = Regex.Split(csvText, @"\r\n|\n|\r");

        for (int i = 1; i < rows.Length; i++) // 0번째는 헤더
        {
            if (string.IsNullOrWhiteSpace(rows[i])) continue;

            string[] columns = Regex.Split(rows[i], CsvParserPattern);
            if (columns.Length < 9) continue;

            int.TryParse(columns[0].Trim().Replace("\"", ""), out int id);
            if (id == 0) continue;

            PostData post = new PostData();
            post.postID = id;
            post.title = columns[1].Trim().Replace("\"", "");
            post.author = columns[2].Trim().Replace("\"", "");
            post.date = columns[3].Trim().Replace("\"", "");

            int.TryParse(columns[4].Trim().Replace("\"", ""), out post.likes);
            int.TryParse(columns[5].Trim().Replace("\"", ""), out post.dislikes);

            string rawContent = columns[6].Trim();
            if (rawContent.StartsWith("\"") && rawContent.EndsWith("\""))
            {
                rawContent = rawContent.Substring(1, rawContent.Length - 2);
            }
            // 💡 [변경] 뉴스(NewsData.body)와 동일하게 '|' 기호로 문단을 구분해서 씁니다.
            // 기존처럼 "\\n"이 들어오면 문단 구분용으로 호환되게 줄바꿈 문자로도 변환해둡니다.
            post.content = rawContent.Replace("\"\"", "\"").Replace("\\n", "\n");

            post.imageName = columns[7].Trim().Replace("\"", "");
            post.clueID = columns[8].Trim().Replace("\"", "");

            // 💡 이 게시글의 이미지를 클릭했을 때 수집할 기존 단서 ID (10번째 컬럼)
            post.imageClueID = (columns.Length >= 10) ? columns[9].Trim().Replace("\"", "") : "";

            // 💡 이미지 생성 퀘스트용 ID (11번째 컬럼)
            post.collectibleImageID = (columns.Length >= 11) ? columns[10].Trim().Replace("\"", "") : "";

            // 💡 제목 클릭 단서 ID (12번째 컬럼). 비어있으면 제목은 수집 대상 아님.
            post.titleClueID = (columns.Length >= 12) ? columns[11].Trim().Replace("\"", "") : "";

            // 💡 [추가] 단서가 숨겨진 본문 문단 번호 (13번째 컬럼)
            int paragraphIdx = 0;
            if (columns.Length >= 13)
            {
                int.TryParse(columns[12].Trim().Replace("\"", ""), out paragraphIdx);
            }
            post.clueParagraphIndex = paragraphIdx;

            // 💡 [추가] 그 문단 클릭 시 수집할 단서 ID (14번째 컬럼)
            post.bodyClueID = (columns.Length >= 14) ? columns[13].Trim().Replace("\"", "") : "";

            postList.Add(post);
        }
    }

    /// <summary>
    /// 댓글 전용 CSV 파싱 후, postID 기준으로 해당 게시글의 comments 리스트에 병합.
    /// 컬럼 순서: postID,author,content,isEmoticon,emoticonName,clueID
    /// </summary>
    private void ParseCommentCSV(string csvText)
    {
        string[] rows = Regex.Split(csvText, @"\r\n|\n|\r");

        for (int i = 1; i < rows.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(rows[i])) continue;

            string[] columns = Regex.Split(rows[i], CsvParserPattern);
            if (columns.Length < 5) continue;

            int.TryParse(columns[0].Trim().Replace("\"", ""), out int postID);
            if (postID == 0) continue;

            PostData targetPost = postList.Find(p => p.postID == postID);
            if (targetPost == null)
            {
                Debug.LogWarning($"[CommunityManager] 댓글의 postID {postID} 에 해당하는 게시글을 찾을 수 없습니다.");
                continue;
            }

            CommentData comment = new CommentData();
            comment.postID = postID;
            comment.author = columns[1].Trim().Replace("\"", "");
            comment.content = columns[2].Trim().Replace("\"", "");
            bool.TryParse(columns[3].Trim().Replace("\"", ""), out comment.isEmoticon);
            comment.emoticonName = columns[4].Trim().Replace("\"", "");
            comment.clueID = (columns.Length >= 6) ? columns[5].Trim().Replace("\"", "") : "";

            targetPost.comments.Add(comment);
        }
    }

    void GenerateUI()
    {
        foreach (Transform child in contentTransform) Destroy(child.gameObject);

        foreach (PostData post in postList)
        {
            GameObject newItem = Instantiate(postItemPrefab, contentTransform);
            PostItemUI itemUI = newItem.GetComponent<PostItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(post, this);
                // 💡 리스트 아이템(PostItemUI)에는 이미지가 없으므로 여기선 수집 등록 안 함.
                // 실제 이미지는 상세 페이지(PostDetailPageUI)에서 보여지므로,
                // 수집 등록은 OpenDetailPage() / PostDetailPageUI.DisplayPost() 쪽에서 처리함.
            }
        }
    }

    // 💡 [변경] 하나의 상세 페이지를 재사용하던 방식에서, 클릭마다 새로 복제해서
    // 여러 창을 동시에 띄우는 방식으로 변경. 같은 게시글이 이미 열려있으면
    // 새로 만들지 않고 그 창을 맨 앞으로만 가져옵니다.
    public void OpenDetailPage(PostData data)
    {
        if (detailPageUI == null) return;

        // 이미 열려있는 게시글이라면 그 창을 맨 앞으로만 가져오고 끝
        if (openPostWindows.TryGetValue(data.postID, out PostDetailPageUI existingWindow) && existingWindow != null)
        {
            existingWindow.transform.SetAsLastSibling();

            PopupSpawnAnimation existingAnim = existingWindow.GetComponent<PopupSpawnAnimation>();
            if (existingAnim != null) existingAnim.PlayPopAnimation();


            return;
        }

        // 새 창 복제 (detailPageUI를 "복제할 원본 템플릿"으로 사용)
        PostDetailPageUI newWindow = Instantiate(detailPageUI, detailPageUI.transform.parent);
        newWindow.gameObject.SetActive(true);
        newWindow.DisplayPost(data);

        openPostWindows[data.postID] = newWindow;

        if (windowManager != null)
        {
            RectTransform detailRect = newWindow.GetComponent<RectTransform>();
            windowManager.RepositionPopupWindow(detailRect);
        }

        PopupSpawnAnimation newAnim = newWindow.GetComponent<PopupSpawnAnimation>();
        if (newAnim != null) newAnim.PlayPopAnimation();
    }

    /// <summary>
    /// 💡 DataLogManager가 "원본 보기"를 요청할 때 호출.
    /// 게시글 제목의 clueID든, 본문 문단의 clueID든, 본문 전체의 clueID든, 댓글의 clueID든
    /// 일치하는 걸 찾아 해당 게시글의 상세 페이지를 열어줍니다.
    /// </summary>
    public bool TryOpenClueSource(string clueID)
    {
        if (string.IsNullOrEmpty(clueID)) return false;

        foreach (var post in postList)
        {
            bool isTitleMatch = !string.IsNullOrEmpty(post.titleClueID) && post.titleClueID == clueID;
            bool isPostMatch = !string.IsNullOrEmpty(post.clueID) && post.clueID == clueID;
            bool isBodyMatch = !string.IsNullOrEmpty(post.bodyClueID) && post.bodyClueID == clueID;
            bool isCommentMatch = post.comments.Exists(c => !string.IsNullOrEmpty(c.clueID) && c.clueID == clueID);

            // 💡 [추가] 본문 안에 "[CLUE:아이디]" 태그로 심어둔 (여러 개일 수 있는) 단서도 검색
            bool isTaggedBodyMatch = ContentContainsClueTag(post.content, clueID);

            if (isTitleMatch || isPostMatch || isBodyMatch || isCommentMatch || isTaggedBodyMatch)
            {

                OpenDetailPage(post);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 💡 [추가] 패널 링크(<link="post:12">)에서 특정 게시글을 ID로 바로 열 때 사용.
    /// </summary>
    public bool OpenPostByID(int postID)
    {
        var data = postList.Find(p => p.postID == postID);
        if (data == null)
        {
            Debug.LogWarning($"[CommunityManager] postID '{postID}' 에 해당하는 게시글을 찾을 수 없습니다.");
            return false;
        }

        OpenDetailPage(data);
        return true;
    }

    /// <summary>
    /// 💡 [추가] 본문을 '|'로 나눈 문단들 중에, "[CLUE:아이디]" 태그로 시작하는 문단이
    /// 주어진 clueID와 일치하는 게 있는지 확인합니다. (문단 여러 개가 단서인 경우 지원)
    /// </summary>
    private bool ContentContainsClueTag(string content, string clueID)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(clueID)) return false;

        string[] paragraphs = content.Split('|');
        foreach (var paragraph in paragraphs)
        {
            string trimmed = paragraph.Trim();
            if (!trimmed.StartsWith("[CLUE:")) continue;

            int closeBracketIndex = trimmed.IndexOf(']');
            if (closeBracketIndex <= 6) continue;

            string tagId = trimmed.Substring(6, closeBracketIndex - 6);
            if (tagId == clueID) return true;
        }

        return false;
    }
}