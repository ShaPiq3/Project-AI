using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScrollController : MonoBehaviour
{
    // 유니티 인스펙터창에서 Scroll View 오브젝트를 연결합니다.
    public ScrollRect scrollRect;

    // 버튼을 한 번 누를 때 이동할 속도/양 (0.1은 전체 길이의 10%를 의미)
    public float scrollSpeed = 0.1f;

    // 위로 가기 버튼을 눌렀을 때 호출할 함수
    public void ScrollUp()
    {
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition += scrollSpeed;
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
        }
    }

    // 아래로 가기 버튼을 눌렀을 때 호출할 함수
    public void ScrollDown()
    {
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition -= scrollSpeed;
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
        }
    }

    // ⭐ 스크롤바를 완전히 맨 아래(0)로 내리는 함수
    public void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            // 유니티 UI 레이아웃 계산 시간을 벌기 위해 코루틴을 실행합니다.
            StartCoroutine(MoveToBottomRoutine());
        }
    }

    private IEnumerator MoveToBottomRoutine()
    {
        // 1프레임 동안 대기하여 말풍선 크기 계산이 완전히 끝나기를 기다립니다.
        yield return new WaitForEndOfFrame();

        // verticalNormalizedPosition을 0으로 만들어 맨 아래로 보냅니다. (1은 맨 위)
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
