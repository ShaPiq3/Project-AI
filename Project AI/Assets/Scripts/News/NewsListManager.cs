using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class NewsListManager : MonoBehaviour
{
    [Header("Data (Excel CSV)")]
    [SerializeField] private TextAsset csvFile;

    [Header("Prefabs & Parents")]
    [SerializeField] private GameObject newsButtonPrefab;
    [SerializeField] private Transform contentParent;

    [Header("Detail Popup Reference")]
    [SerializeField] private NewsCard detailPopup;

    private void Start()
    {
        if (detailPopup != null) detailPopup.gameObject.SetActive(false);

        ParseExcelAndGenerateButtons();
        SelectCategory("ALL");
    }

    // 1. 엑셀 파싱 및 리스트 버튼 동적 생성 (정밀 스캔 방식)
    private void ParseExcelAndGenerateButtons()
    {
        if (csvFile == null || newsButtonPrefab == null || contentParent == null) return;

        List<List<string>> csvData = ParseCSV(csvFile.text);

        // i = 1 부터 시작 (첫 줄은 헤더: ID, Category, Title... 제외)
        for (int i = 1; i < csvData.Count; i++)
        {
            List<string> row = csvData[i];
            if (row.Count < 6) continue;

            // ID 변환 예외 처리
            if (!int.TryParse(row[0], out int idResult))
            {
                Debug.LogWarning($"[CSV 파싱 패스] {i}번째 줄의 ID 형식이 올바르지 않습니다: '{row[0]}'");
                continue;
            }

            NewsData data = new NewsData
            {
                id = idResult,
                category = row[1],
                title = row[2],
                info = row[3],
                body = row[4],       // 문단 구분을 위한 '|'가 깨지지 않고 안전하게 보존됩니다.
                imageName = row[5]
            };

            // 목록 버튼 생성 및 데이터 주입
            GameObject btnGo = Instantiate(newsButtonPrefab, contentParent);
            NewsButton newsBtn = btnGo.GetComponent<NewsButton>();
            if (newsBtn != null)
            {
                newsBtn.SetButton(data, this);
            }
        }
    }

    // 💡 쌍따옴표 내부의 줄바꿈과 쉼표를 완벽하게 판별해내는 하드코어 CSV 파서
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
                    // 쌍따옴표 연속 두 개("")는 진짜 쌍따옴표 문자 하나로 처리
                    if (i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false; // 닫는 쌍따옴표 처리
                    }
                }
                else
                {
                    cell.Append(c); // 쌍따옴표 내부의 글자(쉼표, 줄바꿈 포함)는 그대로 보존
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true; // 여는 쌍따옴표 감지
                }
                else if (c == ',')
                {
                    currentLine.Add(cell.ToString().Trim());
                    cell.Clear();
                }
                else if (c == '\n' || c == '\r')
                {
                    // 행의 끝 처리 (\r\n 대응)
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

        // 마지막 남은 데이터 처리
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
        }
    }
}
