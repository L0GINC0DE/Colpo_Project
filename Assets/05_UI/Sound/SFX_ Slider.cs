using UnityEngine;
using UnityEngine.UI;

public class SFX_Slider : MonoBehaviour
{
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private AudioSource sfxAudioSource;

    private void Start()
    {
        if (sfxSlider != null && sfxAudioSource != null)
        {
            sfxSlider.value = sfxAudioSource.volume;
        }
    }

    public void OnSFXSliding()
    {
        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = sfxSlider.value;
        }
    }
}
