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

    [Header("말풍선 크기 제한")]
    public LayoutElement chatTextLayoutElement;  // chatText 오브젝트에 붙은 LayoutElement 연결
    public float maxWidth = 500f;


    public void SetupBubble(DialogueData data)
    {
        chatText.text = data.dialogueText;

        // 이미지 제어 로직 추가
        if (data.hasImage)
        {
            chatImage.gameObject.SetActive(true);
            Sprite loadedSprite = Resources.Load<Sprite>(data.imagePath);
            if (loadedSprite != null)
            {
                chatImage.sprite = loadedSprite;
            }
            else
            {
                Debug.LogError($"이미지를 찾을 수 없음: {data.imagePath}");
            }
        }
        else
        {
            chatImage.gameObject.SetActive(false);
        }

        // --- 수정: 배경 제어 로직 시작 ---
        // 대사(text)가 비어있고 이미지만 있는 경우에만 배경 숨김
        bool hasText = !string.IsNullOrEmpty(data.dialogueText);
        bool hasImage = data.hasImage;

        // 대사가 없거나 이미지가 있으면 배경을 끈다 (또는 조건에 맞게 조정)
        // 텍스트가 있을 때만 배경을 보여주길 원하시면 아래와 같이 작성하세요:
        bubbleBgObject.SetActive(hasText);
        // --- 수정: 배경 제어 로직 끝 ---

        // 1. 핵심 수정: GetPreferredValues에서 가로 제한(maxWidth)을 넣어 계산해야 합니다.
        if (hasText)
        {
            float targetMaxWidth = (data.speakerType == "USER") ? 468f : 328f;

            // 1. 텍스트가 328을 넘는지 확인
            if (chatText.preferredWidth > targetMaxWidth)
            {
                // 넘으면 328로 고정하여 줄바꿈 유도
                chatTextLayoutElement.preferredWidth = targetMaxWidth;
            }
            else
            {
                // 안 넘으면 텍스트 크기만큼만 차지하도록 설정
                chatTextLayoutElement.preferredWidth = -1;
            }

            chatText.textWrappingMode = TextWrappingModes.Normal;
        }
        StartCoroutine(RebuildLayoutNextFrame());
    }

    private System.Collections.IEnumerator RebuildLayoutNextFrame()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleBgObject.GetComponent<RectTransform>());
    }
}