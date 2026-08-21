using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameOverScene에 붙이는 컨트롤러. 버튼 2개("타이틀로" / "이어하기")를 처리한다.
/// "이어하기"는 CheckpointManager에 저장된 가장 최근 퀘스트 시작 지점으로 되돌아간다.
/// </summary>
public class GameOverController : MonoBehaviour
{
    [SerializeField] private string startSceneName = "StartScene";

    public void GoToTitle()
    {
        SceneManager.LoadScene(startSceneName);
    }

    public void ResumeFromCheckpoint()
    {
        if (!CheckpointManager.RestoreToCheckpoint())
        {
            Debug.LogWarning("[GameOverController] 복귀할 체크포인트가 없어 타이틀로 이동합니다.");
            GoToTitle();
        }
    }
}
