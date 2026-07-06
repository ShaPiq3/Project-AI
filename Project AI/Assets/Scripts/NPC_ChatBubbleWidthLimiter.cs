using UnityEngine;
using TMPro;

public class NPC_ChatBubbleWidthLimiter : MonoBehaviour
{
    public enum BubbleType { Left_NPC, Right_User }

    [Header("=== 말풍선 종류 설정 ===")]
    public BubbleType bubbleType = BubbleType.Left_NPC;

    [Header("=== 연결할 컴포넌트 ===")]
    public TextMeshProUGUI chatText;    // 자식인 Text (TMP) 오브젝트
    public RectTransform bubbleRect;    // 말풍선 이미지 본인

    [Header("=== 여백 설정 ===")]
    public float paddingX = 40f;
    public float paddingY = 30f;

    private const float MAX_TEXT_WIDTH = 340f; // 가로 한계선

    void Update()
    {
        if (chatText == null || bubbleRect == null) return;

        // 1. 피벗(Pivot) 및 정렬 자동 제어 (돌려쓰기용)
        SetPivotAndAlignment();

        // 2. 텍스트 박스 가로 크기 제어
        if (chatText.preferredWidth > MAX_TEXT_WIDTH)
        {
            chatText.rectTransform.sizeDelta = new Vector2(MAX_TEXT_WIDTH, chatText.preferredHeight);
        }
        else
        {
            chatText.rectTransform.sizeDelta = new Vector2(chatText.preferredWidth, chatText.preferredHeight);
        }

        // 3. 말풍선 이미지 크기 동기화
        bubbleRect.sizeDelta = new Vector2(chatText.rectTransform.sizeDelta.x + paddingX, chatText.rectTransform.sizeDelta.y + paddingY);
    }

    private void SetPivotAndAlignment()
    {
        // NPC 말풍선 (왼쪽 정렬)
        if (bubbleType == BubbleType.Left_NPC)
        {
            bubbleRect.pivot = new Vector2(0f, 0.5f); // 왼쪽 중심
            chatText.alignment = TextAlignmentOptions.TopLeft;
        }
        // 유저 말풍선 (오른쪽 정렬)
        else if (bubbleType == BubbleType.Right_User)
        {
            bubbleRect.pivot = new Vector2(1f, 0.5f); // 오른쪽 중심
            chatText.alignment = TextAlignmentOptions.TopRight;
        }
    }
}