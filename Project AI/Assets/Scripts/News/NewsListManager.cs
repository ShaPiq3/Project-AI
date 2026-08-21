using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;

public class NewsListManager : MonoBehaviour
{
    // 💡 다른 스크립트(DataLogManager)에서 접근할 수 있도록 싱글톤 인스턴스 추가
    public static NewsListManager Instance { get; private set; }

    [Header("Data (Excel CSV)")]
    [SerializeField] private TextAsset csvFile;

    [Header("Prefabs & Parents")]
    [SerializeField] private GameObject newsButtonPrefab;
    [SerializeField] private Transform contentParent;

    [Header("Detail Popup Reference")]
    [SerializeField] private NewsCard detailPopup;

    [Header("WindowManager 연동")]
    [SerializeField] private WindowManager windowManager;

    [Header("이 뉴스 창 자체를 여닫는 InGameWindowManager (선택 사항)")]
    [Tooltip("사이드바 등에서 이 뉴스 창을 최소화/복원하는 InGameWindowManager가 따로 있다면 연결하세요. " +
             "원본 보기를 눌렀을 때 창이 닫혀 있어도 자동으로 복원됩니다.")]
    [SerializeField] private InGameWindowManager newsWindowManager;

    // 💡 파싱된 모든 뉴스 데이터를 보관 (clueID로 역참조 검색하기 위함)
    private List<NewsData> allNewsData = new List<NewsData>();

    // 💡 여러 창을 동시에 띄우기 위해, 기사 id별로 "지금 열려있는 창"을 추적합니다.
    // 같은 기사를 또 열려고 하면 새로 만들지 않고 이 창을 맨 앞으로만 가져옵니다.
    private Dictionary<int, NewsCard> openNewsWindows = new Dictionary<int, NewsCard>();

    // 💡 [추가] unlockQuestID가 있는 기사는 처음엔 숨겨두고, 해당 퀘스트가 EP.1_Q4처럼
    // 성공 판정되는 순간 여기서 찾아서 보여줍니다.
    private readonly Dictionary<string, List<GameObject>> lockedButtonsByQuestID = new Dictionary<string, List<GameObject>>();

    // 💡 [추가] 게임 진행 순서대로 나열한 씬 목록. CSV의 unlockSceneName은 이 목록에 있는 이름만
    // 인식합니다. 나중에 씬이 늘어나면(MainScene_3 등) 여기에 순서대로 추가하기만 하면 됩니다.
    private static readonly string[] SceneProgressionOrder = { "MainScene", "MainScene_2" };
    // 💡 [추가] 씬 조건 때문에 숨겨진 버튼들. SelectCategory()에서 카테고리를 바꿔도
    // 다시 켜지지 않도록 lockedButtonsByQuestID와 마찬가지로 IsLocked()에서 함께 체크합니다.
    private readonly HashSet<GameObject> sceneGatedButtons = new HashSet<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void OnEnable()
    {
        DataLogManager.OnQuestJudged += HandleQuestJudged;
    }

    private void OnDisable()
    {
        DataLogManager.OnQuestJudged -= HandleQuestJudged;
    }

    private void Start()
    {
        if (detailPopup != null) detailPopup.gameObject.SetActive(false);

        ParseExcelAndGenerateButtons();
        SelectCategory("ALL");
    }

    /// <summary>
    /// 💡 [추가] 퀘스트가 판정될 때마다 호출됩니다. 그 questID로 잠겨있던 기사가 있고
    /// 성공 판정이었다면, 지금부터 목록에 보이게 풀어줍니다.
    /// </summary>
    private void HandleQuestJudged(string questID, bool isSuccess, float errorWeight)
    {
        if (!isSuccess || string.IsNullOrEmpty(questID)) return;
        if (!lockedButtonsByQuestID.TryGetValue(questID, out var buttons)) return;

        foreach (var btnGo in buttons)
        {
            if (btnGo != null) btnGo.SetActive(true);
        }

        lockedButtonsByQuestID.Remove(questID);
    }

