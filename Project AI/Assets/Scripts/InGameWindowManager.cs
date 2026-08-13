using UnityEngine;
using System.Collections;

public class InGameWindowManager : MonoBehaviour
{
    [Header("Taskbar Connection")]
    [SerializeField] private SidebarController sidebarController;
    [SerializeField] private int myTaskbarIndex = 0;

    [Header("Sub Panels (Auto Close ONLY on Complete Close)")]
    [SerializeField] private GameObject[] subPanels;

    [Header("Pop-up Animation Settings")]
    [SerializeField] private float animationSpeed = 15f;
    [SerializeField] private float scalePunchMultiplier = 1.1f;

    private Coroutine popUpCoroutine;
    private Vector3 originalScale = Vector3.one;

    // 💡 [추가] 최소화(ToggleWindowImmediate)로 인한 비활성화인지 구분하는 플래그.
    // true면 OnDisable에서 사이드바 제거를 건너뜁니다 (이미 status=1로 등록했으므로).
    private bool isMinimizingSelf = false;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        isMinimizingSelf = false; // 💡 추가: 다시 켜지면 플래그 초기화

        if (sidebarController != null)
        {
            sidebarController.UpdateTaskbarStatus(myTaskbarIndex, 2, this);
        }
    }

    // 💡 [추가] Destroy()나 그 외 어떤 경로로든 이 오브젝트가 비활성화/파괴되면
    // (최소화가 아닌 한) 사이드바에서 반드시 자신을 제거합니다.
    // PostDetailPageUI.ClosePage()/NewsCard.ClosePopup()처럼 CloseWindow()를 거치지 않고
    // 바로 Destroy하는 창들도 이걸로 안전하게 정리됩니다.
    private void OnDisable()
    {
        if (isMinimizingSelf)
        {
            isMinimizingSelf = false;
            return;
        }

        if (sidebarController != null)
        {
            sidebarController.UpdateTaskbarStatus(myTaskbarIndex, 0, this);
        }
    }

    public void ToggleWindowImmediate()
    {
        isMinimizingSelf = true; // 💡 추가: OnDisable이 제거하지 않도록 미리 표시

        if (sidebarController != null) sidebarController.UpdateTaskbarStatus(myTaskbarIndex, 1, this);

        TaskbarWindowTrigger taskbarTrigger = GetComponent<TaskbarWindowTrigger>();
        if (taskbarTrigger != null) taskbarTrigger.MarkAsMinimizing();

        this.gameObject.SetActive(false);
    }

    public void RestoreWindow()
    {
        Debug.Log($"[진단] RestoreWindow 호출됨! 대상 오브젝트: {gameObject.name}");

        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();

        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError($"[진단] {gameObject.name}에 RectTransform이 없습니다!");
        }
        else if (WindowManager.Instance == null)
        {
            Debug.LogError($"[진단] WindowManager.Instance가 null입니다!");
        }
        else
        {
            Vector2 beforePos = rectTransform.anchoredPosition;
            Vector2 sizeBefore = rectTransform.rect.size;

            Vector2 pushedPos = WindowManager.Instance.PushOutOfBlockingPanelsWithBounds(rectTransform, rectTransform.anchoredPosition);

            Debug.Log($"[진단] {gameObject.name} | size:{sizeBefore} | before:{beforePos} -> after:{pushedPos}");

            rectTransform.anchoredPosition = pushedPos;
        }

        if (subPanels != null)
        {
            foreach (var subPanel in subPanels)
            {
                if (subPanel != null && subPanel.activeSelf)
                {
                    subPanel.transform.SetAsLastSibling();
                }
            }
        }

        if (sidebarController != null) sidebarController.UpdateTaskbarStatus(myTaskbarIndex, 2, this);

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

    public void CloseWindow()
    {
        if (popUpCoroutine != null) StopCoroutine(popUpCoroutine);
        transform.localScale = originalScale;

        if (subPanels != null)
        {
            foreach (var subPanel in subPanels)
            {
                if (subPanel != null) subPanel.SetActive(false);
            }
        }

        this.gameObject.SetActive(false); // 💡 이 줄이 OnDisable을 트리거해서 사이드바 정리까지 자동으로 됨

        // 아래 줄은 OnDisable에서도 동일하게 처리되지만, 명시적으로 남겨둬도 무방합니다 (중복 호출은 안전함)
        if (sidebarController != null) sidebarController.UpdateTaskbarStatus(myTaskbarIndex, 0, this);
    }
}