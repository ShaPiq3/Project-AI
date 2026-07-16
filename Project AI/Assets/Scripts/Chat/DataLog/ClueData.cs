[System.Serializable]
public class ClueData
{
    public string clueID;       // 단서 고유 ID
    public string sourceType;   // 💡 출처 분류 (예: "NEWS", "SNS", "COMMUNITY")
    public string sourceTitle;  // 출처 제목 (예: "디시인사이드 - 개념글", "트위터 @user")
    public string contentText;  // 수집된 텍스트 내용
    public string imageName;    // 수집된 이미지 이름
}
