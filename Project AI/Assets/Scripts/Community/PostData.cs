[System.Serializable]
public class PostData
{
    public int id;          // 게시글 고유 번호 (엑셀 행 번호 기반)
    public string title;
    public string author;
    public string date;
    public int likes;
    public int dislikes;    // 비추천 추가
    public string content;   // 내용 추가
}