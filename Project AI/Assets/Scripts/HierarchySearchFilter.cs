using UnityEngine;
using TMPro;
using System;

public class HierarchySearchFilter : MonoBehaviour
{
    [System.Serializable]
    public struct SearchGroup
    {
        public string groupName;
        public GameObject parentPanel;
        public GameObject[] itemButtons;
    }

    [Header("Search Input")]
    [SerializeField] private TMP_InputField searchInputField;

    [Header("Target UI Search Groups")]
    [SerializeField] private SearchGroup[] searchGroups;

    // ★ [추가된 내부 변수] 현재 유저가 선택한 카테고리 탭 상태를 기억합니다. (기본값은 전체)
    private string currentCategory = "ALL";

    private void Start()
    {
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnSearchTextChanged);
        }
    }

    // 🔍 [기존 유지] 검색창에 타이핑될 때 호출되는 함수
    public void OnSearchTextChanged(string keyword)
    {
        UpdateFilter(keyword, currentCategory);
    }

    // 🎯 [새로 추가] 카테고리 탭 버튼(뉴스 탭 매니저 등)을 누를 때 호출해 줄 함수!
    public void OnCategoryTabChanged(string categoryName)
    {
        if (string.IsNullOrEmpty(categoryName)) currentCategory = "ALL";
        else currentCategory = categoryName.ToUpper().Replace(" ", "");

        // 카테고리가 바뀔 때도 현재 검색창에 적힌 단어를 기반으로 필터링을 다시 계산합니다.
        string currentKeyword = searchInputField != null ? searchInputField.text : "";
        UpdateFilter(currentKeyword, currentCategory);
    }

    // ⚙️ [핵심 통합 마스터 로직] 검색어와 카테고리를 동시에 연산하여 최종 On/Off 결정
    private void UpdateFilter(string keyword, string category)
    {
        string upperKeyword = string.IsNullOrEmpty(keyword) ? "" : keyword.ToUpper().Replace(" ", "");
        string upperCategory = category.ToUpper().Replace(" ", "");

        foreach (var group in searchGroups)
        {
            if (group.parentPanel == null || group.itemButtons == null) continue;

            // 현재 이 스크립트가 붙은 패널(창)이 활성화되어 있을 때만 자식들을 검사합니다.
            if (group.parentPanel.activeSelf)
            {
                foreach (var btn in group.itemButtons)
                {
                    if (btn == null) continue;

                    // 1. 기사의 진짜 카테고리 태그 가져오기
                    NewsItemTag itemTag = btn.GetComponent<NewsItemTag>();
                    string itemCategory = (itemTag != null) ? itemTag.category : btn.name;
                    itemCategory = itemCategory.ToUpper().Replace(" ", "");

                    // 2. 기사의 제목 텍스트(또는 이름) 가져오기
                    string btnName = btn.name.ToUpper().Replace(" ", "");
                    var tmpText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmpText != null) btnName = tmpText.text.ToUpper().Replace(" ", "");

                    // ----------------------------------------------------
                    // 💡 [버그 해결 핵심 판별식] 두 가지 조건이 모두 교집합을 이루어야 함
                    // ----------------------------------------------------

                    // 조건 A: 카테고리 매칭 (탭이 'ALL'이거나, 기사의 정체가 현재 탭과 일치하는가)
                    bool isCategoryMatch = (upperCategory == "ALL" || itemCategory.Contains(upperCategory));

                    // 조건 B: 검색어 매칭 (검색창이 비어있거나, 기사 제목에 검색어가 포함되어 있는가)
                    bool isKeywordMatch = (string.IsNullOrEmpty(upperKeyword) || btnName.Contains(upperKeyword));

                    // 둘 다 만족할 때만 완벽하게 버튼을 켭니다.
                    if (isCategoryMatch && isKeywordMatch)
                    {
                        btn.SetActive(true);
                    }
                    else
                    {
                        btn.SetActive(false); // 하나라도 어긋나면 숨겨서 레이아웃 그룹 재정렬 유도
                    }
                }
            }
            else
            {
                // 꺼져있는 창의 버튼들은 기본적으로 다 켜두어 초기화 상태를 유지합니다.
                foreach (var btn in group.itemButtons)
                {
                    if (btn != null) btn.SetActive(true);
                }
            }
        }
    }
}