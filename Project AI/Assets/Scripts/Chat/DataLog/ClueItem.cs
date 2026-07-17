using UnityEngine;

public class ClueItem : MonoBehaviour
{
    public string clueID; // 여기에 엑셀의 'ClueID'를 적으세요. 예: "CLUE_01"

    public void OnClick()
    {
        // 클릭하면 매니저에게 ID를 보내서 수집 시작!
        DataLogManager.Instance.AcquireClue(clueID);
    }
}