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

    [Header("WindowManager 연동")]
    [SerializeField] private WindowManager windowManager;

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

            // ⚠️ 최소한 기본 정보(ID~imageName)까지는 제대로 적혀있는지 체크 (최소 6열 필요)
            if (row.Count < 6) continue;

            // ID 변환 예외 처리
            if (!int.TryParse(row[0], out int idResult))
            {
                Debug.LogWarning($"[CSV 파싱 패스] {i}번째 줄의 ID 형식이 올바르지 않습니다: '{row[0]}'");
                continue;
            }

            // ⚠️ 엑셀 빈 칸이나 데이터 부족으로 인한 에러 방지를 위해 삼항 연산자로 안전하게 파싱
            string imgClue = (row.Count > 6) ? row[6] : "";

            int paragraphIdx = 0;
            if (row.Count > 7)
            {
                int.TryParse(row[7], out paragraphIdx);
            }

            string bodyClue = (row.Count > 8) ? row[8] : "";

            // 새로 약속한 데이터 구조에 맞추어 변수를 최종 주입합니다.
            NewsData data = new NewsData
            {
                id = idResult,
                category = row[1],
                title = row[2],
                info = row[3],
                body = row[4],
                imageName = row[5],
                imageClueID = imgClue,                // [추가] 이미지 클릭 시 획득할 단서 ID
                clueParagraphIndex = paragraphIdx,    // [추가] 단서가 숨겨진 본문 문단 번호 (0이면 없음)
                bodyClueID = bodyClue                 // [추가] 본문 문단 클릭 시 획득할 단서 ID
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

    // 💡 쌍따옴표 내부의 줄바꿈과 쉼표를 완벽하게 판별해내는 CSV 파서 (기존 코드 유지)
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

    // 2. 카테고리 선택 마스터 함수 (기존 코드 유지)
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

    // 3. 버튼을 눌렀을 때 상세 팝업을 열어주는 중계 함수 (기존 코드 유지)
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
}