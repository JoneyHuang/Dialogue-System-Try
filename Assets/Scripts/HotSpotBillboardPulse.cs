using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes the HotSpot constantly face the camera (Billboard effect)
/// and provides a subtle, pleasing breathing (pulsing) effect.
/// Also handles distance-based fading and wall-facing culling so hotspots
/// on the other side of walls/corners do not show through or get accidentally clicked.
/// Supports both UI Image/Graphic elements and 3D Renderers/Sprites.
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
    [Tooltip("Optionally pulse alpha / brightness.")]
    [SerializeField] private bool enableAlphaPulse = true;

    [Range(0.1f, 1f)]
    [SerializeField] private float minAlpha = 0.65f;

    [Range(0.1f, 1f)]
    [SerializeField] private float maxAlpha = 1.0f;

    [Header("Distance Fading / Culling")]
    [Tooltip("Enable distance-based fade and culling.")]
    [SerializeField] private bool enableDistanceCulling = true;

    [Tooltip("Distance (meters) within which the hotspot is fully 100% visible.")]
    [SerializeField] private float fadeStartDistance = 4.0f;

    [Tooltip("Distance (meters) beyond which the hotspot is completely hidden and unclickable.")]
    [SerializeField] private float visibleDistance = 8.0f;

    [Header("Wall Facing (Normal) Culling")]
    [Tooltip("Hide hotspot and disable colliders when viewed from behind the wall/corner.")]
    [SerializeField] private bool enableWallNormalCulling = true;

    [Tooltip("The initial forward direction of this hotspot is treated as the wall facing normal (pointing outwards into the room).")]
    [SerializeField] private bool useInitialForwardAsNormal = true;

    [Tooltip("Custom outward normal in local space if not using forward (e.g. Vector3.forward or Vector3.back).")]
    [SerializeField] private Vector3 customLocalNormal = Vector3.forward;

    [Tooltip("Angle cutoff threshold for wall culling. Dot product > this value means front side (0 = 90 deg, -0.2 = allows generous side viewing).")]
    [Range(-0.8f, 0.5f)]
    [SerializeField] private float wallFacingThreshold = -0.25f;

    [Header("Collider Management")]
    [Tooltip("If true, colliders will be automatically disabled when the hotspot is culled (hidden). Set to false to keep colliders always clickable.")]
    [SerializeField] private bool manageCollidersWithVisibility = false;

    private Camera targetCamera;
    
    // Immutable base scale cached once on Awake (never corrupted by SetActive or culling)
    private Vector3 originalBaseScale = Vector3.one;
    private bool baseScaleInitialized = false;

    // Visual elements support (both UI Graphics and 3D/Sprite Renderers)
    private Graphic[] childGraphics;
    private Color[] originalGraphicColors;
    private Renderer[] childRenderers;
    private Color[] originalRendererColors;
    private CanvasGroup childCanvasGroup;

    private Collider[] myColliders;
    private Vector3 worldWallNormal;

    private void Awake()
    {
        targetCamera = Camera.main;

        // 1. Cache the pristine, authentic initial scale once and for all
        if (!baseScaleInitialized)
        {
            originalBaseScale = transform.localScale;
            // Guard against edge cases where prefab might have 0 scale on awake
            if (originalBaseScale == Vector3.zero)
            {
                originalBaseScale = Vector3.one;
            }
            baseScaleInitialized = true;
        }

        // 2. Cache CanvasGroup if present (most performant and robust for UI-based hotspots)
        childCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        if (childCanvasGroup == null)
        {
            Canvas canvas = GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                childCanvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            }
        }

        // 3. Cache UI Graphics (Images, TMP, Text)
        childGraphics = GetComponentsInChildren<Graphic>(true);
        if (childGraphics != null && childGraphics.Length > 0)
        {
            originalGraphicColors = new Color[childGraphics.Length];
            for (int i = 0; i < childGraphics.Length; i++)
            {
                originalGraphicColors[i] = childGraphics[i].color;
            }
        }

        // 4. Cache 3D / Sprite Renderers (if any)
        childRenderers = GetComponentsInChildren<Renderer>(true);
        if (childRenderers != null && childRenderers.Length > 0)
        {
            originalRendererColors = new Color[childRenderers.Length];
            for (int i = 0; i < childRenderers.Length; i++)
            {
                if (childRenderers[i].sharedMaterial != null && childRenderers[i].sharedMaterial.HasProperty("_Color"))
                {
                    originalRendererColors[i] = childRenderers[i].material.color;
                }
                else
                {
                    originalRendererColors[i] = Color.white;
                }
            }
        }

        // 5. Cache Colliders
        myColliders = GetComponentsInChildren<Collider>(true);

        RecordWallNormal();
    }

    private void RecordWallNormal()
    {
        if (useInitialForwardAsNormal)
        {
            worldWallNormal = transform.forward;
        }
        else
        {
            worldWallNormal = transform.TransformDirection(customLocalNormal).normalized;
        }
    }

    private void OnEnable()
    {
        // Always restore the genuine base scale upon enabling
        if (baseScaleInitialized && originalBaseScale != Vector3.zero)
        {
            transform.localScale = originalBaseScale;
        }
        RecordWallNormal();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null) return;

        Vector3 cameraPos = targetCamera.transform.position;
        Vector3 myPos = transform.position;
        Vector3 toCamera = cameraPos - myPos;
        float distance = toCamera.magnitude;

        // ─── 1. Wall Normal Culling (Behind Wall / Corner) ─────────────────────
        bool isBehindWall = false;
        if (enableWallNormalCulling && worldWallNormal != Vector3.zero && distance > 0.001f)
        {
            Vector3 dirToCam = toCamera / distance;
            float dot = Vector3.Dot(dirToCam, worldWallNormal);
            // If dot product is below threshold, camera is behind the wall surface
            if (dot < wallFacingThreshold)
            {
                isBehindWall = true;
            }
        }

        // ─── 2. Distance Culling Factor (0.0 = invisible, 1.0 = fully visible) ─
        float distanceFactor = 1.0f;
        if (enableDistanceCulling)
        {
            if (distance >= visibleDistance)
            {
                distanceFactor = 0.0f;
            }
            else if (distance <= fadeStartDistance)
            {
                distanceFactor = 1.0f;
            }
            else
            {
                float t = (distance - fadeStartDistance) / Mathf.Max(0.01f, (visibleDistance - fadeStartDistance));
                distanceFactor = 1.0f - Mathf.SmoothStep(0f, 1f, t);
            }
        }

        bool shouldBeVisible = !isBehindWall && (distanceFactor > 0.01f);

        // ─── 3. Collider State (Prevent accidental misclick through walls) ──────
        UpdateColliderState(shouldBeVisible);

        // ─── 4. Visibility Control ─────────────────────────────────────────────
        if (!shouldBeVisible)
        {
            ApplyVisibility(0f);
            return;
        }

        // ─── 5. Billboard Rotation ────────────────────────────────────────────
        if (billboard)
        {
            if (lockYAxisOnly)
            {
                Vector3 lookPos = cameraPos;
                lookPos.y = myPos.y;
                transform.LookAt(lookPos);
            }
            else
            {
                transform.rotation = targetCamera.transform.rotation;
            }
        }

        // ─── 6. Breathing / Pulsing Scale ──────────────────────────────────────
        float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0.0 to 1.0 sine wave
        float scaleFactor = 1.0f;

        if (enableScalePulse)
        {
            scaleFactor = 1f + (wave * 2f - 1f) * pulseAmplitude;
        }

        // Apply scale without ever corrupting originalBaseScale
        transform.localScale = originalBaseScale * (scaleFactor * Mathf.Max(0.1f, distanceFactor));

        // ─── 7. Alpha Pulse & Distance Modulation ──────────────────────────────
        float currentAlpha = 1.0f;
        if (enableAlphaPulse)
        {
            currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, wave);
        }
        currentAlpha *= distanceFactor;

        ApplyVisibility(currentAlpha);
    }

    private void ApplyVisibility(float alpha)
    {
        // 1. If CanvasGroup is present, use it for seamless alpha control of all UI children
        if (childCanvasGroup != null)
        {
            childCanvasGroup.alpha = alpha;
            childCanvasGroup.blocksRaycasts = alpha > 0.1f;
            childCanvasGroup.interactable = alpha > 0.1f;
        }
        else
        {
            // Fallback: manually modulate Graphics alpha
            if (childGraphics != null && originalGraphicColors != null)
            {
                for (int i = 0; i < childGraphics.Length; i++)
                {
                    if (childGraphics[i] != null)
                    {
                        Color c = originalGraphicColors[i];
                        c.a = originalGraphicColors[i].a * alpha;
                        childGraphics[i].color = c;
                    }
                }
            }
        }

        // 2. Modulate 3D Renderers if present
        if (childRenderers != null && originalRendererColors != null)
        {
            for (int i = 0; i < childRenderers.Length; i++)
            {
                if (childRenderers[i] != null && childRenderers[i].enabled != (alpha > 0.01f))
                {
                    childRenderers[i].enabled = alpha > 0.01f;
                }
            }
        }
    }

    private void UpdateColliderState(bool active)
    {
        if (!manageCollidersWithVisibility) return;
        if (myColliders == null) return;
        for (int i = 0; i < myColliders.Length; i++)
        {
            if (myColliders[i] != null && myColliders[i].enabled != active)
            {
                myColliders[i].enabled = active;
            }
        }
    }

    private void OnDisable()
    {
        if (baseScaleInitialized && originalBaseScale != Vector3.zero)
        {
            transform.localScale = originalBaseScale;
        }
        ApplyVisibility(1.0f);
        UpdateColliderState(true);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 normal = Application.isPlaying && worldWallNormal != Vector3.zero
            ? worldWallNormal
            : (useInitialForwardAsNormal ? transform.forward : transform.TransformDirection(customLocalNormal).normalized);

        Gizmos.DrawRay(transform.position, normal * 1.5f);

        if (enableDistanceCulling)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, fadeStartDistance);

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, visibleDistance);
        }
    }
}
