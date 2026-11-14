using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// M03 Controller (Coordinator / Sequencer for PoI 3: Mr Watson).
/// Orchestrates the entire PoI 3 lifecycle:
/// 1. Arrival Prompt ("Meet Mr Watson")
/// 2. Intro Dialogue with Mr Watson
/// 3. Interaction Instruction (Prompt explaining the 3 memories in space)
/// 4. Exploration Hub:
///    - Activates 3 spatial hotspots (Rose Window, Choir, Stone).
///    - Displays a lightweight, non-intrusive progress HUD: [ ○ ○ ○ ] -> [ ● ○ ○ ]
///    - Persistent "Finish & Reflect" button providing full user agency (can wrap up anytime).
///    - Confirmation dialog if "Finish" is clicked prematurely (preventing accidental skips).
///    - Never auto-forces the user out of the scene when completed; prompts them gracefully so they can keep observing the physical environment.
/// 5. Reflection Card (Mr Watson's wrap-up question).
/// 6. Notifies ExperimentHubController upon completion.
/// </summary>
public class M03Controller : MonoBehaviour
{
    public static M03Controller Instance { get; private set; }

    public enum FlowState
    {
        Hidden,
        ArrivalPrompt,
        Dialogue,
        InteractionInstruction,
        ExplorationHub,
        Reflection
    }

    [Header("Current Flow State")]
    public FlowState currentState = FlowState.Hidden;

    [Header("Story Data")]
    [Tooltip("Drag Assets/StoryData/PoI_03_Mr Watson.asset here.")]
    public PoIStoryData storyData;

    [Header("Shared UI Controllers")]
    [Tooltip("Generic prompt/hint modal controller (PromptPanel / HintPanel).")]
    public PromptPanelController promptPanelController;

    [Tooltip("Typewriter dialogue controller (DialoguePanel).")]
    public DialogueController dialogueController;

    [Header("PoI 3 Hotspots in Scene")]
    [Tooltip("Root or trigger for the Rose Window hotspot.")]
    public GameObject hotspotRoseWindow;

    [Tooltip("Root or trigger for the Choir hotspot.")]
    public GameObject hotspotChoir;

    [Tooltip("Root or trigger for the Stone hotspot.")]
    public GameObject hotspotStone;

    [Header("Exploration Hub HUD (Progress & Agency)")]
    [Tooltip("Root UI GameObject for the exploration HUD during M03.")]
    public GameObject explorationHudRoot;

    [Tooltip("Text displaying progress, e.g. 'Memories: [ ● ○ ○ ] 1/3'.")]
    public TMP_Text progressText;

    [Tooltip("Button allowing user to conclude exploration and proceed to reflection at their own pace.")]
    public Button finishExplorationButton;

