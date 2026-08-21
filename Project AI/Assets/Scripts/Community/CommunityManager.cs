using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
public class CommunityManager : MonoBehaviour
{
    // 💡 싱글톤 인스턴스
    public static CommunityManager Instance { get; private set; }
    public GameObject postItemPrefab;
    public Transform contentTransform;
    [Header("상세 페이지 UI 스크립트 연결")]
    public PostDetailPageUI detailPageUI;
    [Header("WindowManager 연동")]
    [SerializeField] private WindowManager windowManager;
    [Header("이 커뮤니티 창 자체를 여닫는 InGameWindowManager (선택 사항)")]
    [Tooltip("사이드바 등에서 이 커뮤니티 창을 최소화/복원하는 InGameWindowManager가 따로 있다면 연결하세요.")]
    [SerializeField] private InGameWindowManager communityWindowManager;

    // 💡 [추가] 페이지네이션 관련 설정
    [Header("페이지네이션 (아카라이브 스타일)")]
    [Tooltip("페이지 번호 버튼들이 배치될 부모 Transform (보통 Horizontal Layout Group을 가진 오브젝트)")]
    [SerializeField] private Transform paginationContent;
    [Tooltip("페이지 번호 버튼 프리팹. 루트 또는 자식에 Button과 Text 컴포넌트가 있어야 합니다.")]
    [SerializeField] private GameObject pageButtonPrefab;
    [Tooltip("이전 페이지 버튼 (선택 사항). OnClick에 연결할 필요 없이 스크립트가 자동으로 리스너를 등록합니다.")]
    [SerializeField] private Button prevPageButton;
    [Tooltip("다음 페이지 버튼 (선택 사항). OnClick에 연결할 필요 없이 스크립트가 자동으로 리스너를 등록합니다.")]
    [SerializeField] private Button nextPageButton;
    [Tooltip("현재 페이지 버튼의 원(배경) 색상 - 스크린샷의 파란 원")]
    [SerializeField] private Color currentPageBackgroundColor = new Color32(0x33, 0xB5, 0xE5, 255);
    [Tooltip("현재 페이지 버튼의 글자 색상 - 흰색")]
    [SerializeField] private Color currentPageTextColor = Color.white;
    [Tooltip("현재 페이지가 아닌 버튼의 원(배경) 색상 - 흰색")]
    [SerializeField] private Color defaultPageBackgroundColor = Color.white;
    [Tooltip("현재 페이지가 아닌 버튼의 글자 색상 - 파란색")]
    [SerializeField] private Color defaultPageTextColor = new Color32(0x33, 0xB5, 0xE5, 255);
    [Tooltip("한 페이지에 보여줄 게시글 수")]
    [SerializeField] private int postsPerPage = 10;

    private int currentPage = 1;

