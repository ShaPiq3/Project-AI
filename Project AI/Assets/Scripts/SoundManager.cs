using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    public AudioSource audioSource;
    public AudioClip clickClip;

    void Awake()
    {
        Instance = this;
        // AudioSource가 없다면 자동 추가
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayClickSound()
    {
        if (clickClip != null)
        {
            audioSource.PlayOneShot(clickClip);
        }
    }
}