[System.Serializable]
public class ClueData
{
    public string clueID;       // 단서 고유 ID
    public string sourceType;   // 💡 출처 분류 (예: "NEWS", "SNS", "COMMUNITY")
    public string sourceTitle;  // 출처 제목 (예: "디시인사이드 - 개념글", "트위터 @user")
    public string contentText;  // 수집된 텍스트 내용
    public string imageName;    // 수집된 이미지 이름
    public string questID;
    public bool isCorrect;
    // 💡 [추가] 이 단서(주로 오답 함정)를 모아서 실패했을 때 점프할 대사 ID. 0이면 퀘스트의
    // 기본 incorrectDialogueID를 그대로 씀 (기존 퀘스트는 이 컬럼이 없어도 그대로 동작).
    public int failDialogueID;
    // 💡 [추가] contentText가 비어있는 이미지 전용 단서를 {{CLUE_REPORT}} 보고서에 넣을 때
    // 텍스트 대신 쓸 설명 문구 (예: "첨부된 로고 이미지"). 비어있으면 기본 문구로 대체됨.
    public string imageDescription;
    public string clueName => sourceTitle;
}

