// PromesBrownGuidePrefabBuilder.cs
// -----------------------------------------------------------------------------
// 사용법
// 1) 이 파일을 프로젝트의 "Assets/Editor" 폴더 안에 넣습니다. (없으면 새로 생성)
// 2) 해당 패널의 Scroll View > Viewport > Content 오브젝트를 선택합니다.
//    (선택 안 해도 되지만, 선택하면 바로 그 밑에 생성물이 자동으로 들어갑니다)
// 3) 상단 메뉴 Tools > Promes > Create Brown Guide Prefab 클릭
// 4) Assets/Prefabs/UI/Promes_Brown_Guide.prefab 로 프리팹이 저장되고,
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

public static class PromesBrownGuidePrefabBuilder
{
    private const float CONTENT_WIDTH = 698f; // Scroll View content 너비에 맞춤
    private const float ROOT_PADDING_H = 24f;

    // 색상 스와치 5개 관련 상수
    private const float SWATCH_SPACING = 8f;
    private const int SWATCH_COUNT = 5;
    private static readonly float SWATCH_WIDTH =
        (CONTENT_WIDTH - 2 * ROOT_PADDING_H - (SWATCH_COUNT - 1) * SWATCH_SPACING) / SWATCH_COUNT;

    // 표(연관 시각적 개념) 계산용 상수
    private const float TABLE_PADDING_H = 1f;
    private const float ROW_SPACING = 1f;
    private const float COL_CATEGORY_WIDTH = 110f;
    private const float COL_WEIGHT_WIDTH = 140f;

    private static readonly float COL_CONCEPT_WIDTH =
        (CONTENT_WIDTH - 2 * ROOT_PADDING_H - 2 * TABLE_PADDING_H - 2 * ROW_SPACING)
        - COL_CATEGORY_WIDTH - COL_WEIGHT_WIDTH;

    private static readonly Color TitleColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    private static readonly Color SectionHeadColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Color RuleHeadColor = new Color(0.85f, 0.45f, 0.18f, 1f); // 프롬프트 결합 규칙 (주황)
    private static readonly Color BodyColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private static readonly Color RuleTitleColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    private static readonly Color ArrowLineColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    private static readonly Color TableBorderColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    private static readonly Color TableHeaderBgColor = new Color(0.91f, 0.89f, 0.97f, 1f); // 연보라
    private static readonly Color TableCellBgColor = Color.white;

    // ------------------------------------------------------------------
    // 데이터
    // ------------------------------------------------------------------
    private struct Swatch { public string name, hex; public Swatch(string n, string h) { name = n; hex = h; } }
    private static readonly Swatch[] Swatches = new Swatch[]
    {
        new Swatch("Baisic Brown", "#6C4636"),
        new Swatch("Dark Brown", "#5C4033"),
        new Swatch("Rose Brown", "#664D4E"),
        new Swatch("Rose Brown", "#664D4E"),
        new Swatch("Mahogany Brown", "#5D4037"),
    };

    private struct TableRow { public string category, weight, concept; public TableRow(string c, string w, string co) { category = c; weight = w; concept = co; } }
    private static readonly TableRow[] TableRows = new TableRow[]
    {
        new TableRow("소재/질감", "0.96", "가죽, 나무, 흙, 벽돌, 커피, 판지, 진흙"),
        new TableRow("조명/감정", "0.89", "따뜻한 조명, 세피아, 어둑한 조명, 빈티지, 영화 같은 그림자, 낮은 채도"),
        new TableRow("문체/시대", "0.85", "19세기, 필름 사진, 레트로(복고풍), 포스트 아포칼립스"),
        new TableRow("분위기", "0.81", "먼지, 안개, 연기, 황량함, 아늑함, 역사적인"),
    };

    private struct Rule { public string title; public string[] lines; public Rule(string t, string[] l) { title = t; lines = l; } }
    private static readonly Rule[] Rules = new Rule[]
    {
        new Rule("프롬프트에 (\"갈색\" + \"가죽\" 또는 \"빈티지\" 또는 \"목재\")가 포함된 경우", new string[]
        {
            "→ 주요 잠재 목표: 세월의 흔적이 묻어난 가죽 / 오크나무 텍스처 / 양장본 표지",
            "→ 색상 스펙트럼 범위: #6C4636",
            "→ 표면 특성: 은은한 하이라이트 반사, 따뜻한 조명과의 높은 연관도",
        }),
        new Rule("프롬프트에 (\"갈색\" + \"황무지\" 또는 \"아포칼립스\")가 포함된 경우:", new string[]
        {
            "→ 주요 잠재 목표: 채도가 낮고 붉은 기가 빠진 흙 / 먼지 텍스처",
            "→ 색상 스펙트럼 범위: #5C4033 ~ #3E2723",
            "→ 대기 환경 효과: 뿌연 안개 및 입자 노이즈 덧씌움",
        }),
        new Rule("프롬프트에 (\"갈색\" + \"의상\" 또는 \"옷\")이 포함된 경우:", new string[]
        {
            "→ 주요 잠재 목표: 천의 원단 직물 감촉 / 옷주름 텍스처 / 면·울 질감",
            "→ 색상 스펙트럼 범위: #5A3D28 ~ #8C6239",
            "→ 표면 특성: 낮은 광택(무광), 옷주름 음영 부분의 부드러운 그림자 확산",
        }),
        new Rule("프롬프트에 (\"갈색\" + \"헤어\" 또는 \"머리모양\")이 포함된 경우:", new string[]
        {
            "→ 주요 잠재 목표: 머리카락 결의 방향성 / 머릿결을 따른 빛 반사",
            "→ 색상 스펙트럼 범위: #664D4E ~ #966F33",
            "→ 표면 광택도: 정수리/결 부분에 밝은 샴페인 브라운(#D4B886) 엔젤링 하이라이트 자동 배치",
        }),
        new Rule("프롬프트에 (\"갈색\" + \"자연물\" 또는 \"나무\" 또는 \"흙\" 또는 \"바위\")가 포함된 경우:", new string[]
        {
            "→ 주요 잠재 목표: 나뭇결 패턴 / 습기를 머금은 흙 / 거친 바위 질감 노이즈",
            "→ 색상 스펙트럼 범위: #5D4037 ~ #795548",
            "→ 표면 특성: 자연스러운 불규칙 질감, 울퉁불퉁한 음영 오프셋 적용",
        }),
    };

