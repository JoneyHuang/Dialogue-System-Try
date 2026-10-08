using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Golden rose-window reticle: outer ring, inner circle, eight spokes, radial progress.
/// </summary>
public class RoseReticleUI : MonoBehaviour
{
    Image progressRing;

    public static RoseReticleUI Create(RectTransform parent)
    {
        var go = new GameObject("RoseReticle", typeof(RectTransform), typeof(RoseReticleUI));
        var view = go.GetComponent<RoseReticleUI>();
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.52f);
        rt.anchorMax = new Vector2(0.5f, 0.52f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(420, 420);
        rt.anchoredPosition = Vector2.zero;

        Color gold = AlignStageView.Gold;
        Sprite ring = CreateRingSprite(256, 0.88f, 6);
        Sprite spoke = CreateSpokeSprite(8, 128);

        CreateImage(rt, "Track", ring, gold, 1f);
        view.progressRing = CreateImage(rt, "Progress", ring, gold, 1f);
        view.progressRing.type = Image.Type.Filled;
        view.progressRing.fillMethod = Image.FillMethod.Radial360;
        view.progressRing.fillOrigin = (int)Image.Origin360.Top;
        view.progressRing.fillClockwise = true;
        view.progressRing.fillAmount = 0f;

        var hub = CreateImage(rt, "Hub", ring, gold, 0.39f);
        hub.rectTransform.sizeDelta = new Vector2(164, 164);

        for (int i = 0; i < 8; i++)
        {
            var spokeImage = CreateImage(rt, "Spoke" + i, spoke, gold, 1f);
            var spokeRt = spokeImage.rectTransform;
            spokeRt.sizeDelta = new Vector2(6, 196);
            spokeRt.pivot = new Vector2(0.5f, 0f);
            spokeRt.anchoredPosition = Vector2.zero;
            spokeRt.localEulerAngles = new Vector3(0f, 0f, i * 45f);
        }

        return view;
    }

    public void SetProgress(float value)
    {
        if (progressRing != null)
        {
            progressRing.fillAmount = Mathf.Clamp01(value);
        }
    }

    static Image CreateImage(RectTransform parent, string name, Sprite sprite, Color color, float scale)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = parent.sizeDelta * scale;
        rt.anchoredPosition = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    static Sprite CreateRingSprite(int size, float innerRatio, int thickness)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float cx = (size - 1) * 0.5f;
        float outer = size * 0.5f - 2f;
        float inner = outer * innerRatio;
        float mid = (outer + inner) * 0.5f;
        float half = thickness * 0.5f + 0.6f;

        var clear = new Color(0, 0, 0, 0);
        var solid = Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float r = Mathf.Sqrt((x - cx) * (x - cx) + (y - cx) * (y - cx));
                float d = Mathf.Abs(r - mid);
                tex.SetPixel(x, y, d <= half ? solid : clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite CreateSpokeSprite(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, y, Color.white);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), 100f);
    }
}
