using UnityEngine;
using UnityEngine.UI;

public class Master_Slider : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    private void Start()
    {
        if (masterSlider != null && bgmAudioSource != null)
        {
            masterSlider.value = bgmAudioSource.volume;
        }
    }

    public void OnMasterSliding()
    {
        if (bgmAudioSource != null && sfxAudioSource != null)
        {
            bgmAudioSource.volume = masterSlider.value;
            sfxAudioSource.volume = masterSlider.value;
        }
    }
}