    // 1. 엑셀 파싱 및 리스트 버튼 동적 생성
    private void ParseExcelAndGenerateButtons()
    {
        if (csvFile == null || newsButtonPrefab == null || contentParent == null) return;

        List<List<string>> csvData = ParseCSV(csvFile.text);

        // 💡 [변경] CSV에 나중에 적힌(아래쪽) 기사가 목록 맨 위로 오도록 역순으로 순회합니다.
        // (헤더 행인 0번은 제외하고, 마지막 행부터 1번 행까지)
        for (int i = csvData.Count - 1; i >= 1; i--)
        {
            List<string> row = csvData[i];

            if (row.Count < 6) continue;

            if (!int.TryParse(row[0], out int idResult))
            {
                Debug.LogWarning($"[CSV 파싱 패스] {i}번째 줄의 ID 형식이 올바르지 않습니다: '{row[0]}'");
                continue;
            }

            string imgClue = (row.Count > 6) ? row[6] : "";

            int paragraphIdx = 0;
            if (row.Count > 7)
            {
                int.TryParse(row[7], out paragraphIdx);
            }

            string bodyClue = (row.Count > 8) ? row[8] : "";

            // 💡 이미지 생성 퀘스트용 ID (10번째 컬럼). 비어있으면 수집 대상 아님.
            string collectibleImageID = (row.Count > 9) ? row[9] : "";

            // 💡 제목 클릭 단서 ID (11번째 컬럼). 비어있으면 제목은 수집 대상 아님.
            string titleClue = (row.Count > 10) ? row[10] : "";

            // 💡 [추가] 이 기사를 숨겨뒀다가 풀어줄 questID (12번째 컬럼). 비어있으면 항상 보임.
            string unlockQuestID = (row.Count > 11) ? row[11] : "";

            // 💡 [추가] 이 기사가 등장하기 시작하는 씬 이름 (13번째 컬럼). 비어있으면 항상 보임.
            string unlockSceneName = (row.Count > 12) ? row[12] : "";

            NewsData data = new NewsData
            {
                id = idResult,
                category = row[1],
                title = row[2],
                info = row[3],
                body = row[4],
                imageName = row[5],
                imageClueID = imgClue,
                clueParagraphIndex = paragraphIdx,
                bodyClueID = bodyClue,
                collectibleImageID = collectibleImageID,
                titleClueID = titleClue,
                unlockQuestID = unlockQuestID,
                unlockSceneName = unlockSceneName
            };

            // 💡 나중에 clueID로 검색할 수 있도록 저장
            allNewsData.Add(data);

            GameObject btnGo = Instantiate(newsButtonPrefab, contentParent);
            NewsButton newsBtn = btnGo.GetComponent<NewsButton>();
            if (newsBtn != null)
            {
                newsBtn.SetButton(data, this);

                // 💡 [변경] 목록 썸네일이 아니라 "본문을 열었을 때 보이는 이미지"를
                // 클릭해야 수집되도록, 여기서는 더 이상 바인딩하지 않습니다.
                // (실제 바인딩은 NewsCard.SetNewsData()에서 처리)
            }

            // 💡 [추가] unlockQuestID가 있으면 처음엔 숨겨두고, 나중에 퀘스트 성공 시 풀어줍니다.
            if (!string.IsNullOrEmpty(unlockQuestID))
            {
                btnGo.SetActive(false);

                if (!lockedButtonsByQuestID.ContainsKey(unlockQuestID))
                    lockedButtonsByQuestID[unlockQuestID] = new List<GameObject>();
                lockedButtonsByQuestID[unlockQuestID].Add(btnGo);
            }

            // 💡 [추가] unlockSceneName이 지정되어 있고 아직 그 씬에 도달하지 않았다면 숨겨둡니다.
            if (!string.IsNullOrEmpty(unlockSceneName) && !HasReachedScene(unlockSceneName))
            {
                btnGo.SetActive(false);
                sceneGatedButtons.Add(btnGo);
            }
        }
    }

    private List<List<string>> ParseCSV(string csvText)
    {
        List<List<string>> result = new List<List<string>>();
        List<string> currentLine = new List<string>();
        StringBuilder cell = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    cell.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    currentLine.Add(cell.ToString().Trim());
                    cell.Clear();
                }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    {
                        i++;
                    }
                    currentLine.Add(cell.ToString().Trim());
                    cell.Clear();

