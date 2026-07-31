// PromesYellowGuidePrefabBuilder.cs
// -----------------------------------------------------------------------------
// 사용법
// 1) 이 파일을 프로젝트의 "Assets/Editor" 폴더 안에 넣습니다. (없으면 새로 생성)
// 2) 해당 패널의 Scroll View > Viewport > Content 오브젝트를 선택합니다.
//    (선택 안 해도 되지만, 선택하면 바로 그 밑에 생성물이 자동으로 들어갑니다)
// 3) 상단 메뉴 Tools > Promes > Create Yellow Guide Prefab 클릭
// 4) Assets/Prefabs/UI/Promes_Yellow_Guide.prefab 로 프리팹이 저장되고,
//    선택했던 Content 밑에 인스턴스가 바로 생성됩니다.
//
// * Brown/Green 프리팹과 구조는 동일하되, 색상 스와치 섹션은 데이터가
//   없어서 생략했습니다. 나중에 이름+헥스코드를 알려주시면 추가할 수 있습니다.
// * 저장 전에 LiberationSans SDF의 한글 미지원으로 자동 생성되는
//   TMP SubMeshUI 서브메시를 미리 제거해서 프리팹을 깨끗하게 저장합니다.
//   (단, 실제로 렌더링되는 순간 다시 생기므로 한글 지원 폰트를 Font Asset에
//   지정해주셔야 완전히 사라집니다.)
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

public static class PromesYellowGuidePrefabBuilder
{
    private const float CONTENT_WIDTH = 698f; // Scroll View content 너비에 맞춤
    private const float ROOT_PADDING_H = 24f;

    // 표(연관 시각적 개념) 계산용 상수
    private const float TABLE_PADDING_H = 1f;
    private const float ROW_SPACING = 1f;
    private const float COL_CATEGORY_WIDTH = 110f;
    private const float COL_WEIGHT_WIDTH = 140f;

    private static readonly float COL_CONCEPT_WIDTH =
        (CONTENT_WIDTH - 2 * ROOT_PADDING_H - 2 * TABLE_PADDING_H - 2 * ROW_SPACING)
        - COL_CATEGORY_WIDTH - COL_WEIGHT_WIDTH;

    private static readonly Color TitleColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    private static readonly Color SectionHeadColor = new Color(0.90f, 0.60f, 0.05f, 1f); // 텍스트 임베딩 (주황/노랑)
    private static readonly Color RuleHeadColor = new Color(0.90f, 0.60f, 0.05f, 1f);    // 프롬프트 결합 규칙 (주황/노랑)
    private static readonly Color BodyColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private static readonly Color RuleTitleColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    private static readonly Color ArrowLineColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    private static readonly Color TableBorderColor = new Color(0.95f, 0.85f, 0.55f, 1f);
    private static readonly Color TableHeaderBgColor = new Color(0.96f, 0.69f, 0.13f, 1f); // 진한 노랑/주황
    private static readonly Color TableHeaderTextColor = new Color(0.1f, 0.1f, 0.1f, 1f);  // 헤더 글자는 검정
    private static readonly Color TableCellBgColor = Color.white;

    // ------------------------------------------------------------------
    // 데이터
    // ------------------------------------------------------------------
    private struct TableRow { public string category, weight, concept; public TableRow(string c, string w, string co) { category = c; weight = w; concept = co; } }
    private static readonly TableRow[] TableRows = new TableRow[]
    {
        new TableRow("소재/질감", "0.94", "금속(금/황동), 유리, 햇살, 꽃잎, 모래, 경고 표지판, 네온사인, 과일(레몬/바나나)"),
        new TableRow("조명/감정", "0.91", "강렬한 광원, 활력, 경고/위험, 희망, 따스함, 눈부심, 높은 명도"),
        new TableRow("문체/시대", "0.87", "사이버펑크, 인프라/건설, 팝아트, 여름, SF 가공 기술, 인포그래픽"),
        new TableRow("분위기", "0.83", "선명함, 긴장감, 에너지, 주의집중, 유쾌함, 방사능/오염 영역"),
    };

