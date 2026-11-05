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
    public TMP_Text dialogueText;
    public TMP_Text characterName;
    public Button continueButton;

    [Header("Typewriter Settings")]
    [Tooltip("Seconds per character during dialogue typing.")]
    public float typingSpeed = 0.035f;
    [Tooltip("Pause extra time on punctuation marks (,, ., !, ?, —) to simulate natural breathing.")]
    public bool enablePunctuationPauses = true;

    [Header("Historical Image (Then / Now)")]
    [Tooltip("Dedicated controller for Historical Comparison.")]
    public HistoricalComparisonController historicalComparisonController;

    [Header("Debug")]
    [Tooltip("If true, starts flow automatically on scene start (for testing without walking to Pin).")]
    public bool autoStartOnPlay = false;

    private string[] dialogue =
    {
        "My name is Susan. I grew up and live in Christchurch.",
        "Standing here in Cathedral Square today, it's quiet... It's different from what I remembered.",
        "The tower and spire are missing, but they carry my special memories.",
        "When I was at school, I got lost around Cathedral Square during the trip. I remember my teacher, Mr Watson, told us:",
        " 'If you are lost, look for the Cathedral spire—it's the tallest building in the town!' ",
        " I heard the bell, looked up, and found my way back.",
        "The last time I saw the spire, I was visiting the square with my homestay student.",
        "Now, when I pass by here, I still see the absence of that spire. I try to outline its contours in my mind... just like lifting my phone to capture it — it's a habit that never left me..."
    };

    private int currentLine = 0;
    private Coroutine typingCoroutine = null;
    private bool isTyping = false;
    private Button dialoguePanelButton = null;

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
        // Register button listeners
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnDialogueClicked);
            continueButton.onClick.AddListener(OnDialogueClicked);
        }

        // Auto-support panel clicking (clicking anywhere on DialoguePanel advances or skips)
        if (dialoguePanel != null)
        {
            dialoguePanelButton = dialoguePanel.GetComponent<Button>();
            if (dialoguePanelButton == null)
            {
                dialoguePanelButton = dialoguePanel.AddComponent<Button>();
                dialoguePanelButton.transition = Selectable.Transition.None;
            }
            dialoguePanelButton.onClick.RemoveListener(OnDialogueClicked);
            dialoguePanelButton.onClick.AddListener(OnDialogueClicked);
        }

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
            else if (dialogueText != null && dialogueText.transform.parent != null)
                dialoguePanel = dialogueText.transform.parent.gameObject;
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
            if (dialogueText == null)
                dialogueText = dialoguePanel.transform.Find("Dialogue Text")?.GetComponent<TMP_Text>();

            if (characterName == null)
                characterName = dialoguePanel.transform.Find("NameText")?.GetComponent<TMP_Text>();

            if (continueButton == null)
                continueButton = dialoguePanel.transform.Find("ContinueButton")?.GetComponent<Button>();
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
    /// Step 2: Susan Dialogue conversation
    /// </summary>
    public void StartDialogue()
    {
        currentState = FlowState.Dialogue;

        if (hintPanel != null) hintPanel.SetActive(false);
        if (historicalPanel != null) historicalPanel.SetActive(false);

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        if (characterName != null)
        {
            characterName.text = "Susan";
        }

        currentLine = 0;
        DisplayCurrentLine();
    }

    /// <summary>
    /// Unified click handler: if typing, fast-forwards to show entire sentence; if done, moves to next sentence.
    /// </summary>
    public void OnDialogueClicked()
    {
        if (currentState != FlowState.Dialogue) return;

        if (isTyping)
        {
            // Fast-forward / Skip typing animation
            SkipTyping();
        }
        else
        {
            // Proceed to next sentence
            NextLine();
        }
    }

    void DisplayCurrentLine()
    {
        if (dialogue == null || dialogue.Length == 0 || currentLine >= dialogue.Length) return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeSentenceRoutine(dialogue[currentLine]));
    }

    IEnumerator TypeSentenceRoutine(string sentence)
    {
        isTyping = true;
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
            char[] chars = sentence.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char letter = chars[i];
                dialogueText.text += letter;

                if (enablePunctuationPauses && (letter == ',' || letter == '.' || letter == '!' || letter == '?' || letter == '—' || letter == ':'))
                {
                    yield return new WaitForSeconds(typingSpeed * 4f);
                }
                else
                {
                    yield return new WaitForSeconds(typingSpeed);
                }
            }
        }

        FinishTyping();
    }

    void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialogueText != null && currentLine < dialogue.Length)
        {
            dialogueText.text = dialogue[currentLine];
        }

        FinishTyping();
    }

    void FinishTyping()
    {
        isTyping = false;
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
        }
    }

    void NextLine()
    {
        currentLine++;

        if (currentLine < dialogue.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Transition to Hint 2 (Align Position Hint) before historical picture
        ShowAlignHint();
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