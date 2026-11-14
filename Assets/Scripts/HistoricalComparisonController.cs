using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the past / present / future comparison experience.
/// Slider values 0..0.5 blend out the historical image; 0.5..1 reveal the AR model.
/// </summary>
public class HistoricalComparisonController : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Historical archival photo displayed by the Canvas.")]
    public Image historicalImage;

    [Tooltip("Optional UI material with a _Saturation property.")]
    public Material saturationMaterial;

    [Tooltip("Time scrubber: 0 = past, 0.5 = present, 1 = reconstructed future.")]
    public Slider opacitySlider;

    [Tooltip("AR reconstruction GameObject. It may live under a different scene parent from this Canvas.")]
    public GameObject futureModel;

    [Range(0f, 1f)]
    public float defaultInitialOpacity = 0f;

    [Header("Audio & Gallery")]
    public AudioSource historicalAudioSource;
    public PhotoGalleryController photoGallery;
    public Button exitButton;

    [Header("Responsive Slider Layout")]
    [Tooltip("Optional: assign the slider RectTransform. It is also found from the Slider if left empty.")]
    public RectTransform sliderRect;
    [Tooltip("Slider track length in pixels, applied along its active axis.")]
    public float sliderLength = 360f;
    [Tooltip("Track thickness in pixels.")]
    public float sliderThickness = 64f;
    [Tooltip("Inset from the screen edge.")]
    public float screenEdgeInset = 48f;

    public event Action onComparisonClosed;

    private RectTransform panelRect;
    private bool isClosing;
    private bool layoutWasPortrait;

    private void Awake()
    {
        AutoWireComponents();
        WireListeners();
        ApplyResponsiveLayout(true);
    }

    private void Start()
    {
        if (historicalImage != null && saturationMaterial != null)
            historicalImage.material = saturationMaterial;

        if (historicalAudioSource != null)
        {
            historicalAudioSource.loop = true;
            historicalAudioSource.playOnAwake = false;
            historicalAudioSource.Stop();
            historicalAudioSource.volume = 0f;
        }

        if (photoGallery != null)
            OnHistoricalPhotoChanged(0, photoGallery.GetCurrentAudio());

        SetFutureModelVisible(false);
        UpdateTimeline(defaultInitialOpacity);
    }

    private void Update()
    {
        ApplyResponsiveLayout(false);
    }

    public void AutoWireComponents()
    {
        if (photoGallery == null) photoGallery = GetComponentInChildren<PhotoGalleryController>(true);
        if (opacitySlider == null) opacitySlider = GetComponentInChildren<Slider>(true);
        if (opacitySlider != null && sliderRect == null) sliderRect = opacitySlider.GetComponent<RectTransform>();

        if (historicalImage == null)
        {
            if (photoGallery != null && photoGallery.displayImage != null)
                historicalImage = photoGallery.displayImage;
            else
            {
                Transform imageTransform = transform.Find("HistoricalImage");
                if (imageTransform != null) historicalImage = imageTransform.GetComponent<Image>();
            }
        }

        // Locate the AR model instance, even if it's not a direct child of this Canvas.
        if (futureModel == null)
        {
            // Search all transforms in the scene for a name containing "Cathedral" and a MeshRenderer.
            var candidates = FindObjectsOfType<Transform>(true);
            foreach (var t in candidates)
            {
                if (t.name.Contains("Cathedral") && t.GetComponent<MeshRenderer>() != null)
                {
                    futureModel = t.gameObject;
                    break;
                }
            }
        }

        if (exitButton == null)
        {
            Transform buttonTransform = transform.Find("ExitButton") ?? transform.Find("Button");
            if (buttonTransform != null) exitButton = buttonTransform.GetComponent<Button>();
        }

        if (historicalAudioSource == null)
            historicalAudioSource = GetComponent<AudioSource>() ?? GetComponentInParent<AudioSource>();

        panelRect = GetComponent<RectTransform>();
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
            opacitySlider.onValueChanged.RemoveListener(UpdateTimeline);
            opacitySlider.onValueChanged.AddListener(UpdateTimeline);
        }

        if (photoGallery != null)
        {
            photoGallery.onPhotoChanged -= OnHistoricalPhotoChanged;
            photoGallery.onPhotoChanged += OnHistoricalPhotoChanged;
        }
    }

    public void OpenComparison()
    {
        gameObject.SetActive(true);
        isClosing = false;
        SetFutureModelVisible(false);
        float initialValue = Mathf.Clamp01(defaultInitialOpacity);
        if (opacitySlider != null) opacitySlider.SetValueWithoutNotify(initialValue);
        UpdateTimeline(initialValue);
        ApplyResponsiveLayout(true);
    }

    /// <summary>Compatibility entry point; value now represents the full timeline.</summary>
    public void UpdateOpacity(float value) => UpdateTimeline(value);

    public void UpdateTimeline(float value)
    {
        value = Mathf.Clamp01(value);

        // Past -> present: image fades to transparent. The present midpoint is a clean camera view.
        float historicalAlpha = 1f - Mathf.Clamp01(value / 0.5f);
        if (historicalImage != null)
        {
            Color color = historicalImage.color;
            color.a = historicalAlpha;
            historicalImage.color = color;

            Material material = saturationMaterial != null ? saturationMaterial : historicalImage.material;
            if (material != null && material.HasProperty("_Saturation"))
                material.SetFloat("_Saturation", historicalAlpha);
        }

        // Present -> future: enable the existing AR prefab instance. It remains opaque in this first pass.
        SetFutureModelVisible(value > 0.505f);

        if (historicalAudioSource != null)
        {
            historicalAudioSource.volume = historicalAlpha;
            if (gameObject.activeInHierarchy && historicalAlpha > 0.001f && historicalAudioSource.clip != null && !historicalAudioSource.isPlaying)
                historicalAudioSource.Play();
            else if (historicalAlpha <= 0.001f && historicalAudioSource.isPlaying)
                historicalAudioSource.Pause();
        }
    }

    private void SetFutureModelVisible(bool visible)
    {
        if (futureModel != null && futureModel.activeSelf != visible)
            futureModel.SetActive(visible);
    }

    private void OnHistoricalPhotoChanged(int index, AudioClip clip)
    {
        if (historicalAudioSource == null) return;
        historicalAudioSource.Stop();
        historicalAudioSource.clip = clip;
        UpdateTimeline(opacitySlider != null ? opacitySlider.value : defaultInitialOpacity);
    }

    private void ApplyResponsiveLayout(bool force)
    {
        if (opacitySlider == null || sliderRect == null) return;

        // Force vertical orientation regardless of screen aspect.
        opacitySlider.direction = Slider.Direction.BottomToTop;

        // Position slider at the right edge, spanning vertically.
        sliderRect.anchorMin = new Vector2(1f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 1f);
        sliderRect.pivot = new Vector2(1f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(-screenEdgeInset, 0f);
        sliderRect.localRotation = Quaternion.identity;
        sliderRect.localScale = Vector3.one;
        // Width is thickness, height is the defined length (or limited by screen size).
        float height = Mathf.Min(sliderLength, Screen.height * 0.85f);
        sliderRect.sizeDelta = new Vector2(sliderThickness, height);

        // Adjust fill and handle rects for a clean vertical track.
        if (opacitySlider.fillRect != null)
        {
            var fill = opacitySlider.fillRect;
            fill.anchorMin = new Vector2(0.5f, 0f);
            fill.anchorMax = new Vector2(0.5f, 1f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = new Vector2(sliderThickness, 0f);
        }
        if (opacitySlider.handleRect != null)
        {
            var handle = opacitySlider.handleRect;
            // Handle anchored at bottom initially.
            handle.anchorMin = new Vector2(0.5f, 0f);
            handle.anchorMax = new Vector2(0.5f, 0f);
            handle.anchoredPosition = Vector2.zero;
            handle.sizeDelta = new Vector2(sliderThickness, sliderThickness);
        }

        if (panelRect != null)
            panelRect.ForceUpdateRectTransforms();
    }

    public void CloseComparison()
    {
        if (isClosing) return;
        isClosing = true;

        if (historicalAudioSource != null)
        {
            historicalAudioSource.Stop();
            historicalAudioSource.volume = 0f;
        }

        SetFutureModelVisible(false);
        gameObject.SetActive(false);
        try { onComparisonClosed?.Invoke(); }
        finally { isClosing = false; }
    }

#if UNITY_EDITOR
    private void Reset() => AutoWireComponents();
#endif
}
