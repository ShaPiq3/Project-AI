using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class NewsSearchFilter : MonoBehaviour
{
    [Header("Search Input")]
    [SerializeField] private TMP_InputField searchInputField;

    [Header("News Container References")]
    [SerializeField] private Transform contentParent; // 뉴스 버튼들이 나열되어 있는 Scroll View의 Content

    // 현재 선택된 카테고리 상태 기억 (NewsListManager의 상태와 연동될 수 있도록 관리)
    private string currentCategory = "ALL";

    private void Start()
    {
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnSearchTextChanged);
        }
    }

    // 🔍 검색창에 실시간 타이핑될 때 자동 호출
    public void OnSearchTextChanged(string keyword)
    {
        UpdateFilter(keyword, currentCategory);
    }

    // 🎯 [중요] 카테고리 탭 버튼을 클릭할 때 NewsListManager와 동시에 이 함수도 실행되도록 등록합니다.
    public void OnCategoryTabChanged(string categoryName)
    {
        if (string.IsNullOrEmpty(categoryName)) currentCategory = "ALL";
        else currentCategory = categoryName.ToUpper().Replace(" ", "");

        // 카테고리가 바뀔 때도 현재 인풋필드에 적힌 검색어를 기반으로 필터링을 다시 연산합니다.
        string currentKeyword = searchInputField != null ? searchInputField.text : "";
        UpdateFilter(currentKeyword, currentCategory);
    }

    // ⚙️ [뉴스 전용 핵심 필터링 로직] 카테고리 + 제목 + 본문 내용 동시 체크
    private void UpdateFilter(string keyword, string category)
    {
        if (contentParent == null) return;

        // 공백 제거 및 대문자 치환을 통해 오차율 제로 가공
        string upperKeyword = string.IsNullOrEmpty(keyword) ? "" : keyword.ToUpper().Replace(" ", "");
        string upperCategory = category.ToUpper().Replace(" ", "");

        // 복잡하게 배열을 인스펙터에 수동으로 넣을 필요 없이, Content의 자식들을 자동으로 돕니다.
        for (int i = 0; i < contentParent.childCount; i++)
        {
            Transform child = contentParent.GetChild(i);
            if (child == null) continue;

            NewsButton newsBtn = child.GetComponent<NewsButton>();
            if (newsBtn == null) continue;

            // 1. 카테고리 추출 및 판별
            string itemCategory = newsBtn.category.ToUpper().Replace(" ", "");
            bool isCategoryMatch = (upperCategory == "ALL" || upperCategory == "전체" || itemCategory.Contains(upperCategory));

            // 2. 검색어 판별 (검색창이 비어있으면 무조건 통과)
            bool isKeywordMatch = false;
            if (string.IsNullOrEmpty(upperKeyword))
            {
                isKeywordMatch = true;
            }
            else
            {
                // 💡 [질문하신 핵심 기능] 버튼 안에 심어져 있는 자식 TMP 컴포넌트를 찾아 제목과 본문 요약본을 대조합니다.
                // NewsButton의 인펙터에 매핑해 두었던 텍스트를 직접 읽어옵니다.

                // 기존 구조처럼 하위 텍스트 오브젝트들의 내용을 추출
                string titleAndBodyText = "";

                // 자식 오브젝트들을 돌며 모든 글자(제목, 본문 요약 등)를 다 이어붙여 하나의 검색 타겟으로 만듭니다.
                TextMeshProUGUI[] textComponents = child.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in textComponents)
                {
                    if (tmp != null)
                    {
                        titleAndBodyText += tmp.text;
                    }
                }

                // 가공 후 일치 여부 확인
                titleAndBodyText = titleAndBodyText.ToUpper().Replace(" ", "");
                if (titleAndBodyText.Contains(upperKeyword))
                {
                    isKeywordMatch = true;
                }
            }

            // 3. 교집합 연산 결과에 따라 버튼 노출 결정
            if (isCategoryMatch && isKeywordMatch)
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