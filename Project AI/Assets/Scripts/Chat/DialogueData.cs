[System.Serializable]
public class DialogueData
{
    public int id;
    public string speakerType;
    public string speakerName;
    public string dialogueText;
    public bool hasImage;
    public string imagePath;
    public float delayTime;
    public string ipAddress;
    public string questID;
    public int targetCount;

    // --- [필수 추가] 선택지 분기 연출을 위한 변수들 ---
    public bool isBranch;
    public string branchText1;
    public int nextId1;
    public string branchText2;
    public int nextId2;
    public string branchText3;
    public int nextId3;
    public bool isTrigger;
}