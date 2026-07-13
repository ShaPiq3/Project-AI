using System.Collections.Generic;
using UnityEngine;

public class NewsListManager : MonoBehaviour
{
    [Header("Data (Excel CSV)")]
    [SerializeField] private TextAsset csvFile;

    [Header("Prefabs & Parents")]
    [SerializeField] private GameObject newsButtonPrefab; // 2번에서 만든 NewsButton 프리팹
    [SerializeField] private Transform contentParent;     // ScrollView의 Content 오브젝트

    [Header("Detail Popup Reference")]
    [SerializeField] private NewsCard detailPopup;        // 맨 처음 만들었던 상세화면 DB_News 프리팹 기기

    private void Start()
    {
        if (detailPopup != null) detailPopup.gameObject.SetActive(false);

        ParseExcelAndGenerateButtons();
        SelectCategory("ALL"); // 시작 시 전체 기사 노출
    }

    // 1. 엑셀 파싱 및 리스트 버튼 동적 생성
    private void ParseExcelAndGenerateButtons()
    {
        if (csvFile == null || newsButtonPrefab == null || contentParent == null) return;

        string[] lines = csvFile.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        // i = 1 부터 시작 (첫 줄은 헤더: ID, Category, Title...)
        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            if (row.Length < 6) continue;

            NewsData data = new NewsData
            {
                id = int.Parse(row[0].Trim()),
                category = row[1].Trim(),
                title = row[2].Trim(),
                info = row[3].Trim(),
                body = row[4].Trim(),
                imageName = row[5].Trim()
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

    // 2. 카테고리 선택 마스터 함수 (기존 보내주신 로직의 장점 통합)
    public void SelectCategory(string categoryKeyword)
    {
        if (contentParent == null) return;

        // 공백 제거 및 대문자 변환으로 비교 정확도 향상
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
                // Component에서 직접 카테고리를 가져오거나 오브젝트 이름에서 검사 가능
                NewsButton btnComponent = child.GetComponent<NewsButton>();
                if (btnComponent != null)
                {
                    string cleanTargetCategory = btnComponent.category.Replace(" ", "").ToUpper();

                    // 기존 코드처럼 키워드가 포함되어 있는지 검사 (Contains)
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
            detailPopup.SetNewsData(data); // 이전 답변의 SetNewsData 호출
        }
    }
}