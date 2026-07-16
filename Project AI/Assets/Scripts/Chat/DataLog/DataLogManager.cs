using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // 💡 DOTween 필수 추가

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

    private List<ClueData> collectedClues = new List<ClueData>();
    private bool isOpen = false;
    private float panelWidth;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 💡 [수정] 시작할 때 패널 크기를 가져와서 정확히 그만큼 화면 '우측' 바깥(+Width)으로 숨김
        if (logPanelRect != null)
        {
            panelWidth = logPanelRect.rect.width;
            logPanelRect.anchoredPosition = new Vector2(panelWidth, logPanelRect.anchoredPosition.y);
        }
    }

    // 토글 함수 (열려있으면 닫고, 닫혀있으면 여는 연출)
    public void ToggleLogPanel()
    {
        if (logPanelRect == null) return;
        logPanelRect.DOKill();

        if (!isOpen)
        {
            // 🔓 [열기] 화면 안 왼쪽 방향(X = 0)으로 슬라이드하며 등장!
            logPanelRect.DOAnchorPosX(0f, duration).SetEase(showEase).SetUpdate(true);
            isOpen = true;
        }
        else
        {
            // 🔒 [닫기] 원래 있던 우측 바깥 위치로 숨김
            HideLogPanel();
        }
    }

    // 💡 [수정] 무조건 우측 바깥으로 미끄러지며 숨겨지는 강제 닫기 함수
    public void HideLogPanel()
    {
        if (logPanelRect == null) return;

        logPanelRect.DOKill();

        // 💡 X 좌표를 플러스 패널 크기(+panelWidth)로 돌려서 우측 화면 밖으로 밀어냅니다.
        logPanelRect.DOAnchorPosX(panelWidth, duration)
            .SetEase(hideEase)
            .SetUpdate(true);

        isOpen = false;
    }

    // 단서 데이터 수집용 함수 (기존 구조 유지)
    public void AddClue(ClueData newClue)
    {
        if (collectedClues.Exists(c => c.clueID == newClue.clueID)) return;
        collectedClues.Add(newClue);
        CreateClueSlot(newClue);
    }

    private void CreateClueSlot(ClueData clue)
    {
        if (clueSlotPrefab == null || clueContainer == null) return;
        GameObject slotGo = Instantiate(clueSlotPrefab, clueContainer);
        ClueSlot slotScript = slotGo.GetComponent<ClueSlot>();
        if (slotScript != null) slotScript.SetClueUI(clue);
    }
}
