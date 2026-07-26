using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    public AudioSource audioSource;
    public AudioClip clickClip;

    [Header("알림/효과음 클립")]
    [SerializeField] private AudioClip notification06; // DataLog/이미지 생성 창 오픈
    [SerializeField] private AudioClip notification11; // 메신저 창 오픈
    [SerializeField] private AudioClip notification14; // 단서 수집 성공
    [SerializeField] private AudioClip notification15; // 단서 수집 모드 활성화
    [SerializeField] private AudioClip chatMessageReceived06; // NPC 말풍선 / 선택지 클릭

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this); // 컴포넌트만 제거 (오브젝트 전체를 지우면 EventSystem 자체가 사라지므로 주의)
            return;
        }

        Instance = this;
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayClickSound()
    {
        if (clickClip != null)
        {
            audioSource.PlayOneShot(clickClip);
        }
    }

    // 💡 [추가] 범용 재생 함수 (직접 클립을 넘겨서 재생하고 싶을 때)
    public void PlaySFX(AudioClip clip, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        Debug.Log($"[디버그] PlaySFX 호출됨! clip:{(clip != null ? clip.name : "null")}, 호출한 곳:{caller}");
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }


    // 💡 [추가] 용도별 전용 함수들
    public void PlayPanelOpenSound() => PlaySFX(notification06);
    public void PlayMessengerOpenSound() => PlaySFX(notification11);
    public void PlayClueCollectedSound() => PlaySFX(notification14);
    public void PlayClueSearchModeOnSound() => PlaySFX(notification15);
    public void PlayChatMessageSound() => PlaySFX(chatMessageReceived06);
}