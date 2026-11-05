using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class M01Controller : MonoBehaviour
{
    public static M01Controller Instance { get; private set; }

    public enum FlowState
    {
        Hidden,
        MeetingHint,
        Dialogue,
        AlignHint,
        HistoricalComparison
    }

    [Header("Current Flow State")]
    public FlowState currentState = FlowState.Hidden;

    [Header("Panels")]
    [Tooltip("Panel showing prompts (Hint 1 for meeting Susan, Hint 2 for positioning/alignment).")]
    public GameObject hintPanel;
    [Tooltip("Panel showing Susan's dialogue conversation.")]
    public GameObject dialoguePanel;
    [Tooltip("Panel showing the Then/Now historical photo slider and gallery.")]
    public GameObject historicalPanel;

    [Header("Hint UI")]
    public TMP_Text hintText;
    public Button hintContinueButton;
    public TMP_Text hintButtonLabel;
    [TextArea(2, 4)]
    public string meetingHintPrompt = "You have arrived at Cathedral Square!\nYou are now meeting Susan, who grew up here in Christchurch. Let's hear her story...";
    [TextArea(2, 4)]
    public string alignHintPrompt = "Before viewing the archival photo, please walk towards the site and position yourself to match the historical perspective of the Cathedral.";

    [Header("Dialogue UI")]
    [Tooltip("Dedicated controller for Dialogue.")]
    public DialogueController dialogueController;

    [Header("Historical Image (Then / Now)")]
    [Tooltip("Dedicated controller for Historical Comparison.")]
    public HistoricalComparisonController historicalComparisonController;

    [Header("Debug")]
    [Tooltip("If true, starts flow automatically on scene start (for testing without walking to Pin).")]
    public bool autoStartOnPlay = false;

    void Awake()
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

    void Start()
    {
        if (hintContinueButton != null)
        {
            hintContinueButton.onClick.RemoveListener(OnHintContinueClicked);
            hintContinueButton.onClick.AddListener(OnHintContinueClicked);
        }

        if (historicalComparisonController != null)
        {
            historicalComparisonController.onComparisonClosed -= ExitHistoricalComparison;
            historicalComparisonController.onComparisonClosed += ExitHistoricalComparison;
        }

        // Hide all panels initially until triggered by Pin
        HideAllPanels();

        if (autoStartOnPlay)
        {
            StartStoryFlow();
        }
    }

    /// <summary>
    /// Automatically searches and assigns missing references in the Canvas hierarchy.
    /// Ensures stability even if Inspector references are missing after prefab instantiation.
    /// </summary>
    void AutoWireComponents()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform canvasTr = canvas.transform;

        // Auto-wire panels
        if (hintPanel == null)
        {
            Transform t = canvasTr.Find("HintPanel");
            if (t != null) hintPanel = t.gameObject;
        }

        if (dialoguePanel == null)
        {
            Transform t = canvasTr.Find("DialoguePanel");
            if (t != null) dialoguePanel = t.gameObject;
        }

        if (historicalPanel == null)
        {
            Transform t = canvasTr.Find("HistoricalPanel");
            if (t != null) historicalPanel = t.gameObject;
        }

        // Auto-wire Hint elements
        if (hintPanel != null)
        {
            if (hintText == null)
                hintText = hintPanel.transform.Find("HintText")?.GetComponent<TMP_Text>();

            if (hintContinueButton == null)
            {
                Transform btnTr = hintPanel.transform.Find("HintContinueButton");
                if (btnTr != null)
                {
                    hintContinueButton = btnTr.GetComponent<Button>();
                    if (hintButtonLabel == null)
                        hintButtonLabel = btnTr.GetComponentInChildren<TMP_Text>();
                }
            }
        }

        // Auto-wire Dialogue elements
        if (dialoguePanel != null)
        {
            if (dialogueController == null)
                dialogueController = dialoguePanel.GetComponent<DialogueController>();
        }

        // Auto-wire Historical elements
        if (historicalPanel != null)
        {
            if (historicalComparisonController == null)
                historicalComparisonController = historicalPanel.GetComponent<HistoricalComparisonController>();
        }
    }

    public void HideAllPanels()
    {
        if (hintPanel != null) hintPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (historicalPanel != null) historicalPanel.SetActive(false);
        currentState = FlowState.Hidden;
    }

    /// <summary>
    /// Entry point triggered when user arrives at POI 1 / collides with Pin in real-world AR.
    /// </summary>
    public void StartStoryFlow()
    {
        ShowMeetingHint();
    }

    /// <summary>
    /// Step 1: Show Hint 1 ("You are now meeting Susan...")
    /// </summary>
    public void ShowMeetingHint()
    {
        currentState = FlowState.MeetingHint;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (historicalPanel != null) historicalPanel.SetActive(false);

        if (hintPanel != null)
        {
            hintPanel.SetActive(true);
            Debug.Log($"[M01Controller] ShowMeetingHint: hintPanel '{hintPanel.name}' activated successfully.");

            if (hintText != null)
            {
                hintText.text = meetingHintPrompt;
            }
            if (hintButtonLabel != null)
            {
                hintButtonLabel.text = "Meet Susan";
            }
        }
        else
        {
            Debug.LogWarning("[M01Controller] ShowMeetingHint: hintPanel is NULL! Falling back directly to StartDialogue(). Please check M01Controller Inspector.");
            // If hint panel is missing, jump directly to dialogue
            StartDialogue();
        }
    }

    void OnHintContinueClicked()
    {
        if (currentState == FlowState.MeetingHint)
        {
            // Hint 1 confirmed -> Start Dialogue
            StartDialogue();
        }
        else if (currentState == FlowState.AlignHint)
        {
            // Hint 2 confirmed -> Open Historical Panel
            ShowHistoricalPanel();
        }
    }

    /// <summary>
    /// Step 2: Susan Dialogue conversation (Delegated to DialogueController)
    /// </summary>
    public void StartDialogue()
    {
        currentState = FlowState.Dialogue;

        if (hintPanel != null) hintPanel.SetActive(false);
        if (historicalPanel != null) historicalPanel.SetActive(false);

        // 自动查找防呆
        if (dialogueController == null && dialoguePanel != null)
            dialogueController = dialoguePanel.GetComponent<DialogueController>();

        if (dialogueController != null)
        {
            dialogueController.OnDialogueFinished -= ShowAlignHint;
            dialogueController.OnDialogueFinished += ShowAlignHint;
            dialogueController.StartDialogue();
        }
        else
        {
            Debug.LogError("[M01Controller] 未找到 DialogueController，请在 DialoguePanel 挂载该脚本！");
            ShowAlignHint();
        }
    }

    /// <summary>
    /// Step 3: Show Hint 2 ("Please position yourself to align with the historical perspective...")
    /// </summary>
    public void ShowAlignHint()
    {
        currentState = FlowState.AlignHint;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (historicalPanel != null) historicalPanel.SetActive(false);

        if (hintPanel != null)
        {
            hintPanel.SetActive(true);
            if (hintText != null)
            {
                hintText.text = alignHintPrompt;
            }
            if (hintButtonLabel != null)
            {
                hintButtonLabel.text = "I'm in Position";
            }
        }
        else
        {
            ShowHistoricalPanel();
        }
    }

    /// <summary>
    /// Step 4: Show Historical Comparison Panel (Then / Now Slider & Gallery)
    /// </summary>
    public void ShowHistoricalPanel()
    {
        currentState = FlowState.HistoricalComparison;

        if (hintPanel != null) hintPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        if (historicalComparisonController != null)
        {
            historicalComparisonController.OpenComparison();
        }
    }

    public void UpdateHistoricalOpacity(float value)
    {
        if (historicalComparisonController != null)
        {
            historicalComparisonController.UpdateOpacity(value);
        }
    }

    public void ExitHistoricalComparison()
    {
        if (historicalComparisonController != null)
        {
            historicalComparisonController.CloseComparison();
        }

        currentState = FlowState.Hidden;
    }
}