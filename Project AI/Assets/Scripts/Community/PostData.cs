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

    // 이 주머니가 작동하려면 CommentData가 반드시 필요합니다!
    public System.Collections.Generic.List<CommentData> comments = new System.Collections.Generic.List<CommentData>();
}