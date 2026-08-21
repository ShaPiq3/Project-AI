using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUMANDB 검색 패널(2번 목업 이미지)에 붙는 컴포넌트.
/// 검색창에 이름을 입력하고 검색하면, 정확히 일치하는 인물의 상세 패널(HumanDBCard)을 연다.
/// </summary>
public class HumanDBSearchPanel : MonoBehaviour
{
    [Header("검색 UI 참조")]
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private Button searchButton;
    [SerializeField] private GameObject noResultText;

    private void Start()
    {
        Debug.Log($"[진단] HumanDBSearchPanel.Start() 호출됨. searchInputField:{(searchInputField != null)}, searchButton:{(searchButton != null)}");

        if (searchButton != null) searchButton.onClick.AddListener(OnSearchSubmitted);
        if (searchInputField != null) searchInputField.onSubmit.AddListener(_ => OnSearchSubmitted());

        if (noResultText != null) noResultText.SetActive(false);
    }

    private void OnSearchSubmitted()
    {
        if (searchInputField == null)
        {
            Debug.LogWarning("[진단] searchInputField가 연결되어 있지 않습니다.");
            return;
        }

        string name = searchInputField.text;
        Debug.Log($"[진단] 검색 시도: '{name}'");

        if (string.IsNullOrWhiteSpace(name)) return;

        if (HumanDBManager.Instance == null)
        {
            Debug.LogWarning("[진단] HumanDBManager.Instance가 null입니다! 씬에 HumanDBManager 오브젝트가 없거나 비활성화되어 있습니다.");
            return;
        }

        bool found = HumanDBManager.Instance.SearchByName(name);
        Debug.Log($"[진단] 검색 결과: {(found ? "찾음" : "못 찾음")}");

        if (noResultText != null) noResultText.SetActive(!found);
    }
}
