using UnityEngine;
using UnityEngine.UI;

public class BGM_Slider : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private AudioSource bgmAudioSource;

    private void Start()
    {
        if (bgmSlider != null && bgmAudioSource != null)
        {
            bgmSlider.value = bgmAudioSource.volume;
        }
    }

    public void OnBGMSliding()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = bgmSlider.value;
        }
    }
}
