// PromesEventDocPrefabBuilder.cs
// -----------------------------------------------------------------------------
// 사용법
// 1) 이 파일을 프로젝트의 "Assets/Editor" 폴더 안에 넣습니다. (없으면 새로 생성)
// 2) (선택) 로고+기관명이 합쳐진 배너 이미지를
//    Assets/Resources/UI/Guide/ADLC_Logo_Banner.png 로 넣고
//    Texture Type을 "Sprite (2D and UI)" 로 설정하면 자동으로 채워집니다.
//    안 넣어도 동작하며, 옅은 회색 자리표시 박스가 생성되고 나중에
//    "Logo_Banner_Image"의 Source Image 슬롯에 직접 만든 이미지를 넣으면 됩니다.
// 3) 해당 패널의 Scroll View > Viewport > Content 오브젝트를 선택합니다.
//    (선택 안 해도 되지만, 선택하면 바로 그 밑에 생성물이 자동으로 들어갑니다)
// 4) 상단 메뉴 Tools > Promes > Create Event Doc Prefab 클릭
// 5) Assets/Prefabs/UI/Promes_Event_ADLC.prefab 로 프리팹이 저장되고,
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

public static class PromesEventDocPrefabBuilder
{
    private const float CONTENT_WIDTH = 698f; // Scroll View content 너비에 맞춤
    private const float ROOT_PADDING_H = 20f;
    private const float INNER_PADDING_H = 24f;
    private const float LABEL_COL_WIDTH = 90f;

    private static readonly Color OuterBorderColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    private static readonly Color HeaderBgColor = new Color(0.90f, 0.98f, 0.99f, 1f); // 옅은 청록
    private static readonly Color DividerColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    private static readonly Color TitleColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    private static readonly Color SubTitleColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    private static readonly Color LabelColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    private static readonly Color ValueColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    private static readonly Color HeadingColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    private static readonly Color BodyColor = new Color(0.22f, 0.22f, 0.22f, 1f);

    // ------------------------------------------------------------------
    // 데이터
    // ------------------------------------------------------------------
    private struct InfoRow { public string label, value; public InfoRow(string l, string v) { label = l; value = v; } }
    private static readonly InfoRow[] InfoRows = new InfoRow[]
    {
        new InfoRow("기간", "2016년 3월 12일~17일"),
        new InfoRow("주최", "아티피셜 Artificial"),
        new InfoRow("장소", "대한민국 서울"),
        new InfoRow("대국자", "ProGo vs 이석 九단(대한민국)"),
        new InfoRow("결과", "ProGO 5승 0패, 이석 0승 5패"),
    };

    private struct Section { public string heading, body; public Section(string h, string b) { heading = h; body = b; } }
    private static readonly Section[] Sections = new Section[]
    {
        new Section("1. 개요",
            "아티피셜 딥 러닝 챌린지 매치는 2016년 3월에 진행된 아티피셜사의 바둑 인공지능 ProGo와 대한민국의 프로 바둑 기사인 이석 九단 간의 바둑 대결이다. 경기명이 긴 탓에 일반적으로는 ProGo vs 이석, ProGo 쇼크 등으로 불리는 경우가 많다."),
        new Section("2. 배경",
            "20세기 말, 인공지능이 체스에서 인간 챔피언을 꺾자 알고리즘, 인공지능 연구자들은 과연 인공지능이 바둑까지 정복할 수 있을지 관심을 가지게 되었다. 바둑은 체스보다 훨씬 많은 경우의 수가 존재하는 게임이기 때문에 인공지능이 인간을 넘지 못하는 분야로 여겨졌다. 이러한 배경 속에서 아티피셜이 개발한 바둑 인공지능 ProGo가 유럽 바둑 챔피언을 꺾은 후 연이어 지난 10여년 간 세계 챔피언이었던 한국의 이석에게 도전하였다."),
        new Section("3. 진행",
            "대국 이전 바둑계와 언론에서는 대부분 이석의 승리가 될 것이라 예측했지만 과학계에서는 이석이 질 것이라는 예측도 나왔다. 이석 九단은 본인의 승리를 자신했으나 대국 내내 프로고에게 압도적인 격차로 패배하며 고전을 면치 못했다.\n\n대국은 5판 3선승제이나 3판 선승시에도 남은 대국을 모두 두었다."),
    };

    // ------------------------------------------------------------------
    // "4. 결과" 섹션 데이터
    // ------------------------------------------------------------------
    private const float RESULT_COL_GAME_WIDTH = 70f;
    private const float RESULT_COL_DATE_WIDTH = 170f;
    private const float RESULT_COL_BLACK_WIDTH = 70f;
    private const float RESULT_COL_WHITE_WIDTH = 70f;

