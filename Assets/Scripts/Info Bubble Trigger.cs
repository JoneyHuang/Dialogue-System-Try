using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoBubbleTrigger : MonoBehaviour
{
    [System.Serializable]
    public class InfoBubblePage
    {
        public string title = "Title";
        [TextArea(4, 10)]
        public string body = "Content here...";
        [Tooltip("如果使用 RawImage 组件，请将图片 Texture 拖入此项")]
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

    public bool HasBeenInspected { get; private set; } = false;

    private int currentPage;
    private Collider myCollider;
    private Camera targetCamera;

    private void Awake()
    {
        myCollider = GetComponent<Collider>();
        targetCamera = Camera.main;

        // 如果没有配置新的 pages，但有旧的 pageTitles 和 pageBodies，自动转换合并
        MigrateLegacyDataIfNeeded();
    }

    private void Start()
    {
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (backButton != null) backButton.onClick.AddListener(PreviousPage);
        ShowPage(0);
        if (infoBubble != null)
            infoBubble.SetActive(false);
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

    private void Update()
    {
        // Handle Mobile AR Touch Input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                CheckRaycastHit(touch.position);
            }
        }
        // Handle PC Mouse Click
        else if (Input.GetMouseButtonDown(0))
        {
            CheckRaycastHit(Input.mousePosition);
        }
    }

    private void CheckRaycastHit(Vector2 screenPosition)
    {
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
        if (infoBubble != null)
        {
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

        // 1. 设置标题和文本
        if (titleText != null) titleText.text = currentTitle;
        if (bodyText != null) bodyText.text = currentBody;

        // 2. 设置页码指示器
        if (pageIndicator != null) pageIndicator.text = $"{currentPage + 1} / {totalPages}";

        // 3. 翻页按钮状态
        if (backButton != null) backButton.gameObject.SetActive(currentPage > 0);
        if (nextButton != null)
        {
            bool hasNext = currentPage < totalPages - 1;
            nextButton.gameObject.SetActive(hasNext);
            TMP_Text label = nextButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = currentPage == 0 ? "Know more" : "Next";
        }

        // 4. 更新图片 (优先 RawImage，其次 Image)
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
        if (infoBubble != null)
        {
            infoBubble.SetActive(false);
            onBubbleClosed?.Invoke();
        }
    }
}
