using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class CommunityManager : MonoBehaviour
{
    public GameObject postItemPrefab;
    public Transform contentTransform;

    [Header("상세 페이지 UI 스크립트 연결")]
    public PostDetailPageUI detailPageUI;

    // 🌟 [오늘 추가] 매니저와 연동하여 창을 안 겹치게 배치하기 위한 변수 선언
    [Header("WindowManager 연동")]
    [SerializeField] private WindowManager windowManager;

    private List<PostData> postList = new List<PostData>();

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
        // 1. 기존의 커뮤니티 상세 페이지 화면 출력 로직
        detailPageUI.DisplayPost(data);

        // 🌟 [오늘 추가] 상세 페이지 창 오브젝트의 RectTransform을 추출하여 랜덤 배치 지시!
        if (windowManager != null && detailPageUI != null)
        {
            RectTransform detailRect = detailPageUI.GetComponent<RectTransform>();
            windowManager.RepositionPopupWindow(detailRect);
        }
    }
}
