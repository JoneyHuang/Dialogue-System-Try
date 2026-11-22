using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    public enum OverlayMode
    {
        Model3D = 0,    // AR 3D)
        Photo2D = 1     // Historical 2D
    }

    [Header("Current Flow State")]
    public FlowState currentState = FlowState.Hidden;

    [Header("Overlay View Mode")]
    [Tooltip("Default Overlay Mode）")]
    public OverlayMode defaultOverlayMode = OverlayMode.Model3D;
    public OverlayMode currentOverlayMode = OverlayMode.Model3D;

    [Tooltip("Switch")]
    public GameObject overlaySwitchBar;

    [Tooltip("Toggle")]
    public Toggle toggle3D;
    public Toggle toggle2D;

    [Header("Story Data (ScriptableObject)")]
    [Tooltip("将 Assets/StoryData/PoI_01_Susan.asset 拖入此栏")]
    public PoIStoryData storyData;

    [Header("Sub-Controllers (即插即用组件)")]
    [Tooltip("通用提示/引导弹窗控制器")]
    public PromptPanelController promptPanelController;

    [Tooltip("Meet Me Panel")]
    public MeetMePanelController meetMePanelController;

    [Tooltip("Typewriter 对话系统控制器")]
    public DialogueController dialogueController;

    [Tooltip("历史照片对比控制器")]
    public HistoricalComparisonController historicalComparisonController;

    [Tooltip("Christchurch Cathedral AR 3D Model")]
    public GameObject christchurchCathedral3DModel;

    [Header("Next Destination (optional)")]
    [Tooltip("完成该 POI 后激活的下一个目标物体 (如 PoI 2 Pin 或玫瑰窗 HotSpot)")]
    public GameObject nextPoiTarget;

    [Header("Exit / Continue")]
    [Tooltip("Global finish/continue button located on the overlay switch bar or HUD")]
    public Button finishComparisonButton;

    [Header("Debug")]
    [Tooltip("Play 模式启动时自动触发流程（正式测试请保持 false，等待走到 Pin 触发）")]
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

        if (meetMePanelController == null)
        {
            Transform t = canvasTr.Find("MeetMePanel");
            if (t != null)
                meetMePanelController = t.GetComponent<MeetMePanelController>();
        }

        if (finishComparisonButton != null)
        {
            finishComparisonButton.onClick.RemoveListener(FinishComparisonAndProceed);
            finishComparisonButton.onClick.AddListener(FinishComparisonAndProceed);
        }

    }

    // ─── Panel Utility ────────────────────────────────────────────────────────

    public void HideAllPanels()
    {
        if (promptPanelController != null) promptPanelController.Hide();
        if (dialogueController != null) dialogueController.gameObject.SetActive(false);
        if (historicalComparisonController != null) historicalComparisonController.gameObject.SetActive(false);
        if (meetMePanelController != null) meetMePanelController.gameObject.SetActive(false);
        if (overlaySwitchBar != null) overlaySwitchBar.SetActive(false);
        HideCathedralModel();

        currentState = FlowState.Hidden;
    }

    // ─── Flow Steps ───────────────────────────────────────────────────────────

    /// <summary>
    /// 入口：由现场导航 Pin 的 ProximityTrigger 到达时调用
    /// </summary>
    public void StartStoryFlow()
    {
        ShowMeetMeHint();
    }

    /// <summary>
    /// 阶段 1：展示"初识 Susan"到达提示
    /// </summary>
    public void ShowMeetMeHint()
    {
        currentState = FlowState.MeetingHint;

        string brief = storyData != null ? storyData.arrivalPrompt : kFallbackArrivalPrompt;
        string btnText = storyData != null ? storyData.arrivalButtonText : kFallbackArrivalBtn;
        string name   = storyData != null ? storyData.speakerName : "Storyteller";
        Sprite avatar = storyData != null ? storyData.characterAvatar : null;
      
      // MeetMePanel
       if (meetMePanelController != null)
        {
          meetMePanelController.SetName(name);
          meetMePanelController.SetBrief(brief);
          meetMePanelController.SetAvatar(avatar);
          meetMePanelController.SetAction(StartDialogue);
          meetMePanelController.Show();
        }

        else
        {
            Debug.LogWarning("[M01Controller] MeetMePanel not found. Starts dialogue instead.");
            StartDialogue();
        }
    }

    /// <summary>
    /// 阶段 2：启动 Susan 对话（台词从 ScriptableObject 取得）
    /// </summary>
    public void StartDialogue()
    {
        currentState = FlowState.Dialogue;
        
        if (meetMePanelController != null) meetMePanelController.Hide();
        if (promptPanelController != null) promptPanelController.Hide();

        string[] lines = (storyData != null && storyData.dialogueLines != null && storyData.dialogueLines.Length > 0)
            ? storyData.dialogueLines
            : null;
        string speaker = storyData != null ? storyData.speakerName : null;

        Sprite avatar = storyData != null ? storyData.characterAvatar : null;
        AudioClip[] clips = storyData != null ? storyData.voiceClips : null;

        if (dialogueController != null)
        {
            dialogueController.OnDialogueFinished -= ShowAlignHint;
            dialogueController.OnDialogueFinished += ShowAlignHint;
            dialogueController.StartDialogue(lines, speaker, avatar, clips);
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
    /// 阶段 4：站位完成，打开历史对比与 3D/2D 覆盖层切换
    /// </summary>
    public void ShowHistoricalPanel()
    {
        currentState = FlowState.HistoricalComparison;

        if (promptPanelController != null)
            promptPanelController.Hide();

        // 激活独立切换栏 UI（如果在 Hierarchy 中配置了）
        if (overlaySwitchBar != null)
            overlaySwitchBar.SetActive(true);

        // 应用默认覆盖层模式（互斥切换 3D 或 2D）
        SetOverlayMode(defaultOverlayMode);
    }

    // ─── 覆盖层互斥切换方法 (Overlay Mode Switching) ──────────────────────────

    /// <summary>
    /// 设置并应用指定的覆盖层模式（3D模型 或 2D历史照片）
    /// 现场（AR相机实景）始终作为底图呈现，两层覆盖物严格互斥显示
    /// </summary>
    public void SetOverlayMode(OverlayMode mode)
    {
        currentOverlayMode = mode;
        ApplyOverlayMode(mode);
    }

    /// <summary>
    /// 通过整数索引切换（0 = Model3D, 1 = Photo2D），便于通用 UI 控件绑定
    /// </summary>
    public void SetOverlayModeByIndex(int index)
    {
        OverlayMode mode = (OverlayMode)Mathf.Clamp(index, 0, 1);
        SetOverlayMode(mode);
    }

    /// <summary>
    /// Switch to 3D
    /// </summary>
    public void SwitchTo3D() => SetOverlayMode(OverlayMode.Model3D);

    /// <summary>
    /// Switch to 2D Historical Archival
    /// </summary>
    public void SwitchTo2D() => SetOverlayMode(OverlayMode.Photo2D);

    /// <summary>
    /// Unity Toggle: 3D Toggle
    /// </summary>
    public void OnToggle3DChanged(bool isOn)
    {
        if (isOn) SetOverlayMode(OverlayMode.Model3D);
    }

    /// <summary>
    /// Unity Toggle: 2D Toggle
    /// </summary>
    public void OnToggle2DChanged(bool isOn)
    {
        if (isOn) SetOverlayMode(OverlayMode.Photo2D);
    }

    /// <summary>
    /// Mutual exclusion
    /// </summary>
    private void ApplyOverlayMode(OverlayMode mode)
    {
        if (mode == OverlayMode.Model3D)
        {
            // 1. Hide 2D（注意：直接隐藏 GameObject，切勿调用 CloseComparison 防止误触发 onComparisonClosed）
            if (historicalComparisonController != null)
            {
                historicalComparisonController.gameObject.SetActive(false);
            }

            // 2. Activate 3D
            if (christchurchCathedral3DModel != null)
            {
                christchurchCathedral3DModel.SetActive(true);
            }
        }
        else if (mode == OverlayMode.Photo2D)
        {
            // 1. Hide 3D 
            HideCathedralModel();

            // 2. Open 2D Historical
            if (historicalComparisonController != null)
            {
                historicalComparisonController.gameObject.SetActive(true);
                historicalComparisonController.OpenComparison();
            }
        }

        // Synchronise toggles
        SyncToggleUI();
    }

    /// <summary>
    /// Unified exit point whether user was viewing 2D photo or 3D model.
   /// </summary>
   public void FinishComparisonAndProceed()
     {
       // 1. Clean up 3D model
       HideCathedralModel();
       
       // 2. Clean up 2D Historical panel safely
       if (historicalComparisonController != null)
       {
         historicalComparisonController.gameObject.SetActive(false);
       }
       
       // 3. Hide the switch bar itself
       if (overlaySwitchBar != null)
       {
         overlaySwitchBar.SetActive(false);
       }
       
       // 4. Progress to reflection
       ShowReflection();
     }
     

    /// <summary>
    /// 保持 UI Toggle 的视觉状态与当前 currentOverlayMode 一致
    /// </summary>
    private void SyncToggleUI()
    {
        if (toggle3D != null && toggle3D.isOn != (currentOverlayMode == OverlayMode.Model3D))
        {
            toggle3D.SetIsOnWithoutNotify(currentOverlayMode == OverlayMode.Model3D);
        }

        if (toggle2D != null && toggle2D.isOn != (currentOverlayMode == OverlayMode.Photo2D))
        {
            toggle2D.SetIsOnWithoutNotify(currentOverlayMode == OverlayMode.Photo2D);
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
    /// 注意：此时面板已经由 HistoricalComparisonController 自身关闭，切勿再次调用 CloseComparison() 导致无限递归死锁！
    /// </summary>
    private void OnHistoricalComparisonClosed()
    {
        // 彻底清理 3D 与 2D，并隐藏切换控制条
        HideCathedralModel();
        if (historicalComparisonController != null)
        {
            historicalComparisonController.gameObject.SetActive(false);
        }
        if (overlaySwitchBar != null)
        {
            overlaySwitchBar.SetActive(false);
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
    /// 阶段 6：M01 完成，通知 ExperimentHub 并激活可选后续目标
    /// </summary>
    public void CompleteM01()
    {
        currentState = FlowState.Hidden;

        if (promptPanelController != null) promptPanelController.Hide();

        // 1. 如果存在 ExperimentHubController，通知 Hub：M1 已完成并返回控制台
        if (ExperimentHubController.Instance != null)
        {
            ExperimentHubController.Instance.OnPoICompleted("M1"); 
            HideCathedralModel();

        }

       
        // 2. 兼容原先的直接链式激活（如果设置了 nextPoiTarget）
        if (nextPoiTarget != null)
        {
            nextPoiTarget.SetActive(true);
            Debug.Log($"[M01Controller] M01 完成！已激活下一目的地：{nextPoiTarget.name}");
        }
        else
        {
            Debug.Log("[M01Controller] M01 completed. Reported to hub.");
        }
    }

    // ─── Legacy / Compat ──────────────────────────────────────────────────────

    /// <summary>
    /// 保留方法名兼容旧事件绑定（内部改为调用 OnHistoricalComparisonClosed）
    /// </summary>
    public void ExitHistoricalComparison() => OnHistoricalComparisonClosed();

    /// <summary>
    /// Hide the cathedral 3D model. Called by ExperimentHubController when switching modules.
    /// </summary>
    public void HideCathedralModel()
    {
        if (christchurchCathedral3DModel != null)
        {
            christchurchCathedral3DModel.SetActive(false);
            Debug.Log("[M01Controller] Cathedral model hidden via HideCathedralModel().");
        }
        else
        {
            // Fallback: attempt to find by known names
            var obj = GameObject.Find("Christchurch Cathedral 3D Model") ??
                      GameObject.Find("ChristchurchCathedral");
            if (obj != null)
            {
                obj.SetActive(false);
                Debug.Log("[M01Controller] Cathedral model hidden via fallback lookup.");
            }
            else
            {
                Debug.LogWarning("[M01Controller] Cathedral model not found to hide.");
            }
        }
    }
}
