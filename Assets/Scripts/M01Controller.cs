using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class M01Controller : MonoBehaviour
{
    [Header("Dialogue")]
    public TMP_Text dialogueText;
    public TMP_Text characterName;
    public Button continueButton;

    [Header("Historical Image (Then / Now)")]
    public GameObject historicalPanel;
    public Slider opacitySlider;
    public Image historicalImage;

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
    private GameObject dialoguePanel;

    void Start()
    {
        if (historicalPanel != null)
        {
            historicalPanel.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialoguePanel = dialogueText.transform.parent != null
                ? dialogueText.transform.parent.gameObject
                : null;
        }

        if (characterName != null)
        {
            characterName.text = "Susan";
        }

        if (dialogueText != null && dialogue.Length > 0)
        {
            dialogueText.text = dialogue[currentLine];
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(NextLine);
        }

        if (opacitySlider != null)
        {
            opacitySlider.onValueChanged.AddListener(UpdateHistoricalOpacity);
            opacitySlider.value = 0.5f;
        }


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
        else
        {
            if (dialogueText != null) dialogueText.gameObject.SetActive(false);
            if (characterName != null) characterName.gameObject.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }

        if (historicalPanel != null)
        {
            historicalPanel.SetActive(true);

            if (opacitySlider != null)
            {
                opacitySlider.value = 0.5f;
            }
        }
    }
}
