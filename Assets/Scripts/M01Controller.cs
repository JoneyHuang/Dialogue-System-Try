using System.Collections;
using UnityEngine;

/// <summary>
/// M01 现场体验轻量级流程调度器 (Coordinator / Sequencer)。
/// 负责编排引导提示 (Prompt) -> Susan 对话 (Dialogue) -> 视角对齐 (Align) -> 历史照片对比 (Historical)。
/// 具体的 UI 呈现与交互均委托给独立的子组件完成。
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
        HistoricalComparison
    }

    [Header("Current Flow State")]
    public FlowState currentState = FlowState.Hidden;

    [Header("Sub-Controllers (即插即用组件)")]
    [Tooltip("通用提示/引导弹窗控制器")]
    public PromptPanelController promptPanelController;

    [Tooltip("打字机对话系统控制器")]
    public DialogueController dialogueController;

    [Tooltip("历史照片对比控制器")]
    public HistoricalComparisonController historicalComparisonController;

    [Header("Prompts Content")]
    [TextArea(2, 4)]
    public string meetingHintPrompt = "You have arrived at Cathedral Square!\nYou are now meeting Susan, who grew up here in Christchurch. Let's hear her story...";
    [TextArea(2, 4)]
    public string alignHintPrompt = "Before viewing the archival photo, please walk towards the site and position yourself to match the historical perspective of the Cathedral.";

    [Header("Next Destination (可选)")]
    [Tooltip("完成该 POI 后激活的下一个目标物体 (如 PoI 2 Pin 或玫瑰窗 HotSpot)")]
    public GameObject nextPoiTarget;

    [Header("Debug")]
    [Tooltip("Play 模式启动时自动触发流程（方便无需行走快速调试）")]
    public bool autoStartOnPlay = false;

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
            historicalComparisonController.onComparisonClosed -= ExitHistoricalComparison;
            historicalComparisonController.onComparisonClosed += ExitHistoricalComparison;
        }

        HideAllPanels();

        if (autoStartOnPlay)
        {
            StartStoryFlow();
        }
    }

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

    public void HideAllPanels()
    {
        if (promptPanelController != null) promptPanelController.Hide();
        if (dialogueController != null) dialogueController.gameObject.SetActive(false);
        if (historicalComparisonController != null) historicalComparisonController.gameObject.SetActive(false);

        currentState = FlowState.Hidden;
    }

    /// <summary>
    /// 入口：由现场导航 Pin 的 ProximityTrigger 到达时调用
    /// </summary>
    public void StartStoryFlow()
    {
        ShowMeetingHint();
    }

    /// <summary>
    /// 阶段 1：展示“初识 Susan”提示
    /// </summary>
    public void ShowMeetingHint()
    {
        currentState = FlowState.MeetingHint;

        if (promptPanelController != null)
        {
            promptPanelController.ShowPrompt(meetingHintPrompt, "Meet Susan", StartDialogue);
        }
        else
        {
            Debug.LogWarning("[M01Controller] 未找到 PromptPanelController，直接进入对话！");
            StartDialogue();
        }
    }

    /// <summary>
    /// 阶段 2：启动 Susan 对话
    /// </summary>
    public void StartDialogue()
    {
        currentState = FlowState.Dialogue;

        if (promptPanelController != null) promptPanelController.Hide();

        if (dialogueController != null)
        {
            dialogueController.OnDialogueFinished -= ShowAlignHint;
            dialogueController.OnDialogueFinished += ShowAlignHint;
            dialogueController.StartDialogue();
        }
        else
        {
            Debug.LogError("[M01Controller] 未找到 DialogueController，直接进入站位引导！");
            ShowAlignHint();
        }
    }

    /// <summary>
    /// 阶段 3：对话结束，展示“视角与站位对齐”引导提示
    /// </summary>
    public void ShowAlignHint()
    {
        currentState = FlowState.AlignHint;

        if (dialogueController != null) dialogueController.gameObject.SetActive(false);

        if (promptPanelController != null)
        {
            promptPanelController.ShowPrompt(alignHintPrompt, "I'm in Position", ShowHistoricalPanel);
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
    /// 阶段 5：退出历史对比，完成 M01，交接下一个 POI
    /// </summary>
    public void ExitHistoricalComparison()
    {
        if (historicalComparisonController != null)
        {
            historicalComparisonController.CloseComparison();
        }

        currentState = FlowState.Hidden;

        // 激活下一个 POI 目标
        if (nextPoiTarget != null)
        {
            nextPoiTarget.SetActive(true);
            Debug.Log($"[M01Controller] M01 完成！已激活下一目的地：{nextPoiTarget.name}");
        }
    }
}
