using UnityEngine;
using UnityEngine.InputSystem; // 새 Input System 패키지 사용

/// <summary>
/// ClueFilterPanel(단서 수집 모드 패널) 오브젝트에 붙여서 사용합니다.
/// 이 패널이 활성화되어 있는 동안 ESC 키 또는 마우스 우클릭을 감지하여
/// 1) InGameWindowManager.CloseWindow() 로 창/사이드바 상태를 정상적으로 닫고
/// 2) DataLogManager.ToggleClueSearchMode() 로 단서 수집 모드 플래그를 끕니다.
///
/// UI 이벤트(OnPointerClick) 대신 Input 폴링 방식을 사용하는 이유:
/// ClueFilterPanel의 Image는 Raycast Target이 꺼져 있어야
/// 아래에 있는 단서 텍스트(뉴스 기사 등)의 좌클릭/호버가 정상 작동합니다.
/// 만약 우클릭 감지를 위해 Raycast Target을 다시 켜면 좌클릭까지 같이 막혀버립니다.
/// Update()에서 마우스 버튼을 직접 확인하면 이 문제를 피할 수 있습니다.
/// </summary>
[RequireComponent(typeof(InGameWindowManager))]
public class ClueFilterPanelCloser : MonoBehaviour
{
    private InGameWindowManager windowManager;

    void Awake()
    {
        windowManager = GetComponent<InGameWindowManager>();
    }

    void Update()
    {
        // 이 오브젝트가 활성화되어 있을 때만 Update가 호출되므로
        // 패널이 켜져 있는 동안에만 자동으로 체크됩니다.

        bool escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool rightClickPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

        if (!escPressed && !rightClickPressed) return;

        if (DataLogManager.Instance == null || !DataLogManager.Instance.IsClueSearchModeActive) return;

        // 1. 창/사이드바 상태를 정상적인 닫기 절차로 정리
        //    (팝업 애니메이션 정지, 서브패널 닫기, 사이드바 아이콘 상태 갱신 포함)
        if (windowManager != null)
        {
            windowManager.CloseWindow();
        }

        // 2. 단서 수집 모드 로직 플래그 및 관련 필터 UI 상태 정리
        DataLogManager.Instance.ToggleClueSearchMode();
    }
}