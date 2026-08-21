using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// "퀘스트가 시작된 시점"을 자동으로 저장/복원하는 체크포인트 시스템.
/// ChatThreadController가 퀘스트 트리거 행(isTrigger/isDocumentBubble/isImageGenTrigger)을
/// 재생할 때마다 자동으로 최신 체크포인트 하나를 덮어쓴다. (DocumentQuestManager와 동일하게
/// PlayerPrefs + JsonUtility로 저장한다.)
/// </summary>
public static class CheckpointManager
{
    private const string PlayerPrefsKey = "Checkpoint_Latest";

    /// <summary> 씬 재로드 이후 대기 중인 복원 요청이 있는지. 같은 플레이 세션 동안만 유효한 static 플래그. </summary>
    public static bool PendingRestore { get; private set; } = false;

    public static void SaveCheckpoint(string sceneName, string contactID, int dialogueID)
    {
        if (string.IsNullOrEmpty(contactID)) return;

        var data = new CheckpointData
        {
            sceneName = sceneName,
            contactID = contactID,
            dialogueID = dialogueID
        };

        PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static bool TryLoadCheckpoint(out CheckpointData data)
    {
        data = null;
        if (!PlayerPrefs.HasKey(PlayerPrefsKey)) return false;

        string json = PlayerPrefs.GetString(PlayerPrefsKey);
        if (string.IsNullOrEmpty(json)) return false;

        data = JsonUtility.FromJson<CheckpointData>(json);
        return data != null;
    }

    /// <summary>
    /// GameOverScene의 "이어하기" 버튼에서 호출. 체크포인트가 있으면 그 씬을 불러오고
    /// 복원 플래그를 세운다 (씬 로드 후 ChatCoordinator가 ConsumePendingRestore로 이어받는다).
    /// </summary>
    public static bool RestoreToCheckpoint()
    {
        if (!TryLoadCheckpoint(out var data) || string.IsNullOrEmpty(data.sceneName))
        {
            Debug.LogWarning("[CheckpointManager] 저장된 체크포인트가 없습니다.");
            return false;
        }

        PendingRestore = true;
        SceneManager.LoadScene(data.sceneName);
        return true;
    }

    /// <summary>
    /// 씬 로드 직후 ChatCoordinator.Start()가 호출. 대기 중인 복원 요청을 소비하고
    /// 체크포인트 데이터를 돌려준다 (contactID/dialogueID로 점프시키는 건 호출부 책임).
    /// </summary>
    public static bool ConsumePendingRestore(out CheckpointData data)
    {
        data = null;
        if (!PendingRestore) return false;

        PendingRestore = false;
        return TryLoadCheckpoint(out data);
    }
}
