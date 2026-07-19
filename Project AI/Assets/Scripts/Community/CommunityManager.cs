using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class CommunityManager : MonoBehaviour
{
    // 💡 [추가] 싱글톤 인스턴스
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

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("CommunityData");
        if (csvFile != null)
        {
            ParseCSV(csvFile.text);
            GenerateUI();
        }
    }

    void ParseCSV(string csvText)
    {
        string[] rows = Regex.Split(csvText, @"\r\n|\n|\r");
        string csvParserPattern = ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)";

        for (int i = 1; i < rows.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(rows[i])) continue;

            string[] columns = Regex.Split(rows[i], csvParserPattern);

            if (columns.Length >= 14)
            {
                int.TryParse(columns[0].Trim().Replace("\"", ""), out int id);
                if (id == 0) continue;

                PostData existingPost = postList.Find(p => p.postID == id);

                if (existingPost == null)
                {
                    existingPost = new PostData();
                    existingPost.postID = id;

                    existingPost.title = columns[1].Trim().Replace("\"", "");
                    existingPost.author = columns[2].Trim().Replace("\"", "");
                    existingPost.date = columns[3].Trim().Replace("\"", "");

                    int.TryParse(columns[4].Trim().Replace("\"", ""), out existingPost.likes);
                    int.TryParse(columns[5].Trim().Replace("\"", ""), out existingPost.dislikes);

                    string rawContent = columns[6].Trim();
                    if (rawContent.StartsWith("\"") && rawContent.EndsWith("\""))
                    {
                        rawContent = rawContent.Substring(1, rawContent.Length - 2);
                    }
                    existingPost.content = rawContent.Replace("\"\"", "\"").Replace("\\n", "\n");

                    existingPost.imageName = columns[7].Trim().Replace("\"", "");
                    existingPost.clueID = columns[12].Trim().Replace("\"", "");

                    postList.Add(existingPost);
                }
                else if (string.IsNullOrEmpty(existingPost.clueID) && !string.IsNullOrWhiteSpace(columns[12]))
                {
                    existingPost.clueID = columns[12].Trim().Replace("\"", "");
                }

                if (columns.Length >= 12 && !string.IsNullOrWhiteSpace(columns[8]))
                {
                    CommentData comment = new CommentData();
                    comment.postID = id;
                    comment.author = columns[8].Trim().Replace("\"", "");
                    comment.content = columns[9].Trim().Replace("\"", "");

                    bool.TryParse(columns[10].Trim().Replace("\"", ""), out comment.isEmoticon);
                    comment.emoticonName = columns[11].Trim().Replace("\"", "");
                    comment.clueID = columns[13].Trim().Replace("\"", "");

                    existingPost.comments.Add(comment);
                }
            }
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
            }
        }
    }

    public void OpenDetailPage(PostData data)
    {
        detailPageUI.DisplayPost(data);

        if (windowManager != null && detailPageUI != null)
        {
            RectTransform detailRect = detailPageUI.GetComponent<RectTransform>();
            windowManager.RepositionPopupWindow(detailRect);
        }
    }

    /// <summary>
    /// 💡 [추가] DataLogManager가 "원본 보기"를 요청할 때 호출.
    /// 게시글 본문의 clueID든, 댓글의 clueID든 일치하는 걸 찾아
    /// 해당 게시글의 상세 페이지를 열어줍니다.
    /// </summary>
    public bool TryOpenClueSource(string clueID)
    {
        if (string.IsNullOrEmpty(clueID)) return false;

        foreach (var post in postList)
        {
            bool isPostMatch = !string.IsNullOrEmpty(post.clueID) && post.clueID == clueID;
            bool isCommentMatch = post.comments.Exists(c => !string.IsNullOrEmpty(c.clueID) && c.clueID == clueID);

            if (isPostMatch || isCommentMatch)
            {
                if (communityWindowManager != null)
                {
                    communityWindowManager.RestoreWindow();
                }

                OpenDetailPage(post);
                return true;
            }
        }

        return false;
    }
}