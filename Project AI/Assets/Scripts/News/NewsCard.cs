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

        // 💡 [변경] 제목이 단서인지 여부와 상관없이 항상 ClueTextHoverEffect를 붙입니다.
        // 단서 수집 모드에서는 제목도 다른 문단과 동일하게 반응하고,
        // 실제로 수집 가능한지는 클릭 시 스캔 판정으로 구분됩니다.
        ClueTextHoverEffect titleHoverEffect = titleText.gameObject.GetComponent<ClueTextHoverEffect>();
        if (titleHoverEffect == null)
        {
            titleHoverEffect = titleText.gameObject.AddComponent<ClueTextHoverEffect>();
        }

        titleText.raycastTarget = true;
        titleHoverEffect.Configure(data.titleClueID, "", data.title);

        PanelLinkParagraphEffect titleLinkEffect = titleText.gameObject.GetComponent<PanelLinkParagraphEffect>();
        if (data.title.Contains("<link=\""))
        {
            if (titleLinkEffect == null)
                titleLinkEffect = titleText.gameObject.AddComponent<PanelLinkParagraphEffect>();
            titleLinkEffect.Setup(data.title);
        }
        else
        {
            if (titleLinkEffect != null) Destroy(titleLinkEffect);
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

            // 💡 문단이 여러 개 단서일 수 있으므로, "[CLUE:아이디]" 태그로 시작하는지 확인
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

            // 💡 기존 방식(문단 1개만 지정)도 계속 지원 - 하위 호환
            bool isLegacySingleClueParagraph =
                data.clueParagraphIndex > 0 &&
                currentParagraphNum == data.clueParagraphIndex &&
                !string.IsNullOrEmpty(data.bodyClueID);

            // 💡 태그 방식이 우선, 없으면 기존 단일 지정 방식 사용
            string finalClueID = !string.IsNullOrEmpty(taggedClueID)
                ? taggedClueID
                : (isLegacySingleClueParagraph ? data.bodyClueID : null);

            // 💡 [변경] 단서 문단인지 여부와 상관없이 모든 문단에 항상 ClueTextHoverEffect를 붙입니다.
            ClueTextHoverEffect hoverEffect = newText.gameObject.GetComponent<ClueTextHoverEffect>();
            if (hoverEffect == null)
            {
                hoverEffect = newText.gameObject.AddComponent<ClueTextHoverEffect>();
            }

            newText.raycastTarget = true;
            // 💡 이 기사의 실제 제목을 sourceTitleOverride에 주입
            hoverEffect.Configure(finalClueID, "", data.title);

            if (paragraphText.Contains("<link=\""))
            {
                PanelLinkParagraphEffect linkEffect = newText.gameObject.AddComponent<PanelLinkParagraphEffect>();
                linkEffect.Setup(paragraphText);
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

                // 💡 [변경] 이미지 역시 단서인지 여부와 상관없이 항상 ClueImageHoverEffect를 붙입니다.
                ClueImageHoverEffect imgHover = newsImage.gameObject.GetComponent<ClueImageHoverEffect>();
                Button imgBtn = newsImage.gameObject.GetComponent<Button>();

                if (imgBtn != null) Destroy(imgBtn); // 기존 구식 버튼은 충돌 방지를 위해 제거

                if (imgHover == null) imgHover = newsImage.gameObject.AddComponent<ClueImageHoverEffect>();

                string resolvedImageClueID = (!string.IsNullOrEmpty(data.imageClueID) && data.imageClueID.ToLower() != "none")
                    ? data.imageClueID : "";
                imgHover.Configure(resolvedImageClueID, "", data.title);

                // 💡 [추가] 본문 이미지를 이미지 생성 퀘스트 수집 대상으로 자동 등록
                // (목록 썸네일이 아니라 여기, 상세 본문 이미지에 걸어야 함)
                CollectibleImageBinder.Bind(newsImage, data.collectibleImageID);
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
        // 💡 이 오브젝트는 클릭할 때마다 새로 복제된 것이므로, 완전히 파괴합니다.
        ClearSpawnedTexts();
        Destroy(gameObject);
    }
}