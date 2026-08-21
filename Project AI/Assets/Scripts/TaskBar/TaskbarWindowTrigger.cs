using UnityEngine;
using System.Collections;

public class TaskbarWindowTrigger : MonoBehaviour
{
    public enum WindowType { Archive, News, Community, HumanDB }
    [Header("창 설정")]
    [SerializeField] private WindowType windowType;
    [SerializeField] private string windowTitle;

    private bool isMinimizing = false;

    // 💡 [추가] 최소화 때문에 OnDisable에서 제거를 건너뛰었는지 기억.
    // true면, 다음 OnEnable(복원)에서도 재등록을 건너뛰어야 함 (이미 taskbar에 등록되어 있으므로).
    private bool wasMinimized = false;

    public void SetWindowTitle(string title)
    {
        windowTitle = title;
    }

    public void MarkAsMinimizing()
    {
        isMinimizing = true;
    }

    private void OnEnable()
    {
        // 💡 [수정] 방금 최소화 상태에서 복원된 거라면, 이미 taskbar에 등록되어 있으므로
        // 재등록하지 않고 건너뜁니다.
        if (wasMinimized)
        {
            wasMinimized = false;
            return;
        }

        StartCoroutine(RegisterToTaskbarCo());
    }

    private IEnumerator RegisterToTaskbarCo()
    {
        yield return null;
        if (TaskbarManager.Instance == null)
        {
            Debug.LogError($"{gameObject.name}: 씬에 TaskbarManager 오브젝트가 없거나 생성되지 않았습니다!");
            yield break;
        }
        switch (windowType)
        {
            case WindowType.Archive:
                TaskbarManager.Instance.AddArchiveWindow(windowTitle, this.gameObject);
                break;
            case WindowType.News:
                TaskbarManager.Instance.AddNewsWindow(windowTitle, this.gameObject);
                break;
            case WindowType.Community:
                TaskbarManager.Instance.AddCommunityWindow(windowTitle, this.gameObject);
                break;
            case WindowType.HumanDB:
                TaskbarManager.Instance.AddHumanDBWindow(windowTitle, this.gameObject);
                break;
        }
    }

    private void OnDisable()
    {
        // 💡 [수정] 최소화로 인한 비활성화라면 taskbar 항목을 지우지 않고,
        // 다음 복원 시 재등록도 건너뛰도록 플래그를 세팅
        if (isMinimizing)
        {
            isMinimizing = false;
            wasMinimized = true; // 💡 추가
            return;
        }

        if (TaskbarManager.Instance == null) return;

        Debug.Log("TaskbarWindowTrigger OnDisable called! windowType=" + windowType);

        switch (windowType)
        {
            case WindowType.Archive:
                TaskbarManager.Instance.RemoveArchiveWindow(this.gameObject);
                break;
            case WindowType.News:
                TaskbarManager.Instance.RemoveNewsWindow(this.gameObject);
                break;
            case WindowType.Community:
                TaskbarManager.Instance.RemoveCommunityWindow(this.gameObject);
                break;
            case WindowType.HumanDB:
                TaskbarManager.Instance.RemoveHumanDBWindow(this.gameObject);
                break;
        }
    }
}