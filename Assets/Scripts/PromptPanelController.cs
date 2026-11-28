using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 通用提示/引导弹窗控制器 (Prompt / Hint Panel)。
/// 支持动态修改提示文本、按钮文案，并通过 Action 回调通知调用方，解耦任何特定业务逻辑。
/// </summary>
public class PromptPanelController : MonoBehaviour
{
    [Tooltip("显示提示说明文字的组件")]
    public TMP_Text promptText;

    [Tooltip("locationPhotoImage")]
    public Image locationPhotoImage;

    [Tooltip("TextBreathing")]
    public TextBreathing borderBreathing;

    [Tooltip("扫描描边（仅在寻找地点时开启，其他提示关闭）")]
    public Outline panelOutline;

    [Tooltip("继续/确认按钮")]
    public Button continueButton;

    [Tooltip("确认按钮上的文字（可选）")]
    public TMP_Text buttonLabel;

    private Action onConfirmedCallback;

    private void Awake()
    {
        AutoWireComponents();
        WireListeners();
    }

    /// <summary>
    /// 自动在自身或子节点中寻找 UI 组件，免去手动连线
    /// </summary>
    public void AutoWireComponents()
    {
        if (promptText == null)
            promptText = transform.Find("HintText")?.GetComponent<TMP_Text>() ?? GetComponentInChildren<TMP_Text>();

        if (continueButton == null)
        {
            Transform btnTr = transform.Find("HintContinueButton") ?? transform.Find("Button");
            continueButton = btnTr != null ? btnTr.GetComponent<Button>() : GetComponentInChildren<Button>();
        }

        if (continueButton != null && buttonLabel == null)
        {
            buttonLabel = continueButton.GetComponentInChildren<TMP_Text>();
        }

        if (locationPhotoImage == null)
        {
            locationPhotoImage = transform.Find("LocationImage")?.GetComponent<Image>()
                               ?? transform.Find("Photo")?.GetComponent<Image>();
        }

        if (borderBreathing == null)
        {
            borderBreathing = GetComponentInChildren<TextBreathing>(true);
        }

        if (panelOutline == null)
        {
            panelOutline = GetComponent<Outline>() ?? GetComponentInChildren<Outline>(true);
        }
    }

    private void WireListeners()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnButtonClicked);
            continueButton.onClick.AddListener(OnButtonClicked);
        }
    }

    /// <summary>
    /// Show Prompt for location
    /// </summary>
    /// <param name="message">提示内容</param>
    /// <param name="btnText">按钮上显示的文案（如 "Start Scanning"）</param>
    /// <param name="onConfirmed">点击按钮后的回调</param>
    /// <param name="photo">现场实景参照图（可选）</param>
    /// <param name="enableBreathing">是否开启扫描呼吸边框效果（仅在寻找地点时开启）</param>
    /// <param name="buttonDelay">确认按钮延迟显示的秒数（防连点、让用户先阅读提示）</param>
    public void ShowPrompt(string message, string btnText = "Continue", Action onConfirmed = null, Sprite photo = null, bool enableBreathing = false, float buttonDelay = 0f)
    {
        // 必须先激活自身，否则无法在其上启动 StartCoroutine
        gameObject.SetActive(true);

        if (promptText != null)
            promptText.text = message;

        if (buttonLabel != null)
            buttonLabel.text = btnText;

        if (continueButton != null)
        {
            if (buttonDelay > 0f)
            {
                continueButton.gameObject.SetActive(false);
                StartCoroutine(ShowButtonAfterDelay(buttonDelay));
            }
            else
            {
                continueButton.gameObject.SetActive(true);
            }
        }

        if (locationPhotoImage != null)
        {
            if (photo != null)
            {
                locationPhotoImage.sprite = photo;
                locationPhotoImage.gameObject.SetActive(true);
            }
            else
            {
                locationPhotoImage.gameObject.SetActive(false);
            }
        }

        // 控制呼吸动效
        if (borderBreathing != null)
        {
            borderBreathing.SetBreathing(enableBreathing);
            if (!enableBreathing)
            {
                // 关闭呼吸时恢复不透明，避免半透明残留
                var graphic = borderBreathing.GetComponent<Graphic>();
                if (graphic != null)
                {
                    Color c = graphic.color;
                    c.a = 1f;
                    graphic.color = c;
                }
            }
        }

        // 控制 Outline 描边：仅在寻找地点（enableBreathing = true）时开启，其他提示默认禁用
        if (panelOutline != null)
        {
            panelOutline.enabled = enableBreathing;
        }

        onConfirmedCallback = onConfirmed;
    }

    private System.Collections.IEnumerator ShowButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏弹窗
    /// </summary>
    public void Hide()
    {
        StopAllCoroutines();

        if (borderBreathing != null)
        {
            borderBreathing.SetBreathing(false);
        }

        if (panelOutline != null)
        {
            panelOutline.enabled = false;
        }

        gameObject.SetActive(false);
        onConfirmedCallback = null;
    }

    private void OnButtonClicked()
    {
        gameObject.SetActive(false);
        var cb = onConfirmedCallback;
        onConfirmedCallback = null;
        cb?.Invoke();
    }

#if UNITY_EDITOR
    private void Reset()
    {
        AutoWireComponents();
    }
#endif
}

