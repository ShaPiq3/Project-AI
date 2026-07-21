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

    // 💡 [추가] 이 게시물의 이미지가 이미지 생성 슬롯 시스템에서 수집 가능한 단서일 경우의 ID.
    // 비어있으면 수집 대상이 아님. ImageGenSlotItems.csv 의 ImageID 와 매칭됨.
    public string collectibleImageID;
}
