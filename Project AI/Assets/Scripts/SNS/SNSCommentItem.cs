using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SNSCommentItem : MonoBehaviour
{
    [SerializeField] private TMP_Text authorText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private Image emoticonImage;

    public void SetComment(SNSCommentData data)
    {
        authorText.text = data.author;
        contentText.text = data.content;

        // 이모티콘이 있다면 이미지를 켜고 로드, 없으면 가리기
        if (data.isEmoticon && !string.IsNullOrEmpty(data.emoticonName))
        {
            emoticonImage.gameObject.SetActive(true);
            Sprite emoSprite = Resources.Load<Sprite>("SNSImages/" + data.emoticonName);
            emoticonImage.sprite = emoSprite;
        }
        else
        {
            emoticonImage.gameObject.SetActive(false);
        }
    }
}
