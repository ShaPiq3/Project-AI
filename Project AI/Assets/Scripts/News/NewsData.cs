[System.Serializable]
public class NewsData
{
    public int id;
    public string category;  // ★ 기존 코드의 카테고리 필터링을 위한 필드 추가
    public string title;
    public string info;
    public string body;
    public string imageName;
}