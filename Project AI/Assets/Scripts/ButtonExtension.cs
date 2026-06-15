using UnityEngine;
using UnityEngine.UI;

public static class ButtonExtension
{
    // 버튼에 사운드 재생 기능을 자동으로 추가
    public static void AddSoundOnClick(this Button button, AudioClip clip, AudioSource source)
    {
        button.onClick.AddListener(() => {
            if (source != null && clip != null)
                source.PlayOneShot(clip);
        });
    }
}