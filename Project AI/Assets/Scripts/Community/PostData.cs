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
    public string content;
    public string imageName;
    // 💡 [추가] 게시글 자체를 클릭하거나 읽었을 때 수집할 단서 ID (없으면 빈 값)
    public string clueID;
    public List<CommentData> comments = new List<CommentData>();
}