                    if (currentLine.Count > 0 && !string.IsNullOrWhiteSpace(currentLine[0]))
                    {
                        result.Add(new List<string>(currentLine));
                    }
                    currentLine.Clear();
                }
                else
                {
                    cell.Append(c);
                }
            }
        }

        if (cell.Length > 0 || currentLine.Count > 0)
        {
            currentLine.Add(cell.ToString().Trim());
            result.Add(currentLine);
        }

        return result;
    }

    // 2. 카테고리 선택 마스터 함수
    public void SelectCategory(string categoryKeyword)
    {
        if (contentParent == null) return;

        string cleanKeyword = categoryKeyword.Replace(" ", "").ToUpper();
        bool isAll = string.IsNullOrEmpty(cleanKeyword) || cleanKeyword == "ALL" || cleanKeyword == "전체";

        for (int i = 0; i < contentParent.childCount; i++)
        {
            Transform child = contentParent.GetChild(i);
            if (child == null) continue;

            // 💡 [추가] 아직 퀘스트가 안 풀려서 잠겨있는 기사는 카테고리 필터와 무관하게 계속 숨겨둡니다.
            if (IsLocked(child.gameObject))
            {
                child.gameObject.SetActive(false);
                continue;
            }

            if (isAll)
            {
                child.gameObject.SetActive(true);
            }
            else
            {
                NewsButton btnComponent = child.GetComponent<NewsButton>();
                if (btnComponent != null)
                {
                    string cleanTargetCategory = btnComponent.category.Replace(" ", "").ToUpper();

                    if (cleanTargetCategory.Contains(cleanKeyword))
                    {
                        child.gameObject.SetActive(true);
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 💡 [추가] 현재 활성화된 씬이 requiredSceneName "이상"(같거나 그 이후 진행 단계)인지 확인합니다.
    /// SceneProgressionOrder에 없는 씬 이름이 들어오면(오타 등) 콘텐츠가 영구히 숨겨지는 걸 막기 위해
    /// 안전하게 true(노출)로 처리하고 경고를 남깁니다.
    /// </summary>
    private bool HasReachedScene(string requiredSceneName)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        int currentIndex = Array.IndexOf(SceneProgressionOrder, currentSceneName);
        int requiredIndex = Array.IndexOf(SceneProgressionOrder, requiredSceneName);

        if (currentIndex < 0)
        {
            Debug.LogWarning($"[NewsListManager] 현재 씬 '{currentSceneName}'이 SceneProgressionOrder에 등록되어 있지 않습니다. 기본값(노출)으로 처리합니다.");
            return true;
        }
        if (requiredIndex < 0)
        {
            Debug.LogWarning($"[NewsListManager] CSV의 unlockSceneName '{requiredSceneName}'이 SceneProgressionOrder에 등록되어 있지 않습니다. 기본값(노출)으로 처리합니다.");
            return true;
        }

        return currentIndex >= requiredIndex;
    }

    private bool IsLocked(GameObject btnGo)
    {
        if (sceneGatedButtons.Contains(btnGo)) return true;

        foreach (var buttons in lockedButtonsByQuestID.Values)
        {
            if (buttons.Contains(btnGo)) return true;
        }
        return false;
    }

    // 3. 버튼을 눌렀을 때 상세 팝업을 열어주는 중계 함수
    // 💡 하나의 팝업을 재사용하던 방식에서, 클릭마다 새로 복제해서
    // 여러 창을 동시에 띄우는 방식으로 변경. 같은 기사가 이미 열려있으면
    // 새로 만들지 않고 그 창을 맨 앞으로만 가져옵니다.
    public void OpenDetailPopup(NewsData data)
    {
        if (detailPopup == null) return;

        // 이미 열려있는 기사라면 그 창을 맨 앞으로만 가져오고 끝
        if (openNewsWindows.TryGetValue(data.id, out NewsCard existingWindow) && existingWindow != null)
        {
            // 💡 최소화(ToggleWindowImmediate)로 비활성화된 상태일 수 있으므로, 다시 켜준 뒤에 진행합니다.
            // (비활성 오브젝트에서 바로 애니메이션 코루틴을 돌리면 에러가 납니다.)
            if (!existingWindow.gameObject.activeSelf) existingWindow.gameObject.SetActive(true);

            existingWindow.transform.SetAsLastSibling();

            PopupSpawnAnimation existingAnim = existingWindow.GetComponent<PopupSpawnAnimation>();
            if (existingAnim != null) existingAnim.PlayPopAnimation();

            return;
        }

        // 새 창 복제 (detailPopup을 "복제할 원본 템플릿"으로 사용)
        NewsCard newWindow = Instantiate(detailPopup, detailPopup.transform.parent);
        newWindow.gameObject.SetActive(true);
        newWindow.SetNewsData(data);

        openNewsWindows[data.id] = newWindow;

        if (windowManager != null)
        {
            RectTransform cardRect = newWindow.GetComponent<RectTransform>();
            windowManager.RepositionPopupWindow(cardRect);
        }

        PopupSpawnAnimation newAnim = newWindow.GetComponent<PopupSpawnAnimation>();
        if (newAnim != null) newAnim.PlayPopAnimation();
    }

    /// <summary>
    /// 💡 DataLogManager가 "원본 보기"를 요청할 때 호출하는 함수.
    /// clueID로 어느 기사(제목/본문 문단/이미지)에서 나온 단서인지 찾아서
    /// 해당 기사의 상세 팝업을 열어줍니다.
    /// </summary>
    /// <returns>원본을 찾아서 열었으면 true, 못 찾았으면 false</returns>
    public bool TryOpenClueSource(string clueID)
    {
        if (string.IsNullOrEmpty(clueID)) return false;

        foreach (var data in allNewsData)
        {
            bool isTitleMatch = !string.IsNullOrEmpty(data.titleClueID) && data.titleClueID == clueID;
            bool isBodyMatch = !string.IsNullOrEmpty(data.bodyClueID) && data.bodyClueID == clueID;
            bool isImageMatch = !string.IsNullOrEmpty(data.imageClueID) && data.imageClueID == clueID;

            // 💡 본문 안에 "[CLUE:아이디]" 태그로 심어둔 (여러 개일 수 있는) 단서도 검색
            bool isTaggedBodyMatch = BodyContainsClueTag(data.body, clueID);

            if (isTitleMatch || isBodyMatch || isImageMatch || isTaggedBodyMatch)
            {

                OpenDetailPopup(data);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 💡 [추가] 패널 링크(<link="news:5">)에서 특정 기사를 ID로 바로 열 때 사용.
    /// 이미 열려있는 기사면 TryOpenClueSource와 마찬가지로 맨 앞으로만 가져옵니다.
    /// </summary>
    public bool OpenNewsByID(int newsID)
    {
        var data = allNewsData.Find(n => n.id == newsID);
        if (data == null)
        {
            Debug.LogWarning($"[NewsListManager] newsID '{newsID}' 에 해당하는 기사를 찾을 수 없습니다.");
            return false;
        }

        OpenDetailPopup(data);
        return true;
    }

    /// <summary>
    /// 💡 본문을 '|'로 나눈 문단들 중에, "[CLUE:아이디]" 태그로 시작하는 문단이
    /// 주어진 clueID와 일치하는 게 있는지 확인합니다. (문단 여러 개가 단서인 경우 지원)
    /// </summary>
    private bool BodyContainsClueTag(string body, string clueID)
    {
        if (string.IsNullOrEmpty(body) || string.IsNullOrEmpty(clueID)) return false;

        string[] paragraphs = body.Split('|');
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
    /// 💡 [추가] 뉴스 상세 팝업(NewsCard)이 닫힐 때 호출되어, openNewsWindows 딕셔너리에서
    /// 해당 기사 참조를 제거합니다. 이게 없으면 창을 닫아도 딕셔너리에 죽은 참조가
    /// 남아서, 같은 기사를 다시 열 때 새 창을 안 만들고 죽은 창을 앞으로 가져오려다 실패합니다.
    /// </summary>
    public void NotifyWindowClosed(int newsID)
    {
        if (openNewsWindows.ContainsKey(newsID))
        {
            openNewsWindows.Remove(newsID);
        }
    }
}