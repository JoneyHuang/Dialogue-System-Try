using UnityEngine;

public class ProximityTrigger : MonoBehaviour
{
    // Global progress flag so PoI 1 can know if Landing was visited
    public static bool HasVisitedLanding { get; set; } = false;

    [Header("Navigate Pin & AR Target")]
    public Transform arCamera;
    [Tooltip("The Info Bubble or Welcoming Panel associated with this Pin.")]
    public GameObject infoBubble;

    [Header("Trigger Behavior")]
    [Tooltip("Check this ONLY for PoI 1 to start Susan's Dialogue. Leave UNCHECKED for Landing.")]
    public bool triggerDialogue = false;

    [Tooltip("Activate infoBubble (e.g. Welcoming Panel) when player approaches.")]
    public bool showInfoBubbleOnTrigger = true;

    [Tooltip("Hide this Pin model once triggered.")]
    public bool hidePinOnTrigger = true;

    [Tooltip("If true, this Pin marks Landing as visited. Check this on Landing's Pin.")]
    public bool isLandingPin = false;

    [Tooltip("If true, this Pin only triggers after player has visited Landing.")]
    public bool requireLandingFirst = false;

    [Header("Optional Controller Override")]
    [Tooltip("Optional direct reference to M01Controller. If null, falls back to M01Controller.Instance.")]
    public M01Controller m01Controller;

    [Header("Distance Detect")]
    public float triggerDistance = 2.0f;   
    public float warningDistance = 5.0f;  

    private Vector3 initialScale;
    private bool hasTriggered = false;

    void Start()
    {
        initialScale = transform.localScale;

        // Ensure infoBubble starts hidden until triggered
        if (infoBubble != null)
        {
            infoBubble.SetActive(false);
        }

        // Auto-assign camera if unassigned
        if (arCamera == null && Camera.main != null)
        {
            arCamera = Camera.main.transform;
        }

        // Smart auto-detect: if m01Controller is explicitly assigned, default triggerDialogue to true
        if (m01Controller != null)
        {
            triggerDialogue = true;
        }

        // Smart auto-detect: if name or parent contains 'Landing', mark as landing pin
        if (gameObject.name.ToLower().Contains("landing") || 
            (transform.parent != null && transform.parent.name.ToLower().Contains("landing")))
        {
            isLandingPin = true;
            triggerDialogue = false;
        }
    }

    void Update()
    {
        if (hasTriggered || arCamera == null) return;

        // If this Pin requires Landing first, wait until Landing has been triggered
        if (requireLandingFirst && !HasVisitedLanding) return;

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
        if (requireLandingFirst && !HasVisitedLanding) return;

        // Trigger if camera or player collider touches this pin
        if (other.CompareTag("MainCamera") || (arCamera != null && other.transform == arCamera))
        {
            TriggerPinAction();
        }
    }

    void TriggerPinAction()
    {
        hasTriggered = true;
        Debug.Log($"ProximityTrigger [{gameObject.name}] triggered! (isLanding: {isLandingPin}, triggerDialogue: {triggerDialogue})");

        // 1. Mark Landing visited if applicable
        if (isLandingPin)
        {
            HasVisitedLanding = true;
        }

        // 2. Show Info Bubble / Welcoming Panel
        if (showInfoBubbleOnTrigger && infoBubble != null)
        {
            infoBubble.SetActive(true);
        }

        // 3. Trigger Susan's Dialogue ONLY if configured (for PoI 1)
        if (triggerDialogue)
        {
            if (m01Controller != null)
            {
                m01Controller.StartStoryFlow();
            }
            else if (M01Controller.Instance != null)
            {
                M01Controller.Instance.StartStoryFlow();
            }
            else
            {
                var ctrl = FindFirstObjectByType<M01Controller>();
                if (ctrl != null) ctrl.StartStoryFlow();
                else Debug.LogWarning("ProximityTrigger: No M01Controller found in scene to start dialogue!");
            }
        }

        // 4. Hide the Pin
        if (hidePinOnTrigger)
        {
            gameObject.SetActive(false);
        }
    }
}
