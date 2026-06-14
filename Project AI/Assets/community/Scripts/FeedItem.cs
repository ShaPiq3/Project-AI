using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FeedItem : MonoBehaviour
{
    [Header("Header")]
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtWriter;
    public TextMeshProUGUI txtDate;

    [Header("Body")]
    public Image imgMain;
    public TextMeshProUGUI txtMain;

    [Header("Comments")]
    public Transform commentContainer;
    public GameObject commentPrefab;

    [Header("Image Fixed Size Settings")]
    public float imageWidth = 400f;
    public float imageHeight = 300f;

    public void Setup(FeedData data)
    {
        // 1. 헤더 텍스트 데이터 매칭
        txtTitle.text = data.title;
        txtWriter.text = data.writer;
        txtDate.text = data.date;

        // 2. 본문 이미지 고정 크기 및 자동 축소 처리
        if (string.IsNullOrWhiteSpace(data.mainImageName))
        {
            imgMain.gameObject.SetActive(false);
        }
        else
        {
            imgMain.gameObject.SetActive(true);
            Sprite loadedSprite = Resources.Load<Sprite>($"FeedImages/{data.mainImageName}");
            if (loadedSprite != null)
            {
                imgMain.sprite = loadedSprite;

                LayoutElement imgLayout = imgMain.GetComponent<LayoutElement>();
                if (imgLayout != null)
                {
                    imgLayout.preferredWidth = imageWidth;
                    imgLayout.preferredHeight = imageHeight;
                }
            }
        }

        // 3. 본문 텍스트 처리
        if (string.IsNullOrWhiteSpace(data.mainText))
        {
            txtMain.gameObject.SetActive(false);
        }
        else
        {
            txtMain.gameObject.SetActive(true);
            txtMain.text = data.mainText;
        }

        // 4. 동적 댓글 생성 처리
        foreach (Transform child in commentContainer)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        if (data.comments == null || data.comments.Count == 0)
        {
            commentContainer.gameObject.SetActive(false);
        }
        else
        {
            commentContainer.gameObject.SetActive(true);
            foreach (var cData in data.comments)
            {
                GameObject cObj = Instantiate(commentPrefab, commentContainer);
                CommentItem cScript = cObj.GetComponent<CommentItem>();
                if (cScript != null)
                {
                    cScript.Setup(cData.writer, cData.text, cData.emoticonSpriteName);
                }

                // ★ [핵심 추가 1] 생성된 개별 댓글 자식의 크기를 '그 자리에서 즉시' 먼저 계산합니다.
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)cObj.transform);
            }
        }

        // ★ [핵심 추가 2] 자식들이 제대로 굳은 뒤, 이들을 품은 댓글 바구니의 크기를 계산합니다.
        if (commentContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)commentContainer);
        }

        // ★ [핵심 추가 3] 최종적으로 댓글 창의 정확한 높이까지 포함하여 최상단 하얀 카드를 늘려줍니다.
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }
}