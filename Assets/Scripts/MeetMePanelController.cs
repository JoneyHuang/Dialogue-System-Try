using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MeetMePanelController : MonoBehaviour
{
    [Header("UI refs")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text briefText;
    [SerializeField] private Button actionButton;

    public void SetName(string name) => nameText.text = name;
    public void SetBrief(string txt) => briefText.text = txt;
    public void SetAvatar(Sprite img) => avatarImage.sprite = img;
    
    public void SetAction(UnityEngine.Events.UnityAction onClick)
    {
      if (actionButton != null)
       {
        actionButton.onClick.RemoveAllListeners();
        if (onClick != null) actionButton.onClick.AddListener(onClick);
       }
    }


    // Optional: show / hide helpers
    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}