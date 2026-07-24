using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatBubbleController : MonoBehaviour
{
    [Header("UI 요소 연결")]
    public GameObject bubbleBgObject;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI chatText;
    public Image chatImage;

    [Header("말풍선 크기 제한")]
    public LayoutElement chatTextLayoutElement;
    public float maxWidth = 500f;

    [Header("타이핑 효과 설정")]
    [Tooltip("한 글자가 나타나는 간격 (초). CSV에서 개별 지정이 없을 때(0 이하) 쓰이는 기본값입니다.")]
    public float defaultTypingSpeed = 0.03f;

    private string fullDialogueText = "";
    private float currentTypingSpeed = 0.03f;
    private bool isNpc = false;
    private Coroutine typingCoroutine;

    public bool IsTypingComplete { get; private set; } = true;

    public void SetupBubble(DialogueData data)
    {
        if (nameText != null)
        {
            nameText.text = data.speakerName;
        }

        fullDialogueText = data.dialogueText;
        currentTypingSpeed = data.typingSpeed > 0f ? data.typingSpeed : defaultTypingSpeed;
        isNpc = (data.speakerType != "USER");

        chatText.text = fullDialogueText;

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

        bool hasText = !string.IsNullOrEmpty(data.dialogueText);
        bubbleBgObject.SetActive(hasText);

        if (hasText)
        {
            float targetMaxWidth = (data.speakerType == "USER") ? 468f : 328f;

            if (chatText.preferredWidth > targetMaxWidth)
            {
                chatTextLayoutElement.preferredWidth = targetMaxWidth;
            }
            else
            {
                chatTextLayoutElement.preferredWidth = -1;
            }
            chatText.textWrappingMode = TextWrappingModes.Normal;
        }

        StartCoroutine(RebuildLayoutNextFrame());

        if (hasText)
        {
            chatText.text = "";
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeTextCoroutine());
        }
        else
        {
            IsTypingComplete = true;
        }
    }

    private IEnumerator TypeTextCoroutine()
    {
        IsTypingComplete = false;

        // 💡 실제 줄바꿈이 몇 줄로 될지 미리 확인 (렌더링만 하고 즉시 다시 비움)
        chatText.text = fullDialogueText;
        chatText.ForceMeshUpdate();
        int lineCount = chatText.textInfo.lineCount;
        chatText.text = "";
        yield return null;

        // 💡 [핵심] 한 줄이면 오른쪽→왼쪽, 두 줄 이상이면 왼쪽→오른쪽(원래 방식)
        bool useRightToLeft = isNpc && lineCount <= 1;

        if (useRightToLeft)
        {
            for (int i = fullDialogueText.Length; i >= 0; i--)
            {
                chatText.text = fullDialogueText.Substring(i);
                yield return new WaitForSeconds(currentTypingSpeed);
            }
            chatText.text = fullDialogueText;
        }
        else
        {
            for (int i = 1; i <= fullDialogueText.Length; i++)
            {
                chatText.text = fullDialogueText.Substring(0, i);
                yield return new WaitForSeconds(currentTypingSpeed);
            }
        }

        IsTypingComplete = true;
        typingCoroutine = null;
    }

    public void CompleteTypingInstantly()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        chatText.text = fullDialogueText;
        IsTypingComplete = true;
    }

    private IEnumerator RebuildLayoutNextFrame()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleBgObject.GetComponent<RectTransform>());
    }
}