    private static readonly float RESULT_COL_OUTCOME_WIDTH =
        (CONTENT_WIDTH - 2 * ROOT_PADDING_H - 2f /*outer border padding*/ - 2 * INNER_PADDING_H - 2f /*table padding*/ - 4f /*4 gaps*/)
        - RESULT_COL_GAME_WIDTH - RESULT_COL_DATE_WIDTH - RESULT_COL_BLACK_WIDTH - RESULT_COL_WHITE_WIDTH;

    private static readonly Color ResultTableBorderColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    private static readonly Color ResultTableHeaderBgColor = new Color(0.93f, 0.93f, 0.93f, 1f);
    private static readonly Color ResultTableCellBgColor = Color.white;

    private struct MatchRow { public string game, date, black, white, outcome; public MatchRow(string g, string d, string b, string w, string o) { game = g; date = d; black = b; white = w; outcome = o; } }
    private static readonly MatchRow[] MatchRows = new MatchRow[]
    {
        new MatchRow("제 1국", "2016년 3월 12일 12시", "ProGo", "이석", "86수 흑 불계승. 프로고 1승"),
        new MatchRow("제 2국", "2016년 3월 13일 12시", "이석", "ProGo", "146수 백 불계승. 프로고 2승"),
        new MatchRow("제 3국", "2016년 3월 15일 12시", "ProGo", "이석", "111수 흑 불계승. 프로고 3승"),
        new MatchRow("제 4국", "2016년 3월 16일 12시", "이석", "ProGo", "132수 백 불계승. 프로고 4승"),
        new MatchRow("제 5국", "2016년 3월 17일 12시", "ProGo", "이석", "96수 흑 불계승. 프로고 5승"),
    };

    private const string ResultSummary = "결과: 프로고 5 : 0 이석 九단";

    private static readonly string[] ResultParagraphs = new string[]
    {
        "대국은 5국 모두 충격적인 결과로 마무리되었다. 비록 대국 진행 당시 세계 챔피언은 아니었지만, 21세기 초중반 세계 바둑계를 지배한 바둑계의 상징이었던 이석 九단이 ProGo에게 단 한 경기의 예외도 없이 전 대국을 150수 이하로 불계패하며 인공지능과 인간의 압도적인 격차가 존재함을 실감하게 되었다.",
        "이석 九단은 훗날 자신의 프로 은퇴를 결심하게 된 가장 결정적인 계기로 해당 경기를 꼽았으며, 앞으로 인간은 동등한 조건에서는 인공지능에게 결고 이길 수 없을 것이라 예측했다.",
        "해당 경기는 ProGo 쇼크라고 불리며 인공지능 연구개발 진척에 매우 큰 영향을 주었으며, 프로메스의 LLM 인공지능이 탄생하게 된 계기가 되었다.",
    };

