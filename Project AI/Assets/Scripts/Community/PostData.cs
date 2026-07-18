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


    // 💡 [기존] 게시글 본문을 통해 수집하거나, 글 자체를 읽었을 때 수집할 단서 ID
    public string clueID;

    // 💡 [추가] 게시글 안의 '이미지'를 클릭했을 때 따로 수집할 단서 ID
    public string imageClueID;
    public string imageQuestID;

    public List<CommentData> comments = new List<CommentData>();
}