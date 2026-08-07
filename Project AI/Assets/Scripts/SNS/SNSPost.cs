using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SNSPost : MonoBehaviour
{
    [Header("Layout References")]
    [SerializeField] private RectTransform contentGroupRect; // Content_Group의 RectTransform
    [SerializeField] private RectTransform profileRect;      // 본문_프로필
    [SerializeField] private RectTransform textRect;           // 본문_글
    [SerializeField] private RectTransform imageRect;        // 본문_이미지
    [SerializeField] private RectTransform commentRect;      // 댓글창
    [SerializeField] private string questID;

    [Header("UI Component References (실제 데이터가 주입될 컴포넌트들)")]
    [SerializeField] private TMP_Text authorText;            // 본문 작성자 텍스트
    [SerializeField] private TMP_Text contentText;           // 본문 글 텍스트 (Rich Text가 켜져 있어야 함)
    [SerializeField] private Image profileImage;             // 프로필 이미지 컴포넌트
    [SerializeField] private Image postImage;                // 본문 이미지 컴포넌트

    // 💡 [추가] 외부(SNSManager 등)에서 본문 이미지에 접근할 수 있도록 공개 프로퍼티 노출
    public Image PostImage => postImage;

    [Header("Comment Spawner Settings")]
    [SerializeField] private GameObject commentPrefab;       // 새로 만든 댓글 프리팹
    [SerializeField] private Transform commentContentParent; // 댓글들이 생성되어 담길 부모

    /// <summary>
    /// SNSManager가 생성 직후 엑셀 데이터를 밀어 넣어주는 함수
    /// </summary>
    public void Setup(SNSPostData data)
    {
        // 1. 본문 작성자 주입 (💡 [변경] 단서 데이터는 없지만, "모든 TMP" 상호작용 대상이므로 빈 ID로 부착)
        if (authorText != null)
        {
            authorText.text = data.author;
            authorText.raycastTarget = true;

            ClueTextHoverEffect authorHover = authorText.gameObject.GetComponent<ClueTextHoverEffect>();
            if (authorHover == null) authorHover = authorText.gameObject.AddComponent<ClueTextHoverEffect>();
            authorHover.Configure("", questID, null);
        }

        // 2. 본문 텍스트 주입 및 클릭 단서 처리
        if (contentText != null)
        {
            string rawContent = data.content;
            string resolvedClueID;

            // 💡 본문 텍스트 안에 [CLUE:ID]가 포함되어 있는 경우
            if (rawContent.StartsWith("[CLUE:"))
            {
                int closeBracketIndex = rawContent.IndexOf(']');
                resolvedClueID = rawContent.Substring(6, closeBracketIndex - 6);
                contentText.text = rawContent.Substring(closeBracketIndex + 1);
            }
            // 💡 본문에 [CLUE:ID]는 없지만 별도의 clueID 필드로 단서를 수집하는 경우
            else if (!string.IsNullOrEmpty(data.clueID) && data.clueID.ToLower() != "none")
            {
                resolvedClueID = data.clueID;
                contentText.text = rawContent;
            }
            else
            {
                resolvedClueID = "";
                contentText.text = rawContent;
            }

            // 💡 [변경] 단서인지 여부와 상관없이 항상 ClueTextHoverEffect를 붙입니다.
            Button textBtn = contentText.gameObject.GetComponent<Button>();
            if (textBtn != null) Destroy(textBtn); // 기존 구식 버튼은 충돌 방지를 위해 제거

            contentText.raycastTarget = true;
            ClueTextHoverEffect contentHover = contentText.gameObject.GetComponent<ClueTextHoverEffect>();
            if (contentHover == null) contentHover = contentText.gameObject.AddComponent<ClueTextHoverEffect>();
            contentHover.Configure(resolvedClueID, questID, null);
        }

        // 3. 프로필 이미지 로드
        if (profileImage != null && !string.IsNullOrEmpty(data.profileImageName))
        {
            Sprite profSprite = Resources.Load<Sprite>("SNSImages/" + data.profileImageName);
            if (profSprite != null) profileImage.sprite = profSprite;
        }

        // 4. 본문 첨부 이미지 가공 및 클릭 단서 처리
        if (postImage != null && imageRect != null)
        {
            if (!string.IsNullOrEmpty(data.postImageName) && data.postImageName.ToLower() != "none")
            {
                imageRect.gameObject.SetActive(true); // 이미지 구역 켜기
                Sprite loadedSprite = Resources.Load<Sprite>("SNSImages/" + data.postImageName);
                if (loadedSprite != null)
                {
                    postImage.sprite = loadedSprite;

                    // 💡 [변경] 단서인지 여부와 상관없이 항상 ClueImageHoverEffect를 붙입니다.
                    Button imgBtn = postImage.gameObject.GetComponent<Button>();
                    if (imgBtn != null) Destroy(imgBtn); // 기존 구식 버튼은 충돌 방지를 위해 제거

                    ClueImageHoverEffect imgHover = postImage.gameObject.GetComponent<ClueImageHoverEffect>();
                    if (imgHover == null) imgHover = postImage.gameObject.AddComponent<ClueImageHoverEffect>();

                    string resolvedImageClueID = (!string.IsNullOrEmpty(data.imageClueID) && data.imageClueID.ToLower() != "none")
                        ? data.imageClueID : "";
                    imgHover.Configure(resolvedImageClueID, questID, null);
                }
            }
            else
            {
                // 이미지 구역을 완전히 꺼서 여백 감옥 방지!
                imageRect.gameObject.SetActive(false);
            }
        }

        // 5. 댓글 동적 생성 구역
        if (commentPrefab != null && commentContentParent != null)
        {
            // 기존 찌꺼기 청소
            foreach (Transform child in commentContentParent) Destroy(child.gameObject);

            // 댓글 리스트 생성
            foreach (SNSCommentData commentData in data.comments)
            {
                GameObject newComment = Instantiate(commentPrefab, commentContentParent);
                SNSCommentItem commentScript = newComment.GetComponent<SNSCommentItem>();
                if (commentScript != null)
                {
                    // 💡 [변경] SNS 댓글 CSV엔 아직 clueID 컬럼이 없어 항상 빈 ID로 부착합니다.
                    // (반응은 하되 수집은 안 되는 상태. 나중에 컬럼을 추가하면 그대로 연결됨)
                    commentScript.SetComment(commentData, "", questID, null);
                }
            }
        }

        // 6. 텍스트 주입과 댓글 생성이 "완전히 끝난 최종 크기"를 기준으로 레이아웃 높이 강제 갱신!!
        RefreshLayoutForce();
    }

    /// <summary>
    /// UI 박스 높이를 강제로 재계산하는 최적화 함수
    /// </summary>
    public void RefreshLayoutForce()
    {
        Canvas.ForceUpdateCanvases();

        float imageHeight = imageRect.gameObject.activeSelf ? imageRect.rect.height : 0f;

        float totalHeight = profileRect.rect.height +
                            textRect.rect.height +
                            imageHeight +
                            commentRect.rect.height + 80f; // 간격 여백

        contentGroupRect.sizeDelta = new Vector2(contentGroupRect.sizeDelta.x, totalHeight);

        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        if (transform.parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
        }
    }
}