// HanSeoARecordPrefabBuilder.cs
// -----------------------------------------------------------------------------
// 한서아 학교생활기록부(EP_1_Q3 문서요약 퀘스트의 "원본 문서" 뷰) 프리팹을 만듭니다.
// PromesAdmissionGuidePrefabBuilder.cs와 같은 패턴이지만, 표마다 컬럼 개수가 달라서
// 범용 테이블 생성 함수(CreateTable)를 씁니다.
//
// 사용법
// 1) 문서 패널의 원본(originalPanel) 쪽 Scroll View > Viewport > Content 오브젝트를 선택합니다.
//    (선택 안 해도 되지만, 선택하면 바로 그 밑에 생성물이 자동으로 들어갑니다)
// 2) 상단 메뉴 Tools > Promes > Create Han SeoA Record Prefab 클릭
// 3) Assets/Prefabs/UI/HanSeoA_StudentRecord.prefab 로 프리팹이 저장되고,
//    선택했던 Content 밑에 인스턴스가 바로 생성됩니다.
//
// 주의: 이건 "원본 문서" 뷰용입니다. 스캔 후 고르는 문장 버튼 5개는
// HanSeoARecordSentenceBuilder.cs로 별도의 analysisPanel 쪽에 만드는 겁니다 (다른 오브젝트).
// -----------------------------------------------------------------------------

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class HanSeoARecordPrefabBuilder
{
    private const float CONTENT_WIDTH = 698f;
    private const float ROOT_PADDING_H = 24f;
    private const float TABLE_PADDING_H = 1f;
    private const float ROW_SPACING = 1f;
    private const float CELL_PADDING = 6f;

    private static readonly Color TextColor = new Color(0.13f, 0.13f, 0.13f, 1f);
    private static readonly Color SubTextColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    private static readonly Color DividerColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    private static readonly Color TableBorderColor = new Color(0.56f, 0.85f, 0.90f, 1f);
    private static readonly Color TableHeaderBgColor = new Color(0.80f, 0.96f, 0.98f, 1f);
    private static readonly Color TableCellBgColor = Color.white;

    private struct TableSpec
    {
        public string[] header;
        public float[] colWidths; // 마지막 값이 -1이면 "남는 폭 전부"(flexible)
        public string[][] rows;
    }

    [MenuItem("Tools/Promes/Create Han SeoA Record Prefab")]
    public static void CreatePrefab()
    {
        GameObject root = new GameObject("HanSeoA_StudentRecord", typeof(RectTransform));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(0.5f, 1f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup rootVlg = root.AddComponent<VerticalLayoutGroup>();
        rootVlg.padding = new RectOffset((int)ROOT_PADDING_H, (int)ROOT_PADDING_H, 22, 28);
        rootVlg.spacing = 14f;
        rootVlg.childControlWidth = true;
        rootVlg.childControlHeight = true;
        rootVlg.childForceExpandWidth = true;
        rootVlg.childForceExpandHeight = false;

        ContentSizeFitter rootFitter = root.AddComponent<ContentSizeFitter>();
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ------------------------------------------------------------------
        // 제목
        // ------------------------------------------------------------------
        CreateParagraph(root.transform, "Title", "[학교생활기록부]", 22, FontStyles.Bold, TextColor, TextAlignmentOptions.Center, 0f);
        CreateDivider(root.transform);

        // ------------------------------------------------------------------
        // 1. 인적·학적사항
        // ------------------------------------------------------------------
        CreateSectionHeader(root.transform, "1. 인적·학적사항");

        CreateTable(root.transform, new TableSpec
        {
            header = new[] { "성명", "성별", "주민등록번호", "주소" },
            colWidths = new float[] { 70, 60, 140, -1 },
            rows = new[]
            {
                new[] { "한서아", "여", "080218-4******", "서울특별시 서초구 반포대로 1**번길 (반포자이)" },
            }
        });

        CreateTable(root.transform, new TableSpec
        {
            header = new[] { "구분", "일자", "학년/학반/번호", "학교명", "변동구분 (비고)" },
            colWidths = new float[] { 55, 90, 110, 90, -1 },
            rows = new[]
            {
                new[] { "입학", "2025. 03. 02", "1학년 1반 14번", "대우고등학교", "입학 (일반전형 수석)" },
                new[] { "진급", "2026. 03. 02", "2학년 1반 12번", "대우고등학교", "재학 (COI특화반)" },
            }
        });

        // ------------------------------------------------------------------
        // 2. 출결상황
        // ------------------------------------------------------------------
        CreateSectionHeader(root.transform, "2. 출결상황");

        CreateTable(root.transform, new TableSpec
        {
            header = new[] { "학년", "수업일수", "결석(질병/미인정)", "지각/조퇴/결과", "특기사항" },
            colWidths = new float[] { 55, 70, 110, 110, -1 },
            rows = new[]
            {
                new[] { "1학년", "190", "0 / 0 / 0", "0 / 0 / 0", "개근" },
                new[] { "2학년", "98", "0 / 0 / 0", "0 / 0 / 0", "" },
            }
        });

        // ------------------------------------------------------------------
        // 3. 수상경력
        // ------------------------------------------------------------------
        CreateSectionHeader(root.transform, "3. 수상경력");

        CreateTable(root.transform, new TableSpec
        {
            header = new[] { "구분", "수상명", "등급(위)", "수상연월일", "수여기관", "참가대상(인원)" },
            colWidths = new float[] { 50, 130, 65, 85, 95, -1 },
            rows = new[]
            {
                new[] { "교내", "프로메스 학업역량 최우수상", "1위(금상)", "2025. 12. 20", "대우고등학교장", "1학년 전체(300명)" },
                new[] { "교내", "AI 알고리즘 문제해결 경진대회", "대상", "2025. 07. 15", "대우고등학교장", "1, 2학년 희망자" },
                new[] { "교외", "전국 고교 COI 학업지수 우수표창", "표창", "2026. 05. 10", "프로메스 교육재단", "전국 자사고 재학생" },
            }
        });

        // ------------------------------------------------------------------
        // 4. 창의적 체험활동상황
        // ------------------------------------------------------------------
        CreateSectionHeader(root.transform, "4. 창의적 체험활동상황");

        CreateTable(root.transform, new TableSpec
        {
            header = new[] { "학년", "영역", "시간", "특기사항" },
            colWidths = new float[] { 45, 80, 55, -1 },
            rows = new[]
            {
                new[] { "2", "자율활동", "34시간", "학습 몰입도 센서 측정 결과, 연속 학습 유지 지수 전국 상위 0.01% 기록. 교내 자율 학습 시간 동안 흐트러짐 없는 고도의 집중력을 유지함." },
                new[] { "2", "동아리활동", "28시간", "(AI 융합 알고리즘 연구반) 데이터 모델링 및 프롬프트 제어 기초 탐구 활동을 성실히 수행함." },
                new[] { "2", "진로활동", "20시간", "대우대학교 경영학부 AI 전략경영 특별전형을 목표로 체계적인 학업 로드맵을 구축하고 자기관리 역량을 입증함." },
            }
        });

        // ------------------------------------------------------------------
        // 5. 교과학습발달상황
        // ------------------------------------------------------------------
        CreateSectionHeader(root.transform, "5. 교과학습발달상황");
        CreateParagraph(root.transform, "Sub_Header_A", "가. 성적 및 석차 현황", 15, FontStyles.Bold, TextColor, TextAlignmentOptions.TopLeft, 4f);

        CreateTable(root.transform, new TableSpec
        {
            header = new[] { "학년", "학기", "교과", "과목", "단위수", "원점수/과목평균(수강자수)", "성취도(수강자수)", "석차등급" },
            colWidths = new float[] { 35, 45, 45, 80, 40, 140, 80, -1 },
            rows = new[]
            {
                new[] { "2", "1학기", "국어", "문학", "4", "100 / 72.4 (14.2)", "A (280)", "1" },
                new[] { "2", "1학기", "수학", "수학Ⅰ", "4", "100 / 68.1 (16.8)", "A (280)", "1" },
                new[] { "2", "1학기", "영어", "영어Ⅰ", "4", "99 / 71.0 (15.1)", "A (280)", "1" },
                new[] { "2", "1학기", "정보", "AI 데이터 구조", "3", "100 / 65.5 (18.0)", "A (280)", "1" },
            }
        });

        CreateParagraph(root.transform, "Sub_Header_B", "나. 세부능력 및 특기사항", 15, FontStyles.Bold, TextColor, TextAlignmentOptions.TopLeft, 10f);

        CreateTable(root.transform, new TableSpec
        {
            header = new[] { "과목", "세부능력 및 특기사항" },
            colWidths = new float[] { 110, -1 },
            rows = new[]
            {
                new[] { "AI 데이터 구조", "Edu-Promes AI 맞춤 학습 모듈 이수율 100%를 달성함. 표준 교육과정 전 영역의 최적화 학습을 완벽하게 완수하였으며, 시스템이 제공하는 모든 교과 문제 은행에서 오차율 0.0%를 기록함." },
                new[] { "심화 연계 프로젝트", "대우대 연계 AI 전략경영 기초 모듈을 이수하고 표준 가중치(60%)를 적용받음." },
            }
        });

        // ------------------------------------------------------------------
        // 6. 행동특성 및 종합의견
        // ------------------------------------------------------------------
        CreateSectionHeader(root.transform, "6. 행동특성 및 종합의견");

        CreateTable(root.transform, new TableSpec
        {
            header = new[] { "학년", "행동특성 및 종합의견" },
            colWidths = new float[] { 70, -1 },
            rows = new[]
            {
                new[] { "1학년", "조용하고 단정한 성품으로 학급 규율을 성실히 준수하며 매사 타의 모범이 됨. 교우 관계가 원만하고 주어진 과제를 끝까지 책임감 있게 완수함." },
                new[] { "2학년", "학업 성취와 자기통제력이 탁월하며 학습 패턴이 매우 안정적임. 쉬는 시간이나 자율 활동 시 틈틈이 풍경 소묘와 캐릭터 스케치를 즐겨함." },
            }
        });

        // ------------------------------------------------------------------
        // 저장 전 정리: fallback 서브메시 제거
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

        string path = dir + "/HanSeoA_StudentRecord.prefab";
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);

        GameObject target = Selection.activeGameObject;
        if (target != null)
        {
            instance.transform.SetParent(target.transform, false);
            Debug.Log($"[HanSeoARecordPrefabBuilder] '{target.name}' 하위에 HanSeoA_StudentRecord 인스턴스를 생성했습니다. 프리팹: {path}");
        }
        else
        {
            Debug.Log($"[HanSeoARecordPrefabBuilder] 오브젝트를 선택하지 않아 씬 루트에 생성했습니다. 원본 문서(originalPanel)의 Content 오브젝트 밑으로 옮겨주세요. 프리팹: {path}");
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
        CreateParagraph(parent, "Section_Header", content, 17, FontStyles.Bold, TextColor, TextAlignmentOptions.TopLeft, 10f);
    }

    /// <summary> 표 하나(헤더 행 + 데이터 행들). colWidths의 마지막 값이 -1이면 그 컬럼이 남는 폭을 전부 먹는다(flexible). </summary>
    private static void CreateTable(Transform parent, TableSpec spec)
    {
        GameObject table = CreateBackgroundObject("Table", parent, TableBorderColor);
        LayoutElement tableLe = table.AddComponent<LayoutElement>();
        tableLe.flexibleWidth = 1f;

        VerticalLayoutGroup tableVlg = table.AddComponent<VerticalLayoutGroup>();
        tableVlg.padding = new RectOffset((int)TABLE_PADDING_H, (int)TABLE_PADDING_H, (int)TABLE_PADDING_H, (int)TABLE_PADDING_H);
        tableVlg.spacing = ROW_SPACING;
        tableVlg.childControlWidth = true;
        tableVlg.childControlHeight = true;
        tableVlg.childForceExpandWidth = true;
        tableVlg.childForceExpandHeight = false;

        CreateTableRow(table.transform, spec.header, spec.colWidths, TableHeaderBgColor, FontStyles.Bold, 12, TextAlignmentOptions.Center);

        foreach (string[] row in spec.rows)
        {
            CreateTableRow(table.transform, row, spec.colWidths, TableCellBgColor, FontStyles.Normal, 12, TextAlignmentOptions.TopLeft);
        }
    }

    private static void CreateTableRow(Transform parent, string[] cells, float[] colWidths, Color bgColor, FontStyles style, int fontSize, TextAlignmentOptions bodyAlign)
    {
        GameObject row = CreateBackgroundObject("Row", parent, TableBorderColor);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = ROW_SPACING;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        for (int i = 0; i < cells.Length; i++)
        {
            bool isFlexible = i == colWidths.Length - 1 && colWidths[i] < 0f;
            float width = isFlexible ? 0f : colWidths[i];
            TextAlignmentOptions align = (bodyAlign == TextAlignmentOptions.Center) ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft;
            CreateTableCell(row.transform, cells[i], width, isFlexible, bgColor, style, fontSize, align);
        }
    }

    private static void CreateTableCell(Transform parent, string text, float width, bool isFlexible, Color bgColor, FontStyles style, int fontSize, TextAlignmentOptions align)
    {
        GameObject cell = CreateBackgroundObject("Cell", parent, bgColor);

        LayoutElement cellLe = cell.AddComponent<LayoutElement>();
        if (isFlexible)
        {
            cellLe.flexibleWidth = 1f;
        }
        else
        {
            cellLe.minWidth = width;
            cellLe.preferredWidth = width;
            cellLe.flexibleWidth = 0f;
        }

        VerticalLayoutGroup cellVlg = cell.AddComponent<VerticalLayoutGroup>();
        cellVlg.padding = new RectOffset((int)CELL_PADDING, (int)CELL_PADDING, (int)CELL_PADDING, (int)CELL_PADDING);
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
