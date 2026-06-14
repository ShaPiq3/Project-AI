using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CommentItem : MonoBehaviour
{
    public TextMeshProUGUI txtCommentWriter;
    public TextMeshProUGUI txtCommentText;
    public GameObject emoticonWrapper;
    public Image imgCommentEmoticon;

    public void Setup(string writer, string text, string emoticonName)
    {
        txtCommentWriter.text = writer;

        // 텍스트 댓글 처리
        if (string.IsNullOrEmpty(text))
        {
            txtCommentText.gameObject.SetActive(false);
        }
        else
        {
            txtCommentText.gameObject.SetActive(true);
            txtCommentText.text = text;
        }

        // 이모티콘 처리
        if (string.IsNullOrEmpty(emoticonName))
        {
            emoticonWrapper.SetActive(false);
        }
        else
        {
            emoticonWrapper.SetActive(true);

            Sprite emoticonSprite = Resources.Load<Sprite>($"Emoticons/{emoticonName}");
            if (emoticonSprite != null)
            {
                imgCommentEmoticon.sprite = emoticonSprite;

                // -------------------------------------------------------------
                // [여기서부터 추가할 코드] 디시콘 이미지 원본 비율 자동 계산기
                // -------------------------------------------------------------
                LayoutElement layoutElement = imgCommentEmoticon.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    // 원본 이미지의 비율 (가로 ÷ 세로) 계산
                    float spriteAspectRatio = (float)emoticonSprite.rect.width / emoticonSprite.rect.height;

                    // 기준 높이를 120으로 고정하고, 가로 너비는 원본 비율에 맞춰 동적으로 변경
                    layoutElement.preferredHeight = 120f;
                    layoutElement.preferredWidth = 120f * spriteAspectRatio;
                }
                // -------------------------------------------------------------
            }
        }
    }
}