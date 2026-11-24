using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MeetMePanelController : MonoBehaviour
{
    [Header("UI refs")]
    [SerializeField] private TMP_Text titleText; // Top-left category title (e.g. "MEMORY UNLOCKED" or "LOCATION ARCHIVE")
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text briefText;
    [SerializeField] private Button actionButton;
    [SerializeField] private Button closeButton; // Optional: top-right 'X' or cancel button

    public void SetTitle(string title) { if (titleText != null) titleText.text = title; }
    // Set the name text and force the colour to the configured green.
    [SerializeField] private Color nameColor = new Color(0x69/255f, 0xFF/255f, 0x87/255f);
    public void SetName(string name)
    {
        if (nameText != null)
        {
            nameText.text = name;
            nameText.color = nameColor; // guarantee green
        }
    }

    /// <summary>Set the name text colour (optional, kept for backward compatibility).</summary>
    public void SetNameColor(Color color)
    {
        if (nameText != null) nameText.color = color;
    }

    /// <summary>Set the name text colour.</summary>
    //public void SetNameColor(Color color)
    //{
      //  if (nameText != null)
      //  nameText.color = new Color(0x69/255f, 0xFF/255f, 0x87/255f);
    //}
    public void SetBrief(string txt) { if (briefText != null) briefText.text = txt; }
    public void SetAvatar(Sprite img) { if (avatarImage != null) avatarImage.sprite = img; }

    public void SetButtonText(string text)
    {
        if (actionButton != null)
        {
            var btnTmp = actionButton.GetComponentInChildren<TMP_Text>();
            if (btnTmp != null) btnTmp.text = text;
        }
    }
    
    public void SetAction(UnityEngine.Events.UnityAction onClick)
    {
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            if (onClick != null) actionButton.onClick.AddListener(onClick);
        }
    }

    public void SetCloseAction(UnityEngine.Events.UnityAction onClose)
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            if (onClose != null) closeButton.onClick.AddListener(onClose);
        }
    }

    private void Awake()
    {
        // Auto-wire Title if not assigned
        if (titleText == null)
        {
            Transform t = transform.Find("Title");
            if (t != null) titleText = t.GetComponent<TMP_Text>();
        }

        // Auto-wire Close Button if named "CloseButton", "ExitButton", or "Close"
        if (closeButton == null)
        {
            Transform cb = transform.Find("CloseButton") ?? transform.Find("Close") ?? transform.Find("ExitButton");
            if (cb != null) closeButton = cb.GetComponent<Button>();
        }

        // Default close button behavior (simply hide panel if no custom action set)
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
    }


    // Optional: show / hide helpers
    // Reset all UI fields to defaults to avoid state leakage between uses
    public void ResetPanel()
    {
        if (titleText != null) titleText.text = "";
        if (nameText != null)
        {
            nameText.text = "";
            nameText.color = nameColor; // reset to default green
        }
        if (briefText != null) briefText.text = "";
        // Reset button text to empty
        SetButtonText("");
        // Remove all action listeners
        if (actionButton != null) actionButton.onClick.RemoveAllListeners();
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}