using UnityEngine;
using UnityEngine.UI;

// Added for onboarding page navigation
using UnityEngine.UI;
using TMPro; // support TextMeshProUGUI components



/// <summary>
/// Controls the Welcoming Panel (Stage 0/1).
/// Dismisses the welcoming panel and activates the next destination (PoI 1 Pin)
/// when the user clicks the CTA button (e.g. "Start Exploring").
/// </summary>
public class WelcomingPanelController : MonoBehaviour
{
    [Header("UI & Next Target")]
    [Tooltip("The main Welcoming Panel GameObject. If null, uses this GameObject.")]
    public GameObject panelRoot;

    [Tooltip("The CTA button on the panel (e.g. 'Start Exploring').")]
    public Button ctaButton;

    [Tooltip("The PoI 1 GameObject (PoI 1 Root, Anchor, or Pin) to activate once this panel is dismissed.")]
    public GameObject poi1Target;
        // ---- Structured onboarding data (title + body) ----
    [System.Serializable]
    public class OnboardingPageData
    {
        [Header("Page Title")]
        [TextArea(1,3)]
        public string title;

        [Header("Page Body")]
        [TextArea(5,20)]
        public string content;
    }

    [Header("Onboarding Pages Data")]
    [Tooltip("Define title and body for each page. Order must match the `pages` array.")]
    public OnboardingPageData[] pageData;

    // Optional: assign the Text components that render the page titles (same order as pages)
    [Header("Title Text Components")]
    [Tooltip("Assign the Text or TMP_Text component for each page title.")]
    public Text[] titleTexts;



    // Helper: populate Text components on pages with the provided strings.
    void FillPageContent()
    {
        if (pages == null) return;
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == null) continue;

            // ------- Body text (UI Text or TMP) -------
            var txt = pages[i].GetComponentInChildren<Text>(true);
            var tmp = pages[i].GetComponentInChildren<TMP_Text>(true);

            string body = null;
            // Prefer structured pageData if available
            if (pageData != null && i < pageData.Length && !string.IsNullOrEmpty(pageData[i].content))
                body = pageData[i].content;
            //else if (pageContents != null && i < pageContents.Length && !string.IsNullOrEmpty(pageContents[i]))
               // body = pageContents[i];

            if (!string.IsNullOrEmpty(body))
            {
                if (txt != null) { txt.text = body; Debug.Log($"[WelcomingPanelController] Filled UI.Text on page {i}."); }
                else if (tmp != null) { tmp.text = body; Debug.Log($"[WelcomingPanelController] Filled TMP_Text on page {i}."); }
            }

            // ------- Title text (optional) -------
            if (titleTexts != null && i < titleTexts.Length && titleTexts[i] != null)
            {
                string title = null;
                if (pageData != null && i < pageData.Length && !string.IsNullOrEmpty(pageData[i].title))
                    title = pageData[i].title;
                // fallback to pageTitles if you still use the old array (not defined now)
                // if needed you can expose a separate string[] pageTitles later.
                if (!string.IsNullOrEmpty(title))
                {
                    // Title may be Text or TMP_Text – we accept both via the base class Text (covers UI Text) or TMP_Text.
                    var titleTxt = titleTexts[i].GetComponent<Text>();
                    var titleTmp = titleTexts[i].GetComponent<TMP_Text>();
                    if (titleTxt != null) { titleTxt.text = title; }
                    else if (titleTmp != null) { titleTmp.text = title; }
                }
            }
        }
    }
    [Header("Onboarding Pages")]
    [Tooltip("Assign the three page GameObjects (Page 0 = Welcome, Page 1 = Journey, Page 2 = How to Explore)")]
    public GameObject[] pages;
    // Current page index (0‑based)
    private int currentPage = 0;

    void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (ctaButton == null)
        {
            ctaButton = GetComponentInChildren<Button>(true);
        }
    }

    void Start()
    {
        // Initialise CTA button label and listener
        Debug.Log($"[WelcomingPanelController] Showing onboarding page {currentPage} ({(pages[currentPage]!=null?pages[currentPage].name:"null")})");

        if (ctaButton != null)
        {
            ctaButton.onClick.RemoveListener(OnStartExploringClicked);
            ctaButton.onClick.RemoveListener(OnCtaClicked);
            ctaButton.onClick.AddListener(OnCtaClicked);
        }

        // Show first onboarding page
        ShowPage(0);
        // Populate text content for each page (if you supplied pageContents in the Inspector)
        FillPageContent();

        // Ensure PoI 1 starts completely hidden while user is at Starting Point / Welcoming Panel
        if (poi1Target != null)
        {
            poi1Target.SetActive(false);
        }
    }

    /// <summary>
    /// Show the specified onboarding page (0‑based) and hide the others.
    /// </summary>
    void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("[WelcomingPanelController] No onboarding pages assigned.");
            return;
        }

        currentPage = Mathf.Clamp(index, 0, pages.Length - 1);
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == currentPage);
            }
        }

        Debug.Log($"[WelcomingPanelController] Showing onboarding page {currentPage} ({(pages[currentPage]!=null?pages[currentPage].name:"null")})");
    }

    /// <summary>
    /// Update CTA button label: “>>>>” for intermediate pages, “Start Exploring” on the final page.
    /// </summary>
    void UpdateCtaLabel()
    {
        if (ctaButton == null) return;
        // Try Unity UI Text first
        var txt = ctaButton.GetComponentInChildren<Text>(true);
        // If not found, try TextMeshProUGUI
        TMP_Text tmp = null;
        if (txt == null)
        {
            tmp = ctaButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (pages != null && currentPage < pages.Length - 1)
        {
            if (txt != null) txt.text = ">>>>";
            else if (tmp != null) tmp.text = ">>>>";
        }
        else
        {
            if (txt != null) txt.text = "Start Exploring";
            else if (tmp != null) tmp.text = "Start Exploring";
        }
    }

    /// <summary>
    /// Called when the CTA button is pressed. Advances pages or starts the experience.
    /// </summary>
    void OnCtaClicked()
    {
        if (pages != null && currentPage < pages.Length - 1)
        {
            ShowPage(currentPage + 1);
        }
        else
        {
            // Final step – dismiss panel and activate PoI 1
            OnStartExploringClicked();
        }
    }

    /// <summary>
    /// Opens the welcoming panel (can be called by Starting Pin's onTriggered event).
    /// </summary>
    public void OpenPanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
    }

    /// <summary>
    /// Dismisses the welcoming panel and activates PoI 1.
    /// </summary>
    public void OnStartExploringClicked()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (poi1Target != null)
        {
            poi1Target.SetActive(true);
            Debug.Log($"[WelcomingPanelController] Welcoming panel dismissed. '{poi1Target.name}' is now active!");
        }
        else
        {
            Debug.LogWarning("[WelcomingPanelController] poi1Target is not assigned!");
        }
    }
}
