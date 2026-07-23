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

        // 💡 [추가] 제목(title) 자체가 단서인 경우, 제목 텍스트에도 ClueTextHoverEffect를 붙입니다.
        ClueTextHoverEffect titleHoverEffect = titleText.gameObject.GetComponent<ClueTextHoverEffect>();
        if (!string.IsNullOrEmpty(data.titleClueID))
        {
            if (titleHoverEffect == null)
            {
                titleHoverEffect = titleText.gameObject.AddComponent<ClueTextHoverEffect>();
            }

            titleText.raycastTarget = true;

            var titleIdField = typeof(ClueTextHoverEffect).GetField("targetClueID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (titleIdField != null)
            {
                titleIdField.SetValue(titleHoverEffect, data.titleClueID);
            }

            var titleTitleField = typeof(ClueTextHoverEffect).GetField("sourceTitleOverride", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (titleTitleField != null)
            {
                titleTitleField.SetValue(titleHoverEffect, data.title);
            }
        }
        else
        {
            // 제목이 단서가 아닌 기사로 다시 세팅될 수도 있으므로, 이전에 붙어있던 컴포넌트는 제거
            if (titleHoverEffect != null) Destroy(titleHoverEffect);
        }

        // 원본 템플릿 비활성화
        if (textTemplate != null) textTemplate.gameObject.SetActive(false);

        // '|' 기호를 기준으로 문단 쪼개기
        string[] paragraphs = data.body.Split('|');

        // 1. 본문 문단 생성 루프
        for (int i = 0; i < paragraphs.Length; i++)
        {
            string rawParagraph = paragraphs[i].Trim();
            if (string.IsNullOrEmpty(rawParagraph)) continue;

            // 💡 [추가] 문단이 여러 개 단서일 수 있으므로, "[CLUE:아이디]" 태그로 시작하는지 확인
            // (SNSPost.cs에서 이미 쓰는 것과 동일한 방식). 태그는 화면에 표시되지 않고 잘라냅니다.
            string paragraphText = rawParagraph;
            string taggedClueID = null;

            if (rawParagraph.StartsWith("[CLUE:"))
            {
                int closeBracketIndex = rawParagraph.IndexOf(']');
                if (closeBracketIndex > 6)
                {
                    taggedClueID = rawParagraph.Substring(6, closeBracketIndex - 6);
                    paragraphText = rawParagraph.Substring(closeBracketIndex + 1).TrimStart();
                }
            }

            // 문단 템플릿 생성
            TextMeshProUGUI newText = Instantiate(textTemplate, textContainer);
            newText.richText = true;
            newText.text = paragraphText; // 태그를 제거한 실제 텍스트만 주입

            // 현재 가리키는 문단 번호 (1부터 시작)
            int currentParagraphNum = i + 1;

            // 💡 [변경] 기존 방식(문단 1개만 지정)도 계속 지원 - 하위 호환
            bool isLegacySingleClueParagraph =
                data.clueParagraphIndex > 0 &&
                currentParagraphNum == data.clueParagraphIndex &&
                !string.IsNullOrEmpty(data.bodyClueID);

            // 💡 태그 방식이 우선, 없으면 기존 단일 지정 방식 사용
            string finalClueID = !string.IsNullOrEmpty(taggedClueID)
                ? taggedClueID
                : (isLegacySingleClueParagraph ? data.bodyClueID : null);

            if (!string.IsNullOrEmpty(finalClueID))
            {
                // ❌ 기존의 기습적인 'Button' 추가 및 파란 글씨 색상 지정 코드 전체 제거!
                // 💡 대신, 우리가 작성한 똑똑한 'ClueTextHoverEffect' 컴포넌트를 동적으로 심어줍니다.
                ClueTextHoverEffect hoverEffect = newText.gameObject.GetComponent<ClueTextHoverEffect>();
                if (hoverEffect == null)
                {
                    hoverEffect = newText.gameObject.AddComponent<ClueTextHoverEffect>();
                }

                newText.raycastTarget = true;

                var idField = typeof(ClueTextHoverEffect).GetField("targetClueID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (idField != null)
                {
                    idField.SetValue(hoverEffect, finalClueID);
                }

                // 💡 이 기사의 실제 제목을 sourceTitleOverride에 주입
                // -> DataLog에 저장될 때 엑셀 SourceTitle 대신 실제 기사 제목이 사용됨
                var titleField = typeof(ClueTextHoverEffect).GetField("sourceTitleOverride", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (titleField != null)
                {
                    titleField.SetValue(hoverEffect, data.title);
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

                    // 💡 [추가] 이미지 단서에도 실제 기사 제목을 sourceTitleOverride에 주입
                    var imgTitleField = typeof(ClueImageHoverEffect).GetField("sourceTitleOverride", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (imgTitleField != null)
                    {
                        imgTitleField.SetValue(imgHover, data.title);
                    }
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