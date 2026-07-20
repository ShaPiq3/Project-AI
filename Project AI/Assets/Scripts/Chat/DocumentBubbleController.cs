using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 채팅창 안에 "문서 요약 패널 열기" 전용으로 뜨는 특수 말풍선.
/// 표시되면 로딩바가 차오르고, 다 차면 버튼 없이 바로 문서 패널이 열립니다.
/// </summary>
public class DocumentBubbleController : MonoBehaviour
{
    [Header("로딩바 UI")]
    [SerializeField] private GameObject loadingGroup;   // 로딩바 전체를 감싸는 오브젝트
    [SerializeField] private Image loadingFillImage;    // Fill Amount로 차오르는 이미지

    [Header("기본값")]
    [SerializeField] private float defaultLoadingDuration = 2f;

    private string targetDocumentID;

    /// <summary>
    /// ChatDialogueManager가 이 버블을 생성한 직후 호출합니다.
    /// </summary>
    public void Setup(DialogueData data)
    {
        targetDocumentID = data.documentID;

        float duration = data.bubbleLoadingDuration > 0f ? data.bubbleLoadingDuration : defaultLoadingDuration;

        // 시작 상태: 로딩바 보임
        if (loadingGroup != null) loadingGroup.SetActive(true);
        if (loadingFillImage != null) loadingFillImage.fillAmount = 0f;

        StartCoroutine(LoadThenOpenDocument(duration));
    }

    private IEnumerator LoadThenOpenDocument(float duration)
    {
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

        // 💡 로딩이 끝나면 버튼 없이 바로 문서 패널을 엽니다.
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
}