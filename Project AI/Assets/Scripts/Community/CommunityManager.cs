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
        string[] rows = csvText.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        string csvParserPattern = ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)";

        for (int i = 1; i < rows.Length; i++)
        {
            // 정규식을 이용해 스마트하게 분할
            string[] columns = Regex.Split(rows[i], csvParserPattern);

            // [조건 체크] 엑셀 열 개수가 충분한지 확인 (새 데이터 구조에 맞춰 확장)
            if (columns.Length >= 8)
            {
                // 1. postID 먼저 읽기 (정수로 변환)
                int.TryParse(columns[0].Trim().Trim('"'), out int id);

                // 2. 이미 등록한 게시글 상자가 리스트에 있는지 검사 (중복 처리의 핵심! ⭐)
                PostData existingPost = postList.Find(p => p.postID == id);

                if (existingPost == null)
                {
                    // 3. 처음 보는 postID라면 새로운 PostData 상자를 만들어서 정보 채우기
                    existingPost = new PostData();
                    existingPost.postID = id;
                    existingPost.title = columns[1].Trim().Trim('"');
                    existingPost.author = columns[2].Trim().Trim('"');
                    existingPost.date = columns[3].Trim().Trim('"');

                    int.TryParse(columns[4].Trim().Trim('"'), out existingPost.likes);
                    int.TryParse(columns[5].Trim().Trim('"'), out existingPost.dislikes);

                    // 본문 내용 가공
                    string rawContent = columns[6].Trim();
                    if (rawContent.StartsWith("\"") && rawContent.EndsWith("\""))
                    {
                        rawContent = rawContent.Substring(1, rawContent.Length - 2);
                    }
                    existingPost.content = rawContent.Replace("\"\"", "\"").Replace("\\n", "\n");

                    // 본문 이미지 이름 세팅
                    existingPost.imageName = columns[7].Trim().Trim('"');

                    // 완성된 게시글 상자를 메인 리스트에 보관
                    postList.Add(existingPost);
                }

                // 4. 게시글 등록 여부와 무관하게, 뒤쪽에 '댓글 데이터'가 존재하면 주머니에 무조건 추가하기! ⭐
                // 엑셀 열에서 댓글 작성자(9번째 열)의 정보를 확인합니다.
                if (columns.Length >= 12 && !string.IsNullOrEmpty(columns[8].Trim()))
                {
                    CommentData comment = new CommentData();
                    comment.postID = id;
                    comment.author = columns[8].Trim().Trim('"');
                    comment.content = columns[9].Trim().Trim('"');

                    // 이모티콘 여부 판단 (true / false)
                    bool.TryParse(columns[10].Trim().Trim('"'), out comment.isEmoticon);
                    comment.emoticonName = columns[11].Trim().Trim('"');

                    // 해당 게시글 주머니에 쏙 집어넣기
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
                // 자신(this = CommunityManager)도 같이 넘겨줍니다.
                itemUI.Setup(post, this);
            }
        }
    }

    // 프리팹 버튼이 클릭되면 이 함수가 실행됩니다!
    public void OpenDetailPage(PostData data)
    {
        // 상세 페이지 UI에 데이터 전달 및 오픈 명령
        detailPageUI.DisplayPost(data);
    }
}