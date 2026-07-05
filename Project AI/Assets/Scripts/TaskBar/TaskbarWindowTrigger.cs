using UnityEngine;
using System.Collections; // 👈 IEnumerator 사용을 위해 필요

public class TaskbarWindowTrigger : MonoBehaviour
{
    public enum WindowType { Archive, News }

    [Header("창 설정")]
    [SerializeField] private WindowType windowType;
    [SerializeField] private string windowTitle;

    // 🌟 [수정] OnEnable 대신 코루틴을 사용해 매니저가 완전히 준비된 후 등록하도록 만듭니다.
    private void OnEnable()
    {
        StartCoroutine(RegisterToTaskbarCo());
    }

    private IEnumerator RegisterToTaskbarCo()
    {
        // TaskbarManager가 싱글톤 인스턴스를 채울 때까지 한 프레임 대기합니다.
        yield return null;

        if (TaskbarManager.Instance == null)
        {
            Debug.LogError($"{gameObject.name}: 씬에 TaskbarManager 오브젝트가 없거나 생성되지 않았습니다!");
            yield break;
        }

        // 안전하게 매니저가 준비된 상태에서 버튼 생성 신호를 보냅니다.
        switch (windowType)
        {
            case WindowType.Archive:
                TaskbarManager.Instance.AddArchiveWindow(windowTitle, this.gameObject);
                break;
            case WindowType.News:
                TaskbarManager.Instance.AddNewsWindow(windowTitle, this.gameObject);
                break;
        }
    }

    private void OnDisable()
    {
        if (TaskbarManager.Instance == null) return;

        switch (windowType)
        {
            case WindowType.Archive:
                TaskbarManager.Instance.RemoveArchiveWindow(this.gameObject);
                break;
            case WindowType.News:
                TaskbarManager.Instance.RemoveNewsWindow(this.gameObject);
                break;
        }
    }
}