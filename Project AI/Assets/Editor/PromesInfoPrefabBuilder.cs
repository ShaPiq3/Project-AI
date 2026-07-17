// PromesInfoPrefabBuilder.cs
// -----------------------------------------------------------------------------
// 사용법
// 1) 이 파일을 프로젝트의 "Assets/Editor" 폴더 안에 넣습니다. (없으면 새로 생성)
// 2) (선택) 로고 이미지를 Assets/Resources/UI/Promes_Logo.png 로 복사하고
//    Texture Type을 "Sprite (2D and UI)" 로 설정합니다.
//    -> 이렇게 해두면 스크립트가 자동으로 로고를 채워 넣습니다.
//    안 해도 동작은 하며, 로고 자리에는 빈 회색 박스가 생성되고 나중에
//    직접 Image 슬롯에 로고 스프라이트를 드래그해 넣으면 됩니다.
// 3) Hierarchy 창에서 Promes_Docu_Panel > Scroll View > Viewport > Content
//    오브젝트를 선택합니다. (선택 안 해도 되지만, 선택하면 바로 그 밑에
//    생성물이 자동으로 들어갑니다)
// 4) 상단 메뉴 Tools > Promes > Create Promes Info Prefab 클릭
// 5) Assets/Prefabs/UI/Promes_Info.prefab 로 프리팹이 저장되고,
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

public static class PromesInfoPrefabBuilder
{
    // ---- 색상 정의 (스크린샷 기준 근사치, Inspector에서 자유롭게 조정 가능) ----
    private static readonly Color BorderColor = new Color(0.56f, 0.85f, 0.90f, 1f);   // 표 테두리(청록)
    private static readonly Color HeaderBgColor = new Color(0.80f, 0.96f, 0.98f, 1f); // "프로메스" 타이틀 행 배경
    private static readonly Color CellBgColor = Color.white;                          // 일반 셀 배경
    private static readonly Color TextColor = new Color(0.13f, 0.13f, 0.13f, 1f);

    private const float TABLE_WIDTH = 698f;      // Scroll View content 너비에 맞춤
    private const float LABEL_COL_WIDTH = 130f;  // 좌측 라벨 칸 너비

