using System.Collections;
using UnityEngine;

/// <summary>
/// M01 现场体验流程调度器 (Coordinator / Sequencer)。
/// 负责编排：到达提示 → Susan 对话 → 视角对齐 → 历史照片对比 → 反思 → 进入 M02。
/// 叙事内容全部来自 PoIStoryData ScriptableObject，代码与文案完全分离。
/// </summary>
public class M01Controller : MonoBehaviour
{
    public static M01Controller Instance { get; private set; }

    public enum FlowState
    {
        Hidden,
        MeetingHint,
        Dialogue,
        AlignHint,
        HistoricalComparison,
        Reflection
    }

    [Header("Current Flow State")]
    public FlowState currentState = FlowState.Hidden;

    [Header("Story Data (ScriptableObject)")]
    [Tooltip("将 Assets/StoryData/PoI_01_Susan.asset 拖入此栏")]
    public PoIStoryData storyData;

    [Header("Sub-Controllers (即插即用组件)")]
    [Tooltip("通用提示/引导弹窗控制器")]
    public PromptPanelController promptPanelController;

    [Tooltip("Typewriter 对话系统控制器")]
    public DialogueController dialogueController;

    [Tooltip("历史照片对比控制器")]
    public HistoricalComparisonController historicalComparisonController;

    [Header("Next Destination (optional)")]
    [Tooltip("完成该 POI 后激活的下一个目标物体 (如 PoI 2 Pin 或玫瑰窗 HotSpot)")]
    public GameObject nextPoiTarget;

    [Header("Debug")]
    [Tooltip("Play 模式启动时自动触发流程（方便无需行走快速调试）")]
    public bool autoStartOnPlay = false;

    // ─── Fallback 文案（当 storyData 未赋值时使用）────────────────────────────
    private const string kFallbackArrivalPrompt =
        "You have arrived at Cathedral Square!\nYou are now meeting Susan, who grew up here in Christchurch. Let\u2019s hear her story...";
    private const string kFallbackArrivalBtn = "Meet Susan";
    private const string kFallbackAlignPrompt =
        "Before viewing the archival photo, please walk towards the site and position yourself to match the historical perspective of the Cathedral.";
    private const string kFallbackAlignBtn = "I\u2019m in Position";
    private const string kFallbackReflectionPrompt =
        "Susan\u2019s memory of the spire lives on in this space. Standing here, what do you notice about what has changed \u2014 and what remains?";
    private const string kFallbackReflectionBtn = "Continue to M2";

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

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
        // 绑定历史对比关闭事件
        if (historicalComparisonController != null)
        {
            historicalComparisonController.onComparisonClosed -= OnHistoricalComparisonClosed;
            historicalComparisonController.onComparisonClosed += OnHistoricalComparisonClosed;
        }

        HideAllPanels();

