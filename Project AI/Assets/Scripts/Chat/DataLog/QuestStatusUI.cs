using UnityEngine;
using TMPro;
using UnityEngine.UI; // 버튼을 제어하기 위해 추가

public class QuestStatusUI : MonoBehaviour
{
    // 컨테이너/프리팹 대신 고정된 UI를 직접 연결
    public TMP_Text statusText;      // 0/5 등이 표시될 텍스트
    public Button generateButton;    // 답변 생성 버튼

    public void UpdateDisplay()
    {
        // 1. 진행 중인 퀘스트 데이터를 가져옴 (보통 퀘스트는 하나씩 진행되므로 첫 번째 것을 사용)
        foreach (var questID in DataLogManager.Instance.questTargetCounts.Keys)
        {
            int current = DataLogManager.Instance.questCollectedClues.ContainsKey(questID)
                          ? DataLogManager.Instance.questCollectedClues[questID].Count : 0;
            int target = DataLogManager.Instance.questTargetCounts[questID];

            // 2. 텍스트 업데이트
            if (statusText != null)
            {
                statusText.text = $"{current} / {target}";
            }

            // 3. 목표치 달성 시 답변 생성 버튼 활성화 (이미 5개면 true, 아니면 false)
            if (generateButton != null)
            {
                generateButton.gameObject.SetActive(current >= target);
            }

            // 퀘스트가 하나라면 바로 루프를 빠져나와도 됩니다.
            break;
        }
    }
}