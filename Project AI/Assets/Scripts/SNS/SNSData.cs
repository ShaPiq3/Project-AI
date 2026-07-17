using System.Collections.Generic;

[System.Serializable]
public class SNSCommentData
{
    public int postID;
    public string author;
    public string content;
    public bool isEmoticon;
    public string emoticonName;
}

[System.Serializable]
public class SNSPostData
{
    public int postID;
    public string author;
    public string profileImageName;
    public string content;
    public string postImageName;
    public List<SNSCommentData> comments = new List<SNSCommentData>();
    public string clueID;         // 본문 클릭 시 수집할 단서 ID
    public string imageClueID;    // 이미지 클릭 시 수집할 단서 ID
}
