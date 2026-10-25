using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PoI1InteractionController : MonoBehaviour
{
    public static PoI1InteractionController Instance { get; private set; }
    public enum MarkerKind { Pin, Text, Listen }

    [Header("Screen Space Detail Panel")]
    public GameObject detailPanel;
    public GameObject textMarkerObject;
    public GameObject listenMarkerObject;
    public PoI1ListenPlayer listenPlayer;
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public Image contentImage;
    public Button closeButton;
    public Button audioButton;
    public TMP_Text audioButtonLabel;
    public TMP_Text audioVolumeLabel;
    public Slider audioVolumeSlider;
    public Button storyButton;

    [Header("Location Introduction")]
    [TextArea(3, 6)] public string locationTitle = "Citizen's War Memorial";
    [TextArea(3, 8)] public string locationBody = "You are standing next to the Citizen's War Memorial, where Anzac memorial services have traditionally taken place.";

    [Header("Content")]
    [TextArea(3, 8)]
    public string storyTitle = "Cathedral Square | Susan's memory";
    [TextArea(5, 12)]
    public string storyBody = "Growing up in Christchurch, Susan remembers getting lost on a school trip around Cathedral Square. Her teacher, Mr Watson, told the students to look for the Cathedral spire — the tallest building in town — to find their way. The tower and spire still hold special memories for her.";
    public Sprite storyImage;
    public AudioClip storyAudio;

    [Header("Audio")]
    [Range(0f, 1f)] public float defaultAudioVolume = 0.8f;
    public AudioSource audioSource;

    void Awake()
    {
        Instance = this;
        if (textMarkerObject == null) textMarkerObject = transform.Find("Text")?.gameObject;
        if (listenMarkerObject == null) listenMarkerObject = transform.Find("Listen")?.gameObject;
        SetSupplementaryMarkersActive(false);
        if (listenPlayer == null) listenPlayer = GetComponent<PoI1ListenPlayer>();
        // The compact Listen player owns its own AudioSource. Do not auto-grab or create one here.
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        if (detailPanel != null) detailPanel.SetActive(false);
        if (listenPlayer != null) listenPlayer.Close();
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }
        if (audioButton != null)
        {
            audioButton.onClick.RemoveListener(ToggleAudio);
            audioButton.onClick.AddListener(ToggleAudio);
        }
        if (storyButton != null)
        {
            storyButton.onClick.RemoveListener(StartSusanDialogue);
            storyButton.onClick.AddListener(StartSusanDialogue);
        }
        if (audioVolumeSlider != null)
        {
            audioVolumeSlider.minValue = 0f;
            audioVolumeSlider.maxValue = 1f;
            audioVolumeSlider.SetValueWithoutNotify(defaultAudioVolume);
            audioVolumeSlider.onValueChanged.RemoveListener(SetAudioVolume);
            audioVolumeSlider.onValueChanged.AddListener(SetAudioVolume);
            SetAudioVolume(defaultAudioVolume);
        }
    }

    public void Open(MarkerKind kind)
    {
        Close();
        if (kind == MarkerKind.Listen)
        {
            if (listenPlayer != null) listenPlayer.Open();
            return;
        }

        if (detailPanel == null) return;
        if (titleText != null) titleText.text = storyTitle;
        if (bodyText != null) bodyText.text = storyBody;
        if (storyButton != null) storyButton.gameObject.SetActive(false);
        SetAudioControlsVisible(false);
        if (contentImage != null)
        {
            contentImage.sprite = storyImage;
            contentImage.enabled = storyImage != null;
            contentImage.preserveAspect = true;
        }
        detailPanel.SetActive(true);
    }

    public void OpenLocationIntro()
    {
        Close();
        if (detailPanel == null) return;
        if (titleText != null) titleText.text = locationTitle;
        if (bodyText != null) bodyText.text = locationBody;
        if (contentImage != null)
        {
            contentImage.sprite = storyImage;
            contentImage.enabled = storyImage != null;
            contentImage.preserveAspect = true;
        }
        if (storyButton != null) storyButton.gameObject.SetActive(true);
        SetAudioControlsVisible(false);
        detailPanel.SetActive(true);
    }

    public void StartSusanDialogue()
    {
        Close();
        M01Controller controller = M01Controller.Instance;
        if (controller == null) controller = FindFirstObjectByType<M01Controller>();
        if (controller != null) controller.StartDialogue();
        else Debug.LogWarning("PoI1InteractionController: M01Controller was not found.");
    }

    void SetAudioControlsVisible(bool visible)
    {
        if (audioButton != null) audioButton.gameObject.SetActive(visible);
        if (audioVolumeLabel != null) audioVolumeLabel.gameObject.SetActive(visible);
        if (audioVolumeSlider != null) audioVolumeSlider.gameObject.SetActive(visible);
    }

    public void SetSupplementaryMarkersActive(bool active)
    {
        if (textMarkerObject != null) textMarkerObject.SetActive(active);
        if (listenMarkerObject != null) listenMarkerObject.SetActive(active);
    }

    public void Close()
    {
        if (audioSource != null) audioSource.Stop();
        if (detailPanel != null) detailPanel.SetActive(false);
        if (listenPlayer != null) listenPlayer.Close();
    }

    public void ToggleAudio()
    {
        if (audioSource == null || audioSource.clip == null) return;

        if (audioSource.isPlaying)
        {
            audioSource.Pause();
            SetAudioButtonLabel("Play audio");
        }
        else
        {
            audioSource.Play();
            SetAudioButtonLabel("Pause audio");
        }
    }

    public void SetAudioVolume(float value)
    {
        if (audioSource != null) audioSource.volume = Mathf.Clamp01(value);
    }

    void SetAudioButtonLabel(string label)
    {
        if (audioButtonLabel != null) audioButtonLabel.text = label;
    }
}