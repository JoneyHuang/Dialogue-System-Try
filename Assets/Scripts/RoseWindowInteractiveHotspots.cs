using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates tappable detail hotspots and an information card over the AR rose window.</summary>
public class RoseWindowInteractiveHotspots : MonoBehaviour
{
    [System.Serializable]
    private struct Detail
    {
        public string label;
        [TextArea(3, 8)] public string text;
        public Vector2 position;
    }

    [SerializeField] private Detail[] details =
    {
        new Detail { label = "Design & Symbolism", text = "The rose window was designed by Benjamin Mountfort. Its central Lamb of God image is a powerful Christian symbol.", position = new Vector2(-260, 80) },
        new Detail { label = "Stained Glass", text = "The window is made from 95 pieces of stained glass. Look closely at how the coloured sections form a pattern around the centre.", position = new Vector2(0, 0) },
        new Detail { label = "After the Earthquakes", text = "The earthquakes damaged the cathedral and its rose window. The window became an important part of the story of the building and its community.", position = new Vector2(260, -100) }
    };

    private RectTransform card;
    private TMP_Text heading;
    private TMP_Text copy;
    private Canvas canvas;

    private void Awake()
    {
        BuildInterface();
    }

    private void BuildInterface()
    {
        var canvasObject = new GameObject("Interactive Hotspots UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;
        var canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1200, 900);
        canvasRect.localScale = Vector3.one * 0.001f;
        // ARWorldRoseWindow points its +Z axis along the camera's view direction;
        // flip the UI so its front faces back toward the camera.
        canvasRect.localRotation = Quaternion.Euler(0f, 180f, 0f);
        canvasRect.localPosition = new Vector3(0, 0, -0.01f);

        for (int i = 0; i < details.Length; i++)
        {
            int index = i;
            var button = MakeButton(canvasRect, $"Hotspot {i + 1}", new Vector2(86, 86), details[i].position);
            button.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.16f, 0.88f);
            var label = MakeText(button.transform, "+", 48, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 8);
            button.onClick.AddListener(() => ShowDetail(index));
        }

        card = MakePanel(canvasRect, "Information panel", new Vector2(390, 540), new Vector2(355, 35));
        card.gameObject.SetActive(false);
        var title = MakeText(card, "THE ROSE WINDOW", 18, TextAlignmentOptions.TopLeft);
        title.rectTransform.anchoredPosition = new Vector2(22, -22);
        title.rectTransform.sizeDelta = new Vector2(330, 30);
        heading = MakeText(card, "Design & Symbolism", 27, TextAlignmentOptions.TopLeft);
        heading.rectTransform.anchoredPosition = new Vector2(22, -62);
        heading.rectTransform.sizeDelta = new Vector2(340, 52);
        copy = MakeText(card, "", 21, TextAlignmentOptions.TopLeft);
        copy.rectTransform.anchoredPosition = new Vector2(22, -125);
        copy.rectTransform.sizeDelta = new Vector2(340, 300);
        copy.enableWordWrapping = true;
        var dismiss = MakeButton(card, "Dismiss detail", new Vector2(340, 58), new Vector2(0, -225));
        var dismissText = MakeText(dismiss.transform, "Close detail", 20, TextAlignmentOptions.Center);
        Stretch(dismissText.rectTransform, 4);
        dismiss.onClick.AddListener(() => card.gameObject.SetActive(false));
    }

    private void ShowDetail(int index)
    {
        if (index < 0 || index >= details.Length) return;
        heading.text = details[index].label;
        copy.text = details[index].text;
        card.gameObject.SetActive(true);
    }

    private static Button MakeButton(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return go.GetComponent<Button>();
    }

    private static RectTransform MakePanel(Transform parent, string name, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        go.GetComponent<Image>().color = new Color(0.055f, 0.075f, 0.09f, 0.96f);
        return rect;
    }

    private static TMP_Text MakeText(Transform parent, string value, float size, TextAlignmentOptions alignment)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
