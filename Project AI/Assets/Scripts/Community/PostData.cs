using System.Collections.Generic;

[System.Serializable]
public class PostData
{
    public int postID;
    public string title;
    public string author;
    public string date;
    public int likes;
    public int dislikes;
    public string content; // 💡 본문 글 (여기서 [CLUE:ID] 파싱을 하거나 아래 clueID를 사용)
    public string imageName;
    // 💡 게시글 본문을 통해 수집하거나, 글 자체를 읽었을 때 수집할 단서 ID
    public string clueID;
    // 💡 게시글 안의 '이미지'를 클릭했을 때 따로 수집할 단서 ID
    public string imageClueID;
    public string imageQuestID;
    // 💡 이 게시글의 이미지가 이미지 생성 슬롯 시스템에서 수집 가능한 단서일 경우의 ID.
    // 비어있으면 수집 대상이 아님. ImageGenSlotItems.csv 의 ImageID 와 매칭됨.
    // (기존 imageQuestID 와는 다른 값이므로 절대 혼용하지 마세요)
    public string collectibleImageID;

    // 💡 [추가] 제목(title) 자체를 클릭해서 수집하는 단서 ID. 비어있으면 제목은 수집 대상 아님.
    public string titleClueID;

    public List<CommentData> comments = new List<CommentData>();
}