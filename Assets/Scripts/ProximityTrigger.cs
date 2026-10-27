using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A lightweight, reusable spatial trigger component for Navigation Pins.
/// Detects when the user/AR camera approaches the pin, plays a breathing animation,
/// and invokes onTriggered (e.g. to open a panel or start a PoI story flow) before hiding itself.
/// </summary>
public class ProximityTrigger : MonoBehaviour
{
    [Header("Target & Detection")]
    [Tooltip("The AR camera or player transform. If unassigned, defaults to Camera.main.")]
    public Transform arCamera;

    [Tooltip("Distance in meters at which the trigger action fires.")]
    public float triggerDistance = 2.0f;

    [Tooltip("Distance in meters at which the pin starts a breathing animation to attract attention.")]
    public float warningDistance = 5.0f;

    [Header("Trigger Behavior")]
    [Tooltip("Hide this Pin GameObject once triggered.")]
    public bool hidePinOnTrigger = true;

    [Header("Events")]
    [Tooltip("Invoked when the player enters the triggerDistance or collides with the pin.")]
    public UnityEvent onTriggered;

    private Vector3 initialScale;
    private bool hasTriggered = false;

    void Start()
    {
        initialScale = transform.localScale;

        if (arCamera == null && Camera.main != null)
        {
            arCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        if (hasTriggered || arCamera == null) return;

        float distance = Vector3.Distance(transform.position, arCamera.position);

        if (distance <= warningDistance && distance > triggerDistance)
        {
            // Breathing animation to attract attention
            float t = Mathf.PingPong(Time.time * 2f, 1f);
            transform.localScale = Vector3.Lerp(initialScale, initialScale * 1.25f, t);
        }
        else if (distance <= triggerDistance)
        {
            TriggerPinAction();
        }
        else
        {
            transform.localScale = initialScale;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // Also allow trigger if camera or collider touches this pin
        if (other.CompareTag("MainCamera") || (arCamera != null && other.transform == arCamera))
        {
            TriggerPinAction();
        }
    }

    /// <summary>
    /// Executes the trigger action, fires onTriggered event, and hides the pin.
    /// </summary>
    public void TriggerPinAction()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        Debug.Log($"[ProximityTrigger] Pin '{gameObject.name}' reached! Invoking onTriggered event.");

        onTriggered?.Invoke();

        if (hidePinOnTrigger)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Resets the trigger state if this pin needs to be reused.
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        transform.localScale = initialScale;
        gameObject.SetActive(true);
    }
}


