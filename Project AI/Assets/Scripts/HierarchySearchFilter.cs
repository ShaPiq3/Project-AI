using UnityEngine;
using TMPro;
using System;

public class HierarchySearchFilter : MonoBehaviour
{
    // ★ [핵심 추가] 창(Panel)과 그 창에 소속된 버튼들을 묶어주는 단일 그룹 구조체
    [System.Serializable]
    public struct SearchGroup
    {
        public string groupName;             // 인스펙터 확인용 이름 (예: 1번 창 그룹, 2번 창 그룹)
        public GameObject parentPanel;       // 해당 창 (예: Window_01)
        public GameObject[] itemButtons;     // 그 창 '내부'에 속한 실제 버튼(메뉴)들 배열
    }

    [Header("Search Input")]
    [SerializeField] private TMP_InputField searchInputField;

    [Header("Target UI Search Groups")]
    // ★ 기존 GameObject[] targetPanels 대신, 그룹화된 구조체 배열을 사용합니다.
    [SerializeField] private SearchGroup[] searchGroups;

    private void Start()
    {
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnSearchTextChanged);
        }
    }

    public void OnSearchTextChanged(string keyword)
    {
        // 1. 검색창이 비어있을 때의 처리
        if (string.IsNullOrEmpty(keyword))
        {
            foreach (var group in searchGroups)
            {
                // 현재 활성화되어 있는(눈에 보이는) 창 내부의 버튼들만 다시 전부 켜줍니다.
                if (group.parentPanel != null && group.parentPanel.activeSelf)
                {
                    foreach (var btn in group.itemButtons)
                    {
                        if (btn != null) btn.SetActive(true);
                    }
                }
            }
            return;
        }

        string upperKeyword = keyword.ToUpper().Replace(" ", "");

        // 2. 모든 그룹을 순회하며 필터링 진행
        foreach (var group in searchGroups)
        {
            // 예외 방지 안전장치
            if (group.parentPanel == null || group.itemButtons == null) continue;

            // ★ [핵심 버그 해결 조건] 
            // 현재 화면에 '켜져 있는 창(activeSelf == true)' 내부의 버튼들만 검색에 참여시킵니다!
            // 화면에 꺼져있는 다른 창의 그룹은 아예 검사하지 않고 무시합니다.
            if (group.parentPanel.activeSelf)
            {
                foreach (var btn in group.itemButtons)
                {
                    if (btn != null)
                    {
                        string btnName = btn.name.ToUpper().Replace(" ", "");

                        if (btnName.Contains(upperKeyword))
                        {
                            btn.SetActive(true);
                        }
                        else
                        {
                            btn.SetActive(false);
                        }
                    }
                }
            }
            else
            {
                // 꺼져있는 창의 버튼들은 검색어와 상관없이 기본적으로 손대지 않거나 
                // 원하신다면 전부 활성화(초기화) 상태로 대기시켜 둡니다.
                foreach (var btn in group.itemButtons)
                {
                    if (btn != null) btn.SetActive(true);
                }
            }
        }
    }
}