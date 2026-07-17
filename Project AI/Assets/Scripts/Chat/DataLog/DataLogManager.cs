using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DataLogManager : MonoBehaviour
{
    public static DataLogManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private RectTransform logPanelRect;
    [SerializeField] private GameObject clueSlotPrefab;
    [SerializeField] private Transform clueContainer;

    [Header("DOTween Settings")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private Ease showEase = Ease.OutCubic;
    [SerializeField] private Ease hideEase = Ease.InCubic;

    [Header("Filter Settings")]
    [SerializeField] private GameObject clueFilterPanel;

    // 전체 단서 데이터베이스 (엑셀에서 파싱해서 담아둘 사전)
    private Dictionary<string, ClueData> clueDatabase = new Dictionary<string, ClueData>();

    // 플레이어가 실제로 인게임에서 획득/수집한 단서 목록
    private List<ClueData> collectedClues = new List<ClueData>();
    private bool isOpen = false;
    private float panelWidth;

    // 💡 다른 스크립트에서 참조할 단서 수집 모드 활성화 여부
    public bool IsClueSearchModeActive { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 시작할 때 패널 크기를 가져와서 정확히 그만큼 화면 '우측' 바깥으로 숨김
        if (logPanelRect != null)
        {
            panelWidth = logPanelRect.rect.width;
            logPanelRect.anchoredPosition = new Vector2(panelWidth, logPanelRect.anchoredPosition.y);
        }

        // 게임 시작 시 엑셀 데이터 파싱
        LoadClueDatabase();
    }

    /// <summary>
    /// 엑셀(CSV) 파싱 결과를 가져와서 메모리에 등록하는 함수
    /// </summary>
    private void LoadClueDatabase()
    {
        clueDatabase.Clear();

        List<Dictionary<string, object>> excelRows = CSVReader.Read("ClueExcelData"); // 예시 시트 이름

        if (excelRows == null) return;

        foreach (var row in excelRows)
        {
            ClueData clue = new ClueData();
            clue.clueID = row["ClueID"].ToString();
            clue.sourceType = row["SourceType"].ToString();
            clue.sourceTitle = row["SourceTitle"].ToString();
            clue.contentText = row["ContentText"].ToString().Replace("\\n", "\n"); // 줄바꿈 적용
            clue.imageName = row["ImageName"].ToString();

            clueDatabase.Add(clue.clueID, clue);
        }
    }

    /// <summary>
    /// 플레이어가 단서를 발견했을 때 "ID만" 넘겨서 수집하는 함수
    /// </summary>
    public void AcquireClue(string clueID)
    {
        // 1. 이미 얻은 단서인지 중복 검사
        if (collectedClues.Exists(c => c.clueID == clueID)) return;

        // 2. 엑셀 데이터베이스에서 단서 원본 정보가 있는지 조회
        if (!clueDatabase.TryGetValue(clueID, out ClueData targetClue))
        {
            Debug.LogWarning($"단서 ID '{clueID}'를 데이터베이스에서 찾을 수 없습니다.");
            return;
        }

        // 3. 수집 목록에 추가
        collectedClues.Add(targetClue);

        // 4. UI 슬롯 생성 실행
        CreateClueSlot(targetClue);
    }

    private void CreateClueSlot(ClueData clue)
    {
        if (clueSlotPrefab == null || clueContainer == null) return;

        GameObject slotGo = Instantiate(clueSlotPrefab, clueContainer);
        ClueSlot slotScript = slotGo.GetComponent<ClueSlot>();

        if (slotScript != null)
        {
            slotScript.SetClueUI(clue);
        }
    }

    // 토글 함수 (열려있으면 닫고, 닫혀있으면 여는 연출)
    public void ToggleLogPanel()
    {
        if (logPanelRect == null) return;
        logPanelRect.DOKill();

        if (!isOpen)
        {
            logPanelRect.DOAnchorPosX(0f, duration).SetEase(showEase).SetUpdate(true);
            isOpen = true;
        }
        else
        {
            HideLogPanel();
        }
    }

    // 무조건 우측 바깥으로 미끄러지며 숨겨지는 강제 닫기 함수
    public void HideLogPanel()
    {
        if (logPanelRect == null) return;

        logPanelRect.DOKill();
        logPanelRect.DOAnchorPosX(panelWidth, duration)
            .SetEase(hideEase)
            .SetUpdate(true);

        isOpen = false;
    }

    /// <summary>
    /// 💡 [핵심 수정] 기존에 수집 모드용으로 이미 만들어 두신 버튼 클릭 이벤트(OnClick)에 
    /// 이 함수를 한 줄 추가해서 같이 실행되게 묶어주시면 끝납니다!
    /// </summary>
    public void ToggleClueSearchMode()
    {
        IsClueSearchModeActive = !IsClueSearchModeActive;

        // 💡 수집 모드 상태에 따라 필터 패널을 켜거나 끕니다.
        if (clueFilterPanel != null)
        {
            clueFilterPanel.SetActive(IsClueSearchModeActive);
        }

        Debug.Log($"[시스템] 단서 수집 모드: {IsClueSearchModeActive}");
    }
}