    private struct Rule { public string title; public string[] lines; public Rule(string t, string[] l) { title = t; lines = l; } }
    private static readonly Rule[] Rules = new Rule[]
    {
        new Rule("프롬프트에 (\"노란색\" + \"조명\" 또는 \"광원\" 또는 \"네온\")이 포함된 경우:", new string[]
        {
            "→ 주요 잠재 목표: 발광(Bloom) 효과 / 네온 글공 / 시각적 하이라이트 림 라이트",
            "→ 색상 스펙트럼 범위: #FFEE58 ~ #FFF176",
            "→ 표면 특성: 높은 명도와 발광 강도, 주위 오브젝트로의 빛 산란 오프셋 적용",
        }),
        new Rule("프롬프트에 (\"노란색\" + \"경고\" 또는 \"사이버펑크\" 또는 \"산업/건설\")이 포함된 경우:", new string[]
        {
            "→ 주요 잠재 목표: 높은 채도의 주의/위험 표지판 / 사선 스트라이프 패턴 / 산업용 장비",
            "→ 색상 스펙트럼 범위: #FBC02D ~ #F57F17",
            "→ 대기 환경 효과: 강한 명암 대비 및 시선 집중용 텍스처 하이라이트",
        }),
        new Rule("프롬프트에 (\"노란색\" + \"의상\" 또는 \"옷\")이 포함된 경우:", new string[]
        {
            "→ 주요 잠재 목표: 스포티한 방수 소재(우비) / 캐주얼 면 소재 / 비비드한 포인트 의상",
            "→ 색상 스펙트럼 범위: #FDD835 ~ #FFEB3B",
            "→ 표면 특성: 주름 부분의 선명한 반사광, 주변 스킨 톤과의 높은 명암 대비",
        }),
        new Rule("프롬프트에 (\"노란색\" + \"헤어\" 또는 \"머리모양\")이 포함된 경우:", new string[]
        {
            "→ 주요 잠재 목표: 금발(Blonde) 머리카락 결의 방향성 / 밝은 반사광 엔젤링",
            "→ 색상 스펙트럼 범위: #FFF59D (밝은 백금발) ~ #FBC02D (골든 블론드)",
            "→ 표면 광택도: 정수리/결 부분에 높은 명도의 샴페인 옐로우(#FFF9C4) 림 라이트 자동 배치",
        }),
        new Rule("프롬프트에 (\"노란색\" + \"자연물\" 또는 \"꽃\" 또는 \"모래\" 또는 \"해\")가 포함된 경우:", new string[]
        {
            "→ 주요 잠재 목표: 해변의 미세 모래 입자 / 개나리·해바라기 꽃잎 텍스처 / 석양의 황금빛",
            "→ 색상 스펙트럼 범위: #FDFD96 ~ #F4D03F",
            "→ 표면 특성: 부드러운 투과광(Subsurface Scattering), 따뜻한 그라데이션 적용",
        }),
    };

