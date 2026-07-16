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
}