    private List<PostData> postList = new List<PostData>();
    // 💡 [추가] 여러 창을 동시에 띄우기 위해, 게시글 postID별로 "지금 열려있는 창"을 추적합니다.
    // 같은 게시글을 또 열려고 하면 새로 만들지 않고 이 창을 맨 앞으로만 가져옵니다.
    private Dictionary<int, PostDetailPageUI> openPostWindows = new Dictionary<int, PostDetailPageUI>();
    // CSV 필드 하나 안에 콤마/줄바꿈이 포함될 수 있으므로(따옴표로 감싼 셀) 정규식으로 셀 분리
    private const string CsvParserPattern = ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)";
    // 💡 [추가] CSV 로드 + 첫 UI 생성이 끝났는지 여부. 창이 꺼져있다가 켜질 때(OnEnable)
    // 데이터를 또 파싱하지 않고 레이아웃만 다시 갱신하기 위한 플래그입니다.
    private bool hasLoadedData = false;
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // 💡 [추가] 이 스크립트가 붙어있는 오브젝트(=커뮤니티 창)가 비활성 상태였다가
    // 다시 활성화될 때마다 호출됩니다. Unity는 비활성 상태에서는 레이아웃을 계산하지 않기
    // 때문에, Start()가 창이 꺼진 채로 실행됐거나 / 창을 껐다 켰다 하는 구조라면
    // 여기서 한 번 더 페이지네이션 레이아웃을 강제로 갱신해줘야 겹침 현상이 안 생깁니다.
    // (아직 CSV를 한 번도 안 읽었으면 Start()가 알아서 처리하니 여기선 아무것도 안 함)
    void OnEnable()
    {
        RefreshIfAlreadyLoaded();
    }

    // 💡 [추가] 만약 CommunityManager가 창(패널)과 다른 오브젝트에 붙어있어서
    // OnEnable이 창 열릴 때 자동으로 안 불린다면, 창을 여는 쪽(InGameWindowManager,
    // 태스크바 버튼 OnClick 등)에서 이 함수를 직접 호출해주면 됩니다.
    public void RefreshIfAlreadyLoaded()
    {
        if (hasLoadedData)
        {
            GenerateUI();
        }
    }

    void Start()
    {
        // 💡 [추가] 상세 페이지 템플릿은 시작할 때 꺼둡니다 (여러 창 복제 방식이므로
        // 원본 템플릿 자체는 화면에 보이면 안 됨). NewsListManager와 동일한 패턴.
        if (detailPageUI != null) detailPageUI.gameObject.SetActive(false);

        // 💡 [추가] 이전/다음 버튼은 코드로만 리스너를 등록합니다 (인스펙터에서 따로 연결 안 해도 됨)
        if (prevPageButton != null) prevPageButton.onClick.AddListener(ShowPrevPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(ShowNextPage);

        // 💡 포스트 CSV(CommunityData)와 댓글 CSV(CommentExcelData)를 각각 따로 불러와서 병합
        TextAsset postCsv = Resources.Load<TextAsset>("CommunityExcelData");
        TextAsset commentCsv = Resources.Load<TextAsset>("CommentExcelData");
        if (postCsv != null)
        {
            ParsePostCSV(postCsv.text);
            if (commentCsv != null)
            {
                ParseCommentCSV(commentCsv.text);
            }
            else
            {
                Debug.LogWarning("[CommunityManager] Resources 폴더에서 'CommentExcelData' 를 찾을 수 없습니다. 댓글 없이 진행합니다.");
            }
            currentPage = 1;
            hasLoadedData = true;
            GenerateUI();
        }
        else
        {
            Debug.LogError("[CommunityManager] Resources 폴더에서 'PostData' 를 찾을 수 없습니다.");
        }
    }
    /// <summary>
    /// 포스트 전용 CSV 파싱.
    /// 예상 컬럼 순서: postID,title,author,date,likes,dislikes,content,imageName,clueID,imageClueID,collectibleImageID,titleClueID,clueParagraphIndex,bodyClueID
    /// (실제 CommunityData.csv 컬럼 순서가 다르면 이 함수만 맞춰서 수정하면 됩니다)
    /// </summary>
    private void ParsePostCSV(string csvText)
    {
        string[] rows = Regex.Split(csvText, @"\r\n|\n|\r");
        for (int i = 1; i < rows.Length; i++) // 0번째는 헤더
        {
            if (string.IsNullOrWhiteSpace(rows[i])) continue;
            string[] columns = Regex.Split(rows[i], CsvParserPattern);
            if (columns.Length < 9) continue;
            int.TryParse(columns[0].Trim().Replace("\"", ""), out int id);
            if (id == 0) continue;
            PostData post = new PostData();
            post.postID = id;
            post.title = columns[1].Trim().Replace("\"", "");
            post.author = columns[2].Trim().Replace("\"", "");
            post.date = columns[3].Trim().Replace("\"", "");
            int.TryParse(columns[4].Trim().Replace("\"", ""), out post.likes);
            int.TryParse(columns[5].Trim().Replace("\"", ""), out post.dislikes);
            string rawContent = columns[6].Trim();
            if (rawContent.StartsWith("\"") && rawContent.EndsWith("\""))
            {
                rawContent = rawContent.Substring(1, rawContent.Length - 2);
            }
            // 💡 [변경] 뉴스(NewsData.body)와 동일하게 '|' 기호로 문단을 구분해서 씁니다.
            // 기존처럼 "\\n"이 들어오면 문단 구분용으로 호환되게 줄바꿈 문자로도 변환해둡니다.
            post.content = rawContent.Replace("\"\"", "\"").Replace("\\n", "\n");
            post.imageName = columns[7].Trim().Replace("\"", "");
            post.clueID = columns[8].Trim().Replace("\"", "");
            // 💡 이 게시글의 이미지를 클릭했을 때 수집할 기존 단서 ID (10번째 컬럼)
            post.imageClueID = (columns.Length >= 10) ? columns[9].Trim().Replace("\"", "") : "";
            // 💡 이미지 생성 퀘스트용 ID (11번째 컬럼)
            post.collectibleImageID = (columns.Length >= 11) ? columns[10].Trim().Replace("\"", "") : "";
            // 💡 제목 클릭 단서 ID (12번째 컬럼). 비어있으면 제목은 수집 대상 아님.
            post.titleClueID = (columns.Length >= 12) ? columns[11].Trim().Replace("\"", "") : "";
            // 💡 [추가] 단서가 숨겨진 본문 문단 번호 (13번째 컬럼)
            int paragraphIdx = 0;
            if (columns.Length >= 13)
            {
                int.TryParse(columns[12].Trim().Replace("\"", ""), out paragraphIdx);
            }
            post.clueParagraphIndex = paragraphIdx;
            // 💡 [추가] 그 문단 클릭 시 수집할 단서 ID (14번째 컬럼)
            post.bodyClueID = (columns.Length >= 14) ? columns[13].Trim().Replace("\"", "") : "";
            postList.Add(post);
        }
    }
    /// <summary>
    /// 댓글 전용 CSV 파싱 후, postID 기준으로 해당 게시글의 comments 리스트에 병합.
    /// 컬럼 순서: postID,author,content,isEmoticon,emoticonName,clueID
    /// </summary>
    private void ParseCommentCSV(string csvText)
    {
        string[] rows = Regex.Split(csvText, @"\r\n|\n|\r");
        for (int i = 1; i < rows.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(rows[i])) continue;
            string[] columns = Regex.Split(rows[i], CsvParserPattern);
            if (columns.Length < 5) continue;
            int.TryParse(columns[0].Trim().Replace("\"", ""), out int postID);
            if (postID == 0) continue;
            PostData targetPost = postList.Find(p => p.postID == postID);
            if (targetPost == null)
            {
                Debug.LogWarning($"[CommunityManager] 댓글의 postID {postID} 에 해당하는 게시글을 찾을 수 없습니다.");
                continue;
            }
            CommentData comment = new CommentData();
            comment.postID = postID;
            comment.author = columns[1].Trim().Replace("\"", "");
            comment.content = columns[2].Trim().Replace("\"", "");
            bool.TryParse(columns[3].Trim().Replace("\"", ""), out comment.isEmoticon);
            comment.emoticonName = columns[4].Trim().Replace("\"", "");
            comment.clueID = (columns.Length >= 6) ? columns[5].Trim().Replace("\"", "") : "";
            targetPost.comments.Add(comment);
        }
    }
    // 💡 [변경] 전체 목록을 다 그리지 않고, 현재 페이지(currentPage)에 해당하는
    // postsPerPage(기본 10)개만 잘라서 그립니다. 그린 뒤 페이지 번호 버튼도 함께 갱신합니다.
    void GenerateUI()
    {
        foreach (Transform child in contentTransform) Destroy(child.gameObject);

        int totalPages = GetTotalPages();
        currentPage = Mathf.Clamp(currentPage, 1, totalPages);

        int startIndex = (currentPage - 1) * postsPerPage;
        int endIndex = Mathf.Min(startIndex + postsPerPage, postList.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            PostData post = postList[i];
            GameObject newItem = Instantiate(postItemPrefab, contentTransform);
            PostItemUI itemUI = newItem.GetComponent<PostItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(post, this);
                // 💡 댓글 수 표시(제목 옆 [숫자])는 PostItemUI.Setup() 안에서
                // titleText.text = $"{data.title} [{data.comments.Count}]"; 로 처리하도록
                // PostItemUI.cs 쪽을 수정했습니다.

                // 💡 리스트 아이템(PostItemUI)에는 이미지가 없으므로 여기선 수집 등록 안 함.
                // 실제 이미지는 상세 페이지(PostDetailPageUI)에서 보여지므로,
                // 수집 등록은 OpenDetailPage() / PostDetailPageUI.DisplayPost() 쪽에서 처리함.
            }
        }

        GeneratePaginationButtons(totalPages);
        UpdatePrevNextButtonState(totalPages);
    }

    // 💡 [추가] 전체 페이지 수 계산 (게시글이 0개여도 최소 1페이지로 취급)
    private int GetTotalPages()
    {
        if (postList.Count == 0) return 1;
        return Mathf.CeilToInt(postList.Count / (float)postsPerPage);
    }

    // 💡 [추가] 페이지 번호 버튼(1, 2, 3 ...)을 총 페이지 수만큼 동적으로 생성합니다.
    // 이미 있던 버튼은 지우고 다시 만드므로, 페이지가 바뀔 때마다 호출해도 안전합니다.
    // 스크린샷처럼 현재 페이지는 파란 원 + 흰 글자, 나머지는 흰 원 + 파란 글자로 표시합니다.
    private void GeneratePaginationButtons(int totalPages)
    {
        if (paginationContent == null || pageButtonPrefab == null) return;

        foreach (Transform child in paginationContent) Destroy(child.gameObject);

        for (int page = 1; page <= totalPages; page++)
        {
            int capturedPage = page; // 💡 for 루프 변수를 델리게이트가 캡처할 때 생기는 클로저 버그 방지
            GameObject buttonObj = Instantiate(pageButtonPrefab, paginationContent);
            bool isCurrent = (capturedPage == currentPage);

            // 텍스트(숫자) 세팅 - PostItemUI와 동일하게 TMP_Text 사용
            TMP_Text label = buttonObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = page.ToString();
                label.color = isCurrent ? currentPageTextColor : defaultPageTextColor;
            }

            // 원(배경) 색상 세팅 - 버튼 루트의 Image 컴포넌트를 원 스프라이트로 사용한다고 가정
            Image circleImage = buttonObj.GetComponent<Image>();
            if (circleImage != null)
            {
                circleImage.color = isCurrent ? currentPageBackgroundColor : defaultPageBackgroundColor;
            }

            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => ShowPage(capturedPage));
                // 💡 현재 페이지 버튼은 눌러도 반응 없게 비활성화
                btn.interactable = !isCurrent;
            }
        }

        // 💡 [추가] PageNumbers(paginationContent)가 자기 자신도 Horizontal Layout Group이면서
        // 동시에 또 다른 Horizontal Layout Group(PaginationRow)의 자식인 구조라서, Content Size
        // Fitter로 자동으로 폭을 재게 하면 타이밍에 따라 NextPage 버튼이 잘못된(좁은) 폭을 기준으로
        // 배치되어 마지막 페이지 번호 버튼과 겹쳐 보이는 문제가 있었습니다.
        // 그래서 Content Size Fitter에 맡기지 않고, 버튼 개수를 이미 알고 있는 스크립트가
        // PageNumbers의 폭을 직접 계산해서 강제로 넣어줍니다.
        // ⚠️ 이 코드를 쓰려면 PageNumbers 오브젝트에서 Content Size Fitter 컴포넌트는 제거해주세요
        // (Horizontal Layout Group만 남겨두면 됩니다).
        RectTransform contentRect = paginationContent as RectTransform;
        if (contentRect != null)
        {
            HorizontalLayoutGroup contentHLG = contentRect.GetComponent<HorizontalLayoutGroup>();
            RectTransform prefabRect = pageButtonPrefab.transform as RectTransform;
            if (contentHLG != null && prefabRect != null && totalPages > 0)
            {
                float buttonWidth = prefabRect.rect.width;
                float totalWidth = (totalPages * buttonWidth)
                                    + (Mathf.Max(0, totalPages - 1) * contentHLG.spacing)
                                    + contentHLG.padding.left + contentHLG.padding.right;
                contentRect.sizeDelta = new Vector2(totalWidth, contentRect.sizeDelta.y);
            }

            // 버튼들(1,2,3...) 내부 정렬은 PageNumbers 자신의 Horizontal Layout Group이 처리하도록
            // 즉시 강제 리빌드하고, 그다음 PageNumbers의 "새로 계산된 폭"을 기준으로
            // PrevPage / PageNumbers / NextPage를 다시 배치하도록 부모도 강제 리빌드합니다.
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            if (contentRect.parent is RectTransform parentRowRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRowRect);
            }
        }
    }

    // 💡 [추가] 이전/다음 버튼의 활성/비활성 상태를 현재 페이지에 맞게 갱신
    private void UpdatePrevNextButtonState(int totalPages)
    {
        if (prevPageButton != null) prevPageButton.interactable = (currentPage > 1);
        if (nextPageButton != null) nextPageButton.interactable = (currentPage < totalPages);
    }

    // 💡 [추가] 특정 페이지로 이동 (페이지 번호 버튼 클릭 시 호출됨)
    public void ShowPage(int page)
    {
        int totalPages = GetTotalPages();
        int clamped = Mathf.Clamp(page, 1, totalPages);
        if (clamped == currentPage) return;
        currentPage = clamped;
        GenerateUI();
    }

    // 💡 [추가] 이전/다음 페이지 이동 (버튼 OnClick에 연결하거나, Start()에서 자동 연결됨)
    public void ShowPrevPage() => ShowPage(currentPage - 1);
    public void ShowNextPage() => ShowPage(currentPage + 1);

    // 💡 [변경] 하나의 상세 페이지를 재사용하던 방식에서, 클릭마다 새로 복제해서
    // 여러 창을 동시에 띄우는 방식으로 변경. 같은 게시글이 이미 열려있으면
    // 새로 만들지 않고 그 창을 맨 앞으로만 가져옵니다.
    public void OpenDetailPage(PostData data)
    {
        if (detailPageUI == null) return;
        // 이미 열려있는 게시글이라면 그 창을 맨 앞으로만 가져오고 끝
        if (openPostWindows.TryGetValue(data.postID, out PostDetailPageUI existingWindow) && existingWindow != null)
        {
            // 💡 최소화(ToggleWindowImmediate)로 비활성화된 상태일 수 있으므로, 다시 켜준 뒤에 진행합니다.
            // (비활성 오브젝트에서 바로 애니메이션 코루틴을 돌리면 에러가 납니다.)
            if (!existingWindow.gameObject.activeSelf) existingWindow.gameObject.SetActive(true);

            existingWindow.transform.SetAsLastSibling();
            PopupSpawnAnimation existingAnim = existingWindow.GetComponent<PopupSpawnAnimation>();
            if (existingAnim != null) existingAnim.PlayPopAnimation();
            return;
        }
        // 새 창 복제 (detailPageUI를 "복제할 원본 템플릿"으로 사용)
        PostDetailPageUI newWindow = Instantiate(detailPageUI, detailPageUI.transform.parent);
        newWindow.gameObject.SetActive(true);
        newWindow.DisplayPost(data);
        openPostWindows[data.postID] = newWindow;
        if (windowManager != null)
        {
            RectTransform detailRect = newWindow.GetComponent<RectTransform>();
            windowManager.RepositionPopupWindow(detailRect);
        }
        PopupSpawnAnimation newAnim = newWindow.GetComponent<PopupSpawnAnimation>();
        if (newAnim != null) newAnim.PlayPopAnimation();
    }
    /// <summary>
    /// 💡 DataLogManager가 "원본 보기"를 요청할 때 호출.
    /// 게시글 제목의 clueID든, 본문 문단의 clueID든, 본문 전체의 clueID든, 댓글의 clueID든
    /// 일치하는 걸 찾아 해당 게시글의 상세 페이지를 열어줍니다.
    /// </summary>
    public bool TryOpenClueSource(string clueID)
    {
        if (string.IsNullOrEmpty(clueID)) return false;
        foreach (var post in postList)
        {
            bool isTitleMatch = !string.IsNullOrEmpty(post.titleClueID) && post.titleClueID == clueID;
            bool isPostMatch = !string.IsNullOrEmpty(post.clueID) && post.clueID == clueID;
            bool isBodyMatch = !string.IsNullOrEmpty(post.bodyClueID) && post.bodyClueID == clueID;
            bool isCommentMatch = post.comments.Exists(c => !string.IsNullOrEmpty(c.clueID) && c.clueID == clueID);
            // 💡 [추가] 본문 안에 "[CLUE:아이디]" 태그로 심어둔 (여러 개일 수 있는) 단서도 검색
            bool isTaggedBodyMatch = ContentContainsClueTag(post.content, clueID);
            if (isTitleMatch || isPostMatch || isBodyMatch || isCommentMatch || isTaggedBodyMatch)
            {
                OpenDetailPage(post);
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 💡 [추가] 패널 링크(<link="post:12">)에서 특정 게시글을 ID로 바로 열 때 사용.
    /// </summary>
    public bool OpenPostByID(int postID)
    {
        var data = postList.Find(p => p.postID == postID);
        if (data == null)
        {
            Debug.LogWarning($"[CommunityManager] postID '{postID}' 에 해당하는 게시글을 찾을 수 없습니다.");
            return false;
        }
        OpenDetailPage(data);
        return true;
    }
    /// <summary>
    /// 💡 [추가] 본문을 '|'로 나눈 문단들 중에, "[CLUE:아이디]" 태그로 시작하는 문단이
    /// 주어진 clueID와 일치하는 게 있는지 확인합니다. (문단 여러 개가 단서인 경우 지원)
    /// </summary>
    private bool ContentContainsClueTag(string content, string clueID)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(clueID)) return false;
        string[] paragraphs = content.Split('|');
        foreach (var paragraph in paragraphs)
        {
            string trimmed = paragraph.Trim();
            if (!trimmed.StartsWith("[CLUE:")) continue;
            int closeBracketIndex = trimmed.IndexOf(']');
            if (closeBracketIndex <= 6) continue;
            string tagId = trimmed.Substring(6, closeBracketIndex - 6);
            if (tagId == clueID) return true;
        }
        return false;
    }
    /// <summary>
    /// 💡 [추가] 상세 페이지 창이 닫힐 때 호출되어, openPostWindows 딕셔너리에서
    /// 해당 게시글 참조를 제거합니다. 이게 없으면 창을 닫아도 딕셔너리에 죽은 참조가
    /// 남아서, 같은 글을 다시 열 때 새 창을 안 만들고 죽은 창을 앞으로 가져오려다 실패합니다.
    /// </summary>
    public void NotifyWindowClosed(int postID)
    {
        if (openPostWindows.ContainsKey(postID))
        {
            openPostWindows.Remove(postID);
        }
    }
}