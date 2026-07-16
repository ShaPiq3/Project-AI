[System.Serializable]
public class CommentData
{
    public int postID;
    public string author;
    public string content;
    public bool isEmoticon;
    public string emoticonName;
    // 💡 [추가] 특정 댓글을 클릭했을 때 수집할 단서 ID (없으면 빈 값)
    public string clueID;
}