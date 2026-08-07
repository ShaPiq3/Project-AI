using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SNSCommentItem : MonoBehaviour
{
    [SerializeField] private TMP_Text authorText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private Image emoticonImage;

    /// <summary>
    /// 💡 [변경] clueID/questID/sourceTitle을 함께 받아서, 댓글 텍스트와 이모티콘 이미지에도
    /// 단서인지 여부와 상관없이 항상 상호작용 컴포넌트를 붙입니다.
    /// </summary>
    public void SetComment(SNSCommentData data, string clueID = null, string questID = null, string sourceTitle = null)
    {
        authorText.text = data.author;
        contentText.text = data.content;
        contentText.raycastTarget = true;

        ClueTextHoverEffect textHover = contentText.gameObject.GetComponent<ClueTextHoverEffect>();
        if (textHover == null) textHover = contentText.gameObject.AddComponent<ClueTextHoverEffect>();
        textHover.Configure(clueID, questID, sourceTitle);

        // 이모티콘이 있다면 이미지를 켜서 로드, 없으면 꺼버림
        if (data.isEmoticon && !string.IsNullOrEmpty(data.emoticonName))
        {
            emoticonImage.gameObject.SetActive(true);
            Sprite emoSprite = Resources.Load<Sprite>("SNSImages/" + data.emoticonName);
            emoticonImage.sprite = emoSprite;

            ClueImageHoverEffect imgHover = emoticonImage.gameObject.GetComponent<ClueImageHoverEffect>();
            if (imgHover == null) imgHover = emoticonImage.gameObject.AddComponent<ClueImageHoverEffect>();
            imgHover.Configure(clueID, questID, sourceTitle);
        }
        else
        {
            emoticonImage.gameObject.SetActive(false);
        }
    }
}
