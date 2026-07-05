using UnityEngine;

public class NewsCategoryFilter : MonoBehaviour
{
    [Header("기사들이 모여있는 Content 트랜스폼")]
    [SerializeField] private Transform contentParent; // 올려주신 스크린샷의 'Content' 오브젝트를 드래그앤드롭

    private void Start()
    {
        // 게임 시작 시에는 기본적으로 모든 기사가 다 보이도록 '전체' 상태로 시작합니다.
        SelectAllCategory();
    }

    // 🌐 [전체] 버튼에 연결할 함수
    public void SelectAllCategory()
    {
        if (contentParent == null) return;

        // Content 자식들을 돌면서 전부 다 켜줍니다.
        for (int i = 0; i < contentParent.childCount; i++)
        {
            contentParent.GetChild(i).gameObject.SetActive(true);
        }
    }

    // 🎯 [정치, 사회, 경제, 기술] 등 개별 카테고리 버튼들이 공통으로 사용할 마스터 함수
    // 💡 인스펙터 버튼 OnClick에서 이 함수를 선택하고, 매개변수 칸에 "경제", "사회", "기술" 등 한글을 직접 씁니다.
    public void FilterCategory(string categoryKeyword)
    {
        if (contentParent == null || string.IsNullOrEmpty(categoryKeyword)) return;

        // Content 자식들을 하나씩 검사합니다.
        for (int i = 0; i < contentParent.childCount; i++)
        {
            GameObject newsItem = contentParent.GetChild(i).gameObject;

            // 기사 오브젝트의 이름(예: "경제 1")에 카테고리 키워드("경제")가 포함되어 있는지 검사합니다.
            if (newsItem.name.Contains(categoryKeyword))
            {
                newsItem.SetActive(true);  // 일치하면 화면에 표시
            }
            else
            {
                newsItem.SetActive(false); // 일치하지 않으면 숨김 (Layout Group에 의해 공간도 사라짐)
            }
        }
    }
}