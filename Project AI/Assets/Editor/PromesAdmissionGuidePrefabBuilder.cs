// PromesAdmissionGuidePrefabBuilder.cs
// -----------------------------------------------------------------------------
// 사용법
// 1) 아카이브(또는 문서 패널)의 Scroll View > Viewport > Content 오브젝트를 선택합니다.
//    (선택 안 해도 되지만, 선택하면 바로 그 밑에 생성물이 자동으로 들어갑니다)
// 2) 상단 메뉴 Tools > Promes > Create Admission Guide Prefab 클릭
// 3) Assets/Prefabs/UI/Promes_Admission_Guide.prefab 로 프리팹이 저장되고,
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

public static class PromesAdmissionGuidePrefabBuilder
{
    private const float CONTENT_WIDTH = 698f;
    private const float ROOT_PADDING_H = 24f;

    private const float TABLE_PADDING_H = 1f;
    private const float ROW_SPACING = 1f;
    private const float COL_CATEGORY_WIDTH = 76f;
    private const float COL_ITEM_WIDTH = 118f;
    private const float COL_CODE_WIDTH = 132f;

    private static readonly float COL_CRITERIA_WIDTH =
        (CONTENT_WIDTH - 2 * ROOT_PADDING_H - 2 * TABLE_PADDING_H - 3 * ROW_SPACING)
        - COL_CATEGORY_WIDTH - COL_ITEM_WIDTH - COL_CODE_WIDTH;

    private static readonly Color TextColor = new Color(0.13f, 0.13f, 0.13f, 1f);
    private static readonly Color SubTextColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    private static readonly Color DividerColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    private static readonly Color BadgeBgColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    private static readonly Color TableBorderColor = new Color(0.56f, 0.85f, 0.90f, 1f);
    private static readonly Color TableHeaderBgColor = new Color(0.80f, 0.96f, 0.98f, 1f);
    private static readonly Color TableCellBgColor = Color.white;

    private const string LINK_COLOR = "#2979FF";

    private struct TableRow { public string category, item, criteria, code; public TableRow(string c, string i, string cr, string co) { category = c; item = i; criteria = cr; code = co; } }
    private static readonly TableRow[] TableRows = new TableRow[]
    {
        new TableRow("가산점 01", "AI 맞춤\n학습 이수율", "Edu-Promes AI 맞춤 학습 모듈 이수율 100% 달성", "DATA-DAEWOO-\nRULE1"),
        new TableRow("가산점 02", "학습 연속성\n및 몰입도", "학습 몰입도 센서 측정 결과, 연속 학습 유지 지수 최상위(전국 상위 0.01%) 기록", "DATA-DAEWOO-\nRULE2"),
        new TableRow("가산점 03", "AI 심화\n교과 가중치", "대우대 전용 AI 심화 교과 알고리즘 최고 가중치 결합", "DATA-DAEWOO-\nRULE3"),
    };

