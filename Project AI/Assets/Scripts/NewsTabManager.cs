using UnityEngine;

public class NewsTabManager : MonoBehaviour
{
    [Header("기사들이 모여있는 Content 트랜스폼")]
    // 💡 스크린샷의 'Content' 오브젝트를 여기에 드래그앤드롭합니다.
    [SerializeField] private Transform contentParent;

    // ★ [추가 확장] 나중에 카테고리가 더 늘어나더라도 이 함수 하나로 전부 커버됩니다!
    // 💡 상단/하단의 각 카테고리 버튼 OnClick 이벤트에 이 함수를 연결합니다.
    public void SelectCategory(string categoryKeyword)
    {
        if (contentParent == null) return;

        // 1. 공백이나 문자열이 비어있다면 "전체" 기사를 보여주는 것으로 간주합니다.
        if (string.IsNullOrEmpty(categoryKeyword) || categoryKeyword.ToUpper() == "ALL")
        {
            ShowAllArticles();
            return;
        }

        // 2. 글자(띄어쓰기 무시)를 가공합니다.
        string upperKeyword = categoryKeyword.ToUpper().Replace(" ", "");

        // 3. Content 자식(기사들)을 돌며 이름 기반으로 수루룩 필터링합니다.
        for (int i = 0; i < contentParent.childCount; i++)
        {
            GameObject newsItem = contentParent.GetChild(i).gameObject;
            if (newsItem == null) continue;

            // 기사 이름(예: "경제 1")의 공백을 채우고 대문자로 변경해 비교합니다.
            string cleanItemName = newsItem.name.ToUpper().Replace(" ", "");

            if (cleanItemName.Contains(upperKeyword))
            {
                newsItem.SetActive(true);  // 카테고리가 맞으면 켬
            }
            else
            {
                newsItem.SetActive(false); // 안 맞으면 끔 (Vertical Layout Group에 의해 공간 정렬됨)
            }
        }
    }

    // 전체 기사를 한 번에 다 보여주는 서브 함수
    public void ShowAllArticles()
    {
        if (contentParent == null) return;

        for (int i = 0; i < contentParent.childCount; i++)
        {
            if (contentParent.GetChild(i) != null)
            {
                contentParent.GetChild(i).gameObject.SetActive(true);
            }
        }
    }

    private void Start()
    {
        // 게임 시작 시에는 기본적으로 '전체 기사'가 수루룩 나오도록 세팅합니다.
        ShowAllArticles();
    }
}