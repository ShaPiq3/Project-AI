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
    public GameObject imageContainer;
    public Image chatImage;
    public AudioSource bubbleAudioSource;

    [Header("이미지 크기 제한")]
    public float imageFixedWidth = 140f;

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

        // 💡 [추가] 수집한 단서를 그대로 조립한 보고서로 대사를 대체하고 싶을 때 CSV에 이 토큰을 써두면 됨.
        // 안 쓰면(플레이스홀더가 없으면) 기존처럼 CSV에 적힌 텍스트가 그대로 나간다.
        if (DataLogManager.Instance != null && fullDialogueText.Contains("{{CLUE_REPORT}}"))
        {
            fullDialogueText = fullDialogueText.Replace("{{CLUE_REPORT}}", DataLogManager.Instance.LastGeneratedReport);
        }

        // 💡 [추가] 문서요약 퀘스트에서 선택한 문장들을 그대로 조립한 보고서로 대체하고 싶을 때 CSV에 이 토큰을 써두면 됨.
        if (fullDialogueText.Contains("{{DOCUMENT_REPORT}}"))
        {
            fullDialogueText = fullDialogueText.Replace("{{DOCUMENT_REPORT}}", DocumentQuestManager.LastGeneratedReport);
        }

        currentTypingSpeed = data.typingSpeed > 0f ? data.typingSpeed : defaultTypingSpeed;
        isNpc = (data.speakerType != "USER");

        chatText.text = fullDialogueText;

        if (data.hasImage)
        {
            if (imageContainer != null) imageContainer.SetActive(true);
            chatImage.gameObject.SetActive(true);
            Sprite loadedSprite = Resources.Load<Sprite>(data.imagePath);
            if (loadedSprite != null)
            {
                chatImage.sprite = loadedSprite;

                // 💡 [변경] AspectRatioFitter 대신, 폭 고정 + 비율 계산한 높이를
                // LayoutElement에 직접 써넣음 (VerticalLayoutGroup이 1단계 계산에서부터 정확한 값을 알게 됨)
                float ratio = loadedSprite.rect.width / loadedSprite.rect.height;
                float calculatedHeight = imageFixedWidth / ratio;

                LayoutElement containerLayoutElement = imageContainer.GetComponent<LayoutElement>();
                if (containerLayoutElement != null)
                {
                    containerLayoutElement.preferredWidth = imageFixedWidth;
                    containerLayoutElement.preferredHeight = calculatedHeight;
                }
            }
            else
            {
                Debug.LogError($"이미지를 찾을 수 없음: {data.imagePath}");
            }
        }
        else
        {
            if (imageContainer != null) imageContainer.SetActive(false);
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

        if(hasText) { 
            if (!isNpc) // 💡 [변경] USER일 때 타이핑 효과 적용
            {
                chatText.text = "";
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeTextCoroutine());
            }
            else
            {
                // 💡 [변경] NPC는 타이핑 효과 없이 즉시 전체 텍스트 표시
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                    typingCoroutine = null;
                }
                chatText.text = fullDialogueText;
                IsTypingComplete = true;

                if (bubbleAudioSource != null) bubbleAudioSource.Play();
            }
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

        
        bool useRightToLeft = !isNpc && lineCount <= 1;

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