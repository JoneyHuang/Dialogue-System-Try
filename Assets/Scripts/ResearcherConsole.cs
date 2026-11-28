using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hidden Researcher Console for field study control:
/// - Double-tap a subtle corner button to open/close.
/// - Force-trigger current module's ProximityTrigger (VPS Fallback).
/// - Return to Experiment Hub or reset current module Pin.
/// - Toggle Niantic VPS Debugger visibility at runtime.
/// </summary>
public class ResearcherConsole : MonoBehaviour
{
    [Header("Console UI")]
    [Tooltip("The main console panel (hidden by default).")]
    public GameObject consolePanel;

    [Tooltip("Subtle corner button; double-tap to toggle console.")]
    public Button cornerToggleButton;

    [Tooltip("Live status readout (current module, pin state, distance).")]
    public TMP_Text statusText;

    [Header("Action Buttons")]
    public Button forceTriggerPinButton;
    public Button returnToHubButton;
    public Button resetModuleButton;
    public Button toggleVpsDebugButton;
    public Button closeConsoleButton;

    [Header("External References")]
    [Tooltip("Drag the XR Origin (with AR Location Manager) here to toggle VpsDebuggerEnabled.")]
    public MonoBehaviour arLocationManager;

    [Header("Tap Settings")]
    [Tooltip("If false, a single tap on CornerToggleButton opens/closes the console.")]
    public bool requireDoubleTap = false;
    public float doubleTapWindow = 0.45f;
    private float lastTapTime = -1f;

    void Start()
    {
        if (consolePanel != null)
            consolePanel.SetActive(false);

        SetVpsDebuggerEnabled(false);

        if (cornerToggleButton != null)
            cornerToggleButton.onClick.AddListener(OnCornerButtonTapped);

        if (forceTriggerPinButton != null)
            forceTriggerPinButton.onClick.AddListener(ForceTriggerPinFallback);

        if (returnToHubButton != null)
            returnToHubButton.onClick.AddListener(ReturnToHub);

        if (resetModuleButton != null)
            resetModuleButton.onClick.AddListener(ResetCurrentModule);

        if (toggleVpsDebugButton != null)
            toggleVpsDebugButton.onClick.AddListener(ToggleVpsDebugger);

        if (closeConsoleButton != null)
            closeConsoleButton.onClick.AddListener(() => SetConsoleVisible(false));
    }

    void Update()
    {
        if (consolePanel != null && consolePanel.activeSelf)
        {
            RefreshStatusText();
        }
    }

    private void OnCornerButtonTapped()
    {
        Debug.Log("[ResearcherConsole] Corner Button tapped!");
        if (!requireDoubleTap)
        {
            bool nextState = consolePanel != null && !consolePanel.activeSelf;
            SetConsoleVisible(nextState);
            return;
        }

        if (Time.unscaledTime - lastTapTime <= doubleTapWindow)
        {
            lastTapTime = -1f;
            bool nextState = consolePanel != null && !consolePanel.activeSelf;
            SetConsoleVisible(nextState);
        }
        else
        {
            lastTapTime = Time.unscaledTime;
        }
    }

    public void SetConsoleVisible(bool visible)
    {
        if (consolePanel != null)
            consolePanel.SetActive(visible);

        if (visible)
            RefreshStatusText();
    }

    public void ForceTriggerPinFallback()
    {
        var hub = ExperimentHubController.Instance;
        if (hub == null || hub.CurrentPoI == null)
        {
            Debug.LogWarning("[ResearcherConsole] No active module selected in ExperimentHub.");
            return;
        }

        var pinObj = hub.CurrentPoI.poiTargetObject;
        if (pinObj == null)
        {
            Debug.LogWarning($"[ResearcherConsole] {hub.CurrentPoI.poiId} has no poiTargetObject assigned.");
            return;
        }

        var trigger = pinObj.GetComponent<ProximityTrigger>();
        if (trigger != null)
        {
            Debug.Log($"[ResearcherConsole] [FALLBACK] Force-triggered Pin for {hub.CurrentPoI.poiId}.");
            trigger.TriggerPinAction();
            SetConsoleVisible(false);
        }
        else
        {
            Debug.LogWarning($"[ResearcherConsole] No ProximityTrigger found on {pinObj.name}.");
        }
    }

    public void ReturnToHub()
    {
        if (ExperimentHubController.Instance != null)
        {
            ExperimentHubController.Instance.ReturnToHub();
        }
        SetConsoleVisible(false);
    }

