using UnityEngine;

public class GameExit : MonoBehaviour
{
    public void QuitGame()
    {
        // 에디터에서는 종료되지 않으므로 로그를 남깁니다.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}