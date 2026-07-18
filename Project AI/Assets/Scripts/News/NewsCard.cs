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
            newText.text = paragraphText; // 텍스트 원본 그대로 주입

            // 현재 가리키는 문단 번호 (1부터 시작)
            int currentParagraphNum = i + 1;

            // 🌟 [수정] 엑셀에서 받아온 단서 문단일 때 처리
            if (data.clueParagraphIndex > 0 && currentParagraphNum == data.clueParagraphIndex && !string.IsNullOrEmpty(data.bodyClueID))
            {
                // ❌ 기존의 기습적인 'Button' 추가 및 파란 글씨 색상 지정 코드 전체 제거!
                // 💡 대신, 우리가 작성한 똑똑한 'ClueTextHoverEffect' 컴포넌트를 동적으로 심어줍니다.
                ClueTextHoverEffect hoverEffect = newText.gameObject.GetComponent<ClueTextHoverEffect>();
                if (hoverEffect == null)
                {
                    hoverEffect = newText.gameObject.AddComponent<ClueTextHoverEffect>();
                }

                newText.raycastTarget = true;
                // 리플렉션이나 인스펙터 직렬화 필드 대입을 위해 세팅
                // (만약 targetClueID 변수가 private/protected라면 컴포넌트 인스펙터에서 직접 부여하셔도 됩니다.)
                // 아래 코드는 동적으로 단서 ID를 적용하기 위한 안전장치입니다.
                var idField = typeof(ClueTextHoverEffect).GetField("targetClueID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (idField != null)
                {
                    idField.SetValue(hoverEffect, data.bodyClueID);
                }
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

                // 💡 [수정] 이미지 역시 직접 버튼을 주입해 직접 수집하던 코드에서
                // 우리가 만든 호버/클릭 효과 컴포넌트(ClueImageHoverEffect) 체제로 자동 전환합니다.
                ClueImageHoverEffect imgHover = newsImage.gameObject.GetComponent<ClueImageHoverEffect>();
                Button imgBtn = newsImage.gameObject.GetComponent<Button>();

                if (imgBtn != null) Destroy(imgBtn); // 기존 구식 버튼은 충돌 방지를 위해 제거

                if (!string.IsNullOrEmpty(data.imageClueID) && data.imageClueID.ToLower() != "none")
                {
                    if (imgHover == null) imgHover = newsImage.gameObject.AddComponent<ClueImageHoverEffect>();

                    // 이미지 호버 스크립트에도 마우스 클릭 시 수집을 호출하는 확장 스크립트를 적용하거나 관리할 수 있게 됩니다.
                }
                else
                {
                    if (imgHover != null) Destroy(imgHover);
                }
            }
            else newsImage.gameObject.SetActive(false);
        }
        else newsImage.gameObject.SetActive(false);
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