using UnityEngine;
using UnityEngine.UI;

public class QuestButtonRef : MonoBehaviour
{
    [Header("시작할 대사 그룹 이름 (CSV와 일치 필수)")]
    public string startContextName = "Q1_Start";

    [Header("이 버튼만의 고유 이미지 설정")]
    public Sprite lockedSprite;
    public Sprite activeSprite;

    private Image buttonImage;
    private Button button;
    private NewChatSystem chatSystem;

    void Awake()
    {
        buttonImage = GetComponent<Image>();
        button = GetComponent<Button>();

        // 💡 [수정] 경고 메시지의 권장 사항에 맞춰 함수명을 변경했습니다.
        chatSystem = FindAnyObjectByType<NewChatSystem>();
    }

    // 💡 인스펙터 On Click()에 등록할 전용 함수 (매개변수 없음 ➡️ 무조건 나타남!)
    public void OnClickThisButton()
    {
        if (chatSystem != null)
        {
            // 자신(this)의 정보와 기입한 텍스트를 매니저에게 토스합니다.
            chatSystem.OnClickQuestButton(startContextName, this);
        }
        else
        {
            Debug.LogError("[QuestButtonRef] 씬 내에서 NewChatSystem(ScrollManager)을 찾을 수 없습니다!");
        }
    }

    public void SetLocked()
    {
        if (button != null) button.interactable = false;
        if (buttonImage != null && lockedSprite != null) buttonImage.sprite = lockedSprite;
    }

    public void SetActive()
    {
        if (button != null) button.interactable = true;
        if (buttonImage != null && activeSprite != null) buttonImage.sprite = activeSprite;
    }
}