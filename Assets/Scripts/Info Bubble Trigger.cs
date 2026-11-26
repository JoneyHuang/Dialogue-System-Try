using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InfoBubbleTrigger : MonoBehaviour
{
    [System.Serializable]
    public class InfoBubblePage
    {
        public string title = "Title";
        [TextArea(4, 10)]
        public string body = "Content here...";
        [Tooltip("For RawImage Texture")]
        public Sprite sprite;
    }

    [SerializeField] private GameObject infoBubble;

    [Header("Display UI Components")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [Tooltip("RawImage (Optional)")]
    [SerializeField] private RawImage pictureRawImage;
    [Tooltip("Image (Optional)")]
    [SerializeField] private Image pictureImage;

    [Header("Navigation UI")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text pageIndicator;

    [Header("Structured Pages")]
    [Tooltip("按页配置标题、正文及对应的图片")]
    [SerializeField] private List<InfoBubblePage> pages = new List<InfoBubblePage>();

    [Header("Legacy Fallback Pages (旧版本数据备用)")]
    [TextArea(2, 5)] [SerializeField] private string[] pageTitles = { "简介", "历史与建筑细节" };
    [TextArea(4, 10)] [SerializeField] private string[] pageBodies =
    {
        "用简短文字介绍这个地点。点击 Know more 了解它的历史与建筑细节。",
        "这里介绍这个地点的建造背景、重要年代或建筑特色。"
    };

    [Header("Hotspot Progress Tracking")]
    [Tooltip("Optional event fired when this info bubble is opened or viewed.")]
    public UnityEngine.Events.UnityEvent onBubbleOpened;
    public UnityEngine.Events.UnityEvent onBubbleClosed;

    [Header("Audio First Mode (e.g. Choir)")]
    public bool playAudioFirst = false;
    public AudioClip ambientAudioClip;
    public GameObject listenOnlyBanner;
    public Button openDetailFromBannerButton;
    public Button closeAudioButton;

    private AudioSource audioSource;

    [System.Serializable]
    public class LyricLine
    {
      public float time; // 该句歌词开始的时间（秒）
      public string text;
    }

    [Header("Lyrics")]
    public TMP_Text lyricsText;
    public List<LyricLine> lyrics = new List<LyricLine>();
    public float lyricScrollOffset = 20f;
    public float lyricAnimSpeed = 10f;
    private int currentLyricIndex = -1;
    private Vector2 lyricsTargetPos;
    private CanvasGroup lyricsCanvasGroup;

    [Header("Full Screen Viewer")]
    [SerializeField] private GameObject fullScreenViewerPanel;
    [SerializeField] private Image fullScreenImageDisplay;
    [SerializeField] private Button closeViewerButton;

    public bool HasBeenInspected { get; private set; } = false;
    private bool isFullScreenOpen = false;

    private int currentPage;
    private Collider myCollider;
    private Camera targetCamera;

    private void Awake()
    {
        myCollider = GetComponent<Collider>();
        targetCamera = Camera.main;

        if (lyricsText != null)
        {
            lyricsTargetPos = lyricsText.rectTransform.anchoredPosition;
            lyricsCanvasGroup = lyricsText.GetComponent<CanvasGroup>();
            if (lyricsCanvasGroup == null)
                lyricsCanvasGroup = lyricsText.gameObject.AddComponent<CanvasGroup>();
        }

        MigrateLegacyDataIfNeeded();
    }

    private void Start()
    {
        if (infoBubble != null)
            infoBubble.SetActive(false);
        if (pictureImage != null)
        {
            // Make sure pictureImage has Button component
            Button imgBtn = pictureImage.GetComponent<Button>();
            if (imgBtn != null)
            {
                imgBtn.onClick.AddListener(OpenFullScreenImage);
            }
        }
        if (closeViewerButton != null)
        {
            closeViewerButton.onClick.RemoveListener(CloseFullScreenImage);
            closeViewerButton.onClick.AddListener(CloseFullScreenImage);
        }

        if (ambientAudioClip != null)
        {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = ambientAudioClip;
        audioSource.playOnAwake = false;
        audioSource.loop = false; 
        }

        if (openDetailFromBannerButton != null)
        {
        openDetailFromBannerButton.onClick.AddListener(ShowFullInfoFromBanner);
        }
    
        if (closeAudioButton != null)
        {
        closeAudioButton.onClick.AddListener(CloseAudioBanner);
        }
    }
    
    public void CloseAudioBanner()
    {
       if (audioSource != null && audioSource.isPlaying)
       {
        audioSource.Stop();
       }
       
       if (listenOnlyBanner != null)
       {
        listenOnlyBanner.SetActive(false);
       }
    }
    private void MigrateLegacyDataIfNeeded()
    {
        if (pages == null || pages.Count == 0)
        {
            if (pageTitles != null && pageBodies != null && pageTitles.Length > 0)
            {
                pages = new List<InfoBubblePage>();
                int count = Mathf.Min(pageTitles.Length, pageBodies.Length);
                for (int i = 0; i < count; i++)
                {
                    pages.Add(new InfoBubblePage
                    {
                        title = pageTitles[i],
                        body = pageBodies[i]
                    });
                }
            }
        }
    }

    public void OpenFullScreenImage()
    {
        if (fullScreenViewerPanel == null)
        {
            Debug.LogError("[InfoBubble] fullScreenViewerPanel 未在 Inspector 中绑定！", this);
            return;
        }
        if (fullScreenImageDisplay == null)
        {
            Debug.LogError("[InfoBubble] fullScreenImageDisplay 未在 Inspector 中绑定！", this);
            return;
        }
        if (pictureImage == null || pictureImage.sprite == null)
        {
            Debug.LogWarning("[InfoBubble] pictureImage 没有 Sprite，无法全屏预览！", this);
            return;
        }

        // 赋值并显示
        fullScreenImageDisplay.sprite = pictureImage.sprite;
        fullScreenViewerPanel.SetActive(true);

        // 确保显示在当前父级 Canvas 的最上层，避免被其他 UI 元素遮挡
        fullScreenViewerPanel.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Exit Full Viewer
    /// </summary>
    public void CloseFullScreenImage()
    {
        if (fullScreenViewerPanel != null)
        {
            fullScreenViewerPanel.SetActive(false);
        }

        if (fullScreenImageDisplay != null)
        {
            fullScreenImageDisplay.sprite = null;
        }

        Debug.Log("[InfoBubble] Full view closed.");
    }
    

    private void Update()
    {
        // Handle Mobile AR Touch Input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                CheckRaycastHit(touch.position, touch.fingerId);
            }
        }
        // Handle PC Mouse Click
        else if (Input.GetMouseButtonDown(0))
        {
            CheckRaycastHit(Input.mousePosition);
        }
        
        if (audioSource != null && audioSource.isPlaying && lyricsText != null && lyrics.Count > 0)
        {
          float t = audioSource.time;
          int index = 0;
          for (int i = 0; i < lyrics.Count; i++)
          {
             if (t >= lyrics[i].time) index = i;
            else break;
          }

        if (index != currentLyricIndex)
         {
           currentLyricIndex = index;
           lyricsText.text = lyrics[index].text;
           lyricsText.rectTransform.anchoredPosition = lyricsTargetPos - new Vector2(0f, lyricScrollOffset);
           if (lyricsCanvasGroup != null) lyricsCanvasGroup.alpha = 0f;
          }

          lyricsText.rectTransform.anchoredPosition = Vector2.Lerp(
              lyricsText.rectTransform.anchoredPosition,
              lyricsTargetPos,
              Time.deltaTime * lyricAnimSpeed
          );
          if (lyricsCanvasGroup != null)
          {
              lyricsCanvasGroup.alpha = Mathf.Lerp(lyricsCanvasGroup.alpha, 1f, Time.deltaTime * lyricAnimSpeed);
          }
        }
    }

    private void CheckRaycastHit(Vector2 screenPosition, int fingerId = -1)
    {
        // 若全屏预览已打开，直接拦截所有 3D 点击
        if (isFullScreenOpen) return;

        // 防止 UI 点击穿透到 3D 场景
        if (EventSystem.current != null)
        {
            if (fingerId >= 0)
            {
                if (EventSystem.current.IsPointerOverGameObject(fingerId))
                    return;
            }
            else
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;
            }
        }

        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        Ray ray = targetCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == myCollider || hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                TriggerOpen();
                break;
            }
        }
    }

    private void OnMouseDown()
    {
        TriggerOpen();
    }

    public void TriggerOpen()
    {
        // 如果开启了音频优先模式
        if (playAudioFirst)
        {
            // 1. 播放音乐
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }

            // 2. 显示精简的小 Banner，不弹大 Bubble
            if (listenOnlyBanner != null)
            {
                listenOnlyBanner.SetActive(true);
            }

            HasBeenInspected = true;
            onBubbleOpened?.Invoke();
            return;
        }

        // 普通模式（Stone 等原有逻辑不变）
        OpenFullBubble();
    }

    // 从小 Banner 点击“查看详情”时调用
    public void ShowFullInfoFromBanner()
    {
        if (listenOnlyBanner != null) listenOnlyBanner.SetActive(false);
        OpenFullBubble();
    }

    private void OpenFullBubble()
    {
        if (infoBubble != null)
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(NextPage);
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(PreviousPage);
            }

            ShowPage(0);
            infoBubble.SetActive(true);
            HasBeenInspected = true;
            onBubbleOpened?.Invoke();
        }
    }

    public void NextPage()
    {
        ShowPage(currentPage + 1);
    }

    public void PreviousPage()
    {
        ShowPage(currentPage - 1);
    }

    private void ShowPage(int page)
    {
        int totalPages = GetTotalPageCount();
        if (totalPages == 0) return;

        // Debug log to trace which hotspot and page is being shown
        Debug.Log($"[InfoBubble] {gameObject.name} ShowPage {page} (total {totalPages})");

        currentPage = Mathf.Clamp(page, 0, totalPages - 1);

        string currentTitle = "";
        string currentBody = "";
        Texture currentTexture = null;
        Sprite currentSprite = null;

        if (pages != null && pages.Count > 0 && currentPage < pages.Count)
        {
            InfoBubblePage p = pages[currentPage];
            currentTitle = p.title;
            currentBody = p.body;
            //currentTexture = p.texture;
            currentSprite = p.sprite;
        }
        else
        {
            if (pageTitles != null && currentPage < pageTitles.Length) currentTitle = pageTitles[currentPage];
            if (pageBodies != null && currentPage < pageBodies.Length) currentBody = pageBodies[currentPage];
        }

        // Log the resolved title for further debugging
        Debug.Log($"[InfoBubble] {gameObject.name} resolved title: '{currentTitle}'");

        // 1. title
        if (titleText != null) titleText.text = currentTitle;
        if (bodyText != null) bodyText.text = currentBody;

        // 2. page indicator
        if (pageIndicator != null) pageIndicator.text = $"{currentPage + 1} / {totalPages}";

        // 3. turn page
        if (backButton != null) backButton.gameObject.SetActive(currentPage > 0);
        if (nextButton != null)
        {
            bool hasNext = currentPage < totalPages - 1;
            nextButton.gameObject.SetActive(hasNext);
            TMP_Text label = nextButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = currentPage == 0 ? "Know more" : "Next";
        }

        // 4. update photos
        UpdateImageDisplay(currentTexture, currentSprite);
    }

    private void UpdateImageDisplay(Texture texture, Sprite sprite)
    {
        // 4.1 处理 RawImage
        if (pictureRawImage != null)
        {
            if (texture != null)
            {
                pictureRawImage.gameObject.SetActive(true);
                pictureRawImage.texture = texture;
            }
            else
            {
                pictureRawImage.gameObject.SetActive(false);
            }
        }

        // 4.2 处理 Image (备用)
        if (pictureImage != null)
        {
            if (sprite != null)
            {
                pictureImage.gameObject.SetActive(true);
                pictureImage.sprite = sprite;
            }
            else
            {
                pictureImage.gameObject.SetActive(false);
            }
        }
    }

    private int GetTotalPageCount()
    {
        if (pages != null && pages.Count > 0)
        {
            return pages.Count;
        }
        return Mathf.Min(pageTitles != null ? pageTitles.Length : 0, pageBodies != null ? pageBodies.Length : 0);
    }

    public void CloseInfoBubble()
    {
        if (infoBubble != null) infoBubble.SetActive(false);
        if (listenOnlyBanner != null) listenOnlyBanner.SetActive(false);
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        onBubbleClosed?.Invoke();
    }
}
