using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Then/Now historical comparison panel.
/// Manages image opacity, shader saturation, historical soundscapes, photo gallery synchronization,
/// and exit callbacks. Can be used standalone or orchestrated by M01Controller/FlowManager.
/// </summary>
public class HistoricalComparisonController : MonoBehaviour
{
    [Header("Visual & Saturation")]
    [Tooltip("The main UI Image displaying the historical archival photo.")]
    public Image historicalImage;

    [Tooltip("Material running UI_Saturation shader with a '_Saturation' float property.")]
    public Material saturationMaterial;

    [Tooltip("Slider controlling Then/Now opacity and audio soundscape volume.")]
    public Slider opacitySlider;

    [Tooltip("Default opacity when panel is opened (0.0 = completely transparent, 1.0 = fully historical).")]
    [Range(0f, 1f)]
    public float defaultInitialOpacity = 0.5f;

    [Header("Audio & Gallery")]
    [Tooltip("AudioSource playing the historic soundscape (e.g. Cathedral bells, choir).")]
    public AudioSource historicalAudioSource;

    [Tooltip("Photo gallery component handling swipes and photo switching.")]
    public PhotoGalleryController photoGallery;

    [Tooltip("Button used to exit the comparison view.")]
    public Button exitButton;

    /// <summary>
    /// Event invoked when the user exits the historical comparison panel.
    /// Useful for notifying M01Controller / Flow Coordinators to advance the journey.
    /// </summary>
    public event Action onComparisonClosed;

    private void Awake()
    {
        AutoWireComponents();
        WireListeners();
    }

    private void Start()
    {
        // Apply material to image if assigned
        if (historicalImage != null && saturationMaterial != null)
        {
            historicalImage.material = saturationMaterial;
        }

        // Configure audio source defaults
        if (historicalAudioSource != null)
        {
            historicalAudioSource.loop = true;
            historicalAudioSource.playOnAwake = false;
            historicalAudioSource.Stop();
            historicalAudioSource.volume = 0f;
        }

        // Sync initial photo audio if gallery is present
        if (photoGallery != null)
        {
            OnHistoricalPhotoChanged(0, photoGallery.GetCurrentAudio());
        }
    }

    /// <summary>
    /// Automatically searches child transforms and self to wire missing references.
    /// Prevents manual drag-and-drop mistakes in the Inspector.
    /// </summary>
    public void AutoWireComponents()
    {
        if (photoGallery == null)
        {
            photoGallery = GetComponentInChildren<PhotoGalleryController>(true);
        }

        if (opacitySlider == null)
        {
            opacitySlider = GetComponentInChildren<Slider>(true);
        }

        if (historicalImage == null)
        {
            if (photoGallery != null && photoGallery.displayImage != null)
            {
                historicalImage = photoGallery.displayImage;
            }
            else
            {
                Transform imgTr = transform.Find("HistoricalImage");
                if (imgTr != null) historicalImage = imgTr.GetComponent<Image>();
            }
        }

        if (exitButton == null)
        {
            Transform btnTr = transform.Find("ExitButton") ?? transform.Find("Button");
            if (btnTr != null) exitButton = btnTr.GetComponent<Button>();
        }

        if (historicalAudioSource == null)
        {
            historicalAudioSource = GetComponent<AudioSource>();
            if (historicalAudioSource == null)
            {
                historicalAudioSource = GetComponentInParent<AudioSource>();
            }
        }
    }

    private void WireListeners()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(CloseComparison);
            exitButton.onClick.AddListener(CloseComparison);
        }

        if (opacitySlider != null)
        {
            opacitySlider.onValueChanged.RemoveListener(UpdateOpacity);
            opacitySlider.onValueChanged.AddListener(UpdateOpacity);
        }

        if (photoGallery != null)
        {
            photoGallery.onPhotoChanged -= OnHistoricalPhotoChanged;
            photoGallery.onPhotoChanged += OnHistoricalPhotoChanged;
        }
    }

    /// <summary>
    /// Opens the historical comparison panel, resets the slider, and activates audio/visuals.
    /// </summary>
    public void OpenComparison()
    {
        gameObject.SetActive(true);

        float targetOpacity = defaultInitialOpacity;
        if (opacitySlider != null)
        {
            opacitySlider.SetValueWithoutNotify(targetOpacity);
        }

        UpdateOpacity(targetOpacity);
    }

    /// <summary>
    /// Updates historical photo alpha, saturation shader, and soundscape volume.
    /// </summary>
    /// <param name="value">Normalized opacity between 0.0 and 1.0.</param>
    public void UpdateOpacity(float value)
    {
        value = Mathf.Clamp01(value);

        // 1. Update Historical Image Alpha and Material Saturation
        if (historicalImage != null)
        {
            Color c = historicalImage.color;
            c.a = value;
            historicalImage.color = c;

            Material mat = saturationMaterial != null ? saturationMaterial : historicalImage.material;
            if (mat != null && mat.HasProperty("_Saturation"))
            {
                mat.SetFloat("_Saturation", value);
            }
        }

        // 2. Update Historical Audio Volume & Playback
        if (historicalAudioSource != null)
        {
            historicalAudioSource.volume = value;

            bool isPanelActive = gameObject.activeInHierarchy;
            if (isPanelActive && value > 0.001f && historicalAudioSource.clip != null && !historicalAudioSource.isPlaying)
            {
                historicalAudioSource.Play();
            }
            else if (value <= 0.001f && historicalAudioSource.isPlaying)
            {
                historicalAudioSource.Pause();
            }
        }
    }

    /// <summary>
    /// Callback when PhotoGallery switches photos: updates the soundscape clip and refreshes volume.
    /// </summary>
    private void OnHistoricalPhotoChanged(int index, AudioClip clip)
    {
        if (historicalAudioSource == null) return;

        historicalAudioSource.Stop();
        historicalAudioSource.clip = clip;

        float currentVal = opacitySlider != null ? opacitySlider.value : defaultInitialOpacity;
        UpdateOpacity(currentVal);
    }

    private bool isClosing = false;

    /// <summary>
    /// Closes the comparison panel, pauses/stops audio, and invokes onComparisonClosed.
    /// </summary>
    public void CloseComparison()
    {
        if (isClosing) return;
        isClosing = true;

        if (historicalAudioSource != null)
        {
            historicalAudioSource.Stop();
            historicalAudioSource.volume = 0f;
        }

        gameObject.SetActive(false);

        try
        {
            onComparisonClosed?.Invoke();
        }
        finally
        {
            isClosing = false;
        }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        AutoWireComponents();
    }
#endif
}

