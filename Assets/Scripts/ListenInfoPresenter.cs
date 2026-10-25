using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ListenInfoPresenter : MonoBehaviour
{
    public static ListenInfoPresenter Instance { get; private set; }

    [Header("Teaser card")]
    public GameObject teaserPanel;
    public TMP_Text teaserText;
    public Button knowMoreButton;
    public Button teaserCloseButton;

    [Header("Detailed information card")]
    public GameObject detailPanel;
    public TMP_Text detailTitle;
    public TMP_Text detailBody;
    public Button detailCloseButton;

    ListenContent activeContent;
    public bool IsVisible => (teaserPanel != null && teaserPanel.activeSelf) || (detailPanel != null && detailPanel.activeSelf);

    void Awake()
    {
        Instance = this;
        if (teaserPanel != null) teaserPanel.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(false);

        if (knowMoreButton != null)
        {
            knowMoreButton.onClick.RemoveListener(ShowDetails);
            knowMoreButton.onClick.AddListener(ShowDetails);
        }
        if (teaserCloseButton != null)
        {
            teaserCloseButton.onClick.RemoveListener(CloseAll);
            teaserCloseButton.onClick.AddListener(CloseAll);
        }
        if (detailCloseButton != null)
        {
            detailCloseButton.onClick.RemoveListener(ReturnToTeaser);
            detailCloseButton.onClick.AddListener(ReturnToTeaser);
        }
    }

    public void ShowTeaser(ListenContent content)
    {
        if (content == null || teaserPanel == null) return;
        activeContent = content;
        if (teaserText != null) teaserText.text = content.teaser;
        if (detailPanel != null) detailPanel.SetActive(false);
        teaserPanel.SetActive(true);
    }

    public void ShowDetails()
    {
        if (activeContent == null || detailPanel == null) return;
        if (detailTitle != null) detailTitle.text = activeContent.detailTitle;
        if (detailBody != null) detailBody.text = activeContent.detailBody;
        teaserPanel.SetActive(false);
        detailPanel.SetActive(true);
    }

    public void ReturnToTeaser()
    {
        if (activeContent == null) return;
        if (detailPanel != null) detailPanel.SetActive(false);
        if (teaserText != null) teaserText.text = activeContent.teaser;
        if (teaserPanel != null) teaserPanel.SetActive(true);
    }

    public void CloseAll()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        if (teaserPanel != null) teaserPanel.SetActive(false);
        activeContent = null;
    }
}