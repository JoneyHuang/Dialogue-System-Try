using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoBubbleTrigger: MonoBehaviour
{
    [SerializeField] private GameObject infoBubble;
    [Header("Info pages")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text pageIndicator;
    [TextArea(3, 8)] [SerializeField] private string[] pageTitles = { "简介", "历史与建筑细节" };
    [TextArea(5, 12)] [SerializeField] private string[] pageBodies =
    {
        "用简短文字介绍这个地点。点击 Know more 了解它的历史与建筑细节。",
        "这里介绍这个地点的建造背景、重要年代或建筑特色。建议挑选 2–3 个最值得现场观察的细节，让访客能将屏幕上的信息与眼前的建筑对应起来。"
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
    }

    private void Start()
    {
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (backButton != null) backButton.onClick.AddListener(PreviousPage);
        ShowPage(0);
        if (infoBubble != null)
            infoBubble.SetActive(false);
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
        int pageCount = Mathf.Min(pageTitles.Length, pageBodies.Length);
        if (pageCount == 0) return;
        currentPage = Mathf.Clamp(page, 0, pageCount - 1);

        if (titleText != null) titleText.text = pageTitles[currentPage];
        if (bodyText != null) bodyText.text = pageBodies[currentPage];
        if (pageIndicator != null) pageIndicator.text = $"{currentPage + 1} / {pageCount}";
        if (backButton != null) backButton.gameObject.SetActive(currentPage > 0);
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(currentPage < pageCount - 1);
            TMP_Text label = nextButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = currentPage == 0 ? "Know more" : "Next";
        }
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