    [MenuItem("Tools/Promes/Create Brown Guide Prefab")]
    public static void CreatePrefab()
    {
        // ------------------------------------------------------------------
        // 루트 오브젝트
        // ------------------------------------------------------------------
        GameObject root = new GameObject("Promes_Brown_Guide", typeof(RectTransform));
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
        // 제목 "Brown (갈색)"
        // ------------------------------------------------------------------
        CreateText(root.transform, "Title", "Brown (갈색)", 26, FontStyles.Bold, TitleColor, TextAlignmentOptions.TopLeft);

        // ------------------------------------------------------------------
        // 색상 스와치 5개
        // ------------------------------------------------------------------
        GameObject swatchRow = new GameObject("Swatch_Row", typeof(RectTransform));
        swatchRow.transform.SetParent(root.transform, false);
        LayoutElement swatchRowLe = swatchRow.AddComponent<LayoutElement>();
        swatchRowLe.preferredHeight = 110f;
        swatchRowLe.flexibleWidth = 1f;

        HorizontalLayoutGroup swatchHlg = swatchRow.AddComponent<HorizontalLayoutGroup>();
        swatchHlg.spacing = SWATCH_SPACING;
        swatchHlg.childControlWidth = true;
        swatchHlg.childControlHeight = true;
        swatchHlg.childForceExpandWidth = true;
        swatchHlg.childForceExpandHeight = true;

        foreach (Swatch s in Swatches)
            CreateSwatch(swatchRow.transform, s.name, s.hex);

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

        CreateTableRow(table.transform, "구분", "연관 가중치", "연관 시각적 개념", TableHeaderBgColor, FontStyles.Bold, 13);
        foreach (TableRow r in TableRows)
            CreateTableRow(table.transform, r.category, r.weight, r.concept, TableCellBgColor, FontStyles.Normal, 13);

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
        // -> 나중에 텍스트들을 선택해서 Font Asset만 원하는 한글 폰트로 바꾸면
        //    이 서브메시 없이 정상적으로 렌더링됩니다.
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

        string path = dir + "/Promes_Brown_Guide.prefab";
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);

        GameObject target = Selection.activeGameObject;
        if (target != null)
        {
            instance.transform.SetParent(target.transform, false);
            Debug.Log($"[PromesBrownGuidePrefabBuilder] '{target.name}' 하위에 Promes_Brown_Guide 인스턴스를 생성했습니다. 프리팹: {path}");
        }
        else
        {
            Debug.Log($"[PromesBrownGuidePrefabBuilder] 오브젝트를 선택하지 않아 씬 루트에 생성했습니다. Content 오브젝트 밑으로 옮겨주세요. 프리팹: {path}");
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

    // 색상 스와치 하나 (배경색 + 이름 + 헥스코드, 흰 글씨 중앙 정렬)
    private static void CreateSwatch(Transform parent, string name, string hex)
    {
        Color bg;
        if (!ColorUtility.TryParseHtmlString(hex, out bg))
            bg = Color.gray;

        GameObject swatch = CreateBackgroundObject($"Swatch_{name}", parent, bg);

        LayoutElement swatchLe = swatch.AddComponent<LayoutElement>();
        swatchLe.minWidth = SWATCH_WIDTH;
        swatchLe.preferredWidth = SWATCH_WIDTH;
        swatchLe.flexibleWidth = 0f;

        VerticalLayoutGroup vlg = swatch.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.spacing = 2f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        TextMeshProUGUI nameTmp = CreateChildText(swatch.transform, name, 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        TextMeshProUGUI hexTmp = CreateChildText(swatch.transform, $"({hex})", 11, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
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
    private static void CreateTableRow(Transform parent, string category, string weight, string concept, Color bgColor, FontStyles style, int fontSize)
    {
        GameObject row = CreateBackgroundObject("Row", parent, TableBorderColor);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = ROW_SPACING;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        CreateTableCell(row.transform, category, COL_CATEGORY_WIDTH, bgColor, style, fontSize, TextAlignmentOptions.TopLeft);
        CreateTableCell(row.transform, weight, COL_WEIGHT_WIDTH, bgColor, style, fontSize, TextAlignmentOptions.TopLeft);
        CreateTableCell(row.transform, concept, COL_CONCEPT_WIDTH, bgColor, style, fontSize, TextAlignmentOptions.TopLeft);
    }

    private static void CreateTableCell(Transform parent, string text, float width, Color bgColor, FontStyles style, int fontSize, TextAlignmentOptions align)
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

        CreateChildText(cell.transform, text, fontSize, style, BodyColor, align);
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
