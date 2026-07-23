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
        if (DataLogManager.Instance == null) return;

        // 💡 [변경] 여러 퀘스트가 등록되어 있어도, DataLogManager의 다른 로직
        // (정답 판정, 대화 분기)과 동일하게 "가장 최근에 시작된 퀘스트"를 기준으로 표시합니다.
        var activeQuestIDs = DataLogManager.Instance.activeQuestIDs;

        if (activeQuestIDs == null || activeQuestIDs.Count == 0)
        {
            if (statusText != null)
            {
                statusText.text = "0 / 0";
            }

            if (generateButton != null)
            {
                generateButton.gameObject.SetActive(true);
                generateButton.interactable = false;
            }

            return;
        }

        string currentQuestID = activeQuestIDs[activeQuestIDs.Count - 1];

        int current = DataLogManager.Instance.questCollectedClues.ContainsKey(currentQuestID)
                      ? DataLogManager.Instance.questCollectedClues[currentQuestID].Count : 0;

        int target = DataLogManager.Instance.questTargetCounts.ContainsKey(currentQuestID)
                     ? DataLogManager.Instance.questTargetCounts[currentQuestID] : 0;

        // 텍스트 업데이트
        if (statusText != null)
        {
            statusText.text = $"{current} / {target}";
        }

        // 💡 [변경] 목표 개수를 다 채웠을 때만 버튼이 눌리도록 잠금.
        // 버튼 자체는 계속 보이게 유지하고(SetActive(true)), interactable만 잠갔다 풉니다.
        if (generateButton != null)
        {
            generateButton.gameObject.SetActive(true);
            generateButton.interactable = (target > 0 && current >= target);
        }
    }
}