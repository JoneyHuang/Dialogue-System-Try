using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// M02 现场体验流程调度器 (Coordinator / Sequencer)。
/// 负责编排：到达提示 (Wen) → 对话 → 传送门交互引导 → 激活 Portal 观察全景 → 反思卡 → 汇报给 ExperimentHub 并返回。
/// 叙事内容全部来自 PoIStoryData ScriptableObject。
/// </summary>
public class M02Controller : MonoBehaviour
{
    public static M02Controller Instance { get; private set; }

    public enum FlowState
    {
        Hidden,
        MeetingHint,
        Dialogue,
        PortalInstruction,
        PortalViewing,
        Reflection
    }

    [Header("Current Flow State")]
    public FlowState currentState = FlowState.Hidden;

    [Header("Story Data (ScriptableObject)")]
    [Tooltip("将 Assets/StoryData/PoI_02_Wen.asset 拖入此栏")]
    public PoIStoryData storyData;

    [Header("Sub-Controllers (复用通用 UI 组件)")]
    [Tooltip("通用提示/引导弹窗控制器 (HintPanel)")]
    public PromptPanelController promptPanelController;

    [Tooltip("Typewriter 对话系统控制器 (DialoguePanel)")]
    public DialogueController dialogueController;

    [Header("AR Portal Content")]
    [Tooltip("现场的 Gothic Window 传送门根物体 (包含窗框与全景球)")]
    public GameObject portalObject;

    [Tooltip("观察全景时屏幕上的'完成观察/继续'按钮 (可选，若不指定会自动使用 PromptPanel)")]
    public Button finishViewingButton;

    [Header("Debug")]
    [Tooltip("Play 模式启动时自动触发流程（正式测试请保持 false）")]
    public bool autoStartOnPlay = false;

    // ─── Fallback 文案 ────────────────────────────────────────────────────────
    private const string kFallbackArrivalPrompt =
        "You have arrived at the second site.\nMeet Wen, an architect and heritage researcher. Let's hear his perspective...";
    private const string kFallbackArrivalBtn = "Meet Wen";
    private const string kFallbackPortalPrompt =
        "Step closer and look through the portal to explore the Cathedral's historical interior.";
    private const string kFallbackPortalBtn = "Enter Portal";
    private const string kFallbackReflectionPrompt =
        "Seeing the interior preserved in time, what emotions or details stand out to you the most?";
    private const string kFallbackReflectionBtn = "Continue";

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
        HideAllPanels();

        if (finishViewingButton != null)
        {
            finishViewingButton.onClick.RemoveListener(OnFinishViewingClicked);
            finishViewingButton.onClick.AddListener(OnFinishViewingClicked);
            finishViewingButton.gameObject.SetActive(false);
        }

