using UnityEngine;
using UnityEngine.UI;

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
        if (ctaButton != null)
        {
            ctaButton.onClick.RemoveListener(OnStartExploringClicked);
            ctaButton.onClick.AddListener(OnStartExploringClicked);
        }

        // Ensure PoI 1 starts completely hidden while user is at Starting Point / Welcoming Panel
        if (poi1Target != null)
        {
            poi1Target.SetActive(false);
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
