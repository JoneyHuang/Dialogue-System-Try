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

    private int currentPage;

    private void Start()
    {
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (backButton != null) backButton.onClick.AddListener(PreviousPage);
        ShowPage(0);
        if (infoBubble != null)
            infoBubble.SetActive(false);
    }

    private void OnMouseDown()
    {
        if (infoBubble != null)
        {
            ShowPage(0);
            infoBubble.SetActive(true);
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
            infoBubble.SetActive(false);
    }
}
