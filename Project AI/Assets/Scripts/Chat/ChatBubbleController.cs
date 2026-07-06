using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatBubbleController : MonoBehaviour
{
    [Header("UI 요소 연결")]
    public GameObject bubbleBgObject;   // Bubble_Bg 오브젝트 (대사 없을 때 숨기기 위함)
    public TextMeshProUGUI nameText;     // USER_ChatName 내에 있는 NameText 연결용 (NPC는 인스펙터에서 비워둠)
    public TextMeshProUGUI chatText;     // 대사 텍스트 컴포넌트
    public Image chatImage;               // 독립된 순수 Image 컴포넌트

    public void SetupBubble(DialogueData data)
    {
        // 1. 이름 표기 (유저 프리팹처럼 인스펙터에 nameText 슬롯이 연결되어 있는 경우에만 작동)
        if (nameText != null)
        {
            nameText.text = data.speakerName; // 엑셀의 SpeakerName 데이터를 텍스트창에 주입
        }

        // 2. 대사 처리 (엑셀 대사 칸이 비어있으면 말풍선 자체를 꺼버림)
        if (string.IsNullOrEmpty(data.dialogueText))
        {
            bubbleBgObject.SetActive(false);
        }
        else
        {
            bubbleBgObject.SetActive(true);
            chatText.text = data.dialogueText;
        }

        // 3. 독립 이미지 처리 (말풍선 없이 맹 이미지로만 출력)
        if (data.hasImage && !string.IsNullOrEmpty(data.imagePath))
        {
            Sprite loadedSprite = Resources.Load<Sprite>(data.imagePath);
            if (loadedSprite != null)
            {
                chatImage.gameObject.SetActive(true);
                chatImage.sprite = loadedSprite;
            }
            else
            {
                chatImage.gameObject.SetActive(false);
            }
        }
        else
        {
            chatImage.gameObject.SetActive(false);
        }

        // 4. 자식들이 On/Off 됨에 따라 최상위 부모(프리팹 자체) 크기 즉시 강제 리빌드
        // Content Size Fitter 버그를 방지하고 크기를 칼같이 맞춰줍니다.
        RectTransform myRect = GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(myRect);
    }
}