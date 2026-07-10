using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

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

        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = rows[i].Split(',');

            // 열 개수가 6개로 늘어남 (제목, 글쓴이, 날짜, 추천, 비추천, 내용)
            if (columns.Length >= 6)
            {
                PostData data = new PostData();
                data.id = i; // 고유 ID 부여
                data.title = columns[0].Trim();
                data.author = columns[1].Trim();
                data.date = columns[2].Trim();
                int.TryParse(columns[3].Trim(), out data.likes);
                int.TryParse(columns[4].Trim(), out data.dislikes);
                data.content = columns[5].Trim();

                postList.Add(data);
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