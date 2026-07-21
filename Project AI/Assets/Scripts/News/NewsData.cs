[System.Serializable]
public class NewsData
{
    public int id;
    public string category;  // ★ 기존 코드의 카테고리 필터링을 위한 필드 추가
    public string title;
    public string info;
    public string body;
    public string imageName;
    public string imageClueID;
    public int clueParagraphIndex;
    public string bodyClueID;

    // 💡 [추가] 이 기사의 이미지가 이미지 생성 슬롯 시스템에서 수집 가능한 단서일 경우의 ID.
    // 비어있으면 수집 대상이 아님. ImageGenSlotItems.csv 의 ImageID 와 매칭됨.
    public string collectibleImageID;
}