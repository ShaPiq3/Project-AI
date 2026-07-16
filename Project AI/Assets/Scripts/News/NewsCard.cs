using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class NewsCard : MonoBehaviour
{
    [Header("상세 보기 UI 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Image newsImage;

    [Header("동적 문단 생성 설정")]
    [SerializeField] private TextMeshProUGUI textTemplate; // 복사 원본 TMP (Rich Text가 켜져 있어야 함)
    [SerializeField] private Transform textContainer;     // Vertical Layout Group이 있는 부모

    private List<TextMeshProUGUI> spawnedTexts = new List<TextMeshProUGUI>();

    public void SetNewsData(NewsData data)
    {
        // 기존에 생성된 텍스트 오브젝트들 초기화
        ClearSpawnedTexts();

        titleText.text = data.title;
        infoText.text = data.info;

        // 원본 템플릿 비활성화
        if (textTemplate != null) textTemplate.gameObject.SetActive(false);

        // '|' 기호를 기준으로 문단 쪼개기
        string[] paragraphs = data.body.Split('|');

        // 1. 본문 문단 생성 루프
        for (int i = 0; i < paragraphs.Length; i++)
        {
            string paragraphText = paragraphs[i].Trim();
            if (string.IsNullOrEmpty(paragraphText)) continue;

            // 문단 템플릿 생성
            TextMeshProUGUI newText = Instantiate(textTemplate, textContainer);
            newText.richText = true;

            // 단서 태그 검사 ([CLUE:ID] 형태)
            if (paragraphText.StartsWith("[CLUE:"))
            {
                // 태그와 실제 텍스트 분리
                int closeBracketIndex = paragraphText.IndexOf(']');
                string clueID = paragraphText.Substring(6, closeBracketIndex - 6);
                string realContent = paragraphText.Substring(closeBracketIndex + 1);

                newText.text = realContent;

                // 텍스트 오브젝트에 버튼 컴포넌트 추가 및 이벤트 연결
                Button btn = newText.gameObject.GetComponent<Button>();
                if (btn == null) btn = newText.gameObject.AddComponent<Button>();

                // 시각적 피드백 효과 (선택)
                btn.transition = Selectable.Transition.ColorTint;

                ClueData clue = new ClueData
                {
                    clueID = clueID,
                    sourceTitle = data.title,
                    contentText = realContent,
                    imageName = ""
                };

                // 💡 프리팹 재사용 시 리스너가 꼬이지 않도록 클리어 후 등록
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => CollectClue(clue));
            }
            else
            {
                // 일반 문단 처리
                newText.text = paragraphText;
            }

            newText.gameObject.SetActive(true);
            spawnedTexts.Add(newText);
        }

        // 2. 이미지 로드 로직 (루프 밖, 함수 끝자리에 안전하게 배치)
        if (!string.IsNullOrEmpty(data.imageName))
        {
            Sprite loadedSprite = Resources.Load<Sprite>($"NewsImages/{data.imageName}");
            if (loadedSprite != null)
            {
                newsImage.sprite = loadedSprite;
                newsImage.gameObject.SetActive(true);
            }
            else newsImage.gameObject.SetActive(false);
        }
        else newsImage.gameObject.SetActive(false);
    }

    // 💡 단서 클릭 시 실행될 함수 (독립된 위치로 올바르게 수정)
    private void CollectClue(ClueData clue)
    {
        Debug.Log($"단서 수집됨: {clue.clueID}");

        // 데이터로그 매니저에게 단서 전달
        if (DataLogManager.Instance != null)
        {
            DataLogManager.Instance.AddClue(clue);
        }
        else
        {
            Debug.LogError("DataLogManager 씬에 인스턴스가 존재하지 않습니다!");
        }

        // 수집 완료 이펙트나 사운드 연출을 여기 넣으시면 좋습니다.
    }

    private void ClearSpawnedTexts()
    {
        foreach (var txt in spawnedTexts)
        {
            if (txt != null) Destroy(txt.gameObject);
        }
        spawnedTexts.Clear();
    }

    public void ClosePopup()
    {
        ClearSpawnedTexts();
        gameObject.SetActive(false);
    }
}
