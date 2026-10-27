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

    [Tooltip("The PoI 1 Pin to activate once this panel is dismissed.")]
    public GameObject poi1Pin;

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

        // Ensure PoI 1 Pin starts inactive while in Welcoming / Starting phase
        if (poi1Pin != null)
        {
            poi1Pin.SetActive(false);
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
    /// Dismisses the welcoming panel and activates the PoI 1 Pin.
    /// </summary>
    public void OnStartExploringClicked()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (poi1Pin != null)
        {
            poi1Pin.SetActive(true);
            Debug.Log("[WelcomingPanelController] Welcoming panel dismissed. PoI 1 Pin is now active!");
        }
        else
        {
            Debug.LogWarning("[WelcomingPanelController] poi1Pin is not assigned!");
        }
    }
}