    public void ResetCurrentModule()
    {
        var hub = ExperimentHubController.Instance;
        if (hub == null || hub.CurrentPoI == null || hub.CurrentPoI.poiTargetObject == null)
        {
            Debug.LogWarning("[ResearcherConsole] No active module Pin to reset.");
            return;
        }

        // 1. Clear dialogue panel and audio
        var dialogue = FindFirstObjectByType<DialogueController>(FindObjectsInactive.Include);
        if (dialogue != null)
        {
            dialogue.StopAllCoroutines();
            var audio = dialogue.GetComponent<AudioSource>();
            if (audio != null && audio.isPlaying) audio.Stop();
            dialogue.gameObject.SetActive(false);
        }

        // 2. Hide prompt panel and MeetMe panel
        var prompt = FindFirstObjectByType<PromptPanelController>(FindObjectsInactive.Include);
        if (prompt != null) prompt.Hide();

        var meetMe = FindFirstObjectByType<MeetMePanelController>(FindObjectsInactive.Include);
        if (meetMe != null) meetMe.Hide();

        // 3. According to the current module, clear the AR objects and panels
        string poiId = hub.CurrentPoI.poiId;
        if (poiId == "M1" && M01Controller.Instance != null)
        {
            M01Controller.Instance.HideAllPanels();
        }
        else if (poiId == "M2" && M02Controller.Instance != null)
        {
            M02Controller.Instance.HideAllPanels();
        }
        else if (poiId == "M3" && M03Controller.Instance != null)
        {
            M03Controller.Instance.HideAllPanels();
            M03Controller.Instance.HideAllHotspots();
        }

        // 4. Reset Pin status and show it again
        var trigger = hub.CurrentPoI.poiTargetObject.GetComponent<ProximityTrigger>();
        if (trigger != null)
        {
            trigger.ResetTrigger();
            Debug.Log($"[ResearcherConsole] Cleaned UI and reset Pin trigger for {hub.CurrentPoI.poiId}.");
        }

        SetConsoleVisible(false);
    }

    public void ToggleVpsDebugger()
    {
        if (arLocationManager == null)
        {
            Debug.LogWarning("[ResearcherConsole] arLocationManager is not assigned.");
            return;
        }

        bool nextState = !GetVpsDebuggerEnabled();
        SetVpsDebuggerEnabled(nextState);
        Debug.Log($"[ResearcherConsole] VPS Debugger {(nextState ? "Enabled" : "Disabled")}.");
    }

    private bool GetVpsDebuggerEnabled()
    {
        if (arLocationManager == null) return false;
        var prop = arLocationManager.GetType().GetProperty("VpsDebuggerEnabled");
        if (prop != null && prop.PropertyType == typeof(bool))
            return (bool)prop.GetValue(arLocationManager);
        var field = arLocationManager.GetType().GetField("VpsDebuggerEnabled");
        if (field != null && field.FieldType == typeof(bool))
            return (bool)field.GetValue(arLocationManager);
        return false;
    }

    private void SetVpsDebuggerEnabled(bool enabled)
    {
        if (arLocationManager == null) return;
        var prop = arLocationManager.GetType().GetProperty("VpsDebuggerEnabled");
        if (prop != null && prop.PropertyType == typeof(bool))
        {
            prop.SetValue(arLocationManager, enabled);
            return;
        }
        var field = arLocationManager.GetType().GetField("VpsDebuggerEnabled");
        if (field != null && field.FieldType == typeof(bool))
            field.SetValue(arLocationManager, enabled);
    }

    private void RefreshStatusText()
    {
        if (statusText == null) return;

        var hub = ExperimentHubController.Instance;
        if (hub == null || hub.CurrentPoI == null)
        {
            statusText.text = "Module: None (In Hub)";
            return;
        }

        var poi = hub.CurrentPoI;
        string pinState = "No Pin";
        if (poi.poiTargetObject != null)
        {
            float dist = Camera.main != null
                ? Vector3.Distance(poi.poiTargetObject.transform.position, Camera.main.transform.position)
                : -1f;
            pinState = poi.poiTargetObject.activeInHierarchy
                ? $"Pin Active ({dist:F1}m)"
                : "Pin Inactive";
        }

        string vpsState = GetVpsDebuggerEnabled() ? "ON" : "OFF";
        statusText.text = $"Module: {poi.poiId} | {pinState} | VPS Debug: {vpsState}";
    }
}