    [MenuItem("Tools/Promes/Create Event Doc Prefab")]
    public static void CreatePrefab()
    {
        // ------------------------------------------------------------------
        // 루트 오브젝트
        // ------------------------------------------------------------------
        GameObject root = new GameObject("Promes_Event_ADLC", typeof(RectTransform));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(0.5f, 1f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup rootVlg = root.AddComponent<VerticalLayoutGroup>();
        rootVlg.padding = new RectOffset((int)ROOT_PADDING_H, (int)ROOT_PADDING_H, (int)ROOT_PADDING_H, (int)ROOT_PADDING_H);
        rootVlg.childControlWidth = true;
        rootVlg.childControlHeight = true;
        rootVlg.childForceExpandWidth = true;
        rootVlg.childForceExpandHeight = false;

        ContentSizeFitter rootFitter = root.AddComponent<ContentSizeFitter>();
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ------------------------------------------------------------------
        // 외곽 테두리 박스
        // ------------------------------------------------------------------
        GameObject outer = CreateBackgroundObject("Outer_Border", root.transform, OuterBorderColor);
        LayoutElement outerLe = outer.AddComponent<LayoutElement>();
        outerLe.flexibleWidth = 1f;

        VerticalLayoutGroup outerVlg = outer.AddComponent<VerticalLayoutGroup>();
        outerVlg.padding = new RectOffset(1, 1, 1, 1);
        outerVlg.spacing = 0f;
        outerVlg.childControlWidth = true;
        outerVlg.childControlHeight = true;
        outerVlg.childForceExpandWidth = true;
        outerVlg.childForceExpandHeight = false;

        // --- 헤더(제목) 박스 ---
        GameObject header = CreateBackgroundObject("Header_Box", outer.transform, HeaderBgColor);
        VerticalLayoutGroup headerVlg = header.AddComponent<VerticalLayoutGroup>();
        headerVlg.padding = new RectOffset(20, 20, 14, 14);
        headerVlg.spacing = 4f;
        headerVlg.childAlignment = TextAnchor.MiddleCenter;
        headerVlg.childControlWidth = true;
        headerVlg.childControlHeight = true;
        headerVlg.childForceExpandWidth = true;
        headerVlg.childForceExpandHeight = false;

        CreateChildText(header.transform, "아티피셜 딥 러닝 챌린지 매치", 18, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center);
        CreateChildText(header.transform, "Artificial Deep Learning Challenge Match", 13, FontStyles.Bold, SubTitleColor, TextAlignmentOptions.Center);

        // --- 헤더 구분선 ---
        GameObject headerDivider = CreateBackgroundObject("Header_Divider", outer.transform, DividerColor);
        LayoutElement headerDividerLe = headerDivider.AddComponent<LayoutElement>();
        headerDividerLe.preferredHeight = 1f;
        headerDividerLe.flexibleHeight = 0f;

        // --- 본문 영역 ---
        GameObject inner = CreateBackgroundObject("Inner_Content", outer.transform, Color.white);
        LayoutElement innerLe = inner.AddComponent<LayoutElement>();
        innerLe.flexibleWidth = 1f;

        VerticalLayoutGroup innerVlg = inner.AddComponent<VerticalLayoutGroup>();
        innerVlg.padding = new RectOffset((int)INNER_PADDING_H, (int)INNER_PADDING_H, 24, 24);
        innerVlg.spacing = 26f;
        innerVlg.childControlWidth = true;
        innerVlg.childControlHeight = true;
        innerVlg.childForceExpandWidth = true;
        innerVlg.childForceExpandHeight = false;

        // 로고 + 기관명
        CreateLogoRow(inner.transform);

        // 정보 표
        CreateInfoTable(inner.transform);

        // 번호 섹션들
        foreach (Section s in Sections)
            CreateSection(inner.transform, s);

        // "4. 결과" 섹션 (경기 결과 표 + 요약 + 문단)
        CreateResultsSection(inner.transform);

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

        string path = dir + "/Promes_Event_ADLC.prefab";
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);

        GameObject target = Selection.activeGameObject;
        if (target != null)
        {
            instance.transform.SetParent(target.transform, false);
            Debug.Log($"[PromesEventDocPrefabBuilder] '{target.name}' 하위에 Promes_Event_ADLC 인스턴스를 생성했습니다. 프리팹: {path}");
        }
        else
        {
            Debug.Log($"[PromesEventDocPrefabBuilder] 오브젝트를 선택하지 않아 씬 루트에 생성했습니다. Content 오브젝트 밑으로 옮겨주세요. 프리팹: {path}");
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

    // 로고+기관명 통짜 배너 이미지 (Resources 이미지가 있으면 자동 적용, 없으면 회색 자리표시)
    private static void CreateLogoRow(Transform parent)
    {
        GameObject bannerGo = new GameObject("Logo_Banner_Image", typeof(RectTransform), typeof(Image));
        bannerGo.transform.SetParent(parent, false);

        LayoutElement bannerLe = bannerGo.AddComponent<LayoutElement>();
        bannerLe.flexibleWidth = 1f;
        bannerLe.preferredHeight = 140f;

        Image bannerImg = bannerGo.GetComponent<Image>();
        bannerImg.preserveAspect = false;
        bannerImg.raycastTarget = false;

        Sprite bannerSprite = Resources.Load<Sprite>("UI/Guide/ADLC_Logo_Banner");
        if (bannerSprite != null)
        {
            bannerImg.sprite = bannerSprite;
            bannerImg.color = Color.white;
        }
        else
        {
            // 못 찾으면 자리표시용 옅은 회색 박스로 남겨두고, 나중에 직접 만든 로고+텍스트 이미지를
            // Source Image 슬롯에 드래그해 넣으면 됨
            bannerImg.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        }
    }

    // 정보 표 (기간/주최/장소/대국자/결과) - 행마다 라벨 | 값 + 아래 얇은 구분선
    private static void CreateInfoTable(Transform parent)
    {
        GameObject table = new GameObject("Info_Table", typeof(RectTransform));
        table.transform.SetParent(parent, false);
        LayoutElement tableLe = table.AddComponent<LayoutElement>();
        tableLe.flexibleWidth = 1f;

        VerticalLayoutGroup tableVlg = table.AddComponent<VerticalLayoutGroup>();
        tableVlg.spacing = 0f;
        tableVlg.childControlWidth = true;
        tableVlg.childControlHeight = true;
        tableVlg.childForceExpandWidth = true;
        tableVlg.childForceExpandHeight = false;

        for (int i = 0; i < InfoRows.Length; i++)
        {
            InfoRow r = InfoRows[i];

            GameObject row = new GameObject($"Row_{r.label}", typeof(RectTransform));
            row.transform.SetParent(table.transform, false);
            LayoutElement rowLe = row.AddComponent<LayoutElement>();
            rowLe.flexibleWidth = 1f;

            HorizontalLayoutGroup rowHlg = row.AddComponent<HorizontalLayoutGroup>();
            rowHlg.padding = new RectOffset(0, 0, 10, 10);
            rowHlg.spacing = 10f;
            rowHlg.childControlWidth = true;
            rowHlg.childControlHeight = true;
            rowHlg.childForceExpandWidth = false;
            rowHlg.childForceExpandHeight = true;

            // 라벨 칸: min == preferred 로 고정해서 값이 길어도 절대 눌리지 않음
            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(row.transform, false);
            LayoutElement labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.minWidth = LABEL_COL_WIDTH;
            labelLe.preferredWidth = LABEL_COL_WIDTH;
            labelLe.flexibleWidth = 0f;
            TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = r.label;
            labelTmp.fontSize = 14;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.color = LabelColor;
            labelTmp.alignment = TextAlignmentOptions.TopLeft;
            labelTmp.raycastTarget = false;

            // 값 칸: 나머지 공간
            GameObject valueGo = new GameObject("Value", typeof(RectTransform));
            valueGo.transform.SetParent(row.transform, false);
            LayoutElement valueLe = valueGo.AddComponent<LayoutElement>();
            valueLe.flexibleWidth = 1f;
            TextMeshProUGUI valueTmp = valueGo.AddComponent<TextMeshProUGUI>();
            valueTmp.text = r.value;
            valueTmp.fontSize = 14;
            valueTmp.fontStyle = FontStyles.Normal;
            valueTmp.color = ValueColor;
            valueTmp.alignment = TextAlignmentOptions.TopLeft;
            valueTmp.enableWordWrapping = true;
            valueTmp.overflowMode = TextOverflowModes.Overflow;
            valueTmp.raycastTarget = false;

            // 마지막 행이 아니면 아래 구분선 추가
            if (i < InfoRows.Length - 1)
            {
                GameObject divider = CreateBackgroundObject("Divider", table.transform, DividerColor);
                LayoutElement dividerLe = divider.AddComponent<LayoutElement>();
                dividerLe.preferredHeight = 1f;
                dividerLe.flexibleHeight = 0f;
            }
        }
    }

    // 번호 섹션: 굵은 소제목 + 본문 문단
    private static void CreateSection(Transform parent, Section section)
    {
        GameObject block = new GameObject($"Section_{section.heading}", typeof(RectTransform));
        block.transform.SetParent(parent, false);

        LayoutElement blockLe = block.AddComponent<LayoutElement>();
        blockLe.flexibleWidth = 1f;

        VerticalLayoutGroup blockVlg = block.AddComponent<VerticalLayoutGroup>();
        blockVlg.spacing = 6f;
        blockVlg.childControlWidth = true;
        blockVlg.childControlHeight = true;
        blockVlg.childForceExpandWidth = true;
        blockVlg.childForceExpandHeight = false;

        CreateChildText(block.transform, section.heading, 16, FontStyles.Bold, HeadingColor, TextAlignmentOptions.TopLeft);
        CreateChildText(block.transform, section.body, 13, FontStyles.Normal, BodyColor, TextAlignmentOptions.TopLeft);
    }

    // "4. 결과" 섹션: 소제목 + 5열 경기 결과 표 + 전체 폭 요약 행 + 문단들
    private static void CreateResultsSection(Transform parent)
    {
        GameObject block = new GameObject("Section_4_결과", typeof(RectTransform));
        block.transform.SetParent(parent, false);

        LayoutElement blockLe = block.AddComponent<LayoutElement>();
        blockLe.flexibleWidth = 1f;

        VerticalLayoutGroup blockVlg = block.AddComponent<VerticalLayoutGroup>();
        blockVlg.spacing = 14f;
        blockVlg.childControlWidth = true;
        blockVlg.childControlHeight = true;
        blockVlg.childForceExpandWidth = true;
        blockVlg.childForceExpandHeight = false;

        CreateChildText(block.transform, "4. 결과", 16, FontStyles.Bold, HeadingColor, TextAlignmentOptions.TopLeft);

        // --- 경기 결과 표 ---
        GameObject table = CreateBackgroundObject("Result_Table", block.transform, ResultTableBorderColor);
        LayoutElement tableLe = table.AddComponent<LayoutElement>();
        tableLe.flexibleWidth = 1f;

        VerticalLayoutGroup tableVlg = table.AddComponent<VerticalLayoutGroup>();
        tableVlg.padding = new RectOffset(1, 1, 1, 1);
        tableVlg.spacing = 1f;
        tableVlg.childControlWidth = true;
        tableVlg.childControlHeight = true;
        tableVlg.childForceExpandWidth = true;
        tableVlg.childForceExpandHeight = false;

        // 헤더 행
        CreateResultRow(table.transform, "경기", "날짜", "흑", "백", "결과", ResultTableHeaderBgColor, FontStyles.Bold);

        // 데이터 행
        foreach (MatchRow r in MatchRows)
            CreateResultRow(table.transform, r.game, r.date, r.black, r.white, r.outcome, ResultTableCellBgColor, FontStyles.Normal);

        // 전체 폭 요약 행 ("결과: 프로고 5 : 0 이석 九단")
        GameObject summaryRow = CreateBackgroundObject("Summary_Row", table.transform, ResultTableCellBgColor);
        LayoutElement summaryLe = summaryRow.AddComponent<LayoutElement>();
        summaryLe.preferredHeight = 40f;
        summaryLe.flexibleWidth = 1f;

        VerticalLayoutGroup summaryVlg = summaryRow.AddComponent<VerticalLayoutGroup>();
        summaryVlg.childAlignment = TextAnchor.MiddleCenter;
        summaryVlg.childControlWidth = true;
        summaryVlg.childControlHeight = true;
        summaryVlg.childForceExpandWidth = true;
        summaryVlg.childForceExpandHeight = false;

        CreateChildText(summaryRow.transform, ResultSummary, 14, FontStyles.Bold, HeadingColor, TextAlignmentOptions.Center);

        // --- 문단들 ---
        foreach (string paragraph in ResultParagraphs)
            CreateChildText(block.transform, paragraph, 13, FontStyles.Normal, BodyColor, TextAlignmentOptions.TopLeft);
    }

    // 경기 결과 표의 한 행(5칸) - 각 칸은 min == preferred 로 고정되어 행마다 경계가 절대 어긋나지 않음
    private static void CreateResultRow(Transform parent, string game, string date, string black, string white, string outcome, Color bgColor, FontStyles style)
    {
        GameObject row = CreateBackgroundObject("Row", parent, ResultTableBorderColor);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 1f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        CreateResultCell(row.transform, game, RESULT_COL_GAME_WIDTH, bgColor, style, TextAlignmentOptions.Center);
        CreateResultCell(row.transform, date, RESULT_COL_DATE_WIDTH, bgColor, style, TextAlignmentOptions.Center);
        CreateResultCell(row.transform, black, RESULT_COL_BLACK_WIDTH, bgColor, style, TextAlignmentOptions.Center);
        CreateResultCell(row.transform, white, RESULT_COL_WHITE_WIDTH, bgColor, style, TextAlignmentOptions.Center);
        CreateResultCell(row.transform, outcome, RESULT_COL_OUTCOME_WIDTH, bgColor, style, TextAlignmentOptions.Center);
    }

    private static void CreateResultCell(Transform parent, string text, float width, Color bgColor, FontStyles style, TextAlignmentOptions align)
    {
        GameObject cell = CreateBackgroundObject("Cell", parent, bgColor);

        LayoutElement cellLe = cell.AddComponent<LayoutElement>();
        cellLe.minWidth = width;
        cellLe.preferredWidth = width;
        cellLe.flexibleWidth = 0f;

        VerticalLayoutGroup cellVlg = cell.AddComponent<VerticalLayoutGroup>();
        cellVlg.padding = new RectOffset(8, 8, 8, 8);
        cellVlg.childAlignment = TextAnchor.MiddleCenter;
        cellVlg.childControlWidth = true;
        cellVlg.childControlHeight = true;
        cellVlg.childForceExpandWidth = true;
        cellVlg.childForceExpandHeight = false;

        CreateChildText(cell.transform, text, 13, style, BodyColor, align);
    }
}
