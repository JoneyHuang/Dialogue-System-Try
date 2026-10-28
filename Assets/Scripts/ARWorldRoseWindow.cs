using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the in-world AR Rose Window object.
/// Features a 0.8s spiral zoom-in entrance, billboarding towards camera,
/// and a dedicated close button that hides/shows properly.
/// </summary>
public class ARWorldRoseWindow : MonoBehaviour
{
    [Header("Visual Components")]
    [Tooltip("The SpriteRenderer displaying the Rose Window.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Optional close button (UI Button on screen or in world). Will be automatically shown/hidden with the window.")]
    [SerializeField] private Button closeButton;

    [Header("Animation Settings")]
    [Tooltip("Target local scale in AR space.")]
    [SerializeField] private Vector3 targetScale = new Vector3(1.2f, 1.2f, 1.2f);

    [Tooltip("Total duration of spiral entrance animation in seconds.")]
    [SerializeField] private float animationDuration = 0.8f;

    [Tooltip("Total spin in degrees on Z-axis during entrance.")]
    [SerializeField] private float totalSpinDegrees = 720f;

    [Header("Billboard Settings")]
    [Tooltip("Whether to constantly face the main AR camera.")]
    [SerializeField] private bool billboardToCamera = true;

    private Camera targetCamera;
    private Coroutine activeAnimRoutine;
    private Action onCloseCallback;
    private bool isVisible = false;

    private void Awake()
    {
        targetCamera = Camera.main;
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
            // Hide close button initially
            closeButton.gameObject.SetActive(false);
        }

        // Hide self initially
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!isVisible || !billboardToCamera) return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            transform.forward = targetCamera.transform.forward;
        }
    }

    /// <summary>
    /// Opens the Rose Window at the specified world position, plays spiral zoom-in,
    /// and displays the close button.
    /// </summary>
    public void Open(Vector3 spawnPosition, Action onClose = null)
    {
        onCloseCallback = onClose;
        transform.position = spawnPosition;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            transform.forward = targetCamera.transform.forward;
        }

        gameObject.SetActive(true);
        isVisible = true;

        // Show close button if assigned
        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        if (activeAnimRoutine != null)
            StopCoroutine(activeAnimRoutine);

        activeAnimRoutine = StartCoroutine(SpiralEntranceRoutine());
    }

    private IEnumerator SpiralEntranceRoutine()
    {
        float elapsed = 0f;
        transform.localScale = Vector3.zero;

        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        startColor.a = 0f;
        if (spriteRenderer != null) spriteRenderer.color = startColor;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);

            // Ease-out cubic curve: 1 - (1 - t)^3
            float easeOut = 1f - Mathf.Pow(1f - t, 3f);

            // 1. Scale
            transform.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScale, easeOut);

            // 2. Spiral rotation on Z
            float currentAngle = Mathf.Lerp(totalSpinDegrees, 0f, easeOut);
            transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

            // 3. Alpha
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Clamp01(easeOut);
                spriteRenderer.color = c;
            }

            yield return null;
        }

        transform.localScale = targetScale;
        transform.localRotation = Quaternion.identity;
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        activeAnimRoutine = null;
    }

    /// <summary>
    /// Closes the Rose Window: hides the close button immediately, shrinks the window, and restores the HotSpot.
    /// </summary>
    public void Close()
    {
        if (!isVisible) return;
        isVisible = false;

        // Instantly hide the close button on click
        if (closeButton != null)
            closeButton.gameObject.SetActive(false);

        if (activeAnimRoutine != null)
            StopCoroutine(activeAnimRoutine);

        activeAnimRoutine = StartCoroutine(ExitRoutine());
    }

    private IEnumerator ExitRoutine()
    {
        float elapsed = 0f;
        float exitDuration = 0.2f;
        Vector3 startScale = transform.localScale;

        while (elapsed < exitDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / exitDuration);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 1f - t;
                spriteRenderer.color = c;
            }
            yield return null;
        }

        gameObject.SetActive(false);
        activeAnimRoutine = null;

        // Restore HotSpot
        onCloseCallback?.Invoke();
        onCloseCallback = null;
    }
}
