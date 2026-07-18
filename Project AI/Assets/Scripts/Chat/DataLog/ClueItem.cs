using UnityEngine;

public class ClueItem : MonoBehaviour
{
    [SerializeField] private string questID;       // 💡 인스펙터에서 입력할 퀘스트 ID
    [SerializeField] private string clueID;

    public void OnClick()
    {
        if (DataLogManager.Instance != null)
        {
            DataLogManager.Instance.AcquireClue(this.questID, this.clueID);
        }
    }
}