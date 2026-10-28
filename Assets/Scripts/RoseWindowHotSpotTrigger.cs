using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger attached to HotSpot (1) for PoI3.
/// Supports both PC mouse clicks and Mobile AR Touch Raycasts.
/// Clicking this hotspot opens the in-world AR Rose Window with a spiral zoom-in,
/// hides this hotspot, and restores it once dismissed.
/// </summary>
public class RoseWindowHotSpotTrigger : MonoBehaviour
{
    [Header("Target Media")]
    [Tooltip("Reference to the in-world ARWorldRoseWindow instance in the scene.")]
    [SerializeField] private ARWorldRoseWindow roseWindow;

    [Header("Optional Offset")]
    [Tooltip("Offset relative to this HotSpot where the Rose Window should appear.")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    [Header("Events")]
    [Tooltip("Optional events invoked when opened/closed.")]
    public UnityEvent onHotSpotOpened;
    public UnityEvent onHotSpotRestored;

    private Collider myCollider;
    private Camera arCamera;

    private void Awake()
    {
        myCollider = GetComponent<Collider>();
        arCamera = Camera.main;
    }

    private void Update()
    {
        // Handle Mobile AR Touch Input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (arCamera == null) arCamera = Camera.main;
                if (arCamera != null && myCollider != null)
                {
                    Ray ray = arCamera.ScreenPointToRay(touch.position);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        // Check if this hotspot was tapped
                        if (hit.collider == myCollider || hit.transform == transform || hit.transform.IsChildOf(transform))
                        {
                            TriggerOpen();
                        }
                    }
                }
            }
        }
    }

    private void OnMouseDown()
    {
        // PC Editor fallback
        TriggerOpen();
    }

    /// <summary>
    /// Opens the Rose Window and hides this HotSpot GameObject.
    /// </summary>
    public void TriggerOpen()
    {
        if (roseWindow == null)
        {
            Debug.LogWarning("[RoseWindowHotSpotTrigger] Rose Window reference is not assigned!", this);
            return;
        }

        Vector3 spawnPos = transform.position + spawnOffset;

        // Keep the window active when its HotSpot is hidden.
        roseWindow.transform.SetParent(transform.parent, true);

        // Hide this hotspot
        gameObject.SetActive(false);
        onHotSpotOpened?.Invoke();

        // Open Rose Window with spiral entrance and provide callback to restore this hotspot
        roseWindow.Open(spawnPos, OnRoseWindowClosed);
    }

    private void OnRoseWindowClosed()
    {
        // Restore this hotspot
        gameObject.SetActive(true);
        onHotSpotRestored?.Invoke();
    }
}