        if (autoStartOnPlay)
        {
            StartStoryFlow();
        }
    }

    // ─── Auto-wiring ──────────────────────────────────────────────────────────

    /// <summary>
    /// 自动在场景 Canvas 中寻找并装配子模块控制器，杜绝 Inspector 漏拖导致的空指针
    /// </summary>
    public void AutoWireComponents()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform canvasTr = canvas.transform;

        if (promptPanelController == null)
        {
            Transform t = canvasTr.Find("HintPanel");
            if (t != null)
                promptPanelController = t.GetComponent<PromptPanelController>();
        }

        if (dialogueController == null)
        {
            Transform t = canvasTr.Find("DialoguePanel");
            if (t != null)
                dialogueController = t.GetComponent<DialogueController>();
        }

        if (historicalComparisonController == null)
        {
            Transform t = canvasTr.Find("HistoricalPanel");
            if (t != null)
                historicalComparisonController = t.GetComponent<HistoricalComparisonController>();
        }
    }

    // ─── Panel Utility ────────────────────────────────────────────────────────

    public void HideAllPanels()
    {
        if (promptPanelController != null) promptPanelController.Hide();
        if (dialogueController != null) dialogueController.gameObject.SetActive(false);
        if (historicalComparisonController != null) historicalComparisonController.gameObject.SetActive(false);

        currentState = FlowState.Hidden;
    }

    // ─── Flow Steps ───────────────────────────────────────────────────────────

    /// <summary>
    /// 入口：由现场导航 Pin 的 ProximityTrigger 到达时调用
    /// </summary>
    public void StartStoryFlow()
    {
        ShowMeetingHint();
    }

    /// <summary>
    /// 阶段 1：展示"初识 Susan"到达提示
    /// </summary>
    public void ShowMeetingHint()
    {
        currentState = FlowState.MeetingHint;

        string prompt = storyData != null ? storyData.arrivalPrompt : kFallbackArrivalPrompt;
        string btnText = storyData != null ? storyData.arrivalButtonText : kFallbackArrivalBtn;

        if (promptPanelController != null)
        {
            promptPanelController.ShowPrompt(prompt, btnText, StartDialogue);
        }
        else
        {
            Debug.LogWarning("[M01Controller] 未找到 PromptPanelController，直接进入对话！");
            StartDialogue();
        }
    }

    /// <summary>
    /// 阶段 2：启动 Susan 对话（台词从 ScriptableObject 取得）
    /// </summary>
    public void StartDialogue()
    {
        currentState = FlowState.Dialogue;

        if (promptPanelController != null) promptPanelController.Hide();

        string[] lines = (storyData != null && storyData.dialogueLines != null && storyData.dialogueLines.Length > 0)
            ? storyData.dialogueLines
            : null;
        string speaker = storyData != null ? storyData.speakerName : null;

        if (dialogueController != null)
        {
            dialogueController.OnDialogueFinished -= ShowAlignHint;
            dialogueController.OnDialogueFinished += ShowAlignHint;
            dialogueController.StartDialogue(lines, speaker);
        }
        else
        {
            Debug.LogError("[M01Controller] 未找到 DialogueController，直接进入站位引导！");
            ShowAlignHint();
        }
    }

    /// <summary>
    /// 阶段 3：对话结束，展示"视角与站位对齐"引导提示
    /// </summary>
    public void ShowAlignHint()
    {
        currentState = FlowState.AlignHint;

        if (dialogueController != null) dialogueController.gameObject.SetActive(false);

        string prompt = storyData != null ? storyData.alignmentPrompt : kFallbackAlignPrompt;
        string btnText = storyData != null ? storyData.alignmentButtonText : kFallbackAlignBtn;

        if (promptPanelController != null)
        {
            promptPanelController.ShowPrompt(prompt, btnText, ShowHistoricalPanel);
        }
        else
        {
            ShowHistoricalPanel();
        }
    }

    /// <summary>
    /// 阶段 4：站位完成，打开历史照片对比
    /// </summary>
    public void ShowHistoricalPanel()
    {
        currentState = FlowState.HistoricalComparison;

        if (promptPanelController != null) promptPanelController.Hide();

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

    /// <summary>
    /// 历史对比面板关闭时的回调（由 onComparisonClosed 事件触发）
    /// </summary>
    private void OnHistoricalComparisonClosed()
    {
        if (historicalComparisonController != null)
        {
            historicalComparisonController.CloseComparison();
        }

        ShowReflection();
    }

    /// <summary>
    /// 阶段 5：AR 交互结束后展示反思提示（新增）
    /// </summary>
    public void ShowReflection()
    {
        currentState = FlowState.Reflection;

        string prompt = storyData != null ? storyData.reflectionPrompt : kFallbackReflectionPrompt;
        string btnText = storyData != null ? storyData.reflectionButtonText : kFallbackReflectionBtn;

        if (promptPanelController != null)
        {
            promptPanelController.ShowPrompt(prompt, btnText, CompleteM01);
        }
        else
        {
            CompleteM01();
        }
    }

    /// <summary>
    /// 阶段 6：M01 完成，激活下一个 POI 目标
    /// </summary>
    public void CompleteM01()
    {
        currentState = FlowState.Hidden;

        if (promptPanelController != null) promptPanelController.Hide();

        if (nextPoiTarget != null)
        {
            nextPoiTarget.SetActive(true);
            Debug.Log($"[M01Controller] M01 完成！已激活下一目的地：{nextPoiTarget.name}");
        }
        else
        {
            Debug.Log("[M01Controller] M01 完成！未设置 nextPoiTarget。");
        }
    }

    // ─── Legacy / Compat ──────────────────────────────────────────────────────

    /// <summary>
    /// 保留方法名兼容旧事件绑定（内部改为调用 OnHistoricalComparisonClosed）
    /// </summary>
    public void ExitHistoricalComparison() => OnHistoricalComparisonClosed();
}
