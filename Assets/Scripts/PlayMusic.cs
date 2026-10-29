using UnityEngine;
using UnityEngine.UI;

public class PlaySoundButton : MonoBehaviour
{
    public AudioSource audioSource;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlayMusic()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("Audio Source Not Found");
        }
    }

    /// <summary>
    /// Toggles audio playback between Play and Pause.
    /// Bind this to the Info Bubble Image's Button onClick event in the Inspector.
    /// </summary>
    public void TogglePlayPause()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("Audio Source Not Found");
            return;
        }

        if (audioSource.isPlaying)
            audioSource.Pause();
        else
            audioSource.Play();
    }
}
