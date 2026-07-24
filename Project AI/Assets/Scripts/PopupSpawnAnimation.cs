using System.Collections;
using UnityEngine;

/// <summary>
/// 💡 뉴스/커뮤니티 상세 팝업처럼 Instantiate로 새로 생성되는 개별 창에 붙여서,
/// 나타날 때 살짝 커졌다 작아지는 연출을 재생하는 재사용 컴포넌트입니다.
/// InGameWindowManager.AnimatePopUp()과 동일한 연출을 독립적으로 사용할 수 있게 분리했습니다.
/// </summary>
public class PopupSpawnAnimation : MonoBehaviour
{
    [SerializeField] private float animationSpeed = 15f;
    [SerializeField] private float scalePunchMultiplier = 1.1f;

    private Vector3 originalScale = Vector3.one;
    private Coroutine popUpCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    /// <summary>
    /// 외부(NewsListManager, CommunityManager 등)에서 창을 앞으로 가져오거나
    /// 새로 생성했을 때 호출합니다.
    /// </summary>
    public void PlayPopAnimation()
    {
        if (popUpCoroutine != null) StopCoroutine(popUpCoroutine);
        popUpCoroutine = StartCoroutine(AnimatePopUp());
    }

    private IEnumerator AnimatePopUp()
    {
        Vector3 targetMaxScale = originalScale * scalePunchMultiplier;

        while (Vector3.Distance(transform.localScale, targetMaxScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetMaxScale, Time.deltaTime * animationSpeed);
            yield return null;
        }
        transform.localScale = targetMaxScale;

        while (Vector3.Distance(transform.localScale, originalScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * animationSpeed);
            yield return null;
        }
        transform.localScale = originalScale;
    }
}