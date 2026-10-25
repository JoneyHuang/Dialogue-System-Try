using UnityEngine;
using UnityEngine.EventSystems;

public class PoI1WorldMarker : MonoBehaviour, IPointerClickHandler
{
    public PoI1InteractionController.MarkerKind markerKind;
    public PoI1InteractionController interactionController;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
        if (interactionController == null)
            interactionController = FindFirstObjectByType<PoI1InteractionController>();
        if (markerKind == PoI1InteractionController.MarkerKind.Listen)
        {
            var listenControl = GetComponent<ListenControl>();
            if (listenControl != null) listenControl.TogglePlayback();
            return;
        }
        if (interactionController != null)
            interactionController.Open(markerKind);
    }
}