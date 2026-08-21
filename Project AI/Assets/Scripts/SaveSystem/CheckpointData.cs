/// <summary>
/// "퀘스트가 시작된 시점" 하나를 나타내는 체크포인트. contactID의 대화방을 dialogueID부터
/// 다시 재생하면 그 지점으로 복귀한 것과 같다 (ChatCoordinator.JumpToDialogueSafe 재사용).
/// </summary>
[System.Serializable]
public class CheckpointData
{
    public string sceneName;
    public string contactID;
    public int dialogueID;
}
