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

    private System.Collections.IEnumerator DelayedInitialize()
    {
        yield return new WaitForEndOfFrame();
        if (textComponent != null)
        {
            cleanText = Regex.Replace(textComponent.text, @"<[^>]*>", "");
            textComponent.text = cleanText;

            // 💡 엑셀 초기화 여부 로그 확인 (초기화가 안 되면 클릭도 안 될 수 있음)
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
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("마우스 진입 시도!"); // 이 로그가 찍히나요?

        if (DataLogManager.Instance == null) { Debug.Log("Manager 없음"); return; }
        if (!DataLogManager.Instance.IsClueSearchModeActive) { Debug.Log("검색 모드 꺼짐"); return; }
        if (string.IsNullOrEmpty(targetClueWord)) { Debug.Log("단어 정보 없음"); return; }

        textComponent.text = cleanText.Replace(targetClueWord, $"<mark={hexHighlightColor}>{targetClueWord}</mark>");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (textComponent != null) textComponent.text = cleanText;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 💡 트리거 체크 삭제: 단서 수집 모드만 확인
        if (DataLogManager.Instance == null || !DataLogManager.Instance.IsClueSearchModeActive) return;
        eventData.Use();

        Debug.Log($"[수집 성공] ID: {targetClueID}");
        DataLogManager.Instance.AcquireClue(this.questID, this.targetClueID);
    }
}