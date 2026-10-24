using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Controls a swipeable photo archive gallery for the Then/Now comparison.
/// Supports touch/mouse swipe gestures, dynamic aspect-ratio fitting, and optional navigation buttons/indicators.
/// </summary>
public class PhotoGalleryController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Display Target")]
    [Tooltip("The UI Image displaying the historical photo.")]
    public Image displayImage;

    [Tooltip("Optional. If present, the aspect ratio will dynamically update to match each photo's true proportions.")]
    public AspectRatioFitter aspectRatioFitter;

    [Header("Photo Archive")]
    [Tooltip("List of historical photos users can browse through.")]
    public Sprite[] photos;

    [Header("Optional UI Elements")]
    [Tooltip("Optional text showing current photo index (e.g., '1 / 5') or title.")]
    public TMP_Text pageIndicatorText;

    [Tooltip("Optional titles or captions corresponding to each photo.")]
    public string[] photoCaptions;

    [Header("Photo Soundscapes (Audio)")]
    [Tooltip("Optional soundscape clip for each photo (e.g., bells for Cathedral, music, ambient sounds).")]
    public AudioClip[] photoAudios;

    public Button prevButton;
    public Button nextButton;

    /// <summary>
    /// Event invoked whenever the displayed photo changes. Passes (photoIndex, activeAudioClip).
    /// </summary>
    public System.Action<int, AudioClip> onPhotoChanged;

    [Header("Swipe Settings")]
    [Tooltip("Minimum drag distance (in pixels) required to trigger a swipe to the next/previous photo.")]
    public float swipeThreshold = 50f;

    private int currentIndex = 0;
    private Vector2 dragStartPosition;
    private bool isDragging = false;

    void Awake()
    {
        Initialize();
    }

    void Start()
    {
        // Re-ensure listeners in Start in case buttons were activated or bound later
        WireButtons();
    }

    void Initialize()
    {
        if (displayImage != null)
        {
            displayImage.rectTransform.localScale = Vector3.one;
            displayImage.preserveAspect = true;

            if (aspectRatioFitter == null)
            {
                aspectRatioFitter = displayImage.GetComponent<AspectRatioFitter>();
            }
        }

        WireButtons();
    }

    void WireButtons()
    {
        if (prevButton != null)
        {
            prevButton.onClick.RemoveListener(ShowPreviousPhoto);
            prevButton.onClick.AddListener(ShowPreviousPhoto);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextPhoto);
            nextButton.onClick.AddListener(ShowNextPhoto);
            Debug.Log($"PhotoGallery: Successfully hooked up NextButton ({nextButton.gameObject.name})");
        }
        else
        {
            Debug.LogWarning("PhotoGallery: NextButton field is NULL or not assigned in Inspector!");
        }
    }

    void OnEnable()
    {
        UpdateDisplay();
    }

    /// <summary>
    /// Displays the current photo and updates UI elements.
    /// </summary>
    public void UpdateDisplay()
    {
        if (photos == null || photos.Length == 0 || displayImage == null)
        {
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, photos.Length - 1);
        Sprite activeSprite = photos[currentIndex];

        // Maintain current alpha (from opacity slider) while switching sprite
        float currentAlpha = displayImage.color.a;
        displayImage.sprite = activeSprite;
        Color c = displayImage.color;
        c.a = currentAlpha;
        displayImage.color = c;

        // Auto-match the AspectRatioFitter to the sprite's real dimensions
        if (aspectRatioFitter != null && activeSprite != null && activeSprite.rect.height > 0)
        {
            aspectRatioFitter.aspectRatio = activeSprite.rect.width / activeSprite.rect.height;
        }

        // Update page / caption indicator
        if (pageIndicatorText != null)
        {
            if (photoCaptions != null && currentIndex < photoCaptions.Length && !string.IsNullOrEmpty(photoCaptions[currentIndex]))
            {
                pageIndicatorText.text = $"{photoCaptions[currentIndex]} ({currentIndex + 1}/{photos.Length})";
            }
            else
            {
                pageIndicatorText.text = $"{currentIndex + 1} / {photos.Length}";
            }
        }

        // Update button interactability if you prefer disabling at bounds
        if (prevButton != null)
        {
            prevButton.interactable = (photos.Length > 1);
        }
        if (nextButton != null)
        {
            nextButton.interactable = (photos.Length > 1);
        }

        // Notify listeners (like M01Controller) that the active photo and audio changed
        AudioClip activeAudio = (photoAudios != null && currentIndex < photoAudios.Length) ? photoAudios[currentIndex] : null;
        onPhotoChanged?.Invoke(currentIndex, activeAudio);
    }

    public AudioClip GetCurrentAudio()
    {
        if (photoAudios != null && currentIndex >= 0 && currentIndex < photoAudios.Length)
        {
            return photoAudios[currentIndex];
        }
        return null;
    }

    public void ShowNextPhoto()
    {
        if (photos == null || photos.Length <= 1) return;

        currentIndex++;
        if (currentIndex >= photos.Length)
        {
            currentIndex = 0; // Loop around to beginning
        }
        Debug.Log($"PhotoGallery: Next photo index {currentIndex} ({photos[currentIndex]?.name})");
        UpdateDisplay();
    }

    public void ShowPreviousPhoto()
    {
        if (photos == null || photos.Length <= 1) return;

        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = photos.Length - 1; // Loop around to end
        }
        Debug.Log($"PhotoGallery: Previous photo index {currentIndex} ({photos[currentIndex]?.name})");
        UpdateDisplay();
    }

    public void SetPhotoIndex(int index)
    {
        if (photos == null || index < 0 || index >= photos.Length) return;
        currentIndex = index;
        UpdateDisplay();
    }

    public Sprite GetCurrentPhoto()
    {
        if (photos != null && currentIndex >= 0 && currentIndex < photos.Length)
        {
            return photos[currentIndex];
        }
        return null;
    }

    // --- Touch / Mouse Drag Swipe Implementation ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPosition = eventData.position;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Required by IDragHandler interface so Unity forwards drag events
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        float deltaX = eventData.position.x - dragStartPosition.x;
        float deltaY = eventData.position.y - dragStartPosition.y;

        // Ensure horizontal drag is dominant (so vertical slider gestures aren't confused)
        if (Mathf.Abs(deltaX) > Mathf.Abs(deltaY) && Mathf.Abs(deltaX) >= swipeThreshold)
        {
            if (deltaX < 0)
            {
                // Swiped Left -> Next Photo
                ShowNextPhoto();
            }
            else
            {
                // Swiped Right -> Previous Photo
                ShowPreviousPhoto();
            }
        }
    }
}
