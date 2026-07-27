// PromesGuidePrefabBuilder.cs
// -----------------------------------------------------------------------------
// 사용법
// 1) 이 파일을 프로젝트의 "Assets/Editor" 폴더 안에 넣습니다. (없으면 새로 생성)
// 2) (선택) 스텝별 스크린샷 이미지를 아래 경로에 Sprite로 넣어두면 자동으로 채워집니다.
//      Assets/Resources/UI/Guide/Step1_Prompt.png   (프롬프트 박스 스크린샷)
//      Assets/Resources/UI/Guide/Step2_Sidebar.png  (사이드바 스크린샷)
//      Assets/Resources/UI/Guide/Step3_Panel.png    (패널 스크린샷)
//    Texture Type은 "Sprite (2D and UI)" 로 설정해주세요.
//    안 넣어도 동작하며, 회색 자리표시 박스가 생성되고 나중에 Image 슬롯에
//    직접 드래그해 넣으면 됩니다.
// 3) Hierarchy 창에서 이 프리팹이 들어갈 Scroll View > Viewport > Content
//    오브젝트를 선택합니다. (선택 안 해도 되지만, 선택하면 바로 그 밑에
//    생성물이 자동으로 들어갑니다)
// 4) 상단 메뉴 Tools > Promes > Create Guide Doc Prefab 클릭
// 5) Assets/Prefabs/UI/Promes_Guide_DataCollect.prefab 로 프리팹이 저장되고,
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

public static class PromesGuidePrefabBuilder
{
    private const float CONTENT_WIDTH = 698f; // Scroll View content 너비에 맞춤
    private const float ROOT_PADDING_H = 24f;
    private const float NUMBER_COL_WIDTH = 26f;
    private const float STEP_SPACING = 14f;
    private const float IMAGE_LEFT_INDENT = NUMBER_COL_WIDTH + 6f; // 본문 텍스트 시작 위치에 맞춤

    private static readonly Color TitleColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    private static readonly Color BodyColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    private static readonly Color BrandColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    private static readonly Color PlaceholderColor = new Color(0.88f, 0.9f, 0.91f, 1f);

    private struct Step
    {
        public string text;
        public string resourcePath;
        public float imgWidth, imgHeight;
        public Step(string text, string resourcePath, float w, float h)
        {
            this.text = text; this.resourcePath = resourcePath; imgWidth = w; imgHeight = h;
        }
    }

    private static readonly Step[] Steps = new Step[]
    {
        new Step("프롬프트에서 핵심 키워드를 파악합니다.", "UI/Guide/Step1_Prompt", 460f, 145f),
        new Step("사이드바를 통해 데이터베이스에 접근할 수 있습니다.", "UI/Guide/Step2_Sidebar", 230f, 205f),
        new Step("데이터 수집 모드를 켠 상태로 이미지나 문장을 클릭하면 데이터를 수집할 수 있습니다.", "UI/Guide/Step3_Panel", 330f, 345f),
    };

    [MenuItem("Tools/Promes/Create Guide Doc Prefab")]
    public static void CreatePrefab()
    {
        // ------------------------------------------------------------------
        // 루트 오브젝트
        // ------------------------------------------------------------------
        GameObject root = new GameObject("Promes_Guide_DataCollect", typeof(RectTransform));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(0.5f, 1f);
        rootRt.anchoredPosition = Vector2.zero;
        rootRt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup rootVlg = root.AddComponent<VerticalLayoutGroup>();
        rootVlg.padding = new RectOffset((int)ROOT_PADDING_H, (int)ROOT_PADDING_H, 20, 24);
        rootVlg.spacing = 18f;
        rootVlg.childControlWidth = true;
        rootVlg.childControlHeight = true;
        rootVlg.childForceExpandWidth = true;
        rootVlg.childForceExpandHeight = false;

        ContentSizeFitter rootFitter = root.AddComponent<ContentSizeFitter>();
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ------------------------------------------------------------------
        // "PROMES" 브랜드 워드마크 (우측 정렬)
        // ------------------------------------------------------------------
        TextMeshProUGUI brand = CreateText(root.transform, "Brand_Wordmark", "PROMES", 15, FontStyles.Bold, BrandColor, TextAlignmentOptions.TopRight);
        brand.characterSpacing = 2f;

        // ------------------------------------------------------------------
        // 제목
        // ------------------------------------------------------------------
        CreateText(root.transform, "Title", "[데이터 수집 및 답변 생성]", 19, FontStyles.Bold, TitleColor, TextAlignmentOptions.Top);

        // ------------------------------------------------------------------
        // 안내 문구
        // ------------------------------------------------------------------
        CreateText(root.transform, "Intro", "사용자의 질문을 분석하여 정확한 정보를 전달하십시오.", 14, FontStyles.Normal, BodyColor, TextAlignmentOptions.TopLeft);

        // ------------------------------------------------------------------
        // 번호 목록 (스텝 1~3)
        // ------------------------------------------------------------------
        for (int i = 0; i < Steps.Length; i++)
        {
            CreateStep(root.transform, i + 1, Steps[i]);
        }

        // ------------------------------------------------------------------
        // 프리팹으로 저장
        // ------------------------------------------------------------------
        string dir = "Assets/Prefabs/UI";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string path = dir + "/Promes_Guide_DataCollect.prefab";
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);

