using UnityEngine;
using TMPro;

/// <summary>
/// EP_1_Q0(신경숙 - 대우대학교 프로메스 특별전형 입시 가산점) 퀘스트 전용 바인더.
/// Promes_Admission_Guide는 에디터 스크립트(PromesAdmissionGuidePrefabBuilder)로 생성돼서
/// 자식 텍스트 오브젝트 이름이 전부 "Text"/"Bullet"로 겹치기 때문에, 오브젝트 이름 대신
/// 문구(키워드) 포함 여부로 어떤 텍스트가 어떤 단서인지 매칭한다.
/// 씬의 Promes_Admission_Guide 인스턴스(또는 그 상위 오브젝트)에 이 컴포넌트만 붙이면 동작한다.
/// </summary>
public class PromesAdmissionGuideClueBinder : MonoBehaviour
{
    private const string QuestID = "EP_1_Q0";

    private static readonly (string keyword, string clueID)[] ClueMap = new (string, string)[]
    {
        ("무단 결석·지각·조퇴 0회", "EP_1_Q0_D_1"),
        ("수학 I 성취도 A등급", "EP_1_Q0_D_2"),
        ("AI 전략경영 심화 프로젝트 최고 가중치 결합", "EP_1_Q0_D_3"),
        ("최종 선발 종합 지수", "EP_1_Q0_D_4"),
        ("Edu-Promes 통합 학적 관리 솔루션을 적용받는 피교육자", "EP_1_Q0_D_5"),
    };

    void Start()
    {
        foreach (TextMeshProUGUI tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            // 💡 PromesAdmissionGuidePrefabBuilder가 만든 텍스트는 전부 raycastTarget=false라서
            // 이걸 켜주지 않으면 컴포넌트가 붙어있어도 호버/클릭이 아예 전달되지 않는다.
            tmp.raycastTarget = true;

            ClueTextHoverEffect hover = tmp.GetComponent<ClueTextHoverEffect>();
            if (hover == null) hover = tmp.gameObject.AddComponent<ClueTextHoverEffect>();

            foreach (var (keyword, clueID) in ClueMap)
            {
                if (tmp.text.Contains(keyword))
                {
                    hover.Configure(clueID, QuestID, "");
                    break;
                }
            }
        }
    }
}
