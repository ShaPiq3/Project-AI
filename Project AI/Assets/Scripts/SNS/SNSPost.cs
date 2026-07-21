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
        // 1. 본문 작성자 주입
        if (authorText != null) authorText.text = data.author;

        // 2. 본문 텍스트 주입 및 클릭 단서 처리
        if (contentText != null)
        {
            string rawContent = data.content;

            // 💡 본문 텍스트 안에 [CLUE:ID]가 포함되어 있는 경우
            if (rawContent.StartsWith("[CLUE:"))
            {
                int closeBracketIndex = rawContent.IndexOf(']');
                string tagClueID = rawContent.Substring(6, closeBracketIndex - 6);
                string realContent = rawContent.Substring(closeBracketIndex + 1);

                contentText.text = realContent;

                // 텍스트를 클릭 가능한 버튼으로 전환
                Button textBtn = contentText.gameObject.GetComponent<Button>();
                if (textBtn == null) textBtn = contentText.gameObject.AddComponent<Button>();

                textBtn.transition = Selectable.Transition.ColorTint;
                textBtn.onClick.RemoveAllListeners();
                textBtn.onClick.AddListener(() =>
                {
                    CollectClue(tagClueID);
                });
            }
            // 💡 본문에 [CLUE:ID]는 없지만 별도의 clueID 필드로 단서를 수집하는 경우
            else if (!string.IsNullOrEmpty(data.clueID) && data.clueID.ToLower() != "none")
            {
                contentText.text = rawContent;

                Button textBtn = contentText.gameObject.GetComponent<Button>();
                if (textBtn == null) textBtn = contentText.gameObject.AddComponent<Button>();

                textBtn.transition = Selectable.Transition.ColorTint;
                textBtn.onClick.RemoveAllListeners();
                textBtn.onClick.AddListener(() =>
                {
                    CollectClue(data.clueID);
                });
            }
            else
            {
                // 일반 본문일 때는 버튼 비활성화/제거
                contentText.text = rawContent;
                Button textBtn = contentText.gameObject.GetComponent<Button>();
                if (textBtn != null) Destroy(textBtn);
            }
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

                    // 💡 이미지 클릭 단서 수집 로직 추가
                    Button imgBtn = postImage.gameObject.GetComponent<Button>();

                    // 엑셀에 ImageClueID가 존재하고 none이 아니면 버튼 처리
                    if (!string.IsNullOrEmpty(data.imageClueID) && data.imageClueID.ToLower() != "none")
                    {
                        if (imgBtn == null) imgBtn = postImage.gameObject.AddComponent<Button>();

                        imgBtn.transition = Selectable.Transition.ColorTint;
                        imgBtn.onClick.RemoveAllListeners();
                        imgBtn.onClick.AddListener(() =>
                        {
                            CollectClue(data.imageClueID);
                        });
                    }
                    else
                    {
                        // 단서 수집이 안 되는 일반 이미지라면 버튼 컴포넌트 제거
                        if (imgBtn != null) Destroy(imgBtn);
                    }
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
                    commentScript.SetComment(commentData);
                }
            }
        }

        // 6. 텍스트 주입과 댓글 생성이 "완전히 끝난 최종 크기"를 기준으로 레이아웃 높이 강제 갱신!!
        RefreshLayoutForce();
    }

    /// <summary>
    /// 💡 단서 수집 통신을 담당하는 안전한 내부 함수
    /// </summary>


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

    private void CollectClue(string targetClueID)
    {
        if (string.IsNullOrEmpty(targetClueID) || targetClueID.ToLower() == "none") return;

        // 단서 수집 모드가 켜져 있을 때만 수집 가능하게 제한
        if (DataLogManager.Instance != null && !DataLogManager.Instance.IsClueSearchModeActive)
        {
            Debug.Log("현재 단서 수집 모드가 비활성화되어 있어 수집할 수 없습니다.");
            return;
        }

        Debug.Log($"[SNS] 단서 수집 요청: {targetClueID}");

        if (DataLogManager.Instance != null)
        {
            DataLogManager.Instance.AcquireClue(this.questID, targetClueID);
        }
        else
        {
            Debug.LogError("DataLogManager 인스턴스를 씬에서 찾을 수 없습니다!");
        }
    }
}