using UnityEngine;
using UnityEngine.UI; // 💡 Image 컴포넌트를 제어하기 위해 필수 추가

public class QuestButtonRef : MonoBehaviour
{
    private Button myButton;

    [Header("이미지 설정")]
    [Tooltip("교체할 버튼의 Image 컴포넌트입니다. (본인 오브젝트에 있다면 생략 가능)")]
    public Image buttonImage;

    [Tooltip("퀘스트가 비활성화(잠금) 상태일 때의 이미지")]
    public Sprite lockedSprite;

    [Tooltip("퀘스트가 활성화(등장) 상태일 때의 이미지")]
    public Sprite unlockedSprite;

    private void Awake()
    {
        myButton = GetComponent<Button>(); // 버튼 컴포넌트 가져오기
        if (buttonImage == null) buttonImage = GetComponent<Image>();
    }

    /// <summary>
    /// 💡 뉴챗시스템의 InitAllQuestButtons() 등에서 호출되는 잠금 메서드
    /// </summary>
    public void SetLocked()
    {
        if (buttonImage != null && lockedSprite != null)
            buttonImage.sprite = lockedSprite;

        // 💡 아래 줄을 주석 처리하거나 지우세요! 
        // myButton.interactable = false; 

        // 만약 잠겨있을 때 아예 못 누르게 하고 싶다면, 
        // 클릭 시점에 NewChatSystem에서 '잠겨있음' 체크를 해야 합니다.
    }

    public void SetActive()
    {
        if (buttonImage != null && unlockedSprite != null)
            buttonImage.sprite = unlockedSprite;

        // 활성화되었을 때만 누를 수 있게 합니다.
        if (myButton != null) myButton.interactable = true;
    }
}