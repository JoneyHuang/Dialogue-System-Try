using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime HUD for the align beat: live camera, golden rose-window reticle, prompt, status, skip.
/// </summary>
public class AlignStageView : MonoBehaviour
{
    public static readonly Color Gold = new Color(0.831f, 0.706f, 0.416f);

    public RawImage cameraFeed;
    public RoseReticleUI reticle;
    public TMP_Text promptText;
    public TMP_Text statusText;
    public Button skipButton;
    public TMP_Text skipButtonLabel;

    WebCamTexture webcam;

    public static AlignStageView Create(Canvas canvas)
    {
        var root = new GameObject("AlignStage", typeof(RectTransform), typeof(AlignStageView));
        var view = root.GetComponent<AlignStageView>();
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        Stretch(rt);
        rt.SetAsFirstSibling();

        view.cameraFeed = CreateRawImage(rt, "CameraFeed");
        if (view.HasARCamera())
        {
            view.cameraFeed.color = Color.clear;
            view.cameraFeed.gameObject.SetActive(false);
        }
        view.reticle = RoseReticleUI.Create(rt);
        view.promptText = CreatePrompt(rt);
        view.statusText = CreateStatus(rt);
        view.CreateSkipButton(rt);

        root.SetActive(false);
        return view;
    }

    public bool HasARCamera()
    {
        return FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraBackground>() != null
            || FindFirstObjectByType<UnityEngine.XR.ARFoundation.ARCameraManager>() != null;
    }

    public void ShowAligning()
    {
        gameObject.SetActive(true);
        promptText.text = "Centre the rose window in the circle.";
        statusText.text = "Looking for the rose window";
        skipButton.gameObject.SetActive(false);
        if (reticle != null)
        {
            reticle.SetProgress(0f);
        }
        StartCamera();
    }

    public void Hide()
    {
        StopCamera();
        skipButton.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void ShowSkip()
    {
        skipButton.gameObject.SetActive(true);
    }

    void OnDestroy()
    {
        StopCamera();
    }

    void StartCamera()
    {
        if (HasARCamera())
        {
            if (cameraFeed != null)
            {
                cameraFeed.color = Color.clear;
                cameraFeed.gameObject.SetActive(false);
            }
            return;
        }

        StartCoroutine(StartCameraCoroutine());
    }

    System.Collections.IEnumerator StartCameraCoroutine()
    {
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            
            // Wait for user to accept or deny dialog
            float timeout = 15f;
            while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera) && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            Debug.LogWarning("AlignStageView: Camera permission was denied.");
            cameraFeed.color = new Color(0.1f, 0.09f, 0.07f, 1f);
            yield break;
        }
#endif

        yield return new WaitForSeconds(0.2f);

        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogWarning("AlignStageView: no webcam found. Reticle will still show.");
            cameraFeed.color = new Color(0.1f, 0.09f, 0.07f, 1f);
            yield break;
        }

        string deviceName = PickCameraName();
        StopCamera();

        try
        {
            webcam = new WebCamTexture(deviceName, 1280, 720, 30);
            cameraFeed.texture = webcam;
            cameraFeed.color = Color.white;
            webcam.Play();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AlignStageView: Failed to start camera: {ex.Message}");
            cameraFeed.color = new Color(0.1f, 0.09f, 0.07f, 1f);
            yield break;
        }

        // Wait until webcam initializes dimensions to adjust rotation
        float waitInit = 2.0f;
        while (webcam != null && !webcam.didUpdateThisFrame && waitInit > 0)
        {
            waitInit -= Time.unscaledDeltaTime;
            yield return null;
        }

        AdjustCameraOrientation();
    }

    void AdjustCameraOrientation()
    {
        if (webcam == null || cameraFeed == null) return;

        // Correct rotation on mobile devices
        int videoRotationAngle = webcam.videoRotationAngle;
        cameraFeed.rectTransform.localEulerAngles = new Vector3(0, 0, -videoRotationAngle);

        if (webcam.videoVerticallyMirrored)
        {
            cameraFeed.uvRect = new Rect(0, 1, 1, -1);
        }
        else
        {
            cameraFeed.uvRect = new Rect(0, 0, 1, 1);
        }
    }

    void StopCamera()
    {
        StopAllCoroutines();
        if (webcam == null)
        {
            return;
        }

        webcam.Stop();
        Destroy(webcam);
        webcam = null;
        if (cameraFeed != null)
        {
            cameraFeed.texture = null;
        }
    }

    static string PickCameraName()
    {
        var devices = WebCamTexture.devices;
        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing)
            {
                return devices[i].name;
            }
        }
        return devices.Length > 0 ? devices[0].name : "";
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static RawImage CreateRawImage(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Stretch(rt);
        var raw = go.GetComponent<RawImage>();
        raw.color = new Color(0.1f, 0.09f, 0.07f, 1f);
        raw.raycastTarget = false;
        return raw;
    }

    static TMP_Text CreatePrompt(RectTransform parent)
    {
        var go = new GameObject("AlignPrompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.08f, 0.86f);
        rt.anchorMax = new Vector2(0.92f, 0.96f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var text = go.GetComponent<TextMeshProUGUI>();
        StyleLabel(text, 36);
        text.alignment = TextAlignmentOptions.Center;
        text.text = "Centre the rose window in the circle.";
        return text;
    }

    static TMP_Text CreateStatus(RectTransform parent)
    {
        var go = new GameObject("AlignStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.1f, 0.18f);
        rt.anchorMax = new Vector2(0.9f, 0.24f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var text = go.GetComponent<TextMeshProUGUI>();
        StyleLabel(text, 28);
        text.alignment = TextAlignmentOptions.Center;
        text.text = "Looking for the rose window";
        return text;
    }

    void CreateSkipButton(RectTransform parent)
    {
        var go = new GameObject("AlignSkip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.06f);
        rt.anchorMax = new Vector2(0.5f, 0.06f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(280, 72);
        rt.anchoredPosition = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.color = Gold;

        skipButton = go.GetComponent<Button>();
        var colors = skipButton.colors;
        colors.highlightedColor = new Color(0.9f, 0.8f, 0.5f);
        skipButton.colors = colors;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.SetParent(rt, false);
        Stretch(labelRt);
        skipButtonLabel = labelGo.GetComponent<TextMeshProUGUI>();
        StyleLabel(skipButtonLabel, 28);
        skipButtonLabel.color = new Color(0.1f, 0.09f, 0.07f);
        skipButtonLabel.alignment = TextAlignmentOptions.Center;
        skipButtonLabel.text = "Skip lock";
        skipButtonLabel.raycastTarget = false;
    }

    static void StyleLabel(TMP_Text text, float size)
    {
        text.fontSize = size;
        text.color = Gold;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }
    }
}
