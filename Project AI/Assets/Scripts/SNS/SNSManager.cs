using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

public class SNSManager : MonoBehaviour
{
    // 💡 [추가] 싱글톤 인스턴스
    public static SNSManager Instance { get; private set; }

    [Header("UI 프리팹 및 스크롤 생성 구역")]
    [SerializeField] private GameObject snsPostPrefab;      // SNSPost 컴포넌트가 붙은 개별 피드 프리팹
    [SerializeField] private Transform contentTransform;    // 스크롤뷰 내부의 Content

    [Header("원본 보기 시 스크롤 이동에 필요")]
    [Tooltip("SNS 피드를 감싸는 ScrollRect. 원본 게시물로 스크롤 이동할 때 사용합니다.")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("이 SNS 창 자체를 여닫는 InGameWindowManager (선택 사항)")]
    [SerializeField] private InGameWindowManager snsWindowManager;

    private List<SNSPostData> snsPostList = new List<SNSPostData>();
    // 💡 [추가] snsPostList와 같은 순서로 생성된 실제 SNSPost 오브젝트들을 저장 (스크롤 대상 찾기용)
    private List<SNSPost> instantiatedPosts = new List<SNSPost>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

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

        for (int i = 1; i < grid.Count; i++)
        {
            List<string> columns = grid[i];
            if (columns.Count < 5) continue;

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

            if (columns.Count >= 7 && !string.IsNullOrWhiteSpace(columns[5]))
            {
                SNSCommentData comment = new SNSCommentData();
                comment.postID = id;
                comment.author = columns[5];
                comment.content = columns[6];

                if (columns.Count >= 9)
                {
                    bool.TryParse(columns[7], out comment.isEmoticon);
                    comment.emoticonName = columns[8];
                }

                existingPost.comments.Add(comment);
            }
        }
    }

    public void GenerateSNSUI()
    {
        foreach (Transform child in contentTransform) Destroy(child.gameObject);
        instantiatedPosts.Clear();

        foreach (SNSPostData postData in snsPostList)
        {
            GameObject newItem = Instantiate(snsPostPrefab, contentTransform);
            SNSPost postScript = newItem.GetComponent<SNSPost>();

            if (postScript != null)
            {
                postScript.Setup(postData);
            }

            // 💡 [추가] postData와 같은 순서로 저장 (인덱스로 매칭)
            instantiatedPosts.Add(postScript);
        }
    }

    /// <summary>
    /// 본문 텍스트가 "[CLUE:아이디]"로 시작하는지 검사해서 내부 clueID를 꺼냅니다.
    /// (SNSPost.Setup에서 쓰는 것과 동일한 파싱 규칙)
    /// </summary>
    private bool TryExtractClueIDFromContent(string rawContent, out string extractedClueID)
    {
        extractedClueID = null;
        if (string.IsNullOrEmpty(rawContent) || !rawContent.StartsWith("[CLUE:")) return false;

        int closeBracketIndex = rawContent.IndexOf(']');
        if (closeBracketIndex <= 6) return false;

        extractedClueID = rawContent.Substring(6, closeBracketIndex - 6);
        return true;
    }

    /// <summary>
    /// 💡 [추가] DataLogManager가 "원본 보기"를 요청할 때 호출.
    /// 일치하는 게시물을 찾아 그 위치로 스크롤을 이동시킵니다.
    /// </summary>
    public bool TryOpenClueSource(string clueID)
    {
        if (string.IsNullOrEmpty(clueID)) return false;

        for (int i = 0; i < snsPostList.Count; i++)
        {
            SNSPostData data = snsPostList[i];
            bool isMatch = false;

            // 1) 본문에 [CLUE:ID] 형태로 박혀있는 경우
            if (TryExtractClueIDFromContent(data.content, out string embeddedClueID) && embeddedClueID == clueID)
            {
                isMatch = true;
            }
            // 2) 별도 clueID / imageClueID 필드가 채워져 있는 경우 (추후 CSV 확장 대비)
            else if (!string.IsNullOrEmpty(data.clueID) && data.clueID == clueID)
            {
                isMatch = true;
            }
            else if (!string.IsNullOrEmpty(data.imageClueID) && data.imageClueID == clueID)
            {
                isMatch = true;
            }

            if (isMatch)
            {
                if (snsWindowManager != null)
                {
                    snsWindowManager.RestoreWindow();
                }

                if (i < instantiatedPosts.Count && instantiatedPosts[i] != null)
                {
                    ScrollToPost(instantiatedPosts[i].GetComponent<RectTransform>());
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 스크롤뷰 안의 특정 자식(target)이 보이도록 Content 위치를 이동시킵니다.
    /// </summary>
    private void ScrollToPost(RectTransform target)
    {
        if (scrollRect == null || target == null || scrollRect.viewport == null) return;

        Canvas.ForceUpdateCanvases();

        Vector2 viewportLocalPosition = scrollRect.viewport.localPosition;
        Vector2 childLocalPosition = target.localPosition;

        Vector2 result = new Vector2(
            0f,
            0f - (viewportLocalPosition.y + childLocalPosition.y)
        );

        // 살짝 위쪽 여백을 주어 대상이 화면 상단 근처에 오도록 조정
        float offsetY = target.rect.height * 0.5f;

        scrollRect.content.anchoredPosition = new Vector2(
            scrollRect.content.anchoredPosition.x,
            result.y - offsetY
        );
    }
}