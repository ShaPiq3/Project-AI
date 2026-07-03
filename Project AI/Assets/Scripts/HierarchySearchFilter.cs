using UnityEngine;
using TMPro; // ★ TextMeshPro 컴포넌트 제어를 위해 필수 추가!
using System;

public class HierarchySearchFilter : MonoBehaviour
{
    [Header("Search Input")]
    // ★ [수정] UnityEngine.UI.InputField 대신 TMP_InputField를 사용하여 금지 표시를 해결합니다.
    [SerializeField] private TMP_InputField searchInputField;

    [Header("Target UI Panels")]
    [SerializeField] private GameObject[] targetPanels;

    private void Start()
    {
        if (searchInputField != null)
        {
            // TMP_InputField에 글자가 타이핑될 때마다 실시간으로 함수를 호출합니다.
            searchInputField.onValueChanged.AddListener(OnSearchTextChanged);
        }
    }

    public void OnSearchTextChanged(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            foreach (var panel in targetPanels)
            {
                if (panel != null) panel.SetActive(true);
            }
            return;
        }

        string upperKeyword = keyword.ToUpper();

        foreach (var panel in targetPanels)
        {
            if (panel != null)
            {
                string panelName = panel.name.ToUpper();

                if (panelName.Contains(upperKeyword))
                {
                    panel.SetActive(true);
                }
                else
                {
                    panel.SetActive(false);
                }
            }
        }
    }
}
