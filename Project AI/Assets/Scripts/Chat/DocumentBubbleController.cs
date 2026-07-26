using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; // 💡 추가
using System.Collections;

/// <summary>
/// 채팅창 안에 "문서 요약 패널 열기" 전용으로 뜨는 특수 말풍선.
/// 표시되면 대기 상태로 있다가, 클릭하면 로딩바가 차오르고 다 차면 문서 패널이 열립니다.
/// 로딩이 끝나면 "다운로드 완료" 텍스트가 표시됩니다.
/// 한 번 로딩이 끝난 뒤에는 버블을 다시 클릭하면 로딩 없이 바로 문서 패널이 재오픈됩니다.
/// </summary>
public class DocumentBubbleController : MonoBehaviour, IPointerClickHandler
{
    [Header("로딩바 UI")]
    [SerializeField] private GameObject loadingGroup;   // 로딩바 전체를 감싸는 오브젝트
    [SerializeField] private Image loadingFillImage;    // Fill Amount로 차오르는 이미지

    [Header("완료 표시 UI")]
    [SerializeField] private GameObject completeGroup;      // "다운로드 완료" 문구를 감싸는 오브젝트 (없으면 completeText만 써도 됨)
    [SerializeField] private TMP_Text completeText;         // 완료 텍스트
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

        // 💡 추가: 완료 문구는 처음엔 꺼둠
        if (completeGroup != null) completeGroup.SetActive(false);
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

        // 💡 로딩 재시작 시 완료 문구는 다시 숨김
        if (completeGroup != null) completeGroup.SetActive(false);
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

        // 💡 [추가] 로딩바가 다 찬 시점에 완료 문구 표시
        if (completeGroup != null) completeGroup.SetActive(true);
        if (completeText != null) completeText.gameObject.SetActive(true);

        isLoading = false;
        isLoadingComplete = true;

        OpenDocument();
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

        if (isLoadingComplete)
        {
            OpenDocument();
        }
        else
        {
            StartCoroutine(LoadThenOpenDocument(loadingDuration));
        }
    }
}