        GameObject target = Selection.activeGameObject;
        if (target != null)
        {
            instance.transform.SetParent(target.transform, false);
            Debug.Log($"[PromesGuidePrefabBuilder] '{target.name}' 하위에 Promes_Guide_DataCollect 인스턴스를 생성했습니다. 프리팹: {path}");
        }
        else
        {
            Debug.Log($"[PromesGuidePrefabBuilder] 오브젝트를 선택하지 않아 씬 루트에 생성했습니다. Content 오브젝트 밑으로 옮겨주세요. 프리팹: {path}");
        }

        Selection.activeGameObject = instance;
        EditorGUIUtility.PingObject(prefabAsset);
    }

    // =====================================================================
    // Helper 함수들
    // =====================================================================

    // 번호(N.) + 본문 텍스트 한 줄 + 그 아래 인덴트된 이미지 자리
    private static void CreateStep(Transform parent, int number, Step step)
    {
        GameObject stepGo = new GameObject($"Step_{number}", typeof(RectTransform));
        stepGo.transform.SetParent(parent, false);

        LayoutElement stepLe = stepGo.AddComponent<LayoutElement>();
        stepLe.flexibleWidth = 1f;

        VerticalLayoutGroup stepVlg = stepGo.AddComponent<VerticalLayoutGroup>();
        stepVlg.padding = new RectOffset(0, 0, 0, 0);
        stepVlg.spacing = STEP_SPACING;
        stepVlg.childControlWidth = true;
        stepVlg.childControlHeight = true;
        stepVlg.childForceExpandWidth = true;
        stepVlg.childForceExpandHeight = false;

        // --- 번호 + 텍스트 행 ---
        GameObject row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(stepGo.transform, false);
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.flexibleWidth = 1f;

        HorizontalLayoutGroup rowHlg = row.AddComponent<HorizontalLayoutGroup>();
        rowHlg.padding = new RectOffset(0, 0, 0, 0);
        rowHlg.spacing = 6f;
        rowHlg.childControlWidth = true;
        rowHlg.childControlHeight = true;
        rowHlg.childForceExpandWidth = false;
        rowHlg.childForceExpandHeight = true;

        // 번호 칸: min == preferred 로 고정해서 옆의 본문 칸이 아무리 길어도 절대 눌리지 않도록 함
        GameObject numberGo = new GameObject("Number", typeof(RectTransform));
        numberGo.transform.SetParent(row.transform, false);
        LayoutElement numberLe = numberGo.AddComponent<LayoutElement>();
        numberLe.minWidth = NUMBER_COL_WIDTH;
        numberLe.preferredWidth = NUMBER_COL_WIDTH;
        numberLe.flexibleWidth = 0f;
        TextMeshProUGUI numberTmp = numberGo.AddComponent<TextMeshProUGUI>();
        numberTmp.text = $"{number}.";
        numberTmp.fontSize = 14;
        numberTmp.fontStyle = FontStyles.Bold;
        numberTmp.color = BodyColor;
        numberTmp.alignment = TextAlignmentOptions.TopLeft;
        numberTmp.raycastTarget = false;

        // 본문 칸: 나머지 공간을 모두 차지 (flexible)
        GameObject bodyGo = new GameObject("Body", typeof(RectTransform));
        bodyGo.transform.SetParent(row.transform, false);
        LayoutElement bodyLe = bodyGo.AddComponent<LayoutElement>();
        bodyLe.flexibleWidth = 1f;
        TextMeshProUGUI bodyTmp = bodyGo.AddComponent<TextMeshProUGUI>();
        bodyTmp.text = step.text;
        bodyTmp.fontSize = 14;
        bodyTmp.fontStyle = FontStyles.Normal;
        bodyTmp.color = BodyColor;
        bodyTmp.alignment = TextAlignmentOptions.TopLeft;
        bodyTmp.enableWordWrapping = true;
        bodyTmp.overflowMode = TextOverflowModes.Overflow;
        bodyTmp.raycastTarget = false;

        // --- 인덴트된 이미지 자리 ---
        GameObject imageWrap = new GameObject("Image_Wrap", typeof(RectTransform));
        imageWrap.transform.SetParent(stepGo.transform, false);
        LayoutElement wrapLe = imageWrap.AddComponent<LayoutElement>();
        wrapLe.flexibleWidth = 1f;
        wrapLe.preferredHeight = step.imgHeight;

        HorizontalLayoutGroup wrapHlg = imageWrap.AddComponent<HorizontalLayoutGroup>();
        wrapHlg.padding = new RectOffset((int)IMAGE_LEFT_INDENT, 0, 0, 0);
        wrapHlg.childControlWidth = false;
        wrapHlg.childControlHeight = false;
        wrapHlg.childForceExpandWidth = false;
        wrapHlg.childForceExpandHeight = false;
        wrapHlg.childAlignment = TextAnchor.UpperLeft;

        GameObject imgGo = new GameObject("Screenshot_Image", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(imageWrap.transform, false);
        RectTransform imgRt = imgGo.GetComponent<RectTransform>();
        imgRt.sizeDelta = new Vector2(step.imgWidth, step.imgHeight);

        Image img = imgGo.GetComponent<Image>();
        img.preserveAspect = false;

        Sprite sprite = Resources.Load<Sprite>(step.resourcePath);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
        }
        else
        {
            // 못 찾으면 자리표시용 옅은 회색 박스로 남겨두고, 나중에 수동으로 이미지를 넣으면 됨
            img.color = PlaceholderColor;
        }
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
}