    [MenuItem("Tools/Promes/Create Promes Info Prefab")]
    public static void CreatePrefab()
    {
        // ------------------------------------------------------------------
        // 루트 오브젝트
        // ------------------------------------------------------------------
        GameObject root = new GameObject("Promes_Info", typeof(RectTransform));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(0.5f, 1f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = new Vector2(0f, 0f); // 너비는 부모에 맞춰 스트레치, 높이는 아래 Fitter가 계산

        VerticalLayoutGroup rootVlg = root.AddComponent<VerticalLayoutGroup>();
        rootVlg.padding = new RectOffset(0, 0, 10, 20);
        rootVlg.spacing = 14f;
        rootVlg.childAlignment = TextAnchor.UpperLeft;
        rootVlg.childControlWidth = true;
        rootVlg.childControlHeight = true;
        rootVlg.childForceExpandWidth = true;
        rootVlg.childForceExpandHeight = false;

        ContentSizeFitter rootFitter = root.AddComponent<ContentSizeFitter>();
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ------------------------------------------------------------------
        // 표 (Table)
        // ------------------------------------------------------------------
        GameObject table = CreateBackgroundObject("Table", root.transform, BorderColor);
        VerticalLayoutGroup tableVlg = table.AddComponent<VerticalLayoutGroup>();
        tableVlg.padding = new RectOffset(1, 1, 1, 1);
        tableVlg.spacing = 1f;
        tableVlg.childControlWidth = true;
        tableVlg.childControlHeight = true;
        tableVlg.childForceExpandWidth = true;
        tableVlg.childForceExpandHeight = false;

        // 타이틀 행 ("프로메스")
        CreateSingleCellRow(table.transform, "프로메스", 40f, HeaderBgColor, 18, FontStyles.Bold, TextAlignmentOptions.Center);

        // 로고 행
        CreateLogoRow(table.transform, 240f);

        // 데이터 행들
        CreateDataRow(table.transform, "국가", "대한민국", 32f);
        CreateDataRow(table.transform, "설립일", "2001년 4월 9일", 32f);
        CreateDataRow(table.transform, "창업주", "심영환", 32f);
        CreateDataRow(table.transform, "대표자", "심영환 (대표이사)", 32f);
        CreateDataRow(table.transform, "업종", "IT, AI 서비스업, 생명과학, 제약, 로봇 제조업", 50f);
        CreateDataRow(table.transform, "유형", "대기업", 32f);
        CreateDataRow(table.transform, "매출액", "약 300조원", 32f);
        CreateDataRow(table.transform, "영업이익", "약 124조원", 32f);
        CreateDataRow(table.transform, "시가 총액", "약 1080조원", 32f);
        CreateDataRow(table.transform, "고용 인원", "약 1500명", 32f);
        CreateDataRow(table.transform, "소재지", "서울시 금천구 가산동 프로메스 타워", 40f);

        // ------------------------------------------------------------------
        // "개요" 헤더 + 문단 텍스트
        // ------------------------------------------------------------------
        CreateParagraph(root.transform, "개요", 20, FontStyles.Bold, 30f);

        CreateParagraph(root.transform, "프로메스(Promes)는 대한민국의 종합 IT 기업이다.", 15, FontStyles.Normal, 0f);

        CreateParagraph(root.transform,
            "검색 포털 서비스를 기반으로 성장하였으며 2010년대 중반 인공지능 서비스를 시작하면서 인공지능 서비스 이용률의 90% 이상을 독점, 전 세계 최대 규모의 인공지능 서비스 기업으로 자리매김하였다.",
            15, FontStyles.Normal, 0f);

        CreateParagraph(root.transform,
            "2020년대 이후로는 생명과학과 제약, 로봇 등 다양한 산업 분야의 회사들을 인수하여 기술 산업 그룹으로서의 입지를 다지고 있다.",
            15, FontStyles.Normal, 0f);

        // ------------------------------------------------------------------
        // 프리팹으로 저장
        // ------------------------------------------------------------------
        string dir = "Assets/Prefabs/UI";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = dir + "/Promes_Info.prefab";
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);

        GameObject target = Selection.activeGameObject;
        if (target != null)
        {
            instance.transform.SetParent(target.transform, false);
            Debug.Log($"[PromesInfoPrefabBuilder] '{target.name}' 하위에 Promes_Info 인스턴스를 생성했습니다. 프리팹: {path}");
        }
        else
        {
            Debug.Log($"[PromesInfoPrefabBuilder] 오브젝트를 선택하지 않아 씬 루트에 생성했습니다. Content 오브젝트 밑으로 옮겨주세요. 프리팹: {path}");
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

    private static TextMeshProUGUI CreateText(Transform parent, string content, int fontSize, FontStyles style, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(8f, 2f);
        rt.offsetMax = new Vector2(-8f, -2f);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = TextColor;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    // 표 안의 한 줄짜리(라벨 없이 전체 폭 사용) 행 - 타이틀 행에 사용
    private static void CreateSingleCellRow(Transform parent, string text, float height, Color bg, int fontSize, FontStyles style, TextAlignmentOptions align)
    {
        GameObject row = CreateBackgroundObject("Row_Title", parent, bg);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleHeight = 0f;

        CreateText(row.transform, text, fontSize, style, align);
    }

    // 로고 전용 행
    private static void CreateLogoRow(Transform parent, float height)
    {
        GameObject row = CreateBackgroundObject("Row_Logo", parent, CellBgColor);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleHeight = 0f;

        GameObject logoGo = new GameObject("Logo_Image", typeof(RectTransform), typeof(Image));
        logoGo.transform.SetParent(row.transform, false);
        RectTransform rt = logoGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(height - 40f, height - 40f);
        rt.anchoredPosition = Vector2.zero;

        Image logoImg = logoGo.GetComponent<Image>();
        logoImg.preserveAspect = true;
        logoImg.raycastTarget = false;

        // Resources/UI/Promes_Logo.png (Sprite) 가 있으면 자동으로 채워 넣음
        Sprite logoSprite = Resources.Load<Sprite>("UI/Promes_Logo");
        if (logoSprite != null)
        {
            logoImg.sprite = logoSprite;
            logoImg.color = Color.white;
        }
        else
        {
            // 못 찾으면 자리표시용 옅은 회색 박스로 남겨두고, 나중에 수동으로 스프라이트를 넣으면 됨
            logoImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        }
    }

    // 라벨 | 값 2칸짜리 데이터 행
    private static void CreateDataRow(Transform parent, string label, string value, float height)
    {
        GameObject row = CreateBackgroundObject("Row_" + label, parent, BorderColor);
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = height;
        rowLe.flexibleHeight = 0f;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.spacing = 1f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // 라벨 셀
        GameObject labelCell = CreateBackgroundObject("Label", row.transform, CellBgColor);
        LayoutElement labelLe = labelCell.AddComponent<LayoutElement>();
        labelLe.preferredWidth = LABEL_COL_WIDTH;
        labelLe.flexibleWidth = 0f;
        CreateText(labelCell.transform, label, 14, FontStyles.Bold, TextAlignmentOptions.Center);

        // 값 셀
        GameObject valueCell = CreateBackgroundObject("Value", row.transform, CellBgColor);
        LayoutElement valueLe = valueCell.AddComponent<LayoutElement>();
        valueLe.flexibleWidth = 1f;
        CreateText(valueCell.transform, value, 14, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
    }

    // 표 아래 문단 (개요 헤더 및 본문 3개)
    private static void CreateParagraph(Transform parent, string content, int fontSize, FontStyles style, float topSpacing)
    {
        GameObject go = new GameObject(style == FontStyles.Bold ? "Header_Text" : "Paragraph_Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        if (topSpacing > 0f)
        {
            // VerticalLayoutGroup spacing으로 대체 가능하지만, 개요 헤더 전용 여백을 주고 싶을 때 사용
            VerticalLayoutGroup marginVlg = parent.GetComponent<VerticalLayoutGroup>();
            if (marginVlg != null)
            {
                // 별도 여백 오브젝트 대신 padding-top 느낌으로 LayoutElement minHeight를 살짝 키움
            }
        }

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = TextColor;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.margin = new Vector4(4f, topSpacing, 4f, 0f);
    }
}
