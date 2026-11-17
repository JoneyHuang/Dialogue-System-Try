using UnityEngine;

/// <summary>
/// Makes the HotSpot constantly face the camera (Billboard effect)
/// and provides a subtle, pleasing breathing (pulsing) effect.
/// </summary>
[DisallowMultipleComponent]
public class HotSpotBillboardPulse : MonoBehaviour
{
    [Header("Billboard Settings")]
    [Tooltip("Keep facing the main AR / Scene camera.")]
    [SerializeField] private bool billboard = true;

    [Tooltip("If checked, locks Y-axis (only rotates horizontally like a tree billboard). If unchecked, full 3D facing.")]
    [SerializeField] private bool lockYAxisOnly = false;

    [Header("Breathing (Pulse) Settings")]
    [Tooltip("Enable breathing animation on scale.")]
    [SerializeField] private bool enableScalePulse = true;

    [Tooltip("Breathing cycle speed (Hz / speed multiplier).")]
    [SerializeField] private float pulseSpeed = 2.0f;

    [Tooltip("Scale pulse amplitude (e.g. 0.08 means scale varies between 92% and 108%).")]
    [SerializeField] private float pulseAmplitude = 0.08f;

    [Header("Optional Alpha/Color Pulse")]
    [Tooltip("Optionally pulse SpriteRenderer alpha / brightness if attached.")]
    [SerializeField] private bool enableAlphaPulse = true;

    [Range(0.1f, 1f)]
    [SerializeField] private float minAlpha = 0.65f;

    [Range(0.1f, 1f)]
    [SerializeField] private float maxAlpha = 1.0f;

    private Camera targetCamera;
    private Vector3 initialBaseScale;
    private SpriteRenderer spriteRenderer;
    private Color originalSpriteColor = Color.white;

    private void Awake()
    {
        targetCamera = Camera.main;
        initialBaseScale = transform.localScale;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSpriteColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        // Re-cache scale if it was changed before enabling
        if (transform.localScale != Vector3.zero)
        {
            initialBaseScale = transform.localScale;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        // 1. Billboard Logic (Eliminates vanishing when viewed from side/angle)
        if (billboard && targetCamera != null)
        {
            if (lockYAxisOnly)
            {
                Vector3 lookPos = targetCamera.transform.position;
                lookPos.y = transform.position.y;
                transform.LookAt(lookPos);
            }
            else
            {
                // Full billboard parallel to camera view plane
                transform.rotation = targetCamera.transform.rotation;
            }
        }

        // 2. Breathing / Pulsing Effect
        float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0.0 to 1.0 sine wave

        if (enableScalePulse)
        {
            float scaleMultiplier = 1f + (wave * 2f - 1f) * pulseAmplitude;
            transform.localScale = initialBaseScale * scaleMultiplier;
        }

        if (enableAlphaPulse && spriteRenderer != null)
        {
            Color c = originalSpriteColor;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, wave);
            spriteRenderer.color = c;
        }
    }

    private void OnDisable()
    {
        // Restore initial scale and color when disabled
        transform.localScale = initialBaseScale;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalSpriteColor;
        }
    }
}
