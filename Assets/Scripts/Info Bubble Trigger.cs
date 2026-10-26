using UnityEngine;

public class InfoBubbleTrigger: MonoBehaviour
{
    [SerializeField] private GameObject infoBubble;

    private void Start()
    {
        if (infoBubble != null)
            infoBubble.SetActive(false);
    }

    private void OnMouseDown()
    {
        if (infoBubble != null)
            infoBubble.SetActive(true);
    }

    public void CloseInfoBubble()
    {
        if (infoBubble != null)
            infoBubble.SetActive(false);
    }
}
