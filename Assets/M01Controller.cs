using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class M01Controller : MonoBehaviour
{
    [Header("Dialogue")]
    public TMP_Text dialogueText;
    public TMP_Text characterName;
    public Button continueButton;

    [Header("Historical Image")]
    public GameObject historicalPanel;

    [Header("Then / Now")]
    public Slider opacitySlider;
    public Image historicalImage;

    [Header("Rose Window")]
    public GameObject roseWindowModel;

    [Header("Scan Hint")]
    public GameObject scanHintPanel;
    public TMP_Text scanHintText;
    public Button scanHintButton;
    [Tooltip("Optional. Overlay creates its own status if this is empty.")]
    public TMP_Text scanStatusText;

    [Header("Align")]
    [Tooltip("Seconds before Skip appears. Matches the web AR 8s fallback.")]
    public float skipLockDelaySeconds = 8f;
    [Tooltip("Optional. Built at runtime under the Canvas if empty.")]
    public AlignStageView alignStage;

    private string[] dialogue =
    {
        "Standing here in Cathedral Square today, it's quiet... It's different from what I remembered.",
        "Right next to the rose window and facade, the Cathedral Spire used to stand above it.",
        "This 63-meters tower and spire carry my special memories.",
        "When I was at school, I got lost around Cathedral Square during the trip. I remember my teacher, Mr Watson, told us:",
        " 'If you are lost, look for the Cathedral spire—it's the tallest building in the town!' ",
        " 'If you could see the spire, you knew where you were.'",
        "Walk closer to the interpretation panel. Can you spot a picture of the rose window etched into the facade?"
    };

    private int currentLine = 0;
    private bool waitingForRoseWindow = false;
    private Coroutine showSkipCoroutine;
    private GameObject dialoguePanel;

    void Start()
    {
        historicalPanel.SetActive(false);

        if (scanHintPanel != null)
        {
            scanHintPanel.SetActive(false);
        }
        HideSkipButton();

        if (roseWindowModel != null)
        {
            roseWindowModel.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialoguePanel = dialogueText.transform.parent != null
                ? dialogueText.transform.parent.gameObject
                : null;
        }

        characterName.text = "Susan";
        dialogueText.text = dialogue[currentLine];

        continueButton.onClick.AddListener(NextLine);

        opacitySlider.onValueChanged.AddListener(UpdateHistoricalOpacity);

        if (scanHintButton != null)
        {
            scanHintButton.onClick.AddListener(OnScanHintButtonClicked);
        }

        EnsureAlignStage();
        if (alignStage != null)
        {
            alignStage.skipButton.onClick.AddListener(OnScanHintButtonClicked);
            alignStage.gameObject.SetActive(false);
        }
    }

    void EnsureAlignStage()
    {
        if (alignStage != null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("M01Controller: no Canvas found for the align stage.");
            return;
        }

        alignStage = AlignStageView.Create(canvas);
    }

    void UpdateHistoricalOpacity(float value)
    {
        Color c = historicalImage.color;
        c.a = value;
        historicalImage.color = c;
    }

    void NextLine()
    {
        if (waitingForRoseWindow)
        {
            return;
        }

        // When reaching the end of the tower story, trigger the rose window alignment interaction
        if (currentLine == dialogue.Length - 1)
        {
            ShowRoseWindowHint();
            return;
        }

        currentLine++;

        if (currentLine < dialogue.Length)
        {
            dialogueText.text = dialogue[currentLine];
        }
        else
        {
            EndDialogue();
        }
    }

    void ShowRoseWindowHint()
    {
        waitingForRoseWindow = true;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        else
        {
            dialogueText.gameObject.SetActive(false);
            characterName.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(false);
        }

        if (scanHintPanel != null)
        {
            scanHintPanel.SetActive(false);
        }

        EnsureAlignStage();
        if (alignStage != null)
        {
            alignStage.ShowAligning();
        }

        if (scanHintText != null)
        {
            scanHintText.text = "Centre the rose window in the circle.";
        }

        if (scanStatusText != null)
        {
            scanStatusText.text = "Looking for the rose window";
            scanStatusText.gameObject.SetActive(true);
        }

        HideSkipButton();

        if (showSkipCoroutine != null)
        {
            StopCoroutine(showSkipCoroutine);
        }
        showSkipCoroutine = StartCoroutine(ShowSkipAfterDelay());
    }

    IEnumerator ShowSkipAfterDelay()
    {
        yield return new WaitForSeconds(skipLockDelaySeconds);

        if (!waitingForRoseWindow)
        {
            yield break;
        }

        if (alignStage != null)
        {
            alignStage.ShowSkip();
        }

        if (scanHintButton != null)
        {
            scanHintButton.gameObject.SetActive(true);
        }
    }

    void HideSkipButton()
    {
        if (scanHintButton != null)
        {
            scanHintButton.gameObject.SetActive(false);
        }
    }

    void OnScanHintButtonClicked()
    {
        RoseWindowScanned();
    }

    public void RoseWindowScanned()
    {
        Debug.Log("Rose Window scanned!");

        if (showSkipCoroutine != null)
        {
            StopCoroutine(showSkipCoroutine);
            showSkipCoroutine = null;
        }

        if (alignStage != null)
        {
            alignStage.Hide();
        }

        if (scanHintPanel != null)
        {
            scanHintPanel.SetActive(false);
        }
        HideSkipButton();

        if (scanStatusText != null)
        {
            scanStatusText.gameObject.SetActive(false);
        }

        if (roseWindowModel != null)
        {
            roseWindowModel.SetActive(true);
        }

        waitingForRoseWindow = false;

        // Transition directly to the historical panel after the alignment interaction
        EndDialogue();
    }

    void EndDialogue()
    {
        if (historicalPanel != null)
        {
            historicalPanel.SetActive(true);
        }
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        else
        {
            if (dialogueText != null) dialogueText.gameObject.SetActive(false);
            if (characterName != null) characterName.gameObject.SetActive(false);
        }
    }
}