        if (autoStartOnPlay)
        {
            StartStoryFlow();
        }
    }

    /// <summary>
    /// 自动在场景 Canvas 中寻找并装配子模块控制器，杜绝 Inspector 漏拖
    /// </summary>
    public void AutoWireComponents()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Transform canvasTr = canvas.transform;

        if (promptPanelController == null)
        {
            Transform t = canvasTr.Find("HintPanel") ?? canvasTr.Find("PromptPanel");
            if (t != null)
                promptPanelController = t.GetComponent<PromptPanelController>();
        }

        if (dialogueController == null)
        {
            Transform t = canvasTr.Find("DialoguePanel");
            if (t != null)
                dialogueController = t.GetComponent<DialogueController>();
        }
    }

    public void HideAllPanels()
    {
        if (promptPanelController != null) promptPanelController.Hide();
        if (dialogueController != null) dialogueController.gameObject.SetActive(false);
        if (portalObject != null) portalObject.SetActive(false);
        if (finishViewingButton != null) finishViewingButton.gameObject.SetActive(false);

        currentState = FlowState.Hidden;
    }

    // ─── Flow Steps ───────────────────────────────────────────────────────────

    /// <summary>
    /// 入口：由现场 PoI2 Pin 的 ProximityTrigger 到达时调用
    /// </summary>
    public void StartStoryFlow()
    {
        ShowMeetingHint();
    }

    /// <summary>
    /// 阶段 1：展示初识 Wen 到达提示
    /// </summary>
    public void ShowMeetingHint()
    {
        currentState = FlowState.MeetingHint;

        string prompt = (storyData != null && !string.IsNullOrEmpty(storyData.arrivalPrompt))
            ? storyData.arrivalPrompt
            : kFallbackArrivalPrompt;
        string btnText = (storyData != null && !string.IsNullOrEmpty(storyData.arrivalButtonText))
            ? storyData.arrivalButtonText
            : kFallbackArrivalBtn;

        if (promptPanelController != null)
        {
            promptPanelController.ShowPrompt(prompt, btnText, StartDialogue);
        }
        else
        {
            Debug.LogWarning("[M02Controller] 未找到 PromptPanelController，直接进入对话！");
            StartDialogue();
        }
    }

    /// <summary>
    /// 阶段 2：启动 Wen 对话
    /// </summary>
    public void StartDialogue()
    {
        currentState = FlowState.Dialogue;

        if (promptPanelController != null) promptPanelController.Hide();

        string[] lines = (storyData != null && storyData.dialogueLines != null && storyData.dialogueLines.Length > 0)
            ? storyData.dialogueLines
            : null;
        string speaker = storyData != null ? storyData.speakerName : "Wen";

        if (dialogueController != null)
        {
            dialogueController.OnDialogueFinished -= ShowPortalInstruction;
            dialogueController.OnDialogueFinished += ShowPortalInstruction;
            dialogueController.StartDialogue(lines, speaker);
        }
        else
        {
            Debug.LogError("[M02Controller] 未找到 DialogueController，直接进入 Portal 引导！");
            ShowPortalInstruction();
        }
    }

    /// <summary>
    /// 阶段 3：对话结束，展示交互操作引导
    /// </summary>
    public void ShowPortalInstruction()
    {
        currentState = FlowState.PortalInstruction;

        if (dialogueController != null) dialogueController.gameObject.SetActive(false);

        string prompt = (storyData != null && !string.IsNullOrEmpty(storyData.alignmentPrompt))
            ? storyData.alignmentPrompt
            : ((storyData != null && !string.IsNullOrEmpty(storyData.interactionInstruction))
                ? storyData.interactionInstruction
                : kFallbackPortalPrompt);

        string btnText = (storyData != null && !string.IsNullOrEmpty(storyData.alignmentButtonText))
            ? storyData.alignmentButtonText
            : kFallbackPortalBtn;

        if (promptPanelController != null)
        {
            promptPanelController.ShowPrompt(prompt, btnText, EnterPortalViewing);
        }
        else
        {
            EnterPortalViewing();
        }
    }

    /// <summary>
    /// 阶段 4：激活 Portal，开始全景观察
    /// </summary>
    public void EnterPortalViewing()
    {
        currentState = FlowState.PortalViewing;

        if (promptPanelController != null) promptPanelController.Hide();

        // 激活 3D Gothic Window 传送门
        if (portalObject != null)
        {
            portalObject.SetActive(true);
            Debug.Log("[M02Controller] 传送门已激活！用户可环顾查看历史全景。");
        }
        else
        {
            Debug.LogWarning("[M02Controller] portalObject 未分配！");
        }

        // 提供退出/完成观察的触发方式
        if (finishViewingButton != null)
        {
            finishViewingButton.gameObject.SetActive(true);
        }
        else
        {
            // 如果未单独配置按钮，利用 PromptPanel 作为轻量常驻提示卡
            if (promptPanelController != null)
            {
                promptPanelController.ShowPrompt("Look around to explore the interior.\nWhen you are ready, continue to reflect.", "Finish Viewing", OnFinishViewingClicked);
            }
        }
    }

    /// <summary>
    /// 结束观察传送门
    /// </summary>
    public void OnFinishViewingClicked()
    {
        if (finishViewingButton != null)
        {
            finishViewingButton.gameObject.SetActive(false);
        }

        if (portalObject != null)
        {
            portalObject.SetActive(false);
        }

        ShowReflection();
    }

    /// <summary>
    /// 阶段 5：展示反思卡
    /// </summary>
    public void ShowReflection()
    {
        currentState = FlowState.Reflection;

        string prompt = (storyData != null && !string.IsNullOrEmpty(storyData.reflectionPrompt))
            ? storyData.reflectionPrompt
            : kFallbackReflectionPrompt;
        string btnText = (storyData != null && !string.IsNullOrEmpty(storyData.reflectionButtonText))
            ? storyData.reflectionButtonText
            : kFallbackReflectionBtn;

        if (promptPanelController != null)
        {
            promptPanelController.ShowPrompt(prompt, btnText, CompleteM02);
        }
        else
        {
            CompleteM02();
        }
    }

    /// <summary>
    /// 阶段 6：M02 完成，通知 Hub 并恢复状态
    /// </summary>
    public void CompleteM02()
    {
        currentState = FlowState.Hidden;

        if (promptPanelController != null) promptPanelController.Hide();

        if (portalObject != null) portalObject.SetActive(false);

        // 通知 Hub：M2 完成！
        if (ExperimentHubController.Instance != null)
        {
            ExperimentHubController.Instance.OnPoICompleted("M2");
            Debug.Log("[M02Controller] M02 流程结束，已向 ExperimentHub 汇报完成！");
        }
    }
}

