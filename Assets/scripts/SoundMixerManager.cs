using UnityEngine;
using UnityEngine.Audio;

public class SoundMixerManager : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    public void setMasterVolume (float level)
    {
        audioMixer.SetFloat("masterVolume",Mathf.Log10(level)*20f);
    }
    public void setSoundFXVolume(float level)
    {
        audioMixer.SetFloat("soundFXVolume", Mathf.Log10(level) * 20f);
    }
}
