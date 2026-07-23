using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Text.RegularExpressions;

public class ClueTextHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private TMP_Text textComponent;
    private string cleanText;
    private string targetClueWord = "";
    private bool isInitialized = false;
    [SerializeField] private string targetClueID;
    [SerializeField] private string hexHighlightColor = "#FFFF00AA";
    [SerializeField] private string questID;

    // 💡 [추가] 실제 뉴스 기사/게시글 제목. NewsCard 등이 동적으로 값을 넣어주면
    // 엑셀 SourceTitle 대신 이 값을 그대로 DataLog에 표시합니다.
    // Inspector에서 직접 입력해도 되고(아카이브 수동 태깅), 비워두면 기존처럼 엑셀 값 사용.
    [SerializeField] private string sourceTitleOverride;

    void Awake() => textComponent = GetComponent<TMP_Text>();
    void OnEnable() => StartCoroutine(DelayedInitialize());

    private void OnDestroy()
    {
        // 오브젝트가 사라질 때 아카이브 위치 등록도 함께 해제
        if (ArchiveManager.Instance != null && !string.IsNullOrEmpty(targetClueID))
        {
            ArchiveManager.Instance.UnregisterClueLocation(targetClueID);
        }
    }

    private System.Collections.IEnumerator DelayedInitialize()
    {
        yield return new WaitForEndOfFrame();
        if (textComponent != null)
        {
            cleanText = Regex.Replace(textComponent.text, @"<[^>]*>", "");
            textComponent.text = cleanText;
            TryInitializeFromExcel();
        }
    }

    private void TryInitializeFromExcel()
    {
        if (isInitialized || string.IsNullOrEmpty(targetClueID)) return;
        if (DataLogManager.Instance == null) return;
        ClueData clueData = DataLogManager.Instance.GetClueData(targetClueID.Trim());
        if (clueData != null && !string.IsNullOrEmpty(clueData.contentText))
        {
            targetClueWord = Regex.Replace(clueData.contentText.Trim(), @"<[^>]*>", "");
            isInitialized = true;
            // 아카이브 매니저에 "이 단서는 여기 있다"고 스스로 등록
            if (ArchiveManager.Instance != null)
            {
                ArchiveManager.Instance.RegisterClueLocation(targetClueID, GetComponent<RectTransform>());
            }
        }
    }

    /// <summary>
    /// 이 단서가 지금 상호작용 가능한 상태인지 공통으로 체크합니다.
    /// (수집 모드가 켜져 있고, 이 단서가 속한 퀘스트가 실제로 시작된 상태여야 함)
    /// </summary>
    private bool IsInteractable()
    {
        if (DataLogManager.Instance == null) return false;
        if (!DataLogManager.Instance.IsClueSearchModeActive) return false;
        if (!DataLogManager.Instance.IsQuestActive(questID)) return false;
        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        if (string.IsNullOrEmpty(targetClueWord)) return;
        textComponent.text = cleanText.Replace(targetClueWord, $"<mark={hexHighlightColor}>{targetClueWord}</mark>");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (textComponent != null) textComponent.text = cleanText;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        eventData.Use();
        Debug.Log($"[수집 성공] ID: {targetClueID}");
        // 💡 [변경] 실제 제목(sourceTitleOverride)이 세팅되어 있으면 그걸 같이 전달
        DataLogManager.Instance.AcquireClue(this.questID, this.targetClueID, this.sourceTitleOverride);
    }
}