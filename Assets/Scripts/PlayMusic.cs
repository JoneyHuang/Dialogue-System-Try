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
}