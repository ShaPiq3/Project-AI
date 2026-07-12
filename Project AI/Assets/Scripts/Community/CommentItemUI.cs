using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CommentItemUI : MonoBehaviour
{
    [Header("UI 요소 연결 (인스펙터에서 드래그)")]
    public TMP_Text authorText;       // 닉네임 표시용 (AuthorText 오브젝트 연결)
    public TMP_Text contentText;      // 일반 텍스트 댓글용 (ContextText 오브젝트 연결)
    public Image emoticonImage;       // 이모티콘 이미지 댓글용 (EmoticonImage 오브젝트 연결)

    /// <summary>
    /// 일반 게시판용 댓글 데이터를 UI에 세팅하는 함수
    /// </summary>
    public void Setup(CommentData data)
    {
        // 1. 작성자 닉네임 세팅
        authorText.text = data.author;

        // 2. 이모티콘 댓글 처리 (isEmoticon == true)
        if (data.isEmoticon == true)
        {
            contentText.gameObject.SetActive(false);  // 텍스트 컴포넌트 끄기
            emoticonImage.gameObject.SetActive(true); // 이모티콘 이미지 켜기

            // Resources/Emoticons/ 폴더에서 엑셀에 적힌 파일명으로 이미지 로드
            Sprite loadedSprite = Resources.Load<Sprite>($"Emoticons/{data.emoticonName}");
            if (loadedSprite != null)
            {
                emoticonImage.sprite = loadedSprite;
            }
            else
            {
                Debug.LogWarning($"이모티콘을 찾을 수 없습니다: Resources/Emoticons/{data.emoticonName}");
                emoticonImage.gameObject.SetActive(false); // 로드 실패 시 이미지 컴포넌트 끄기
            }
        }
        // 3. 일반 텍스트 댓글 처리 (isEmoticon == false)
        else
        {
            emoticonImage.gameObject.SetActive(false); // 이모티콘 이미지 끄기
            contentText.gameObject.SetActive(true);   // 텍스트 컴포넌트 켜기
            contentText.text = data.content;          // 텍스트 내용 세팅
        }
    }
}