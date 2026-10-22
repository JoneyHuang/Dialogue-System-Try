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

    [Header("Historical Image (Then / Now)")]
    public Slider opacitySlider;
    public Image historicalImage;

    [Header("Debug")]
    [Tooltip("If true, starts flow automatically on scene start (for testing without walking to Pin).")]
    public bool autoStartOnPlay = false;

    private string[] dialogue =
    {
        "Standing here in Cathedral Square today, it's quiet... It's different from what I remembered.",
        "The tower and spire carry my special memories.",
        "When I was at school, I got lost around Cathedral Square during the trip. I remember my teacher, Mr Watson, told us:",
        " 'If you are lost, look for the Cathedral spire—it's the tallest building in the town!' ",
        " 'If you could see the spire, you knew where you were.'",
        "Walk closer to the interpretation panel. Can you spot a picture of the rose window etched into the facade?"
    };

    private int currentLine = 0;

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
            continueButton.onClick.RemoveListener(NextLine);
            continueButton.onClick.AddListener(NextLine);
        }

        if (hintContinueButton != null)
        {
            hintContinueButton.onClick.RemoveListener(OnHintContinueClicked);
            hintContinueButton.onClick.AddListener(OnHintContinueClicked);
        }

        if (opacitySlider != null)
        {
            opacitySlider.onValueChanged.RemoveListener(UpdateHistoricalOpacity);
            opacitySlider.onValueChanged.AddListener(UpdateHistoricalOpacity);
            opacitySlider.value = 0.5f;
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
            if (opacitySlider == null)
                opacitySlider = historicalPanel.GetComponentInChildren<Slider>(true);

            if (historicalImage == null)
            {
                Transform imgTr = historicalPanel.transform.Find("HistoricalImage");
                if (imgTr != null) historicalImage = imgTr.GetComponent<Image>();
            }
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
        if (dialogueText != null && dialogue.Length > 0)
        {
            dialogueText.text = dialogue[currentLine];
        }
    }

    void NextLine()
    {
        currentLine++;

        if (currentLine < dialogue.Length)
        {
            if (dialogueText != null)
            {
                dialogueText.text = dialogue[currentLine];
            }
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
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

        if (historicalPanel != null)
        {
            historicalPanel.SetActive(true);

            if (opacitySlider != null)
            {
                opacitySlider.value = 0.5f;
            }
        }

        // NOTE: AlignStageView (玫瑰窗对位环) 暂时不用，如需启用可在此处调用:
        // if (FindFirstObjectByType<AlignStageView>() != null)
        // {
        //     FindFirstObjectByType<AlignStageView>().ShowAligning();
        // }
    }

    void UpdateHistoricalOpacity(float value)
    {
        if (historicalImage != null)
        {
            Color c = historicalImage.color;
            c.a = value;
            historicalImage.color = c;
        }
    }
}

