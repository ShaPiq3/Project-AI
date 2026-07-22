// PromesFeedbackTablePrefabBuilder.cs
// -----------------------------------------------------------------------------
// 사용법
// 1) 이 파일을 프로젝트의 "Assets/Editor" 폴더 안에 넣습니다. (없으면 새로 생성)
// 2) 기존에 Content 밑에 들어있던 Promes_Feedback 인스턴스는 삭제(또는 비활성화)
//    해주세요. 이 스크립트는 그것을 대체하는 표 형태의 새 프리팹을 만듭니다.
// 3) Hierarchy 창에서 DocuGame_panel_1 > Scroll View > Viewport > Content
//    오브젝트를 선택합니다. (선택 안 해도 되지만, 선택하면 바로 그 밑에
//    생성물이 자동으로 들어갑니다)
// 4) 상단 메뉴 Tools > Promes > Create Feedback Table Prefab 클릭
// 5) Assets/Prefabs/UI/Promes_Feedback_Table.prefab 로 프리팹이 저장되고,
//    선택했던 Content 밑에 인스턴스가 바로 생성됩니다.
//
// 주의: Content 오브젝트에 Vertical Layout Group + Content Size Fitter
// (Vertical Fit = Preferred Size) 가 없다면 스크롤이 제대로 동작하지
// 않을 수 있습니다. 없다면 추가해 주세요.
// -----------------------------------------------------------------------------

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class PromesFeedbackTablePrefabBuilder
{
    private const float TABLE_WIDTH = 500f; // Scroll View content 너비에 맞춤

    // 루트 / 표 자체의 좌우 패딩과 행 안의 셀 간격 (아래 CreatePrefab의 값과 반드시 일치해야 함)
    private const float ROOT_PADDING_H = 8f;  // 좌우 각각
    private const float TABLE_PADDING_H = 1f; // 좌우 각각
    private const float ROW_SPACING = 1f;     // 셀 사이 간격 (4칸 -> 3군데)

    // 좁은 3개 열의 고정 너비
    private const float COL_AGE_WIDTH = 56f;
    private const float COL_MODEL_WIDTH = 68f;
    private const float COL_SCORE_WIDTH = 60f;

    // "서비스 이용 경험" 열 너비 = 실제로 사용 가능한 남은 폭을 정확히 계산해서 고정.
    // ※ 이전 버전처럼 이 값을 비워두고(content-driven) flexible에만 맡기면, 문장이 긴 행일수록
    //    이 칸이 더 큰 "필요 폭"을 주장하게 되어 Unity가 부족한 공간을 메우려고 다른 고정폭
    //    칸들(연령대/사용모델/만족도)까지 행마다 다르게 줄여버려 셀 경계가 어긋나는 문제가 있었음.
    //    그래서 4번째 칸도 다른 칸들처럼 "고정된 숫자"로 명시해서 모든 행이 완전히 동일한
    //    칸 너비를 갖도록 함.
    private static readonly float COL_EXPERIENCE_WIDTH =
        (TABLE_WIDTH - 2 * ROOT_PADDING_H - 2 * TABLE_PADDING_H - 3 * ROW_SPACING)
        - COL_AGE_WIDTH - COL_MODEL_WIDTH - COL_SCORE_WIDTH;

    private static readonly Color BorderColor = new Color(0.56f, 0.85f, 0.90f, 1f);
    private static readonly Color HeaderBgColor = new Color(0.80f, 0.96f, 0.98f, 1f);
    private static readonly Color CellBgColor = Color.white;
    private static readonly Color TextColor = new Color(0.13f, 0.13f, 0.13f, 1f);

    private struct Row
    {
        public string age, model, score, experience;
        public Row(string age, string model, string score, string experience)
        {
            this.age = age; this.model = model; this.score = score; this.experience = experience;
        }
    }

    // 사진 2번 내용
    private static readonly Row[] Rows = new Row[]
    {
        new Row("20대", "PGT", "5", "졸업 논문의 목차와 초안을 함께 정리했습니다. 앞에서 설명한 연구 목적을 계속 기억하면서 문단을 수정해 줘서 실제 조교와 대화하는 느낌이었습니다."),
        new Row("10대", "PGT", "4", "수학 문제에서 이해되지 않는 부분을 계속 질문해도 다른 예시를 들어가며 설명해 줍니다. 틀렸다고 지적하기보다 제가 어느 단계에서 막혔는지 먼저 찾아주는 점이 좋았습니다."),
        new Row("40대", "PGT", "5", "해외 고객을 위한 상품 안내문과 자주 묻는 질문을 여러 언어로 작성했습니다. 번역투가 거의 없고 고객층에 맞게 말투도 바꿀 수 있어 실무에 바로 활용했습니다."),
        new Row("10대", "PGT", "1", "이용자가 몰리는 시간에는 답변 속도가 느려지거나 대화 횟수 제한이 걸렸습니다. 과제 마감 직전에 사용할 수 없었던 경험 때문에 신뢰도가 떨어졌습니다."),
        new Row("30대", "PGT", "2", "업데이트 이후 같은 프롬프트에도 답변 말투와 분량이 달라졌습니다. 업무용 양식을 일정하게 유지해야 하는데 결과가 예고 없이 바뀌어 불편했습니다."),
        new Row("20대", "Plaude", "4", "원하는 웹서비스를 자연어로 설명하자 데이터베이스 구조부터 화면과 서버 코드까지 만들어 줬습니다. 코딩 경험이 많지 않아도 실제로 작동하는 결과물을 완성할 수 있었습니다."),
        new Row("30대", "Plaude", "1", "보안 테스트용 코드를 요청했는데 악용 가능성이 있다는 이유로 일부 기능 생성을 거절했습니다. 회사 내부 시스템이라는 상황을 여러 번 설명해도 제한이 풀리지 않았습니다."),
        new Row("20대", "Plaude", "5", "동아리 게임을 만들면서 코드가 왜 작동하는지 질문했습니다. 완성된 답만 주지 않고 변수와 함수의 원리를 제 수준에 맞춰 설명해 줘서 공부에도 도움이 됐습니다."),
        new Row("40대", "Plaude", "2", "아주 간단한 프로그램을 요청했는데 확장성과 유지보수까지 고려한 복잡한 구조를 만들었습니다. 결과는 잘 작동했지만 작은 프로젝트에는 지나치게 무거웠습니다."),
    };

    [MenuItem("Tools/Promes/Create Feedback Table Prefab")]
    public static void CreatePrefab()
    {
        // ------------------------------------------------------------------
        // 루트 오브젝트
        // ------------------------------------------------------------------
        GameObject root = new GameObject("Promes_Feedback_Table", typeof(RectTransform));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(0.5f, 1f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup rootVlg = root.AddComponent<VerticalLayoutGroup>();
        rootVlg.padding = new RectOffset((int)ROOT_PADDING_H, (int)ROOT_PADDING_H, 10, 10);
        rootVlg.spacing = 0f;
        rootVlg.childControlWidth = true;
        rootVlg.childControlHeight = true;
        rootVlg.childForceExpandWidth = true;
        rootVlg.childForceExpandHeight = false;

        ContentSizeFitter rootFitter = root.AddComponent<ContentSizeFitter>();
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ------------------------------------------------------------------
        // 표
        // ------------------------------------------------------------------
        GameObject table = CreateBackgroundObject("Table", root.transform, BorderColor);
        LayoutElement tableLe = table.AddComponent<LayoutElement>();
        tableLe.flexibleWidth = 1f;

        VerticalLayoutGroup tableVlg = table.AddComponent<VerticalLayoutGroup>();
        tableVlg.padding = new RectOffset((int)TABLE_PADDING_H, (int)TABLE_PADDING_H, (int)TABLE_PADDING_H, (int)TABLE_PADDING_H);
        tableVlg.spacing = 1f;
        tableVlg.childControlWidth = true;
        tableVlg.childControlHeight = true;
        tableVlg.childForceExpandWidth = true;
        tableVlg.childForceExpandHeight = false;

        // 헤더 행
        CreateRow(table.transform, "연령대", "사용 모델", "만족도 (1~5)", "서비스 이용 경험",
            HeaderBgColor, FontStyles.Bold, 13);

        // 데이터 행들
        foreach (Row r in Rows)
        {
            CreateRow(table.transform, r.age, r.model, r.score, r.experience,
                CellBgColor, FontStyles.Normal, 12);
        }

        // ------------------------------------------------------------------
        // 프리팹으로 저장
        // ------------------------------------------------------------------
        string dir = "Assets/Prefabs/UI";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = dir + "/Promes_Feedback_Table.prefab";
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);

        GameObject target = Selection.activeGameObject;
        if (target != null)
        {
            instance.transform.SetParent(target.transform, false);
            Debug.Log($"[PromesFeedbackTablePrefabBuilder] '{target.name}' 하위에 Promes_Feedback_Table 인스턴스를 생성했습니다. 프리팹: {path}");
        }
        else
        {
            Debug.Log($"[PromesFeedbackTablePrefabBuilder] 오브젝트를 선택하지 않아 씬 루트에 생성했습니다. Content 오브젝트 밑으로 옮겨주세요. 프리팹: {path}");
        }

        Selection.activeGameObject = instance;
        EditorGUIUtility.PingObject(prefabAsset);
    }

    // =====================================================================
    // Helper 함수들
    // =====================================================================

    private static GameObject CreateBackgroundObject(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    // 한 행(4칸) 생성 - 각 셀은 내용 길이에 따라 높이가 자동으로 늘어남
    private static void CreateRow(Transform parent, string age, string model, string score, string experience,
        Color bgColor, FontStyles style, int fontSize)
    {
        GameObject row = CreateBackgroundObject("Row", parent, BorderColor);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.spacing = ROW_SPACING;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        CreateCell(row.transform, age, COL_AGE_WIDTH, bgColor, style, fontSize, TextAlignmentOptions.Center);
        CreateCell(row.transform, model, COL_MODEL_WIDTH, bgColor, style, fontSize, TextAlignmentOptions.Center);
        CreateCell(row.transform, score, COL_SCORE_WIDTH, bgColor, style, fontSize, TextAlignmentOptions.Center);
        // 4번째 칸도 다른 칸들과 동일하게 "고정 너비"로 명시 (더 이상 content-driven 아님)
        CreateCell(row.transform, experience, COL_EXPERIENCE_WIDTH, bgColor, style, fontSize, TextAlignmentOptions.TopLeft);
    }

    // 셀 하나 생성. 모든 칸이 명시적인 고정 너비(width)를 가짐 -> 모든 행에서 칸 경계가 완전히 동일.
    // 셀 자체가 VerticalLayoutGroup을 가지고 있어서, 내부 TMP 텍스트의 줄바꿈 "높이"만
    // 셀 -> 행 -> 표 -> 루트까지 자동으로 전달되어 행 높이는 내용에 맞게 늘어나되,
    // "너비"는 LayoutElement가 우선순위(priority=1, 기본값)로 강제 고정함.
    private static void CreateCell(Transform parent, string text, float width, Color bgColor, FontStyles style, int fontSize, TextAlignmentOptions align)
    {
        GameObject cell = CreateBackgroundObject("Cell", parent, bgColor);

        LayoutElement cellLe = cell.AddComponent<LayoutElement>();
        cellLe.minWidth = width;
        cellLe.preferredWidth = width;
        cellLe.flexibleWidth = 0f;

        VerticalLayoutGroup cellVlg = cell.AddComponent<VerticalLayoutGroup>();
        cellVlg.padding = new RectOffset(6, 6, 5, 5);
        cellVlg.spacing = 0f;
        cellVlg.childAlignment = align == TextAlignmentOptions.Center ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft;
        cellVlg.childControlWidth = true;
        cellVlg.childControlHeight = true;
        cellVlg.childForceExpandWidth = true;
        cellVlg.childForceExpandHeight = false;

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(cell.transform, false);
        LayoutElement textLe = textGo.AddComponent<LayoutElement>();
        textLe.flexibleWidth = 1f;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = TextColor;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
    }
}
