// HanSeoARecordSentenceBuilder.cs
// -----------------------------------------------------------------------------
// 한서아(EP_1_Q3, 문서요약) 퀘스트용 문장 버튼 5개를 자동으로 채워주는 도구입니다.
// 로딩바/토글 애니메이션 등 DocumentQuestGroup의 복잡한 구조는 전혀 건드리지 않고,
// "문장 버튼 하나"를 템플릿으로 복제해서 스타일을 그대로 유지합니다.
//
// 사용법
// 1) MainScene_2에서 기존 문서 패널(documentTitle: "Promes AI 사용자 피드백", documentID: T_Q3)을
//    복제해서 한서아용으로 하나 만드세요 (하이어라키에서 Ctrl+D).
// 2) 그 복제본 안의 Promes_Analysis_Sentences(문장 컨테이너) 밑에 있는 기존 문장 버튼 중
//    아무거나 하나를 하이어라키에서 선택합니다. (SentenceBlock/Button이 붙어있는 오브젝트)
// 3) 상단 메뉴 Tools > Promes > Build Han SeoA Sentence Blocks 클릭
// 4) 선택했던 컨테이너 밑의 기존 문장 버튼들은 전부 지워지고, 한서아 퀘스트용 문장 5개로
//    새로 채워집니다(선택했던 오브젝트 자신도 재사용되어 1번째 문장이 됨).
// 5) 완료 후 DocumentQuestManager 컴포넌트에서:
//    - Correct Sentence Indices = [3] (4번째 "표준 가중치(60%)를 적용받음"만 정답)
//    - Document ID = HAN_RECORD
//    - Success Dialogue ID / Failure Dialogue ID / Second Failure Dialogue ID = 100 / 200 / 300
//    - Contact ID = C2_NPC_HAN
//    를 채워주세요.
// -----------------------------------------------------------------------------

using UnityEditor;
using UnityEngine;
using TMPro;

public static class HanSeoARecordSentenceBuilder
{
    private static readonly string[] Sentences = new string[]
    {
        "학습 몰입도 센서 측정 결과, 연속 학습 유지 지수 전국 상위 0.01% 기록.",
        "대우대학교 경영학부 AI 전략경영 특별전형을 목표로 체계적인 학업 로드맵을 구축하고 자기관리 역량을 입증함.",
        "Edu-Promes AI 맞춤 학습 모듈 이수율 100%를 달성함.",
        "대우대 연계 AI 전략경영 기초 모듈을 이수하고 표준 가중치(60%)를 적용받음.",
        "쉬는 시간이나 자율 활동 시 틈틈이 풍경 소묘와 캐릭터 스케치를 즐겨함.",
    };

    [MenuItem("Tools/Promes/Build Han SeoA Sentence Blocks")]
    public static void Build()
    {
        GameObject template = Selection.activeGameObject;
        if (template == null || template.GetComponent<UnityEngine.UI.Button>() == null)
        {
            EditorUtility.DisplayDialog("한서아 문장 만들기",
                "먼저 하이어라키에서 기존 문장 버튼(SentenceBlock, Button이 붙은 오브젝트) 하나를 선택하세요.",
                "확인");
            return;
        }

        Transform container = template.transform.parent;
        if (container == null)
        {
            EditorUtility.DisplayDialog("한서아 문장 만들기", "선택한 오브젝트에 부모(문장 컨테이너)가 없습니다.", "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog("한서아 문장 만들기",
            $"'{container.name}' 밑의 기존 문장 버튼들을(선택한 것 제외) 전부 지우고, 한서아 퀘스트용 문장 {Sentences.Length}개로 새로 채웁니다. 계속할까요?",
            "계속", "취소"))
        {
            return;
        }

        // 컨테이너 밑에 있던 기존 문장 버튼들을 전부 지운다(선택했던 템플릿만 예외로 남겨서 복제에 씀)
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            if (child == template.transform) continue;
            Undo.DestroyObjectImmediate(child.gameObject);
        }

        int missingBodyCount = 0;

        for (int i = 0; i < Sentences.Length; i++)
        {
            GameObject instance = (i == 0) ? template : (GameObject)Object.Instantiate(template, container);
            instance.name = $"SentenceBlock_{i}";

            Transform bodyTransform = instance.transform.Find("Body");
            TextMeshProUGUI bodyText = bodyTransform != null ? bodyTransform.GetComponent<TextMeshProUGUI>() : null;
            if (bodyText != null)
            {
                bodyText.text = Sentences[i];
            }
            else
            {
                missingBodyCount++;
                Debug.LogWarning($"[HanSeoARecordSentenceBuilder] '{instance.name}'에서 'Body' 텍스트를 찾지 못했습니다.");
            }

            if (i > 0) Undo.RegisterCreatedObjectUndo(instance, "Build Han SeoA Sentence Block");
        }

        EditorUtility.SetDirty(container.gameObject);

        string warning = missingBodyCount > 0
            ? $"\n\n⚠ {missingBodyCount}개 항목에서 'Body' 텍스트를 못 찾았습니다. 콘솔 경고를 확인하세요."
            : "";

        EditorUtility.DisplayDialog("한서아 문장 만들기 완료",
            $"문장 {Sentences.Length}개를 생성했습니다 (4번째 = 정답, index=3).{warning}\n\n" +
            "이제 DocumentQuestManager 컴포넌트에서:\n" +
            "- Correct Sentence Indices = [3]\n" +
            "- Document ID = HAN_RECORD\n" +
            "- Success/Failure/Second Failure Dialogue ID = 100 / 200 / 300\n" +
            "- Contact ID = C2_NPC_HAN\n" +
            "를 채워주세요.",
            "확인");
    }
}
