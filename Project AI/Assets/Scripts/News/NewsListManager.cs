using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class NewsListManager : MonoBehaviour
{
    // 💡 [추가] 다른 스크립트(DataLogManager)에서 접근할 수 있도록 싱글톤 인스턴스 추가
    public static NewsListManager Instance { get; private set; }

    [Header("Data (Excel CSV)")]
    [SerializeField] private TextAsset csvFile;

    [Header("Prefabs & Parents")]
    [SerializeField] private GameObject newsButtonPrefab;
    [SerializeField] private Transform contentParent;

    [Header("Detail Popup Reference")]
    [SerializeField] private NewsCard detailPopup;

    [Header("WindowManager 연동")]
    [SerializeField] private WindowManager windowManager;

    [Header("이 뉴스 창 자체를 여닫는 InGameWindowManager (선택 사항)")]
    [Tooltip("사이드바 등에서 이 뉴스 창을 최소화/복원하는 InGameWindowManager가 따로 있다면 연결하세요. " +
             "원본 보기를 눌렀을 때 창이 닫혀 있어도 자동으로 복원됩니다.")]
    [SerializeField] private InGameWindowManager newsWindowManager;

    // 💡 [추가] 파싱된 모든 뉴스 데이터를 보관 (clueID로 역참조 검색하기 위함)
    private List<NewsData> allNewsData = new List<NewsData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (detailPopup != null) detailPopup.gameObject.SetActive(false);

        ParseExcelAndGenerateButtons();
        SelectCategory("ALL");
    }

    // 1. 엑셀 파싱 및 리스트 버튼 동적 생성
    private void ParseExcelAndGenerateButtons()
    {
        if (csvFile == null || newsButtonPrefab == null || contentParent == null) return;

        List<List<string>> csvData = ParseCSV(csvFile.text);

        // i = 1 부터 시작 (첫 줄 헤더 제외)
        for (int i = 1; i < csvData.Count; i++)
        {
            List<string> row = csvData[i];

            if (row.Count < 6) continue;

            if (!int.TryParse(row[0], out int idResult))
            {
                Debug.LogWarning($"[CSV 파싱 패스] {i}번째 줄의 ID 형식이 올바르지 않습니다: '{row[0]}'");
                continue;
            }

            string imgClue = (row.Count > 6) ? row[6] : "";

            int paragraphIdx = 0;
            if (row.Count > 7)
            {
                int.TryParse(row[7], out paragraphIdx);
            }

            string bodyClue = (row.Count > 8) ? row[8] : "";

            // 💡 [추가] 이미지 생성 퀘스트용 ID (10번째 컬럼). 비어있으면 수집 대상 아님.
            string collectibleImageID = (row.Count > 9) ? row[9] : "";

            NewsData data = new NewsData
            {
                id = idResult,
                category = row[1],
                title = row[2],
                info = row[3],
                body = row[4],
                imageName = row[5],
                imageClueID = imgClue,
                clueParagraphIndex = paragraphIdx,
                bodyClueID = bodyClue,
                collectibleImageID = collectibleImageID
            };

            // 💡 [추가] 나중에 clueID로 검색할 수 있도록 저장
            allNewsData.Add(data);

            GameObject btnGo = Instantiate(newsButtonPrefab, contentParent);
            NewsButton newsBtn = btnGo.GetComponent<NewsButton>();
            if (newsBtn != null)
            {
                newsBtn.SetButton(data, this);

                // 💡 [추가] 이미지 생성 퀘스트 수집 대상으로 자동 등록
                CollectibleImageBinder.Bind(newsBtn.ThumbnailImage, data.collectibleImageID);
            }
        }
    }

    private List<List<string>> ParseCSV(string csvText)
    {
        List<List<string>> result = new List<List<string>>();
        List<string> currentLine = new List<string>();
        StringBuilder cell = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            if (inQuotes)
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
                        inQuotes = false;
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
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    currentLine.Add(cell.ToString().Trim());
                    cell.Clear();
                }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    {
                        i++;
                    }
                    currentLine.Add(cell.ToString().Trim());
                    cell.Clear();

                    if (currentLine.Count > 0 && !string.IsNullOrWhiteSpace(currentLine[0]))
                    {
                        result.Add(new List<string>(currentLine));
                    }
                    currentLine.Clear();
                }
                else
                {
                    cell.Append(c);
                }
            }
        }

        if (cell.Length > 0 || currentLine.Count > 0)
        {
            currentLine.Add(cell.ToString().Trim());
            result.Add(currentLine);
        }

        return result;
    }

    // 2. 카테고리 선택 마스터 함수
    public void SelectCategory(string categoryKeyword)
    {
        if (contentParent == null) return;

        string cleanKeyword = categoryKeyword.Replace(" ", "").ToUpper();
        bool isAll = string.IsNullOrEmpty(cleanKeyword) || cleanKeyword == "ALL" || cleanKeyword == "전체";

        for (int i = 0; i < contentParent.childCount; i++)
        {
            Transform child = contentParent.GetChild(i);
            if (child == null) continue;

            if (isAll)
            {
                child.gameObject.SetActive(true);
            }
            else
            {
                NewsButton btnComponent = child.GetComponent<NewsButton>();
                if (btnComponent != null)
                {
                    string cleanTargetCategory = btnComponent.category.Replace(" ", "").ToUpper();

                    if (cleanTargetCategory.Contains(cleanKeyword))
                    {
                        child.gameObject.SetActive(true);
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    // 3. 버튼을 눌렀을 때 상세 팝업을 열어주는 중계 함수
    public void OpenDetailPopup(NewsData data)
    {
        if (detailPopup != null)
        {
            detailPopup.gameObject.SetActive(true);
            detailPopup.SetNewsData(data);

            if (windowManager != null)
            {
                RectTransform cardRect = detailPopup.GetComponent<RectTransform>();
                windowManager.RepositionPopupWindow(cardRect);
            }
        }
    }

    /// <summary>
    /// 💡 [추가] DataLogManager가 "원본 보기"를 요청할 때 호출하는 함수.
    /// clueID로 어느 기사(본문 문단 또는 이미지)에서 나온 단서인지 찾아서
    /// 해당 기사의 상세 팝업을 열어줍니다.
    /// </summary>
    /// <returns>원본을 찾아서 열었으면 true, 못 찾았으면 false</returns>
    public bool TryOpenClueSource(string clueID)
    {
        if (string.IsNullOrEmpty(clueID)) return false;

        foreach (var data in allNewsData)
        {
            bool isBodyMatch = !string.IsNullOrEmpty(data.bodyClueID) && data.bodyClueID == clueID;
            bool isImageMatch = !string.IsNullOrEmpty(data.imageClueID) && data.imageClueID == clueID;

            if (isBodyMatch || isImageMatch)
            {
                // 뉴스 창 자체가 최소화되어 있었다면 먼저 복원
                if (newsWindowManager != null)
                {
                    newsWindowManager.RestoreWindow();
                }

                OpenDetailPopup(data);
                return true;
            }
        }

        return false;
    }
}