    [MenuItem("Tools/Promes/Create Admission Guide Prefab")]
    public static void CreatePrefab()
    {
        // ------------------------------------------------------------------
        // 루트 오브젝트
        // ------------------------------------------------------------------
        GameObject root = new GameObject("Promes_Admission_Guide", typeof(RectTransform));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(0.5f, 1f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup rootVlg = root.AddComponent<VerticalLayoutGroup>();
        rootVlg.padding = new RectOffset((int)ROOT_PADDING_H, (int)ROOT_PADDING_H, 22, 28);
        rootVlg.spacing = 16f;
        rootVlg.childControlWidth = true;
        rootVlg.childControlHeight = true;
        rootVlg.childForceExpandWidth = true;
        rootVlg.childForceExpandHeight = false;

        ContentSizeFitter rootFitter = root.AddComponent<ContentSizeFitter>();
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ------------------------------------------------------------------
        // 상단 공고 번호 + 제목
        // ------------------------------------------------------------------
        CreateParagraph(root.transform, "Notice_No", "[대우대학교 공고 제2026-042호]", 13, FontStyles.Normal, SubTextColor, TextAlignmentOptions.TopLeft, 0f);

        CreateParagraph(root.transform, "Title", "2026학년도 대우대학교 프로메스 특별전형 입시 가산점 산출 가이드라인", 23, FontStyles.Bold, TextColor, TextAlignmentOptions.TopLeft, 0f);

        // ------------------------------------------------------------------
        // 메타 정보 (발행처 / 시행일자 / 문서번호)
        // ------------------------------------------------------------------
        CreateMetaLine(root.transform, "발행처: 대우대학교 입학처 / 프로메스 교육재단 입학관리위원회");
        CreateMetaLine(root.transform, "시행일자: 2026년 3월 1일");
        CreateDocNumberRow(root.transform, "문서번호:", "DWU-ADM-2026-PROMES");

        CreateDivider(root.transform);

        // ------------------------------------------------------------------
        // [안내]
        // ------------------------------------------------------------------
        CreateParagraph(root.transform, "Notice_Header", "[안내]", 17, FontStyles.Bold, TextColor, TextAlignmentOptions.TopLeft, 0f);

        CreateParagraph(root.transform, "Notice_Body",
            "본 가이드라인은 대우대학교 프로메스 특별전형 지원자의 AI 학습 데이터 및 포트폴리오 성적을 정밀 검증하기 위해 제정된 공식 지침입니다. 지원자 및 보호자는 아래 항목을 숙지하여 가산점 산출에 불이익이 없도록 유의하시기 바랍니다.",
            14, FontStyles.Italic, TextColor, TextAlignmentOptions.TopLeft, 0f);

        // ------------------------------------------------------------------
        // 1. 전형 개요 및 지원 자격
        // ------------------------------------------------------------------
        CreateSectionHeader(root.transform, "1. 전형 개요 및 지원 자격");

        CreateBullet(root.transform, "<b>전형 명칭:</b> 2026학년도 대우대학교 프로메스 특별전형");
        CreateBullet(root.transform, $"<b>지원 자격:</b> 대우대학교 프로메스 특별전형은 AI 성적 최상위권 자율고 사립고(대우고등학교 등) 재학생 중, <color={LINK_COLOR}><u>Edu-Promes</u></color> 케어 솔루션을 적용받는 피교육자를 대상으로 실시합니다.");
        CreateBullet(root.transform, "<b>평가 방식:</b> AI 기반 포트폴리오 종합 데이터 평가 + 가산점 알고리즘 산출");

        // ------------------------------------------------------------------
        // 2. 가산점 부여 세부 항목 및 데이터 결합 기준
        // ------------------------------------------------------------------
        CreateSectionHeader(root.transform, "2. 가산점 부여 세부 항목 및 데이터 결합 기준");

        CreateParagraph(root.transform, "Section2_Body",
            $"입학 평가 시스템은 지원자의 생활기록부 및 <color={LINK_COLOR}><u>Edu-Promes</u></color> 단말기 데이터를 실시간으로 분석하여 아래 3가지 핵심 가산점 항목을 추출 및 적용합니다.",
            14, FontStyles.Normal, TextColor, TextAlignmentOptions.TopLeft, 0f);

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

        CreateTableRow(table.transform, "구분", "평가 항목", "세부 인정 기준", "데이터 코드",
            TableHeaderBgColor, FontStyles.Bold, 13, TextAlignmentOptions.Center);

        foreach (TableRow r in TableRows)
            CreateTableRow(table.transform, r.category, r.item, r.criteria, r.code,
                TableCellBgColor, FontStyles.Normal, 12, TextAlignmentOptions.TopLeft);

        CreateBullet(root.transform, "<b>수석 합격권 산출 조건:</b> 위 3가지 가산점 데이터 코드가 완벽히 결합될 경우, 최종 COI 지수 최고점(99.0% 이상)이 부여됩니다.");

        // ------------------------------------------------------------------
        // 3. 서류 제출 및 행정 처리 규정
        // ------------------------------------------------------------------
        CreateSectionHeader(root.transform, "3. 서류 제출 및 행정 처리 규정");

        CreateNumbered(root.transform, 1, $"<b>제출 서류:</b> 학생생활기록부, <color={LINK_COLOR}><u>Edu-Promes</u></color> 학습 이수 증명서, AI 포트폴리오 검증 리포트");
        CreateNumbered(root.transform, 2, "<b>보호자 동의 절차:</b> 포트폴리오 제출 시 보호자의 성적 보정 동의서가 포함되어야 최종 접수가 완료됩니다.");
        CreateNumbered(root.transform, 3, "<b>데이터 검증 책임:</b> 수치 보정에 따른 모든 행정적 절차는 프로메스 입학관리위원회의 인증을 거쳐야 효력이 발생합니다.");

        // ------------------------------------------------------------------
        // 서명 + 직인
        // ------------------------------------------------------------------
        CreateSignatureFooter(root.transform, "대우대학교 입학처장 / 프로메스 교육재단 이사장");

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

        string path = dir + "/Promes_Admission_Guide.prefab";
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);

        GameObject target = Selection.activeGameObject;
        if (target != null)
        {
            instance.transform.SetParent(target.transform, false);
            Debug.Log($"[PromesAdmissionGuidePrefabBuilder] '{target.name}' 하위에 Promes_Admission_Guide 인스턴스를 생성했습니다. 프리팹: {path}");
        }
        else
        {
            Debug.Log($"[PromesAdmissionGuidePrefabBuilder] 오브젝트를 선택하지 않아 씬 루트에 생성했습니다. Content 오브젝트 밑으로 옮겨주세요. 프리팹: {path}");
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

    private static TextMeshProUGUI CreateParagraph(Transform parent, string name, string content, int fontSize, FontStyles style, Color color, TextAlignmentOptions align, float topMargin)
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
        tmp.richText = true;
        tmp.raycastTarget = false;
        if (topMargin > 0f) tmp.margin = new Vector4(0f, topMargin, 0f, 0f);
        return tmp;
    }

    private static void CreateMetaLine(Transform parent, string content)
    {
        CreateParagraph(parent, "Meta_Line", content, 14, FontStyles.Normal, TextColor, TextAlignmentOptions.TopLeft, 0f);
    }

    // "문서번호:" 라벨 + 회색 뱃지 스타일의 값
    private static void CreateDocNumberRow(Transform parent, string label, string value)
    {
        GameObject row = new GameObject("Meta_DocNumber", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.flexibleWidth = 1f;
        rowLe.preferredHeight = 26f;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(row.transform, false);
        LayoutElement labelLe = labelGo.AddComponent<LayoutElement>();
        labelLe.preferredWidth = 62f;
        TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 14;
        labelTmp.color = TextColor;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.raycastTarget = false;

        GameObject badge = CreateBackgroundObject("Badge", row.transform, BadgeBgColor);
        LayoutElement badgeLe = badge.AddComponent<LayoutElement>();
        badgeLe.preferredWidth = 200f;
        badgeLe.preferredHeight = 24f;
        HorizontalLayoutGroup badgeHlg = badge.AddComponent<HorizontalLayoutGroup>();
        badgeHlg.padding = new RectOffset(10, 10, 3, 3);
        badgeHlg.childControlWidth = true;
        badgeHlg.childControlHeight = true;
        badgeHlg.childForceExpandWidth = true;
        badgeHlg.childForceExpandHeight = true;

        GameObject valueGo = new GameObject("Value", typeof(RectTransform));
        valueGo.transform.SetParent(badge.transform, false);
        TextMeshProUGUI valueTmp = valueGo.AddComponent<TextMeshProUGUI>();
        valueTmp.text = value;
        valueTmp.fontSize = 13;
        valueTmp.fontStyle = FontStyles.Normal;
        valueTmp.color = TextColor;
        valueTmp.alignment = TextAlignmentOptions.MidlineLeft;
        valueTmp.raycastTarget = false;
    }

    private static void CreateDivider(Transform parent)
    {
        GameObject divider = CreateBackgroundObject("Divider", parent, DividerColor);
        LayoutElement le = divider.AddComponent<LayoutElement>();
        le.preferredHeight = 1f;
        le.flexibleWidth = 1f;
        le.flexibleHeight = 0f;
    }

    private static void CreateSectionHeader(Transform parent, string content)
    {
        CreateParagraph(parent, "Section_Header", content, 18, FontStyles.Bold, TextColor, TextAlignmentOptions.TopLeft, 8f);
    }

    // "●" 로 시작하는 들여쓰기된 불릿 문단
    private static void CreateBullet(Transform parent, string content)
    {
        TextMeshProUGUI tmp = CreateParagraph(parent, "Bullet", "●  " + content, 14, FontStyles.Normal, TextColor, TextAlignmentOptions.TopLeft, 0f);
        tmp.margin = new Vector4(10f, 0f, 0f, 0f);
    }

    // "1. / 2. / 3." 로 시작하는 들여쓰기된 번호 문단
    private static void CreateNumbered(Transform parent, int index, string content)
    {
        TextMeshProUGUI tmp = CreateParagraph(parent, "Numbered", $"{index}.  {content}", 14, FontStyles.Normal, TextColor, TextAlignmentOptions.TopLeft, 0f);
        tmp.margin = new Vector4(10f, 0f, 0f, 0f);
    }

    // 표의 한 행(4칸)
    private static void CreateTableRow(Transform parent, string category, string item, string criteria, string code,
        Color bgColor, FontStyles style, int fontSize, TextAlignmentOptions bodyAlign)
    {
        GameObject row = CreateBackgroundObject("Row", parent, TableBorderColor);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = ROW_SPACING;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        CreateTableCell(row.transform, category, COL_CATEGORY_WIDTH, bgColor, style, fontSize, TextAlignmentOptions.Center);
        CreateTableCell(row.transform, item, COL_ITEM_WIDTH, bgColor, style, fontSize, bodyAlign == TextAlignmentOptions.Center ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft);
        CreateTableCell(row.transform, criteria, COL_CRITERIA_WIDTH, bgColor, style, fontSize, bodyAlign);
        CreateTableCell(row.transform, code, COL_CODE_WIDTH, bgColor, style, fontSize, bodyAlign == TextAlignmentOptions.Center ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft);
    }

    private static void CreateTableCell(Transform parent, string text, float width, Color bgColor, FontStyles style, int fontSize, TextAlignmentOptions align)
    {
        GameObject cell = CreateBackgroundObject("Cell", parent, bgColor);

        LayoutElement cellLe = cell.AddComponent<LayoutElement>();
        cellLe.minWidth = width;
        cellLe.preferredWidth = width;
        cellLe.flexibleWidth = 0f;

        VerticalLayoutGroup cellVlg = cell.AddComponent<VerticalLayoutGroup>();
        cellVlg.padding = new RectOffset(6, 6, 6, 6);
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

    // 우측 정렬 서명 문구 + 이미지 스탬프 자리(빈 슬롯).
    // Stamp는 Ignore Layout이 켜져 있어서 부모의 자동 정렬을 받지 않고, Rect Transform을
    // 자유롭게 옮기고 크기를 바꿀 수 있습니다. Assets/Resources/UI/Promes_Admission_Stamp
    // 이름으로 스프라이트를 넣어두면 자동으로 채워지고(Promes_Logo와 동일한 방식),
    // 없으면 빈 회색 박스로 남아 나중에 Image 슬롯에 직접 스프라이트를 드래그하면 됩니다.
    private static void CreateSignatureFooter(Transform parent, string signatureText)
    {
        CreateParagraph(parent, "Signature", signatureText, 14, FontStyles.Normal, TextColor, TextAlignmentOptions.TopRight, 20f);

        GameObject row = new GameObject("Stamp_Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.flexibleWidth = 1f;
        rowLe.preferredHeight = 90f;

        GameObject stamp = new GameObject("Stamp", typeof(RectTransform), typeof(Image));
        stamp.transform.SetParent(row.transform, false);
        RectTransform stampRt = stamp.GetComponent<RectTransform>();
        stampRt.anchorMin = new Vector2(1f, 0.5f);
        stampRt.anchorMax = new Vector2(1f, 0.5f);
        stampRt.pivot = new Vector2(1f, 0.5f);
        stampRt.sizeDelta = new Vector2(80f, 80f);
        stampRt.anchoredPosition = Vector2.zero;

        // 부모(VerticalLayoutGroup/이 Row)의 자동 배치에서 제외 -> 에디터에서 자유롭게 위치/크기 조정 가능
        LayoutElement stampLe = stamp.AddComponent<LayoutElement>();
        stampLe.ignoreLayout = true;

        Image stampImg = stamp.GetComponent<Image>();
        stampImg.preserveAspect = true;
        stampImg.raycastTarget = false;

        Sprite stampSprite = Resources.Load<Sprite>("UI/Promes_Admission_Stamp");
        if (stampSprite != null)
        {
            stampImg.sprite = stampSprite;
            stampImg.color = Color.white;
        }
        else
        {
            // 못 찾으면 자리표시용 옅은 회색 박스로 남겨두고, 나중에 수동으로 스프라이트를 넣으면 됨
            stampImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        }
    }
}
