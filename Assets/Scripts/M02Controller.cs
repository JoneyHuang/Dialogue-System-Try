using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// M02 现场体验流程调度器 (Coordinator / Sequencer)。
/// 负责编排：到达提示 (Wen) → 对话 → 传送门站位引导 → 动态落点激活 Portal 
/// → 5秒沉浸期 → 延迟显现操作栏 (拍照/完成) → 反思卡 → 汇报给 ExperimentHub 并返回。
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

    [Header("AR Portal Content & Placement")]
    [Tooltip("现场的 Gothic Window 传送门根物体 (包含窗框与全景球)")]
    public GameObject portalObject;

    [Tooltip("是否在用户正前方动态生成 Portal (破除固定位置被人流/围栏阻挡的限制)")]
    public bool spawnInFrontOfUser = true;

    [Tooltip("动态生成时与相机的水平距离（米）")]
    public float spawnDistance = 2.2f;

    [Header("Portal Viewing HUD (延迟显现操作栏)")]
    [Tooltip("Portal 观看期间的操作容器面板 (包含拍照与结束按钮)")]
    public GameObject portalHudRoot;

    [Tooltip("拍照按钮 (可选)")]
    public Button capturePhotoButton;

    [Tooltip("完成观察/跳过按钮")]
    public Button finishViewingButton;

    [Tooltip("延迟显示操作栏的等待时间 (秒)，给被试自由探索的心理沉淀期")]
    public float delayBeforeHudVisible = 5.0f;

    [Header("Debug")]
    [Tooltip("Play 模式启动时自动触发流程（正式测试请保持 false）")]
    public bool autoStartOnPlay = false;

    // ─── Fallback 文案 ────────────────────────────────────────────────────────
    private const string kFallbackArrivalPrompt =
        "You have arrived at the second site.\nMeet Wen, an architect and heritage researcher. Let's hear his perspective...";
    private const string kFallbackArrivalBtn = "Meet Wen";
    private const string kFallbackPortalPrompt =
        "Look towards an open area in front of you. Step through the portal to explore the historical interior.";
    private const string kFallbackPortalBtn = "Open Portal";
    private const string kFallbackReflectionPrompt =
        "Seeing the interior preserved in time, what emotions or details stand out to you the most?";
    private const string kFallbackReflectionBtn = "Continue";

    private Coroutine hudDelayCoroutine = null;

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

        // 绑定按钮事件
        if (finishViewingButton != null)
        {
            finishViewingButton.onClick.RemoveListener(OnFinishViewingClicked);
            finishViewingButton.onClick.AddListener(OnFinishViewingClicked);
        }

        if (capturePhotoButton != null)
        {
            capturePhotoButton.onClick.RemoveListener(OnCapturePhotoClicked);
            capturePhotoButton.onClick.AddListener(OnCapturePhotoClicked);
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
        if (hudDelayCoroutine != null)
        {
            StopCoroutine(hudDelayCoroutine);
            hudDelayCoroutine = null;
        }

        if (promptPanelController != null) promptPanelController.Hide();
        if (dialogueController != null) dialogueController.gameObject.SetActive(false);
        if (portalObject != null) portalObject.SetActive(false);
        if (portalHudRoot != null) portalHudRoot.SetActive(false);
        if (finishViewingButton != null) finishViewingButton.gameObject.SetActive(false);
        if (capturePhotoButton != null) capturePhotoButton.gameObject.SetActive(false);

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
    /// 阶段 3：对话结束，展示清晰的空间与站位操作引导
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
    /// 阶段 4：激活 Portal，进入纯净沉浸观察期 (彻底隐藏 PromptPanel，延迟呈现操作栏)
    /// </summary>
    public void EnterPortalViewing()
    {
        currentState = FlowState.PortalViewing;

        // 1. 彻底隐藏中央 PromptPanel，将视野完全还给 AR 现场
        if (promptPanelController != null)
        {
            promptPanelController.Hide();
        }

        // 2. 动态安全定位或直接激活 Portal
        if (portalObject != null)
        {
            if (spawnInFrontOfUser && Camera.main != null)
            {
                PlacePortalInFrontOfCamera();
            }

            portalObject.SetActive(true);
            Debug.Log("[M02Controller] 传送门已激活！用户可自由环顾与穿过门框。");
        }

        // 3. 初始隐藏操作栏，开启延迟沉浸计时
        if (portalHudRoot != null) portalHudRoot.SetActive(false);
        if (finishViewingButton != null) finishViewingButton.gameObject.SetActive(false);
        if (capturePhotoButton != null) capturePhotoButton.gameObject.SetActive(false);

        if (hudDelayCoroutine != null) StopCoroutine(hudDelayCoroutine);
        hudDelayCoroutine = StartCoroutine(ShowHudAfterDelayRoutine());
    }

    /// <summary>
    /// 在当前相机正前方安全平坦处放置 Portal，避免固定锚点被现场人群撞上
    /// </summary>
    private void PlacePortalInFrontOfCamera()
    {
        Transform cam = Camera.main.transform;
        Vector3 forwardXZ = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;

        if (forwardXZ.sqrMagnitude < 0.001f)
            forwardXZ = Vector3.forward;

        // 计算相机正前方 spawnDistance 处的水平位置
        Vector3 targetPos = cam.position + forwardXZ * spawnDistance;
        
        // 保持 Portal 原有的 Y 轴高度（贴地），或以相机脚下为参考
        targetPos.y = portalObject.transform.position.y;

        portalObject.transform.position = targetPos;
        portalObject.transform.rotation = Quaternion.LookRotation(forwardXZ, Vector3.up);

        Debug.Log($"[M02Controller] Portal 已动态安放在用户前方：{targetPos}");
    }

    /// <summary>
    /// 延迟显示操作栏：给被试 5 秒没有任何按钮打扰的纯粹沉浸探索期
    /// </summary>
    private IEnumerator ShowHudAfterDelayRoutine()
    {
        yield return new WaitForSeconds(delayBeforeHudVisible);

        // 沉浸期结束，轻柔唤出操作栏
        if (portalHudRoot != null)
        {
            portalHudRoot.SetActive(true);
        }
        else
        {
            // 若未配置专属容器，单独点亮按钮
            if (finishViewingButton != null) finishViewingButton.gameObject.SetActive(true);
            if (capturePhotoButton != null) capturePhotoButton.gameObject.SetActive(true);
        }

        Debug.Log("[M02Controller] 沉浸期结束，已显示操作按钮 (拍照 / 完成)。");
    }

    /// <summary>
    /// 用户点击拍照按钮（先预留接口，后续挂接正式截屏）
    /// </summary>
    public void OnCapturePhotoClicked()
    {
        Debug.Log("[M02Controller] 用户点击了拍照按钮！准备记录并赠送给 Wen。");
        
        // 拍照后直接顺理成章进入结束与反思
        OnFinishViewingClicked();
    }

    /// <summary>
    /// 用户主动结束观察 (无论是点击 Finish Viewing 还是拍完照)
    /// </summary>
    public void OnFinishViewingClicked()
    {
        if (hudDelayCoroutine != null)
        {
            StopCoroutine(hudDelayCoroutine);
            hudDelayCoroutine = null;
        }

        // 隐藏 HUD 和 Portal
        if (portalHudRoot != null) portalHudRoot.SetActive(false);
        if (finishViewingButton != null) finishViewingButton.gameObject.SetActive(false);
        if (capturePhotoButton != null) capturePhotoButton.gameObject.SetActive(false);
        if (portalObject != null) portalObject.SetActive(false);

        // 此时才正式展示反思阶段！
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
