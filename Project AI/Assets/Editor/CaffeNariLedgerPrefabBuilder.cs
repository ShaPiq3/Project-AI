// CaffeNariLedgerPrefabBuilder.cs
// -----------------------------------------------------------------------------
// 사용법
// 1) 이 파일은 Assets/Editor 폴더 안에 있어야 합니다.
// 2) (선택) Hierarchy 창에서 원하는 부모 오브젝트(예: DocuGame 패널의 Content)를
//    선택해두면 바로 그 밑에 인스턴스가 생성됩니다. 선택 안 해도 됩니다.
// 3) 상단 메뉴 Tools > Promes > Create Caffe Nari Ledger Prefab 클릭
// 4) Assets/Prefabs/UI/Caffe_Nari_Ledger_Panel.prefab 로 프리팹이 저장되고,
//    선택했던 오브젝트 밑에 인스턴스가 바로 생성됩니다.
//
// 표/카드 안 숫자(문구)는 스크린샷 기준으로 하드코딩되어 있습니다.
// 나중에 다른 매장/기간 데이터로 바꾸고 싶으면 이 스크립트의 Rows 배열과
// 상단 카드 3개의 텍스트 값을 고치고 다시 실행하거나, 생성된 프리팹의
// TextMeshProUGUI 값을 인스펙터에서 직접 수정하면 됩니다.
// -----------------------------------------------------------------------------

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CaffeNariLedgerPrefabBuilder
{
    // ---- 색상 정의 (스크린샷 기준 근사치, Inspector에서 자유롭게 조정 가능) ----
    private static readonly Color PageBgColor = new Color(0.94f, 0.96f, 0.975f, 1f);   // 바깥 배경
    private static readonly Color BorderColor = new Color(0.56f, 0.85f, 0.90f, 1f);    // 박스 테두리(청록)
    private static readonly Color BoxFillColor = new Color(0.87f, 0.965f, 0.975f, 1f); // 박스 채우기(연한 청록)
    private static readonly Color TealColor = new Color(0.12f, 0.70f, 0.77f, 1f);      // 뱃지/강조/그래프 채움
    private static readonly Color TextDarkColor = new Color(0.13f, 0.17f, 0.22f, 1f);
    private static readonly Color TextGrayColor = new Color(0.45f, 0.49f, 0.56f, 1f);
    private static readonly Color TrackGrayColor = new Color(0.85f, 0.88f, 0.91f, 1f);

    private const float ROOT_PADDING = 20f;
    private const float SECTION_SPACING = 16f;
    private const float BORDER_WIDTH = 2f;
    private const float BOX_PADDING = 16f;

    // 표 칸 너비 (표 박스 안쪽 기준)
    private const float COL_NO = 40f;
    private const float COL_TIME = 170f;
    private const float COL_COUNT = 100f;
    private const float COL_AMOUNT = 110f;
    private const float COL_SHARE_PERCENT_TEXT = 40f;
    private const float COL_SHARE_BAR = 116f;
    private const float ROW_COL_SPACING = 10f;

    private struct Row
    {
        public string time, count, amount;
        public float percent;
        public bool isPeak;
        public Row(string time, string count, string amount, float percent, bool isPeak)
        {
            this.time = time; this.count = count; this.amount = amount;
            this.percent = percent; this.isPeak = isPeak;
        }
    }

    private static readonly Row[] Rows = new Row[]
    {
        new Row("08:00 ~ 10:30", "8건",  "32,000원", 20.0f, false),
        new Row("10:30 ~ 11:30", "1건",  "5,000원",   3.0f, false),
        new Row("11:30 ~ 14:00", "13건", "50,000원", 31.0f, true),
        new Row("14:00 ~ 16:30", "12건", "51,000원", 32.0f, true),
        new Row("16:30 ~ 18:30", "4건",  "16,000원", 10.0f, false),
        new Row("18:30 ~ 20:00", "2건",  "6,000원",   4.0f, false),
    };

    private const float MAX_PERCENT_FOR_BAR = 32.0f; // 가장 높은 점유율 = 막대 꽉 참 기준

    [MenuItem("Tools/Promes/Create Caffe Nari Ledger Prefab")]
    public static void CreatePrefab()
    {
        // ------------------------------------------------------------------
        // 루트 오브젝트 (배경까지 포함하는 패널 전체)
        // ------------------------------------------------------------------
        GameObject root = new GameObject("Caffe_Nari_Ledger_Panel", typeof(RectTransform), typeof(Image));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(0.5f, 1f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = Vector2.zero;

        Image rootImg = root.GetComponent<Image>();
        rootImg.color = PageBgColor;
        rootImg.raycastTarget = false;

        VerticalLayoutGroup rootVlg = root.AddComponent<VerticalLayoutGroup>();
        rootVlg.padding = new RectOffset((int)ROOT_PADDING, (int)ROOT_PADDING, (int)ROOT_PADDING, (int)ROOT_PADDING);
        rootVlg.spacing = SECTION_SPACING;
        rootVlg.childAlignment = TextAnchor.UpperLeft;
        rootVlg.childControlWidth = true;
        rootVlg.childControlHeight = true;
        rootVlg.childForceExpandWidth = true;
        rootVlg.childForceExpandHeight = false;

        ContentSizeFitter rootFitter = root.AddComponent<ContentSizeFitter>();
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ------------------------------------------------------------------
        // 1) 타이틀
        // ------------------------------------------------------------------
        BuildTitleRow(root.transform, "Caffe 나리 장부");

        // ------------------------------------------------------------------
        // 2) 조회 조건 정보 바
        // ------------------------------------------------------------------
        BuildInfoBar(root.transform,
            "조회 기간: 최근 90일 평균",
            "매장: Caffe 나리 (12평/1인)",
            "기준: 평일 영업일");

        // ------------------------------------------------------------------
        // 3) 요약 카드 3개
        // ------------------------------------------------------------------
        BuildStatCardsRow(root.transform);

        // ------------------------------------------------------------------
        // 4) 시간대별 표
        // ------------------------------------------------------------------
        BuildTable(root.transform);

        // ------------------------------------------------------------------
        // 5) 하단 합계 바
        // ------------------------------------------------------------------
        BuildSummaryBar(root.transform);

        // ------------------------------------------------------------------
        // 프리팹으로 저장
        // ------------------------------------------------------------------
        string dir = "Assets/Prefabs/UI";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = dir + "/Caffe_Nari_Ledger_Panel.prefab";
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);

        GameObject target = Selection.activeGameObject;
        if (target != null)
        {
            instance.transform.SetParent(target.transform, false);
            Debug.Log($"[CaffeNariLedgerPrefabBuilder] '{target.name}' 하위에 Caffe_Nari_Ledger_Panel 인스턴스를 생성했습니다. 프리팹: {path}");
        }
        else
        {
            Debug.Log($"[CaffeNariLedgerPrefabBuilder] 오브젝트를 선택하지 않아 씬 루트에 생성했습니다. 원하는 부모 밑으로 옮겨주세요. 프리팹: {path}");
        }

        Selection.activeGameObject = instance;
        EditorGUIUtility.PingObject(prefabAsset);
    }

    // =====================================================================
    // 섹션 빌더
    // =====================================================================

    private static void BuildTitleRow(Transform parent, string title)
    {
        GameObject row = new GameObject("Title_Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        GameObject bar = CreateBackgroundObject("Accent_Bar", row.transform, TealColor);
        LayoutElement barLe = bar.AddComponent<LayoutElement>();
        barLe.minWidth = 6f; barLe.preferredWidth = 6f; barLe.flexibleWidth = 0f;
        barLe.minHeight = 24f; barLe.preferredHeight = 24f; barLe.flexibleHeight = 0f;

        TextMeshProUGUI titleText = CreateText(row.transform, title, 22, FontStyles.Bold, TextDarkColor, TextAlignmentOptions.MidlineLeft);
        LayoutElement titleLe = titleText.gameObject.AddComponent<LayoutElement>();
        titleLe.flexibleWidth = 1f;
    }

    private static void BuildInfoBar(Transform parent, string item1, string item2, string item3)
    {
        GameObject outer = CreateRoundedImage("InfoBar_Border", parent, BorderColor);
        VerticalLayoutGroup outerVlg = outer.AddComponent<VerticalLayoutGroup>();
        int bw = (int)BORDER_WIDTH;
        outerVlg.padding = new RectOffset(bw, bw, bw, bw);
        outerVlg.childControlWidth = true; outerVlg.childControlHeight = true;
        outerVlg.childForceExpandWidth = true; outerVlg.childForceExpandHeight = true;
        ContentSizeFitter outerFitter = outer.AddComponent<ContentSizeFitter>();
        outerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject inner = CreateRoundedImage("InfoBar_Fill", outer.transform, BoxFillColor);
        HorizontalLayoutGroup innerHlg = inner.AddComponent<HorizontalLayoutGroup>();
        innerHlg.padding = new RectOffset(18, 18, 11, 11);
        innerHlg.spacing = 16f;
        innerHlg.childAlignment = TextAnchor.MiddleLeft;
        innerHlg.childControlWidth = true; innerHlg.childControlHeight = true;
        innerHlg.childForceExpandWidth = false; innerHlg.childForceExpandHeight = true;

        string[] items = { item1, item2, item3 };
        for (int i = 0; i < items.Length; i++)
        {
            TextMeshProUGUI t = CreateText(inner.transform, items[i], 13, FontStyles.Normal, TextGrayColor, TextAlignmentOptions.MidlineLeft);
            LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 0f;

            if (i < items.Length - 1)
            {
                GameObject divider = CreateBackgroundObject("Divider", inner.transform, BorderColor);
                LayoutElement dividerLe = divider.AddComponent<LayoutElement>();
                dividerLe.minWidth = 1.5f; dividerLe.preferredWidth = 1.5f; dividerLe.flexibleWidth = 0f;
                dividerLe.preferredHeight = 16f; dividerLe.flexibleHeight = 0f;
            }
        }
    }

    private static void BuildStatCardsRow(Transform parent)
    {
        GameObject row = new GameObject("Stat_Cards_Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        BuildStatCard(row.transform, "일평균 주문 건수", "기본", "40", " 건", 30);
        BuildStatCard(row.transform, "일평균 매출액", null, "160,000", " 원", 30);
        BuildStatCard(row.transform, "최고 매출 시간대", "피크타임", "11:30~14:00", "", 22);
    }

    private static void BuildStatCard(Transform parent, string label, string badge, string valueMain, string valueSuffix, int valueFontSize)
    {
        GameObject outer = CreateRoundedImage("Card_" + label + "_Border", parent, BorderColor);
        LayoutElement outerLe = outer.AddComponent<LayoutElement>();
        outerLe.flexibleWidth = 1f;

        int bw = (int)BORDER_WIDTH;
        VerticalLayoutGroup outerVlg = outer.AddComponent<VerticalLayoutGroup>();
        outerVlg.padding = new RectOffset(bw, bw, bw, bw);
        outerVlg.childControlWidth = true; outerVlg.childControlHeight = true;
        outerVlg.childForceExpandWidth = true; outerVlg.childForceExpandHeight = true;
        ContentSizeFitter outerFitter = outer.AddComponent<ContentSizeFitter>();
        outerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject inner = CreateRoundedImage("Card_" + label + "_Fill", outer.transform, BoxFillColor);
        VerticalLayoutGroup innerVlg = inner.AddComponent<VerticalLayoutGroup>();
        innerVlg.padding = new RectOffset((int)BOX_PADDING, (int)BOX_PADDING, 14, 14);
        innerVlg.spacing = 10f;
        innerVlg.childControlWidth = true; innerVlg.childControlHeight = true;
        innerVlg.childForceExpandWidth = true; innerVlg.childForceExpandHeight = false;
        ContentSizeFitter innerFitter = inner.AddComponent<ContentSizeFitter>();
        innerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 라벨 + 뱃지 행
        GameObject topRow = new GameObject("Top_Row", typeof(RectTransform));
        topRow.transform.SetParent(inner.transform, false);
        HorizontalLayoutGroup topHlg = topRow.AddComponent<HorizontalLayoutGroup>();
        topHlg.spacing = 8f;
        topHlg.childAlignment = TextAnchor.MiddleLeft;
        topHlg.childControlWidth = true; topHlg.childControlHeight = true;
        topHlg.childForceExpandWidth = false; topHlg.childForceExpandHeight = false;

        TextMeshProUGUI labelText = CreateText(topRow.transform, label, 13, FontStyles.Bold, TextGrayColor, TextAlignmentOptions.MidlineLeft);
        LayoutElement labelLe = labelText.gameObject.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1f;

        if (!string.IsNullOrEmpty(badge))
        {
            CreateBadge(topRow.transform, badge);
        }

        // 값 텍스트
        string rich = valueSuffix.Length > 0
            ? $"{valueMain}<size=60%><color=#{ColorUtility.ToHtmlStringRGB(TextGrayColor)}>{valueSuffix}</color></size>"
            : valueMain;
        CreateText(inner.transform, rich, valueFontSize, FontStyles.Bold, TextDarkColor, TextAlignmentOptions.MidlineLeft);
    }

    private static void BuildTable(Transform parent)
    {
        GameObject outer = CreateRoundedImage("Table_Border", parent, BorderColor);
        int bw = (int)BORDER_WIDTH;
        VerticalLayoutGroup outerVlg = outer.AddComponent<VerticalLayoutGroup>();
        outerVlg.padding = new RectOffset(bw, bw, bw, bw);
        outerVlg.childControlWidth = true; outerVlg.childControlHeight = true;
        outerVlg.childForceExpandWidth = true; outerVlg.childForceExpandHeight = true;
        ContentSizeFitter outerFitter = outer.AddComponent<ContentSizeFitter>();
        outerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject inner = CreateRoundedImage("Table_Fill", outer.transform, BoxFillColor);
        VerticalLayoutGroup innerVlg = inner.AddComponent<VerticalLayoutGroup>();
        innerVlg.padding = new RectOffset((int)BOX_PADDING, (int)BOX_PADDING, 14, 6);
        innerVlg.spacing = 0f;
        innerVlg.childControlWidth = true; innerVlg.childControlHeight = true;
        innerVlg.childForceExpandWidth = true; innerVlg.childForceExpandHeight = false;
        ContentSizeFitter innerFitter = inner.AddComponent<ContentSizeFitter>();
        innerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 헤더 행
        BuildTableRow(inner.transform, "NO", "시간대", "일평균 건수", "일평균 매출", "매출 비중 (점유율)",
            FontStyles.Bold, 12, TextGrayColor, -1f, false, true);

        BuildDivider(inner.transform, 10f);

        // 데이터 행
        for (int i = 0; i < Rows.Length; i++)
        {
            Row r = Rows[i];
            BuildTableRow(inner.transform, (i + 1).ToString(), r.time, r.count, r.amount, r.percent + "%",
                FontStyles.Normal, 13, TextDarkColor, r.percent, r.isPeak, false);

            if (i < Rows.Length - 1)
                BuildDivider(inner.transform, 12f);
        }
    }

    private static void BuildDivider(Transform parent, float verticalMargin)
    {
        GameObject wrap = new GameObject("Divider_Wrap", typeof(RectTransform));
        wrap.transform.SetParent(parent, false);
        VerticalLayoutGroup vlg = wrap.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, (int)(verticalMargin / 2f), (int)(verticalMargin / 2f));
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        GameObject line = CreateBackgroundObject("Line", wrap.transform, BorderColor);
        LayoutElement le = line.AddComponent<LayoutElement>();
        le.preferredHeight = 1f; le.flexibleHeight = 0f;
    }

    // percent < 0 이면 헤더 행(막대바 없이 라벨 텍스트만 표시)
    private static void BuildTableRow(Transform parent, string no, string time, string count, string amount, string shareLabel,
        FontStyles style, int fontSize, Color color, float percent, bool isPeak, bool isHeader)
    {
        GameObject row = new GameObject(isHeader ? "Header_Row" : "Data_Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = ROW_COL_SPACING;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = isHeader ? 24f : 40f;
        rowLe.flexibleHeight = 0f;

        CreateFixedCell(row.transform, no, COL_NO, style, fontSize, color, TextAlignmentOptions.Center);

        // 시간대 칸 (PEAK 뱃지 포함 가능)
        GameObject timeCell = new GameObject("Cell_Time", typeof(RectTransform));
        timeCell.transform.SetParent(row.transform, false);
        LayoutElement timeCellLe = timeCell.AddComponent<LayoutElement>();
        timeCellLe.minWidth = COL_TIME; timeCellLe.preferredWidth = COL_TIME; timeCellLe.flexibleWidth = 0f;
        HorizontalLayoutGroup timeHlg = timeCell.AddComponent<HorizontalLayoutGroup>();
        timeHlg.spacing = 6f;
        timeHlg.childAlignment = TextAnchor.MiddleLeft;
        timeHlg.childControlWidth = true; timeHlg.childControlHeight = true;
        timeHlg.childForceExpandWidth = false; timeHlg.childForceExpandHeight = true;

        TextMeshProUGUI timeText = CreateText(timeCell.transform, time, fontSize, style, color, TextAlignmentOptions.MidlineLeft);
        LayoutElement timeTextLe = timeText.gameObject.AddComponent<LayoutElement>();
        timeTextLe.flexibleWidth = 0f;
        if (isPeak) CreateBadge(timeCell.transform, "PEAK");

        CreateFixedCell(row.transform, count, COL_COUNT, style, fontSize, color, TextAlignmentOptions.MidlineRight);
        CreateFixedCell(row.transform, amount, COL_AMOUNT, style, fontSize, color, TextAlignmentOptions.MidlineRight);

        // 매출 비중 칸 (퍼센트 텍스트 + 막대)
        GameObject shareCell = new GameObject("Cell_Share", typeof(RectTransform));
        shareCell.transform.SetParent(row.transform, false);
        LayoutElement shareCellLe = shareCell.AddComponent<LayoutElement>();
        shareCellLe.flexibleWidth = 1f;
        HorizontalLayoutGroup shareHlg = shareCell.AddComponent<HorizontalLayoutGroup>();
        shareHlg.spacing = 10f;
        shareHlg.childAlignment = TextAnchor.MiddleLeft;
        shareHlg.childControlWidth = true; shareHlg.childControlHeight = true;
        shareHlg.childForceExpandWidth = false; shareHlg.childForceExpandHeight = true;

        CreateFixedCell(shareCell.transform, shareLabel, COL_SHARE_PERCENT_TEXT, style, fontSize,
            isHeader ? color : TealColor, isHeader ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight);

        if (!isHeader)
        {
            CreateProgressBar(shareCell.transform, COL_SHARE_BAR, COL_SHARE_BAR * (percent / MAX_PERCENT_FOR_BAR), 8f);
        }
    }

    private static void BuildSummaryBar(Transform parent)
    {
        GameObject outer = CreateRoundedImage("Summary_Border", parent, BorderColor);
        int bw = (int)BORDER_WIDTH;
        VerticalLayoutGroup outerVlg = outer.AddComponent<VerticalLayoutGroup>();
        outerVlg.padding = new RectOffset(bw, bw, bw, bw);
        outerVlg.childControlWidth = true; outerVlg.childControlHeight = true;
        outerVlg.childForceExpandWidth = true; outerVlg.childForceExpandHeight = true;
        ContentSizeFitter outerFitter = outer.AddComponent<ContentSizeFitter>();
        outerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject inner = CreateRoundedImage("Summary_Fill", outer.transform, BoxFillColor);
        VerticalLayoutGroup innerVlg = inner.AddComponent<VerticalLayoutGroup>();
        innerVlg.padding = new RectOffset((int)BOX_PADDING, (int)BOX_PADDING, 14, 14);
        innerVlg.spacing = 8f;
        innerVlg.childControlWidth = true; innerVlg.childControlHeight = true;
        innerVlg.childForceExpandWidth = true; innerVlg.childForceExpandHeight = false;
        ContentSizeFitter innerFitter = inner.AddComponent<ContentSizeFitter>();
        innerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject topRow = new GameObject("Top_Row", typeof(RectTransform));
        topRow.transform.SetParent(inner.transform, false);
        HorizontalLayoutGroup topHlg = topRow.AddComponent<HorizontalLayoutGroup>();
        topHlg.childAlignment = TextAnchor.MiddleLeft;
        topHlg.childControlWidth = true; topHlg.childControlHeight = true;
        topHlg.childForceExpandWidth = false; topHlg.childForceExpandHeight = false;

        TextMeshProUGUI totalLabel = CreateText(topRow.transform, "하단 합계", 13, FontStyles.Bold, TextGrayColor, TextAlignmentOptions.MidlineLeft);
        LayoutElement totalLabelLe = totalLabel.gameObject.AddComponent<LayoutElement>();
        totalLabelLe.flexibleWidth = 1f;

        CreateBadge(topRow.transform, "요약");

        string grayHex = ColorUtility.ToHtmlStringRGB(TextGrayColor);
        string bigRich = $"40건 / 160,000원 <size=65%><color=#{grayHex}>(월 환산 약 480만 원)</color></size>";
        CreateText(inner.transform, bigRich, 20, FontStyles.Bold, TextDarkColor, TextAlignmentOptions.MidlineLeft);

        CreateText(inner.transform, "임대료·원가 제외 시 적자", 12, FontStyles.Normal, TextGrayColor, TextAlignmentOptions.MidlineLeft);
    }

    // =====================================================================
    // 공용 Helper 함수들
    // =====================================================================

    private static Sprite _roundedSprite;
    private static Sprite RoundedSprite
    {
        get
        {
            if (_roundedSprite == null)
                _roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            return _roundedSprite;
        }
    }

    private static GameObject CreateRoundedImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = RoundedSprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    private static GameObject CreateBackgroundObject(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string content, int fontSize, FontStyles style, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    // 고정 너비 칸 (표에서 사용)
    private static void CreateFixedCell(Transform parent, string text, float width, FontStyles style, int fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject cell = new GameObject("Cell", typeof(RectTransform));
        cell.transform.SetParent(parent, false);
        LayoutElement le = cell.AddComponent<LayoutElement>();
        le.minWidth = width; le.preferredWidth = width; le.flexibleWidth = 0f;

        RectTransform rt = cell.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;

        TextMeshProUGUI tmp = CreateText(cell.transform, text, fontSize, style, color, align);
        RectTransform textRt = tmp.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;
    }

    private static void CreateBadge(Transform parent, string text)
    {
        GameObject badge = CreateRoundedImage("Badge", parent, TealColor);
        LayoutElement le = badge.AddComponent<LayoutElement>();
        le.flexibleWidth = 0f; le.flexibleHeight = 0f;

        HorizontalLayoutGroup hlg = badge.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 3, 3);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = badge.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText(badge.transform, text, 11, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
    }

    private static void CreateProgressBar(Transform parent, float trackWidth, float fillWidth, float height)
    {
        GameObject track = CreateRoundedImage("Track", parent, TrackGrayColor);
        LayoutElement trackLe = track.AddComponent<LayoutElement>();
        trackLe.minWidth = trackWidth; trackLe.preferredWidth = trackWidth; trackLe.flexibleWidth = 0f;
        trackLe.minHeight = height; trackLe.preferredHeight = height; trackLe.flexibleHeight = 0f;

        GameObject fill = CreateRoundedImage("Fill", track.transform, TealColor);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = Vector2.zero;
        fillRt.sizeDelta = new Vector2(Mathf.Clamp(fillWidth, 0f, trackWidth), 0f);
    }
}
