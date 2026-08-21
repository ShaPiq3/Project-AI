[System.Serializable]
public class HumanData
{
    public string id;
    public string name;
    public string birth;
    public string gender;
    public string bloodType;
    public string occupation;
    public string contact;
    public string email;
    public string address;
    public string note;
    public string imageName;

    // 💡 단서 수집 시스템 연동용. 비어있으면 해당 필드는 단서로 취급되지 않음.
    public string addressClueID;
    public string occupationClueID;
    // note 필드와 마찬가지로 '|'로 구분된 항목별 단서 ID (항목 개수와 순서가 note와 1:1로 대응되어야 함)
    public string noteClueIDs;
}
