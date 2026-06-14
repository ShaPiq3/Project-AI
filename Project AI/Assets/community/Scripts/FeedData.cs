using System.Collections.Generic;

[System.Serializable]
public class CommentData
{
    public string writer;
    public string text;
    public string emoticonSpriteName;
}

[System.Serializable]
public class FeedData
{
    public int id;
    public string title;
    public string writer;
    public string date;
    public string mainText;
    public string mainImageName;
    public string requiredFlag; // 게시글 해금 조건용 플래그 명

    // FeedItem에서 사용하는 댓글 리스트 데이터
    public List<CommentData> comments = new List<CommentData>();
}