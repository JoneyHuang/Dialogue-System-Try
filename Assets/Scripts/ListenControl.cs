using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ListenControl : MonoBehaviour
{
    [Header("Reusable Listen content")]
    public ListenContent content;
    public ListenInfoPresenter infoPresenter;

    [Header("World-space circular player")]
    public AudioSource audioSource;
    public Image progressTrack;
    public Image progressRing;
    public Button playPauseButton;
    public TMP_Text playPauseIcon;

    bool pausedByUser;
    bool wasPlaying;
    bool completed;
    float lastProgress;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.clip = content != null ? content.recording : null;

        if (playPauseButton != null)
        {
            playPauseButton.onClick.RemoveListener(TogglePlayback);
            playPauseButton.onClick.AddListener(TogglePlayback);
        }

        SetAvailable(audioSource.clip != null);
        SetIcon("▶");
        SetProgress(0f);
    }

    void Start()
    {
        if (infoPresenter == null)
            infoPresenter = FindFirstObjectByType<ListenInfoPresenter>();
    }

    public void TogglePlayback()
    {
        if (audioSource == null || audioSource.clip == null) return;

        if (audioSource.isPlaying)
        {
            audioSource.Pause();
            pausedByUser = true;
            SetIcon("▶");
            return;
        }

        if (completed)
        {
            audioSource.timeSamples = 0;
            lastProgress = 0f;
            completed = false;
        }

        if (pausedByUser)
        {
            audioSource.UnPause();
            pausedByUser = false;
        }
        else
        {
            audioSource.Play();
        }

        wasPlaying = true;
        SetIcon("Ⅱ");

        if (infoPresenter == null)
            infoPresenter = FindFirstObjectByType<ListenInfoPresenter>();
        if (infoPresenter != null && content != null && !infoPresenter.IsVisible)
            infoPresenter.ShowTeaser(content);
    }

    void Update()
    {
        if (audioSource == null || audioSource.clip == null) return;

        float progress = GetPlaybackProgress();
        if (audioSource.isPlaying)
        {
            wasPlaying = true;
            lastProgress = progress;
            SetProgress(progress);
            return;
        }

        if (pausedByUser) return;

        if (wasPlaying)
        {
            wasPlaying = false;
            if (lastProgress >= .98f || progress >= .98f)
            {
                completed = true;
                lastProgress = 1f;
                SetProgress(1f);
                SetIcon("✓");
            }
        }
    }

    float GetPlaybackProgress()
    {
        AudioClip clip = audioSource.clip;
        if (clip.samples > 0)
            return Mathf.Clamp01((float)audioSource.timeSamples / clip.samples);
        return clip.length > 0f ? Mathf.Clamp01(audioSource.time / clip.length) : 0f;
    }

    void SetProgress(float value)
    {
        if (progressTrack != null) progressTrack.fillAmount = 1f;
        if (progressRing != null) progressRing.fillAmount = Mathf.Clamp01(value);
    }

    void SetAvailable(bool available)
    {
        if (playPauseButton != null) playPauseButton.interactable = available;
    }

    void SetIcon(string value)
    {
        if (playPauseIcon != null) playPauseIcon.text = value;
    }
}