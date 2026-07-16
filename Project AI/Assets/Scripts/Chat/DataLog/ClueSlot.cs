using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClueSlot : MonoBehaviour
{
    [Header("단서 슬롯 UI 컴포넌트들")]
    [SerializeField] private TextMeshProUGUI sourceText;  // 출처 (기사 제목, SNS 아이디 등)
    [SerializeField] private TextMeshProUGUI contentText; // 수집된 단서 본문 내용
    [SerializeField] private Image clueImage;             // 단서 이미지 (있을 경우)

    // 💡 DataLogManager가 단서를 생성할 때 이 함수를 호출하여 화면에 값을 채웁니다.
    public void SetClueUI(ClueData data)
    {
        // 1. 출처 및 내용 텍스트 채우기
        if (sourceText != null)
        {
            // 만약 ClueData에 sourceType이 추가되어 있다면 함께 표기 가능
            // 예: [뉴스] 기사 제목 / [SNS] 유저 아이디
            sourceText.text = string.IsNullOrEmpty(data.sourceType) ?
                data.sourceTitle : $"[{data.sourceType}] {data.sourceTitle}";
        }

        if (contentText != null)
        {
            contentText.text = data.contentText;
        }

        // 2. 이미지 처리
        if (clueImage != null)
        {
            if (!string.IsNullOrEmpty(data.imageName))
            {
                // Resources/NewsImages/ 경로 또는 지정된 폴더에서 이미지 로드
                Sprite loadedSprite = Resources.Load<Sprite>($"NewsImages/{data.imageName}");
                if (loadedSprite != null)
                {
                    clueImage.sprite = loadedSprite;
                    clueImage.gameObject.SetActive(true);
                }
                else
                {
                    clueImage.gameObject.SetActive(false); // 로드 실패 시 숨김
                }
            }
            else
            {
                clueImage.gameObject.SetActive(false); // 이미지 데이터가 없으면 이미지 칸 숨김
            }
        }
    }
}
