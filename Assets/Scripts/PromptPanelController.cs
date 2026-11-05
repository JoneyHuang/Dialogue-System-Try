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
    [Header("UI References")]
    [Tooltip("显示提示说明文字的组件")]
    public TMP_Text promptText;

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
    /// 打开提示弹窗并显示指定的提示文案与按钮文本
    /// </summary>
    /// <param name="message">提示内容</param>
    /// <param name="btnText">按钮上显示的文案（如 "Meet Susan" 或 "I'm in Position"）</param>
    /// <param name="onConfirmed">点击按钮后的回调</param>
    public void ShowPrompt(string message, string btnText = "Continue", Action onConfirmed = null)
    {
        if (promptText != null)
            promptText.text = message;

        if (buttonLabel != null)
            buttonLabel.text = btnText;

        onConfirmedCallback = onConfirmed;

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏弹窗
    /// </summary>
    public void Hide()
    {
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

