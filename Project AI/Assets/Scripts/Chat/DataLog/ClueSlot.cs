using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static UnityEditor.Tilemaps.RuleTileTemplate;

public class ClueSlot : MonoBehaviour
{
    [Header("단서 슬롯 UI 컴포넌트들")]
    // 💡 sourceText 변수는 하이어라키 연결 해제를 위해 남겨두거나 주석 처리해도 됩니다.
    [SerializeField] private TextMeshProUGUI sourceText;
    [SerializeField] private TextMeshProUGUI contentText; // 수집된 단서 본문 내용
    [SerializeField] private Image clueImage;             // 단서 이미지 (있을 경우)

    // 💡 DataLogManager가 단서를 생성할 때 이 함수를 호출하여 화면에 값을 채눕니다.

    public ClueData clueData;
    public void SetClueUI(ClueData data)
    {
        this.clueData = data;
        // 1. 출처 텍스트 채우기 제거 (버튼 내 표시 불필요)     
        if (sourceText != null)
        {
            sourceText.text = string.IsNullOrEmpty(data.sourceType) ?
                data.sourceTitle : $"[{data.sourceType}] {data.sourceTitle}";
        }
        

        // 2. 내용 텍스트 채우기
        if (contentText != null)
        {
            contentText.text = data.contentText;
        }

        // 3. 이미지 처리
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
                // 이미지 데이터가 없으면 이미지 칸 숨김
                clueImage.gameObject.SetActive(false);
            }
        }
    }

    public void OnClickSlot()
    {
        if (DataLogManager.Instance.isDeleteMode)
        {
            // 💡 UIManager를 DataLogManager 안에 넣었으므로 경로를 맞춰줍니다.
            DataLogManager.Instance.uiManager.ShowConfirmPopup(
                "이 단서를 수집 상태에서 해제하시겠습니까?",
                () => { DataLogManager.Instance.RemoveClueAndRefreshUI(this.clueData); },
                () => { Debug.Log("취소"); }
            );
        }
        else
        {
            // 원래 기능: 상세 보기 호출 등
            Debug.Log("단서 상세 보기: " + clueData.clueName);
        }
    }
}