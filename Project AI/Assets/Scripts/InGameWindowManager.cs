using System.Collections;
using UnityEngine;

public class InGameWindowManager : MonoBehaviour
{
    [Header("Taskbar Connection")]
    [SerializeField] private SidebarController sidebarController;
    [SerializeField] private int myTaskbarIndex = 0;

    [Header("Sub Panels (Auto Close ONLY on Complete Close)")]
    [SerializeField] private GameObject[] subPanels;
    
    [Header("Pop-up Animation Settings")]
    [SerializeField] private float animationSpeed = 15f;    // 애니메이션 속도
    [SerializeField] private float scalePunchMultiplier = 1.1f; // 최대로 커질 때의 배율 (1.1 = 110%)

    private Coroutine popUpCoroutine;
    private Vector3 originalScale = Vector3.one;

    private void Awake()
    {
        // 원래 오브젝트의 스케일을 기억해 둡니다. (기본값 Vector3.one)
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (sidebarController != null)
        {
            sidebarController.UpdateTaskbarStatus(myTaskbarIndex, 2);
        }
    }

    public void ToggleWindowImmediate()
    {
        if (sidebarController != null) sidebarController.UpdateTaskbarStatus(myTaskbarIndex, 1);
        this.gameObject.SetActive(false);
    }

    // ★ [사이드바 내부의 메뉴 그룹 버튼]에 연결하는 핵심 함수
    public void RestoreWindow()
    {
        // 1. 창 전체를 즉시 화면에 활성화합니다.
        this.gameObject.SetActive(true);

        // 2. 이 창을 형제 오브젝트들 중 가장 맨 아래(화면 맨 앞)로 강제 정렬합니다!
        this.transform.SetAsLastSibling();

        // 3. 만약 서브 패널이 최소화 전부터 켜져 있었다면 서브 패널을 내 메인 화면보다 더 앞으로 강제 정렬
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

        // 4. 사이드바에게 다시 켜짐 신호를 보냅니다.
        if (sidebarController != null) sidebarController.UpdateTaskbarStatus(myTaskbarIndex, 2);

        // ★ [새로 추가] 맨 앞으로 나올 때 잠깐 커졌다 작아지는 효과 실행
        if (popUpCoroutine != null) StopCoroutine(popUpCoroutine);
        popUpCoroutine = StartCoroutine(AnimatePopUp());
    }

    // ★ [새로 추가] 팝업 연출 코루틴
    private IEnumerator AnimatePopUp()
    {
        // 1단계: 타겟 크기를 원래 크기보다 살짝 크게 잡고 Lerp로 빠르게 키웁니다.
        Vector3 targetMaxScale = originalScale * scalePunchMultiplier;

        while (Vector3.Distance(transform.localScale, targetMaxScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetMaxScale, Time.deltaTime * animationSpeed);
            yield return null;
        }
        transform.localScale = targetMaxScale;

        // 2단계: 다시 원래 정상 크기로 되돌립니다.
        while (Vector3.Distance(transform.localScale, originalScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * animationSpeed);
            yield return null;
        }
        transform.localScale = originalScale;
    }

    public void CloseWindow()
    {
        // 창이 닫힐 때는 코루틴을 멈추고 스케일을 원상복구해 둡니다.
        if (popUpCoroutine != null) StopCoroutine(popUpCoroutine);
        transform.localScale = originalScale;

        if (subPanels != null)
        {
            foreach (var subPanel in subPanels)
            {
                if (subPanel != null) subPanel.SetActive(false);
            }
        }

        this.gameObject.SetActive(false);

        if (sidebarController != null) sidebarController.UpdateTaskbarStatus(myTaskbarIndex, 0);
    }
}