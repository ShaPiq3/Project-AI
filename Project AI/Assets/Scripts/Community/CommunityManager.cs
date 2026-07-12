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
        // [수정] 윈도우(\r\n)와 맥(\n) 환경의 줄바꿈을 완벽히 분리하기 위해 정규식으로 줄바꿈 분할 ⭐
        string[] rows = Regex.Split(csvText, @"\r\n|\n|\r");
        string csvParserPattern = ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)";

        for (int i = 1; i < rows.Length; i++)
        {
            // 빈 줄은 과감히 패스
            if (string.IsNullOrWhiteSpace(rows[i])) continue;

            string[] columns = Regex.Split(rows[i], csvParserPattern);

            // [안전장치] 혹시 모를 배열 길이 부족 예방 및 데이터 검증
            if (columns.Length >= 8)
            {
                // postID 읽기 (앞뒤 공백 및 쌍따옴표 완전 제거)
                int.TryParse(columns[0].Trim().Replace("\"", ""), out int id);
                if (id == 0) continue; // ID 파싱 실패 시 건너뜀

                // 중복 체크
                PostData existingPost = postList.Find(p => p.postID == id);

                if (existingPost == null)
                {
                    existingPost = new PostData();
                    existingPost.postID = id;

                    // [수정] Trim('"') 대신 안전하게 Replace("\"","")로 쌍따옴표를 완전히 걷어냅니다. ⭐
                    existingPost.title = columns[1].Trim().Replace("\"", "");
                    existingPost.author = columns[2].Trim().Replace("\"", "");
                    existingPost.date = columns[3].Trim().Replace("\"", "");

                    int.TryParse(columns[4].Trim().Replace("\"", ""), out existingPost.likes);
                    int.TryParse(columns[5].Trim().Replace("\"", ""), out existingPost.dislikes);

                    // 본문 내용 가공
                    string rawContent = columns[6].Trim();
                    if (rawContent.StartsWith("\"") && rawContent.EndsWith("\""))
                    {
                        rawContent = rawContent.Substring(1, rawContent.Length - 2);
                    }
                    existingPost.content = rawContent.Replace("\"\"", "\"").Replace("\\n", "\n");

                    // 본문 이미지 이름 세팅
                    existingPost.imageName = columns[7].Trim().Replace("\"", "");

                    postList.Add(existingPost);
                }

                // 댓글 데이터 처리 (12열까지 확보되었는지 안전하게 검사)
                if (columns.Length >= 12 && !string.IsNullOrWhiteSpace(columns[8]))
                {
                    CommentData comment = new CommentData();
                    comment.postID = id;
                    comment.author = columns[8].Trim().Replace("\"", "");
                    comment.content = columns[9].Trim().Replace("\"", "");

                    bool.TryParse(columns[10].Trim().Replace("\"", ""), out comment.isEmoticon);
                    comment.emoticonName = columns[11].Trim().Replace("\"", "");

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
    }
}