    [MenuItem("Tools/Promes/Create Yellow Guide Prefab")]
    public static void CreatePrefab()
    {
        // ------------------------------------------------------------------
        // 루트 오브젝트
        // ------------------------------------------------------------------
        GameObject root = new GameObject("Promes_Yellow_Guide", typeof(RectTransform));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(0.5f, 1f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup rootVlg = root.AddComponent<VerticalLayoutGroup>();
        rootVlg.padding = new RectOffset((int)ROOT_PADDING_H, (int)ROOT_PADDING_H, 20, 24);
        rootVlg.spacing = 22f;
        rootVlg.childControlWidth = true;
        rootVlg.childControlHeight = true;
        rootVlg.childForceExpandWidth = true;
        rootVlg.childForceExpandHeight = false;

        ContentSizeFitter rootFitter = root.AddComponent<ContentSizeFitter>();
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ------------------------------------------------------------------
        // 제목 "Yellow (노란색)"
        // ------------------------------------------------------------------
        CreateText(root.transform, "Title", "Yellow (노란색)", 26, FontStyles.Bold, TitleColor, TextAlignmentOptions.TopLeft);

        // ※ 색상 스와치 섹션은 이름/헥스코드 데이터가 없어서 생략했습니다.

        // ------------------------------------------------------------------
        // "텍스트 임베딩" 섹션 제목 + 표
        // ------------------------------------------------------------------
        CreateText(root.transform, "Section_TextEmbedding", "텍스트 임베딩", 19, FontStyles.Bold, SectionHeadColor, TextAlignmentOptions.TopLeft);

        GameObject table = CreateBackgroundObject("Table", root.transform, TableBorderColor);
        LayoutElement tableLe = table.AddComponent<LayoutElement>();
        tableLe.flexibleWidth = 1f;

        VerticalLayoutGroup tableVlg = table.AddComponent<VerticalLayoutGroup>();
        tableVlg.padding = new RectOffset((int)TABLE_PADDING_H, (int)TABLE_PADDING_H, (int)TABLE_PADDING_H, (int)TABLE_PADDING_H);
        tableVlg.spacing = ROW_SPACING;
        tableVlg.childControlWidth = true;
        tableVlg.childControlHeight = true;
        tableVlg.childForceExpandWidth = true;
        tableVlg.childForceExpandHeight = false;

        CreateTableRow(table.transform, "구분", "연관 가중치", "연관 시각적 개념", TableHeaderBgColor, TableHeaderTextColor, FontStyles.Bold, 13);
        foreach (TableRow r in TableRows)
            CreateTableRow(table.transform, r.category, r.weight, r.concept, TableCellBgColor, BodyColor, FontStyles.Normal, 13);

        // ------------------------------------------------------------------
        // "프롬프트 결합 규칙" 섹션 제목 + 규칙 블록들
        // ------------------------------------------------------------------
        CreateText(root.transform, "Section_CombineRules", "프롬프트 결합 규칙", 19, FontStyles.Bold, RuleHeadColor, TextAlignmentOptions.TopLeft);

        foreach (Rule r in Rules)
            CreateRuleBlock(root.transform, r);

        // ------------------------------------------------------------------
        // 저장 전 정리: LiberationSans SDF(기본 폰트)에 한글 글리프가 없어서
        // TMP가 자동으로 만들어 붙인 "TMP SubMeshUI [...]" fallback 서브메시들을
        // 프리팹에 같이 저장되지 않도록 미리 제거합니다.
        // ------------------------------------------------------------------
        foreach (TMP_SubMeshUI subMesh in root.GetComponentsInChildren<TMP_SubMeshUI>(true))
        {
            Object.DestroyImmediate(subMesh.gameObject);
        }

        // ------------------------------------------------------------------
        // 프리팹으로 저장
        // ------------------------------------------------------------------
        string dir = "Assets/Prefabs/UI";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = dir + "/Promes_Yellow_Guide.prefab";
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);

        GameObject target = Selection.activeGameObject;
        if (target != null)
        {
            instance.transform.SetParent(target.transform, false);
            Debug.Log($"[PromesYellowGuidePrefabBuilder] '{target.name}' 하위에 Promes_Yellow_Guide 인스턴스를 생성했습니다. 프리팹: {path}");
        }
        else
        {
            Debug.Log($"[PromesYellowGuidePrefabBuilder] 오브젝트를 선택하지 않아 씬 루트에 생성했습니다. Content 오브젝트 밑으로 옮겨주세요. 프리팹: {path}");
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

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content, int fontSize, FontStyles style, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static TextMeshProUGUI CreateChildText(Transform parent, string content, int fontSize, FontStyles style, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        return tmp;
    }

    // 표의 한 행(3칸) - 각 칸은 min == preferred 로 고정되어 행마다 경계가 절대 어긋나지 않음
    private static void CreateTableRow(Transform parent, string category, string weight, string concept, Color bgColor, Color textColor, FontStyles style, int fontSize)
    {
        GameObject row = CreateBackgroundObject("Row", parent, TableBorderColor);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = ROW_SPACING;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        CreateTableCell(row.transform, category, COL_CATEGORY_WIDTH, bgColor, textColor, style, fontSize, TextAlignmentOptions.TopLeft);
        CreateTableCell(row.transform, weight, COL_WEIGHT_WIDTH, bgColor, textColor, style, fontSize, TextAlignmentOptions.TopLeft);
        CreateTableCell(row.transform, concept, COL_CONCEPT_WIDTH, bgColor, textColor, style, fontSize, TextAlignmentOptions.TopLeft);
    }

    private static void CreateTableCell(Transform parent, string text, float width, Color bgColor, Color textColor, FontStyles style, int fontSize, TextAlignmentOptions align)
    {
        GameObject cell = CreateBackgroundObject("Cell", parent, bgColor);

        LayoutElement cellLe = cell.AddComponent<LayoutElement>();
        cellLe.minWidth = width;
        cellLe.preferredWidth = width;
        cellLe.flexibleWidth = 0f;

        VerticalLayoutGroup cellVlg = cell.AddComponent<VerticalLayoutGroup>();
        cellVlg.padding = new RectOffset(10, 10, 10, 10);
        cellVlg.childControlWidth = true;
        cellVlg.childControlHeight = true;
        cellVlg.childForceExpandWidth = true;
        cellVlg.childForceExpandHeight = false;

        CreateChildText(cell.transform, text, fontSize, style, textColor, align);
    }

    // 규칙 블록 하나: 굵은 제목 줄 + "→" 로 시작하는 들여쓰기된 설명 줄들
    private static void CreateRuleBlock(Transform parent, Rule rule)
    {
        GameObject block = new GameObject("Rule_Block", typeof(RectTransform));
        block.transform.SetParent(parent, false);

        LayoutElement blockLe = block.AddComponent<LayoutElement>();
        blockLe.flexibleWidth = 1f;

        VerticalLayoutGroup blockVlg = block.AddComponent<VerticalLayoutGroup>();
        blockVlg.padding = new RectOffset(0, 0, 0, 0);
        blockVlg.spacing = 5f;
        blockVlg.childControlWidth = true;
        blockVlg.childControlHeight = true;
        blockVlg.childForceExpandWidth = true;
        blockVlg.childForceExpandHeight = false;

        CreateChildText(block.transform, rule.title, 14, FontStyles.Bold, RuleTitleColor, TextAlignmentOptions.TopLeft);

        foreach (string line in rule.lines)
        {
            GameObject lineGo = new GameObject("Arrow_Line", typeof(RectTransform));
            lineGo.transform.SetParent(block.transform, false);
            LayoutElement lineLe = lineGo.AddComponent<LayoutElement>();
            lineLe.flexibleWidth = 1f;

            TextMeshProUGUI tmp = lineGo.AddComponent<TextMeshProUGUI>();
            tmp.text = line;
            tmp.fontSize = 13;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color = ArrowLineColor;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.margin = new Vector4(16f, 0f, 0f, 0f); // 들여쓰기
            tmp.raycastTarget = false;
        }
    }
}
