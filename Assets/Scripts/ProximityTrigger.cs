using UnityEngine;

public class ProximityTrigger : MonoBehaviour
{
    [Header("Navigate Pin")]
    public Transform arCamera;
    public GameObject infoBubble;

    [Header("Distance Detect")]
    public float triggerDistance = 2.5f;   
    public float warningDistance = 5.0f;  

    private Vector3 initialScale;
    private bool hasTriggered = false;


    void Start()
    {
        initialScale = transform.localScale;
        if (infoBubble != null) infoBubble.SetActive(false);
    }

    void Update()
    {
        if (hasTriggered || arCamera == null ) return;

        float distance = Vector3.Distance(transform.position, arCamera.position);

        if (distance <= warningDistance && distance > triggerDistance)
        {
            float t = Mathf.PingPong(Time.time * 2f, 1f);
            transform.localScale = Vector3.Lerp(initialScale, initialScale * 1.2f, t);
        }

        else if (distance <= triggerDistance)
        {
            TriggerInfoBubble();
        }
        else
        {
            transform.localScale = initialScale;
        }
    }

    void TriggerInfoBubble()
    {
        hasTriggered = true;
        Debug.Log("Approaching Pin -> Info Bubble！");

        infoBubble.SetActive(true);

        gameObject.SetActive(false);
    }
}