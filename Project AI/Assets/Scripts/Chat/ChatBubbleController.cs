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

            // 1. Resources 폴더 내 경로 설정 (Assets/Resources/ 폴더가 기준)
            // 엑셀 imagePath가 "F_1"이라면, 아래는 "F_1"을 로드합니다.
            Sprite loadedSprite = Resources.Load<Sprite>(data.imagePath);

            if (loadedSprite != null)
            {
                chatImage.sprite = loadedSprite; // 여기서 실제 이미지로 교체합니다!
            }
            else
            {
                Debug.LogError($"이미지를 찾을 수 없음: {data.imagePath}");
            }
        }
        else
        {
            chatImage.gameObject.SetActive(false); // 이미지가 없는 대화면 이미지 숨김
        }

        // 1. 핵심 수정: GetPreferredValues에서 가로 제한(maxWidth)을 넣어 계산해야 합니다.
        // 이렇게 하면 340을 넘는 순간 줄바꿈이 일어났을 때의 높이와 너비를 알려줍니다.
        Vector2 preferredSize = chatText.GetPreferredValues(data.dialogueText, maxWidth, 0);

        // 2. 만약 글자 너비가 maxWidth를 넘는다면
        if (preferredSize.x >= maxWidth)
        {
            chatTextLayoutElement.preferredWidth = maxWidth;
            chatText.textWrappingMode = TextWrappingModes.Normal;
        }
        else
        {
            // 3. 짧을 때는 글자만큼만 너비를 가지게 함
            chatTextLayoutElement.preferredWidth = -1f;
            chatText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        // 4. 레이아웃 갱신
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleBgObject.GetComponent<RectTransform>());
    }
}