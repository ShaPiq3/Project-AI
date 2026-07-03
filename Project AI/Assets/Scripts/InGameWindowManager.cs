using UnityEngine;

public class InGameWindowManager : MonoBehaviour
{
    [Header("Taskbar Connection")]
    [SerializeField] private SidebarController sidebarController;
    [SerializeField] private int myTaskbarIndex = 0;

    [Header("Sub Panels (Auto Close ONLY on Complete Close)")]
    [SerializeField] private GameObject[] subPanels;

    private void OnEnable()
    {
        if (sidebarController != null)
        {
            sidebarController.UpdateTaskbarStatus(myTaskbarIndex, 2);
        }
    }

    // 창 안의 [최소화 버튼]에 연결하는 함수
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

        // 2. ★ [핵심 추가] 이 창을 형제 오브젝트들 중 가장 맨 아래(화면 맨 앞)로 강제 정렬합니다!
        // 이 한 줄 덕분에 다른 어떤 창들이 켜져 있더라도, 이 아이콘을 누르는 순간 무조건 화면 최전방으로 레이어가 상승합니다.
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
    }

    // 창 안의 [닫기/나가기 버튼]에 연결할 함수
    public void CloseWindow()
    {
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
