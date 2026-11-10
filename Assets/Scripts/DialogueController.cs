using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 专用的通用对话系统组件。
/// 支持全屏点击跳过/推进、逐字打字机动画、标点停顿以及防误触冷却。
/// </summary>
public class DialogueController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("显示说话角色名字的文本")]
    public TMP_Text characterNameText;

    [Tooltip("显示台词正文的文本")]
    public TMP_Text dialogueBodyText;

    [Tooltip("下一句箭头或确认按钮（可选）")]
    public Button continueButton;

    [Header("UX & Click Settings")]
    [Tooltip("是否允许点击屏幕任意区域推进对话")]
    public bool enableFullScreenClick = true;

    [Tooltip("打字时跳过动画后的微小防误触冷却（秒），防止快速连点吃掉下一句")]
    public float debounceAfterSkip = 0.15f;

    [Header("Typewriter Settings")]
    [Tooltip("每个字的打字速度（秒）")]
    public float typingSpeed = 0.035f;

    [Tooltip("标点符号额外停顿，模拟说话自然语气")]
    public bool enablePunctuationPauses = true;

    [Header("Dialogue Content (Fallback — 通常由 PoIStoryData 覆盖)")]
    [Tooltip("当调用方未传入 lines 参数时使用的后备台词。生产环境建议留空，内容改由 PoIStoryData 管理。")]
    public string defaultSpeaker = "Storyteller";
    [TextArea(2, 4)]
    public string[] defaultLines = { };

    /// <summary>
    /// 对话全部播放完毕后触发的事件
    /// </summary>
    public event Action OnDialogueFinished;

    private int currentLine = 0;
    private Coroutine typingCoroutine = null;
    private bool isTyping = false;
    private float lastSkipTime = 0f;
    private string[] activeLines;
    private GameObject fullScreenTapArea;

    private void Awake()
    {
        AutoWireComponents();
        SetupFullScreenClickArea();
        WireListeners();
    }

    /// <summary>
    /// 自动查找并填充 Inspector 字段，免去手动连线
    /// </summary>
    public void AutoWireComponents()
    {
        if (dialogueBodyText == null)
            dialogueBodyText = transform.Find("Dialogue Text")?.GetComponent<TMP_Text>() ?? GetComponentInChildren<TMP_Text>();

        if (characterNameText == null)
            characterNameText = transform.Find("NameText")?.GetComponent<TMP_Text>();

        if (continueButton == null)
            continueButton = transform.Find("ContinueButton")?.GetComponent<Button>() ?? GetComponentInChildren<Button>();
    }

    private void SetupFullScreenClickArea()
    {
        if (!enableFullScreenClick) return;

        // 如果已经创建过，直接复用
        Transform existing = transform.Find("FullScreenTapArea");
        if (existing != null)
        {
            fullScreenTapArea = existing.gameObject;
            return;
        }

        // 仅在真实 Play 模式运行时创建，避免在 Editor 视图中影响 Hierarchy
        if (!Application.isPlaying) return;

        // 在自身下挂一个透明背景接收触控
        fullScreenTapArea = new GameObject("FullScreenTapArea", typeof(RectTransform), typeof(Image), typeof(Button));
        fullScreenTapArea.transform.SetParent(transform, false);
        fullScreenTapArea.transform.SetAsFirstSibling(); // 置于最底层，不挡文字

        var rt = fullScreenTapArea.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = fullScreenTapArea.GetComponent<Image>();
        img.color = Color.clear; // 完全透明
        img.raycastTarget = true;

        var btn = fullScreenTapArea.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(OnUserClickAdvance);
    }

    private void WireListeners()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnUserClickAdvance);
            continueButton.onClick.AddListener(OnUserClickAdvance);
        }

        // 面板自身点击响应
        var panelBtn = GetComponent<Button>();
        if (panelBtn == null)
        {
            panelBtn = gameObject.AddComponent<Button>();
            panelBtn.transition = Selectable.Transition.None;
        }
        panelBtn.onClick.RemoveListener(OnUserClickAdvance);
        panelBtn.onClick.AddListener(OnUserClickAdvance);
    }

    /// <summary>
    /// 启动对话流程
    /// </summary>
    public void StartDialogue(string[] lines = null, string speaker = null)
    {
        activeLines = (lines != null && lines.Length > 0) ? lines : defaultLines;
        string activeSpeaker = !string.IsNullOrEmpty(speaker) ? speaker : defaultSpeaker;

        if (characterNameText != null)
            characterNameText.text = activeSpeaker;

        currentLine = 0;
        gameObject.SetActive(true);
        DisplayCurrentLine();
    }

    /// <summary>
    /// 统一点击交互逻辑：打字中则快进，打字结束则翻页
    /// </summary>
    public void OnUserClickAdvance()
    {
        if (activeLines == null || activeLines.Length == 0) return;

        // 防误触保护：跳过动画后的极短时间内忽略连点
        if (Time.unscaledTime - lastSkipTime < debounceAfterSkip) return;

        if (isTyping)
        {
            // 状态 1：正在打字 -> 立即显示整句话
            SkipTyping();
        }
        else
        {
            // 状态 2：打字已完成 -> 进入下一句
            currentLine++;
            if (currentLine < activeLines.Length)
            {
                DisplayCurrentLine();
            }
            else
            {
                FinishDialogue();
            }
        }
    }

    private void DisplayCurrentLine()
    {
        if (currentLine >= activeLines.Length) return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeSentenceRoutine(activeLines[currentLine]));
    }

    private IEnumerator TypeSentenceRoutine(string sentence)
    {
        isTyping = true;
        if (continueButton != null) continueButton.gameObject.SetActive(false);

        if (dialogueBodyText != null)
        {
            dialogueBodyText.text = "";
            char[] chars = sentence.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                dialogueBodyText.text += c;

                if (enablePunctuationPauses && (c == ',' || c == '.' || c == '!' || c == '?' || c == '—' || c == ':'))
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

    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialogueBodyText != null && currentLine < activeLines.Length)
        {
            dialogueBodyText.text = activeLines[currentLine];
        }

        lastSkipTime = Time.unscaledTime;
        FinishTyping();
    }

    private void FinishTyping()
    {
        isTyping = false;
        if (continueButton != null) continueButton.gameObject.SetActive(true);
    }

    private void FinishDialogue()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        isTyping = false;

        gameObject.SetActive(false);
        OnDialogueFinished?.Invoke();
    }

#if UNITY_EDITOR
    private void Reset()
    {
        AutoWireComponents();
    }
#endif
}

