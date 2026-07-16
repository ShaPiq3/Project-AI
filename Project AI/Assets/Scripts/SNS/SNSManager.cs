using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class SNSManager : MonoBehaviour
{
    [Header("UI 프리팹 및 스크롤 생성 구역")]
    [SerializeField] private GameObject snsPostPrefab;      // SNSPost 컴포넌트가 붙은 개별 피드 프리팹
    [SerializeField] private Transform contentTransform;    // 스크롤뷰 내부의 Content

    private List<SNSPostData> snsPostList = new List<SNSPostData>();

    void Start()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("SNSData");
        if (csvFile != null)
        {
            ParseAdvancedCSV(csvFile.text);
            GenerateSNSUI();
        }
        else
        {
            Debug.LogError("Resources 폴더 내에 'SNSData' CSV 파일을 찾을 수 없습니다!");
        }
    }

    void ParseAdvancedCSV(string csvText)
    {
        List<List<string>> grid = new List<List<string>>();
        List<string> row = new List<string>();
        StringBuilder cell = new StringBuilder();

        bool insideQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            if (insideQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        insideQuotes = false;
                    }
                }
                else
                {
                    cell.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    insideQuotes = true;
                }
                else if (c == ',')
                {
                    row.Add(cell.ToString().Trim());
                    cell.Clear();
                }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    {
                        i++;
                    }

                    row.Add(cell.ToString().Trim());
                    cell.Clear();

                    if (row.Count > 0 && !string.IsNullOrWhiteSpace(row[0]))
                    {
                        grid.Add(new List<string>(row));
                    }
                    row.Clear();
                }
                else
                {
                    cell.Append(c);
                }
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString().Trim());
            if (!string.IsNullOrWhiteSpace(row[0])) grid.Add(row);
        }

        // --- 데이터 바인딩 시작 (인덱스 보정 반영) ---
        for (int i = 1; i < grid.Count; i++)
        {
            List<string> columns = grid[i];
            if (columns.Count < 5) continue; // 최소 게시글 데이터 열 개수 방어

            int.TryParse(columns[0], out int id);
            if (id == 0) continue;

            SNSPostData existingPost = snsPostList.Find(p => p.postID == id);

            if (existingPost == null)
            {
                existingPost = new SNSPostData();
                existingPost.postID = id;
                existingPost.author = columns[1];
                existingPost.profileImageName = columns[2];
                existingPost.content = columns[3];
                existingPost.postImageName = columns[4];

                snsPostList.Add(existingPost);
            }

            // 💡 [좋아요 삭제 보정] 댓글 데이터는 인덱스 5번(6번째 열)부터 파싱합니다.
            if (columns.Count >= 7 && !string.IsNullOrWhiteSpace(columns[5]))
            {
                SNSCommentData comment = new SNSCommentData();
                comment.postID = id;
                comment.author = columns[5];       // commentAuthor
                comment.content = columns[6];      // commentContent

                if (columns.Count >= 9)
                {
                    bool.TryParse(columns[7], out comment.isEmoticon); // isEmoticon
                    comment.emoticonName = columns[8];                 // emoticonName
                }

                existingPost.comments.Add(comment);
            }
        }
    }

    public void GenerateSNSUI()
    {
        foreach (Transform child in contentTransform) Destroy(child.gameObject);

        foreach (SNSPostData postData in snsPostList)
        {
            GameObject newItem = Instantiate(snsPostPrefab, contentTransform);
            SNSPost postScript = newItem.GetComponent<SNSPost>();

            if (postScript != null)
            {
                postScript.Setup(postData);
            }
        }
    }
}
