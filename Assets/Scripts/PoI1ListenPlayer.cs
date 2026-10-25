using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PoI1ListenPlayer : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Assign the choir recording here when it is available. The player stays disabled if no clip is assigned.")]
    public AudioClip choirClip;
    public AudioSource audioSource;

    [Header("Compact Screen-Space Player")]
    public GameObject playerPanel;
    public Image progressTrack;
    public Image progressRing;
    public Button playPauseButton;
    public TMP_Text playPauseIcon;
    public TMP_Text statusText;
    public TMP_Text contextText;
    public Button closeButton;

    [Header("Listening Context")]
    [TextArea(2, 4)]
    public string openingPrompt = "Press play and listen to the choir in this place. As you listen, notice how the voices shape the space.";
    [TextArea(2, 4)]
    public string[] contextSteps =
    {
        "Choral voices turn a large room into a shared acoustic space.",
        "In Christian worship, singing carries hymns and prayers and helps shape the rhythm of a service.",
        "Singing together can make a service feel communal, with familiar melodies passed between generations.",
        "For an individual, a hymn or a familiar voice can hold memories of family, worship and belonging."
    };

    bool pausedByUser;
    bool wasPlaying;
    bool completed;
    float lastProgress;
    int displayedStep = -1;

    void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.clip = choirClip;

        if (playerPanel != null) playerPanel.SetActive(false);

        if (playPauseButton != null)
        {
            playPauseButton.onClick.RemoveListener(TogglePlayback);
            playPauseButton.onClick.AddListener(TogglePlayback);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        SetPlaybackAvailable(choirClip != null);
        SetIcon("▶");
        if (statusText != null)
            statusText.text = choirClip == null ? "Choir recording not assigned" : "Ready to listen";
        if (contextText != null) contextText.text = openingPrompt;
    }

    public void Open()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = choirClip;
            if (choirClip != null) audioSource.timeSamples = 0;
        }

        pausedByUser = false;
        wasPlaying = false;
        completed = false;
        lastProgress = 0f;
        displayedStep = -1;

        if (playerPanel != null) playerPanel.SetActive(true);
        if (contextText != null) contextText.text = openingPrompt;
        if (statusText != null)
            statusText.text = choirClip == null ? "Choir recording not assigned" : "Ready to listen";
        SetPlaybackAvailable(choirClip != null);
        SetIcon("▶");
        UpdateProgressVisual(0f);
    }

    public void Close()
    {
        if (audioSource != null) audioSource.Stop();
        pausedByUser = false;
        wasPlaying = false;
        completed = false;
        lastProgress = 0f;
        if (playerPanel != null) playerPanel.SetActive(false);
    }

    public void TogglePlayback()
    {
        if (audioSource == null || choirClip == null) return;

        if (audioSource.isPlaying)
        {
            audioSource.Pause();
            pausedByUser = true;
            SetIcon("▶");
            if (statusText != null) statusText.text = "Paused";
            return;
        }

        if (completed)
        {
            audioSource.timeSamples = 0;
            lastProgress = 0f;
            completed = false;
            displayedStep = -1;
            if (contextText != null) contextText.text = openingPrompt;
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
        if (statusText != null) statusText.text = "Playing";
    }

    void Update()
    {
        if (audioSource == null || choirClip == null) return;

        float progress = GetPlaybackProgress();
        if (audioSource.isPlaying)
        {
            wasPlaying = true;
            lastProgress = progress;
            UpdateProgressVisual(progress);
            UpdateContextStep(progress);
            return;
        }

        if (pausedByUser)
        {
            UpdateProgressVisual(progress);
            UpdateContextStep(progress);
            return;
        }

        if (wasPlaying)
        {
            wasPlaying = false;
            if (lastProgress >= 0.98f || progress >= 0.98f)
            {
                completed = true;
                lastProgress = 1f;
                UpdateProgressVisual(1f);
                UpdateContextStep(1f);
                SetIcon("✓");
                if (statusText != null) statusText.text = "Finished · Tap to replay";
            }
        }
    }

    float GetPlaybackProgress()
    {
        if (choirClip == null) return 0f;

        // timeSamples tracks the clip's actual sample position and also remains stable when paused.
        if (choirClip.samples > 0)
            return Mathf.Clamp01((float)audioSource.timeSamples / choirClip.samples);

        return choirClip.length > 0f ? Mathf.Clamp01(audioSource.time / choirClip.length) : 0f;
    }

    void UpdateProgressVisual(float progress)
    {
        if (progressTrack != null) progressTrack.fillAmount = 1f;
        if (progressRing != null) progressRing.fillAmount = Mathf.Clamp01(progress);
    }

    void UpdateContextStep(float progress)
    {
        if (contextText == null || contextSteps == null || contextSteps.Length == 0) return;
        int step = Mathf.Clamp(Mathf.FloorToInt(progress * contextSteps.Length), 0, contextSteps.Length - 1);
        if (step == displayedStep) return;
        displayedStep = step;
        contextText.text = contextSteps[step];
    }

    void SetPlaybackAvailable(bool available)
    {
        if (playPauseButton != null) playPauseButton.interactable = available;
    }

    void SetIcon(string value)
    {
        if (playPauseIcon != null) playPauseIcon.text = value;
    }
}