    [Header("Accidental Click Guard (Confirmation Modal)")]
    [Tooltip("Optional confirmation panel shown if user clicks Finish before finding all 3 spots.")]
    public GameObject confirmFinishDialog;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("Debug")]
    [Tooltip("Automatically start flow on Play (keep false for production/field tests).")]
    public bool autoStartOnPlay = false;

    // Hotspot tracking
    private readonly HashSet<string> inspectedHotspots = new HashSet<string>();
    private const int kTotalHotspots = 3;

    // Fallback texts
    private const string kFallbackArrival =
        "As a choirister in the 1970s, a lifetime of memories about the Cathedral stays with Mr Watson. Even after the earthquakes, he has continued to follow the Cathedral's story.";
    private const string kFallbackArrivalBtn = "Meet Mr Watson";
    private const string kFallbackInstruction =
        "Three memories are waiting in this space \u2014 carved in stone, reflected in glass, and carried in music. Explore them at your own pace, and feel free to look around and touch your surroundings.";
    private const string kFallbackInstructionBtn = "Start Exploring";
    private const string kFallbackReflection =
        "\"What do you think makes a place worth remembering?\"";
    private const string kFallbackReflectionBtn = "Finish Visit";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        AutoWireComponents();
    }

    private void Start()
    {
        HideAllHotspots();
        HideAllPanels();
        WireHudButtons();

        if (autoStartOnPlay)
        {
            StartStoryFlow();
        }
    }

    /// <summary>
    /// Auto-discovers UI components in the canvas to prevent missing Inspector references.
    /// </summary>
    public void AutoWireComponents()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            if (promptPanelController == null)
            {
                promptPanelController = canvas.GetComponentInChildren<PromptPanelController>(true);
            }
            if (dialogueController == null)
            {
                dialogueController = canvas.GetComponentInChildren<DialogueController>(true);
            }
        }

        // Auto-find scene hotspots if not assigned
        if (hotspotRoseWindow == null)
            hotspotRoseWindow = GameObject.Find("HotSpot (Rose Window)");
        if (hotspotChoir == null)
            hotspotChoir = GameObject.Find("HotSpot (Choir)");
        if (hotspotStone == null)
            hotspotStone = GameObject.Find("HotSpot (Stone)");
    }

    private void WireHudButtons()
    {
        if (finishExplorationButton != null)
        {
            finishExplorationButton.onClick.RemoveListener(OnFinishButtonClicked);
            finishExplorationButton.onClick.AddListener(OnFinishButtonClicked);
        }

        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveListener(OnConfirmFinishConfirmed);
            confirmYesButton.onClick.AddListener(OnConfirmFinishConfirmed);
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveListener(OnConfirmFinishDismissed);
            confirmNoButton.onClick.AddListener(OnConfirmFinishDismissed);
        }

        if (confirmFinishDialog != null)
        {
            confirmFinishDialog.SetActive(false);
        }
    }

    // ─── Public Flow Trigger ──────────────────────────────────────────────────

    /// <summary>
    /// Starts the M03 experience sequence. Called by proximity triggers or ExperimentHub.
    /// </summary>
    public void StartStoryFlow()
    {
        Debug.Log("[M03Controller] Starting PoI 3 sequence (Mr Watson)...");
        inspectedHotspots.Clear();
        UpdateProgressHUD();

        ShowArrivalPrompt();
    }

    // ─── Phase 1: Arrival Prompt ──────────────────────────────────────────────

    private void ShowArrivalPrompt()
    {
        currentState = FlowState.ArrivalPrompt;
        HideAllPanels();

        string text = storyData != null && !string.IsNullOrEmpty(storyData.arrivalPrompt)
            ? storyData.arrivalPrompt
            : kFallbackArrival;

        string btn = storyData != null && !string.IsNullOrEmpty(storyData.arrivalButtonText)
            ? storyData.arrivalButtonText
            : kFallbackArrivalBtn;

        if (promptPanelController != null)
        {
            promptPanelController.ShowPrompt(text, btn, OnArrivalPromptConfirmed);
        }
        else
        {
            Debug.LogWarning("[M03Controller] PromptPanelController missing, proceeding to Dialogue.");
            OnArrivalPromptConfirmed();
        }
    }

    private void OnArrivalPromptConfirmed()
    {
        if (promptPanelController != null)
            promptPanelController.Hide();

        ShowIntroDialogue();
    }

    // ─── Phase 2: Intro Dialogue ──────────────────────────────────────────────

    private void ShowIntroDialogue()
    {
        currentState = FlowState.Dialogue;

        if (dialogueController != null)
        {
            dialogueController.OnDialogueFinished -= OnDialogueFinished;
            dialogueController.OnDialogueFinished += OnDialogueFinished;

            string speaker = storyData != null && !string.IsNullOrEmpty(storyData.speakerName)
                ? storyData.speakerName
                : "Mr Watson";

            string[] lines = storyData != null && storyData.dialogueLines != null && storyData.dialogueLines.Length > 0
                ? storyData.dialogueLines
                : new string[] { "Hello, young friend. Let me show you around this sacred place." };

            dialogueController.StartDialogue(lines, speaker);
        }
        else
        {
            Debug.LogWarning("[M03Controller] DialogueController missing, proceeding to Instruction.");
            OnDialogueFinished();
        }
    }

    private void OnDialogueFinished()
    {
        if (dialogueController != null)
        {
            dialogueController.OnDialogueFinished -= OnDialogueFinished;
            dialogueController.gameObject.SetActive(false);
        }

        ShowInteractionInstruction();
    }

    // ─── Phase 3: Interaction Instruction ────────────────────────────────────

    private void ShowInteractionInstruction()
    {
        currentState = FlowState.InteractionInstruction;

        string text = storyData != null && !string.IsNullOrEmpty(storyData.interactionInstruction)
            ? storyData.interactionInstruction
            : kFallbackInstruction;

        string btn = storyData != null && !string.IsNullOrEmpty(storyData.interactionButtonText)
            ? storyData.interactionButtonText
            : kFallbackInstructionBtn;

        if (promptPanelController != null)
        {
            promptPanelController.ShowPrompt(text, btn, OnInstructionConfirmed);
        }
        else
        {
            OnInstructionConfirmed();
        }
    }

    private void OnInstructionConfirmed()
    {
        if (promptPanelController != null)
            promptPanelController.Hide();

        EnterExplorationHub();
    }

    // ─── Phase 4: Exploration Hub ────────────────────────────────────────────

    private void EnterExplorationHub()
    {
        currentState = FlowState.ExplorationHub;
        Debug.Log("[M03Controller] Entering Exploration Hub. Hotspots active.");

        // Show all 3 hotspots
        ShowAllHotspots();
        SubscribeHotspotEvents();

        // Show Exploration HUD
        if (explorationHudRoot != null)
        {
            explorationHudRoot.SetActive(true);
        }
        UpdateProgressHUD();
    }

    private void SubscribeHotspotEvents()
    {
        // Rose Window trigger
        if (hotspotRoseWindow != null)
        {
            var trigger = hotspotRoseWindow.GetComponent<RoseWindowHotSpotTrigger>();
            if (trigger != null)
            {
                trigger.onHotSpotOpened.RemoveListener(OnRoseWindowOpened);
                trigger.onHotSpotOpened.AddListener(OnRoseWindowOpened);
            }
        }

        // Choir & Stone (InfoBubbleTrigger)
        SubscribeBubbleTrigger(hotspotChoir, "Choir");
        SubscribeBubbleTrigger(hotspotStone, "Stone");
    }

    private void SubscribeBubbleTrigger(GameObject targetObj, string hotspotId)
    {
        if (targetObj == null) return;
        var bubble = targetObj.GetComponentInChildren<InfoBubbleTrigger>(true);
        if (bubble != null)
        {
            bubble.onBubbleOpened.RemoveListener(() => MarkHotspotVisited(hotspotId));
            bubble.onBubbleOpened.AddListener(() => MarkHotspotVisited(hotspotId));
        }
    }

    private void OnRoseWindowOpened()
    {
        MarkHotspotVisited("RoseWindow");
    }

    /// <summary>
    /// Records a hotspot as inspected, updating the progress indicator.
    /// </summary>
    public void MarkHotspotVisited(string hotspotId)
    {
        if (!inspectedHotspots.Contains(hotspotId))
        {
            inspectedHotspots.Add(hotspotId);
            Debug.Log($"[M03Controller] Visited hotspot: {hotspotId}. Total: {inspectedHotspots.Count}/{kTotalHotspots}");
            UpdateProgressHUD();
        }
    }

    private void UpdateProgressHUD()
    {
        if (progressText != null)
        {
            int count = inspectedHotspots.Count;
            string dot1 = inspectedHotspots.Contains("Stone") ? "●" : "○";
            string dot2 = inspectedHotspots.Contains("RoseWindow") ? "●" : "○";
            string dot3 = inspectedHotspots.Contains("Choir") ? "●" : "○";

            if (count >= kTotalHotspots)
            {
                progressText.text = $"Memories: [{dot1} {dot2} {dot3}] 3/3 (All Discovered)";
            }
            else
            {
                progressText.text = $"Memories: [{dot1} {dot2} {dot3}] {count}/3";
            }
        }
    }

    private void OnFinishButtonClicked()
    {
        // Prevent accidental skip if not all hotspots were visited
        if (inspectedHotspots.Count < kTotalHotspots)
        {
            if (confirmFinishDialog != null)
            {
                confirmFinishDialog.SetActive(true);
                return;
            }
        }

        // Fully completed or confirmed: proceed to reflection
        TransitionToReflection();
    }

    private void OnConfirmFinishConfirmed()
    {
        if (confirmFinishDialog != null) confirmFinishDialog.SetActive(false);
        TransitionToReflection();
    }

    private void OnConfirmFinishDismissed()
    {
        if (confirmFinishDialog != null) confirmFinishDialog.SetActive(false);
    }

    // ─── Phase 5: Reflection & Exit ──────────────────────────────────────────

    private void TransitionToReflection()
    {
        currentState = FlowState.Reflection;
        Debug.Log("[M03Controller] Exiting exploration, entering Reflection.");

        HideAllHotspots();
        if (explorationHudRoot != null) explorationHudRoot.SetActive(false);
        if (confirmFinishDialog != null) confirmFinishDialog.SetActive(false);

        string text = storyData != null && !string.IsNullOrEmpty(storyData.reflectionPrompt)
            ? storyData.reflectionPrompt
            : kFallbackReflection;

        string btn = storyData != null && !string.IsNullOrEmpty(storyData.reflectionButtonText)
            ? storyData.reflectionButtonText
            : kFallbackReflectionBtn;

        if (promptPanelController != null)
        {
            promptPanelController.ShowPrompt(text, btn, OnReflectionConfirmed);
        }
        else
        {
            OnReflectionConfirmed();
        }
    }

    private void OnReflectionConfirmed()
    {
        if (promptPanelController != null)
            promptPanelController.Hide();

        currentState = FlowState.Hidden;
        Debug.Log("[M03Controller] PoI 3 completed. Reporting to ExperimentHubController...");

        // Notify ExperimentHub if present
        if (ExperimentHubController.Instance != null)
        {
            ExperimentHubController.Instance.OnPoICompleted("M3");
        }
    }

    // ─── Utility Methods ──────────────────────────────────────────────────────

    public void ShowAllHotspots()
    {
        if (hotspotRoseWindow != null) hotspotRoseWindow.SetActive(true);
        if (hotspotChoir != null) hotspotChoir.SetActive(true);
        if (hotspotStone != null) hotspotStone.SetActive(true);
    }

    public void HideAllHotspots()
    {
        if (hotspotRoseWindow != null) hotspotRoseWindow.SetActive(false);
        if (hotspotChoir != null) hotspotChoir.SetActive(false);
        if (hotspotStone != null) hotspotStone.SetActive(false);
    }

    public void HideAllPanels()
    {
        if (promptPanelController != null) promptPanelController.Hide();
        if (dialogueController != null) dialogueController.gameObject.SetActive(false);
        if (explorationHudRoot != null) explorationHudRoot.SetActive(false);
        if (confirmFinishDialog != null) confirmFinishDialog.SetActive(false);
    }
}
