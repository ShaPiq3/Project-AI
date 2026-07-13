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

        for (int i = 0; i < paragraphs.Length; i++)
        {
            string paragraphText = paragraphs[i].Trim();
            if (string.IsNullOrEmpty(paragraphText)) continue;

            // TMP 문단 생성
            TextMeshProUGUI newText = Instantiate(textTemplate, textContainer);

            // 리치 텍스트 기능 활성화 보장
            newText.richText = true;

            // 엑셀에 적힌 태그(<b>, <color> 등)가 포함된 텍스트 주입
            newText.text = paragraphText;
            newText.gameObject.SetActive(true);

            spawnedTexts.Add(newText);
        }

        // 이미지 로드 로직
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