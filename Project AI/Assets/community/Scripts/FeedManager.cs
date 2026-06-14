using System.Collections.Generic;
using UnityEngine;

public class FeedManager : MonoBehaviour
{
    // 다른 스크립트에서 FeedManager.Instance로 즉시 접근할 수 있도록 싱글톤 선언
    public static FeedManager Instance { get; private set; }

    public GameObject feedPrefab;
    public Transform contentTransform;

    // CSV 데이터 마스터 리스트 및 해금 플래그 바구니
    private List<FeedData> masterFeedList = new List<FeedData>();
    private HashSet<string> unlockedFlags = new HashSet<string>();

    // 실시간으로 화면에 출력할 피드들의 정렬 순서를 제어할 바구니
    private List<FeedData> activeFeedList = new List<FeedData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadAllFeedsFromCSV();    // 1. 게시글 데이터 먼저 로드
        LoadAllCommentsFromCSV(); // 2. 댓글 데이터 로드 및 게시글 매칭
        InitDefaultFeeds();       // 3. 기본 일반 글 필터링
        RefreshFeedUI();          // 4. 최종 UI 출력
    }

    // 기본 게시글 필터링 함수 (요구 플래그가 없는 글만 먼저 활성화)
    private void InitDefaultFeeds()
    {
        activeFeedList.Clear();
        foreach (var data in masterFeedList)
        {
            if (string.IsNullOrEmpty(data.requiredFlag))
            {
                activeFeedList.Add(data);
            }
        }
    }

    // 현재 해금 상태에 맞춰 UI를 다시 그려주는 함수
    public void RefreshFeedUI()
    {
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        foreach (var data in activeFeedList)
        {
            GameObject obj = Instantiate(feedPrefab, contentTransform);
            FeedItem itemScript = obj.GetComponent<FeedItem>();
            if (itemScript != null)
            {
                itemScript.Setup(data);
            }
        }
    }

    // ★ [외부 호출용 핵심 함수] 조건 달성 시 이 함수를 호출하면 피드가 최상단에 자동 누적됩니다.
    public void UnlockFeedByFlag(string flagName)
    {
        if (!unlockedFlags.Contains(flagName))
        {
            unlockedFlags.Add(flagName);

            bool isChanged = false;

            foreach (var data in masterFeedList)
            {
                if (!string.IsNullOrEmpty(data.requiredFlag) && data.requiredFlag == flagName)
                {
                    if (!activeFeedList.Contains(data))
                    {
                        // 타임라인의 맨 앞(0번째 인덱스)에 새치기로 밀어 넣습니다.
                        activeFeedList.Insert(0, data);
                        isChanged = true;
                    }
                }
            }

            if (isChanged)
            {
                RefreshFeedUI();
                Debug.Log($"[커뮤니티] 새로운 플래그 해금 및 최상단 배치 완료: {flagName}");
            }
        }
    }

    // 1. 게시글(FeedsData.csv) 파싱 함수
    void LoadAllFeedsFromCSV()
    {
        masterFeedList.Clear();

        TextAsset csvFile = Resources.Load<TextAsset>("FeedsData");
        if (csvFile == null)
        {
            Debug.LogError("[FeedManager] Resources 폴더에서 'FeedsData' CSV 파일을 찾을 수 없습니다!");
            return;
        }

        string cleanText = csvFile.text.Replace("\r", "");
        string[] lines = cleanText.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] row = lines[i].Split(',');
            FeedData data = new FeedData();

            if (row.Length > 0)
            {
                string idStr = row[0].Trim();
                string digitOnly = "";
                foreach (char c in idStr)
                {
                    if (char.IsDigit(c)) digitOnly += c;
                }

                if (int.TryParse(digitOnly, out int parsedId))
                {
                    data.id = parsedId;
                }
                else
                {
                    continue;
                }
            }

            if (row.Length > 1) data.title = row[1].Trim();
            if (row.Length > 2) data.writer = row[2].Trim();
            if (row.Length > 3) data.date = row[3].Trim();
            if (row.Length > 4) data.mainImageName = row[4].Trim(); // E열 이미지
            if (row.Length > 5) data.mainText = row[5].Trim();      // F열 본문 텍스트
            if (row.Length > 6) data.requiredFlag = row[6].Trim();  // G열 해금 플래그

            data.comments = new List<CommentData>();
            masterFeedList.Add(data);
        }
    }

    // 2. 댓글(CommentsData.csv) 파싱 및 게시글 매칭 함수
    void LoadAllCommentsFromCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("CommentsData");
        if (csvFile == null)
        {
            Debug.LogError("[FeedManager] Resources 폴더에서 'CommentsData' CSV 파일을 찾을 수 없습니다!");
            return;
        }

        string cleanText = csvFile.text.Replace("\r", "");
        string[] lines = cleanText.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] row = lines[i].Split(',');
            if (row.Length < 2) continue;

            string feedIdStr = row[0].Trim();
            string digitOnly = "";
            foreach (char c in feedIdStr)
            {
                if (char.IsDigit(c)) digitOnly += c;
            }

            if (int.TryParse(digitOnly, out int targetFeedId))
            {
                FeedData targetFeed = masterFeedList.Find(f => f.id == targetFeedId);

                if (targetFeed != null)
                {
                    CommentData cData = new CommentData();
                    cData.writer = row[1].Trim();
                    cData.text = row.Length > 2 ? row[2].Trim() : "";
                    cData.emoticonSpriteName = row.Length > 3 ? row[3].Trim() : "";

                    targetFeed.comments.Add(cData);
                }
            }
        }
        Debug.Log("[커뮤니티] 모든 댓글 데이터 로드 및 매칭 완료");
    }
}