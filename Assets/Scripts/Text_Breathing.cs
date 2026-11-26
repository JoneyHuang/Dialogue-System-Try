using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Graphic))]
public class TextBreathing : MonoBehaviour
{
    [Header("Intensity")]
    [Range(0f, 1f)] public float minAlpha = 0.25f;
    [Range(0f, 1f)] public float maxAlpha = 1f;
    public float period = 1.6f;
    public bool onlyWhenInteractable = true;

    private Graphic _graphic;
    private Button   _button;
    private float    _timer;

    void Awake()
    {
        _graphic = GetComponent<Graphic>();
        var parentBtn = GetComponentInParent<Button>();
        if (parentBtn != null) _button = parentBtn;
    }

    void Update()
    {
        if (onlyWhenInteractable && _button != null && !_button.interactable)
        {
            SetAlpha(1f);
            return;
        }

        _timer += Time.unscaledDeltaTime;
        float phase = Mathf.Sin((_timer / period) * Mathf.PI * 2f) * 0.5f + 0.5f;
        float curAlpha = Mathf.Lerp(minAlpha, maxAlpha, phase);
        SetAlpha(curAlpha);
    }

    private void SetAlpha(float a)
    {
        Color c = _graphic.color;
        c.a = a;
        _graphic.color = c;
    }

    public void SetBreathing(bool on) => enabled = on;
}