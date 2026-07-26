using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class DocumentBubbleController : MonoBehaviour, IPointerClickHandler
{
    [Header("로딩바 UI")]
    [SerializeField] private GameObject loadingGroup;   // 로딩바 전체를 감싸는 오브젝트
    [SerializeField] private Image loadingFillImage;    // Fill Amount로 차오르는 이미지
    [SerializeField] private TMP_Text loadingText;       // 💡 [추가] "분석중..." 등 진행 중 문구 (완료 시 숨김)

    [Header("완료 표시 UI")]
    [SerializeField] private TMP_Text completeText;
    [SerializeField] private string completeMessage = "다운로드 완료";

    [Header("기본값")]
    [SerializeField] private float defaultLoadingDuration = 2f;

    private string targetDocumentID;
    private float loadingDuration;

    private bool isLoading = false;
    private bool isLoadingComplete = false;

    public void Setup(DialogueData data)
    {
        targetDocumentID = data.documentID;
        loadingDuration = data.bubbleLoadingDuration > 0f ? data.bubbleLoadingDuration : defaultLoadingDuration;

        isLoading = false;
        isLoadingComplete = false;

        if (loadingGroup != null) loadingGroup.SetActive(false);
        if (loadingFillImage != null) loadingFillImage.fillAmount = 0f;

        // 💡 진행 중 문구는 다시 보이게, 완료 문구는 숨김
        if (loadingText != null) loadingText.gameObject.SetActive(true);
        if (completeText != null)
        {
            completeText.text = completeMessage;
            completeText.gameObject.SetActive(false);
        }
    }

    private IEnumerator LoadThenOpenDocument(float duration)
    {
        isLoading = true;

        if (loadingGroup != null) loadingGroup.SetActive(true);
        if (loadingFillImage != null) loadingFillImage.fillAmount = 0f;

        if (loadingText != null) loadingText.gameObject.SetActive(true);
        if (completeText != null) completeText.gameObject.SetActive(false);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (loadingFillImage != null)
            {
                loadingFillImage.fillAmount = Mathf.Clamp01(elapsed / duration);
            }
            yield return null;
        }

        if (loadingFillImage != null) loadingFillImage.fillAmount = 1f;

        // 💡 [변경] 완료 시점에 진행 중 문구는 끄고, 완료 문구를 켬
        if (loadingText != null) loadingText.gameObject.SetActive(false);
        if (completeText != null) completeText.gameObject.SetActive(true);

        isLoading = false;
        isLoadingComplete = true;

        OpenDocument();

        Image bubbleImage = GetComponent<Image>();
        if (bubbleImage != null) bubbleImage.raycastTarget = false;
    }
    private void OpenDocument()
    {
        DocumentQuestManager targetDoc = DocumentQuestManager.GetByID(targetDocumentID);
        if (targetDoc != null)
        {
            targetDoc.OpenFromChatBubble();
        }
        else
        {
            Debug.LogWarning($"[DocumentBubbleController] documentID '{targetDocumentID}' 에 해당하는 DocumentQuestManager를 찾지 못했습니다.");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isLoading) return;
        if (isLoadingComplete) return;

        StartCoroutine(LoadThenOpenDocument(loadingDuration));

    }
}