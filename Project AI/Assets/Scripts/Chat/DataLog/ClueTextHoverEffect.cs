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

    void Awake() => textComponent = GetComponent<TMP_Text>();
    void OnEnable() => StartCoroutine(DelayedInitialize());

    private void OnDestroy()
    {
        // 💡 [추가] 오브젝트가 사라질 때 아카이브 위치 등록도 함께 해제
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

            // 💡 [추가] 아카이브 매니저에 "이 단서는 여기 있다"고 스스로 등록
            // (뉴스 등 다른 곳에서 동적으로 붙는 경우에도 무해합니다.
            //  ArchiveManager는 sourceType이 "아카이브"일 때만 조회되기 때문입니다.)
            if (ArchiveManager.Instance != null)
            {
                ArchiveManager.Instance.RegisterClueLocation(targetClueID, GetComponent<RectTransform>());
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DataLogManager.Instance == null) return;
        if (!DataLogManager.Instance.IsClueSearchModeActive) return;
        if (string.IsNullOrEmpty(targetClueWord)) return;
        textComponent.text = cleanText.Replace(targetClueWord, $"<mark={hexHighlightColor}>{targetClueWord}</mark>");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (textComponent != null) textComponent.text = cleanText;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (DataLogManager.Instance == null || !DataLogManager.Instance.IsClueSearchModeActive) return;
        eventData.Use();
        Debug.Log($"[수집 성공] ID: {targetClueID}");
        DataLogManager.Instance.AcquireClue(this.questID, this.targetClueID);
    }
}