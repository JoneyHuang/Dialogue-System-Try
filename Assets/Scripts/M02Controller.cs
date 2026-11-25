using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// M02现场体验流程调度器 (Coordinator / Sequencer)。
/// 负责从到达提示、对话、传送门站位引导、动态激活 Portal、5秒沉浸期、延迟操作栏、反思卡、汇报到 ExperimentHub 的完整流程。
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

    [Header("Sub-Controllers")]
    [Tooltip("通用提示/引导弹窗控制器 (HintPanel)")]
    public PromptPanelController promptPanelController;

    [Tooltip("Typewriter 对话系统控制器 (DialoguePanel)")]
    public DialogueController dialogueController;

    [Tooltip("Meet Me Panel")]
    public MeetMePanelController meetMePanelController;

    [Header("Current POI Trigger Pin")]
    [Tooltip("Trigger current POI")]
    public ProximityTrigger currentPinTrigger;

    [Header("AR Portal Content & Placement")]
    [Tooltip("现场的 Gothic Window 传送门根物体 (包含窗框与全景球)")]
    public GameObject portalObject;

    [Tooltip("是否在用户正前方动态生成 Portal (破除固定位置被人流/围栏阻挡的限制)")]
    public bool spawnInFrontOfUser = false; // Default: use fixed portal position, can be reset to front of user via UI button

    [Tooltip("动态生成时与相机的水平距离（米）")]
    public float spawnDistance = 2.2f;

    [Tooltip("是否让 Portal 的正面面向用户 (true 为面对用户，false 为背对用户)")]
    public bool faceTowardsUser = true;

    [Tooltip("高度偏移量（米，相对于相机的高度。设为 -0.2 表示门中心略低于视线，脚踏实地）")]
    public float heightOffset = -0.2f;

    [Tooltip("门的 Y 轴旋转微调偏角（度数，若模型自身正反面有偏差可在此加减如 180）")]
    public float rotationOffsetAngle = 0f;

    [Header("Portal Viewing HUD (延迟显现操作栏)")]
    [Tooltip("Portal 观看期间的操作容器面板 (包含拍照与结束按钮)")]
    public GameObject portalHudRoot;

    [Tooltip("拍照按钮 (可选)")]
    public Button capturePhotoButton;

    [Tooltip("完成观察/跳过按钮")]
    public Button finishViewingButton;

    [Tooltip("Reset portal button (fallback to front of camera)")]
    public Button resetPortalButton;

    [Tooltip("延迟显示操作栏的等待时间 (秒)，给被试自由探索的心理沉淀期")]
    public float delayBeforeHudVisible = 5.0f;

    [Header("Debug")]
    [Tooltip("Play 模式启动时自动触发流程（正式测试请保持 false）")]
    public bool autoStartOnPlay = false;

    // ─── Fallback 文案 ────────────────────────────────────────────────────────
    private const string kFallbackArrivalPrompt = "You have arrived at the second site.\nMeet Wen, an architect and heritage researcher. Let's hear his perspective...";
    private const string kFallbackArrivalBtn = "Meet Wen";
    private const string kFallbackPortalPrompt = "Look towards an open area in front of you. Step through the portal to explore the historical interior.";
    private const string kFallbackPortalBtn = "Open Portal";
    private const string kFallbackReflectionPrompt = "Seeing the interior preserved in time, what emotions or details stand out to you the most?";
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

        // Bind button events
        if (finishViewingButton != null)
        {
            finishViewingButton.onClick.RemoveListener(OnFinishViewingClicked);
            finishViewingButton.onClick.AddListener(OnFinishViewingClicked);
        }

        //if (capturePhotoButton != null)
        //{
          //  capturePhotoButton.onClick.RemoveListener(OnCapturePhotoClicked);
            //capturePhotoButton.onClick.AddListener(OnCapturePhotoClicked);
        //}

        if (resetPortalButton != null)
        {
            resetPortalButton.onClick.RemoveListener(OnResetPortalClicked);
            resetPortalButton.onClick.AddListener(OnResetPortalClicked);
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
            if (t != null) promptPanelController = t.GetComponent<PromptPanelController>();
        }

        if (dialogueController == null)
        {
            Transform t = canvasTr.Find("DialoguePanel");
            if (t != null) dialogueController = t.GetComponent<DialogueController>();
        }

        if (meetMePanelController == null)
        {
            Transform t = canvasTr.Find("MeetMePanel");
            if (t != null) meetMePanelController = t.GetComponent<MeetMePanelController>();
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
        if (resetPortalButton != null) resetPortalButton.gameObject.SetActive(false);
        if (meetMePanelController != null) meetMePanelController.Hide();
        currentState = FlowState.Hidden;
    }

    // ─── Flow Steps ───────────────────────────────────────────────────────────
    public void StartStoryFlow()
    {
        ShowMeetingHint();
    }

    public void ShowMeetingHint()
    {
        currentState = FlowState.MeetingHint;
        string brief = storyData != null ? storyData.arrivalPrompt : kFallbackArrivalPrompt;
        string btnText = storyData != null ? storyData.arrivalButtonText : kFallbackArrivalBtn;
        string name = storyData != null ? storyData.speakerName : "Storyteller";
        Sprite avatar = storyData != null ? storyData.characterAvatar : null;

        if (meetMePanelController != null)
        {
            meetMePanelController.SetName(name);
            meetMePanelController.SetBrief(brief);
            meetMePanelController.SetAvatar(avatar);
            meetMePanelController.SetAction(StartDialogue);

            meetMePanelController.SetCloseAction(() =>
            {
                currentState = FlowState.Hidden;
                if (currentPinTrigger != null)
                {
                    currentPinTrigger.ResetTrigger();
                }
            });

            meetMePanelController.Show();
        }
        
        else
        {
            Debug.LogWarning("[M02Controller] MeetMePanel not found. Starts dialogue instead.");
            StartDialogue();
        }
    }

    public void StartDialogue()
    {
        currentState = FlowState.Dialogue;
        if (meetMePanelController != null) meetMePanelController.Hide();

        string[] lines = (storyData != null && storyData.dialogueLines != null && storyData.dialogueLines.Length > 0)
            ? storyData.dialogueLines
            : null;
        string speaker = storyData != null ? storyData.speakerName : "Wen";
        Sprite avatar = storyData != null ? storyData.characterAvatar : null;
        AudioClip[] clips = storyData != null ? storyData.voiceClips : null;

        if (dialogueController != null)
        {
            dialogueController.OnDialogueFinished -= ShowPortalInstruction;
            dialogueController.OnDialogueFinished += ShowPortalInstruction;
            dialogueController.StartDialogue(lines, speaker, avatar, clips);
        }
        else
        {
            Debug.LogError("[M02Controller] 未找到 DialogueController，直接进入 Portal 引导！");
            ShowPortalInstruction();
        }
    }

    public void ShowPortalInstruction()
    {
        currentState = FlowState.PortalInstruction;
        if (dialogueController != null) dialogueController.gameObject.SetActive(false);

        // Build prompt
        string prompt = "";
        if (storyData != null)
        {
            if (!string.IsNullOrEmpty(storyData.alignmentPrompt))
                prompt += storyData.alignmentPrompt + "\n\n";
            if (!string.IsNullOrEmpty(storyData.interactionInstruction))
                prompt += storyData.interactionInstruction;
        }
        string btnText = (storyData != null && !string.IsNullOrEmpty(storyData.alignmentButtonText))
            ? storyData.alignmentButtonText
            : kFallbackPortalBtn;

        if (promptPanelController != null)
            promptPanelController.ShowPrompt(prompt, btnText, EnterPortalViewing);
        else
            EnterPortalViewing();
    }

    public void EnterPortalViewing()
    {
        currentState = FlowState.PortalViewing;

        // Hide central prompt panel
        if (promptPanelController != null)
            promptPanelController.Hide();

        // Position portal (fixed or in front of user)
        if (portalObject != null)
        {
            if (spawnInFrontOfUser && Camera.main != null)
                PlacePortalInFrontOfCamera();
            portalObject.SetActive(true);
            Debug.Log("[M02Controller] Portal activated!");
        }

        // Show reset button immediately
        if (resetPortalButton != null)
            resetPortalButton.gameObject.SetActive(true);

        // Hide HUD panels initially and start delayed show coroutine
        if (portalHudRoot != null) portalHudRoot.SetActive(false);
        if (finishViewingButton != null) finishViewingButton.gameObject.SetActive(false);
        if (capturePhotoButton != null) capturePhotoButton.gameObject.SetActive(false);

        if (hudDelayCoroutine != null) StopCoroutine(hudDelayCoroutine);
        hudDelayCoroutine = StartCoroutine(ShowHudAfterDelayRoutine());
    }

    private void PlacePortalInFrontOfCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[M02Controller] Camera.main 为空，无法动态放置 Portal！");
            return;
        }
        Transform camTr = cam.transform;
        Vector3 forwardXZ = Vector3.ProjectOnPlane(camTr.forward, Vector3.up).normalized;
        if (forwardXZ.sqrMagnitude < 0.001f) forwardXZ = Vector3.forward;
        Vector3 targetWorldPos = camTr.position + forwardXZ * spawnDistance;
        targetWorldPos.y = camTr.position.y + heightOffset;
        Vector3 lookDir = faceTowardsUser ? -forwardXZ : forwardXZ;
        Quaternion targetWorldRot = Quaternion.LookRotation(lookDir, Vector3.up);
        if (Mathf.Abs(rotationOffsetAngle) > 0.1f)
            targetWorldRot *= Quaternion.Euler(0f, rotationOffsetAngle, 0f);
        portalObject.transform.SetPositionAndRotation(targetWorldPos, targetWorldRot);
        Debug.Log($"[M02Controller] Portal placed at {targetWorldPos} with rotation {targetWorldRot.eulerAngles}");
    }

    private IEnumerator ShowHudAfterDelayRoutine()
    {
        yield return new WaitForSeconds(delayBeforeHudVisible);
        if (portalHudRoot != null) portalHudRoot.SetActive(true);
        if (finishViewingButton != null) finishViewingButton.gameObject.SetActive(true);
        if (capturePhotoButton != null) capturePhotoButton.gameObject.SetActive(true);
        Debug.Log("[M02Controller] User can choose Move On at this stage");
    }

    //public void OnCapturePhotoClicked()
    //{
      //  Debug.Log("[M02Controller] User clicked Capture Photo button! Preparing to record and give to Wen.");
        //OnFinishViewingClicked();
    //}

    public void OnResetPortalClicked()
    {
        if (portalObject != null)
        {
            PlacePortalInFrontOfCamera();
            portalObject.SetActive(true);
            Debug.Log("[M02Controller] Reset portal to front of camera via Reset button.");
        }
    }

    public void OnFinishViewingClicked()
    {
        if (hudDelayCoroutine != null)
        {
            StopCoroutine(hudDelayCoroutine);
            hudDelayCoroutine = null;
        }
        // Hide HUD, portal, and reset button
        if (portalHudRoot != null) portalHudRoot.SetActive(false);
        if (finishViewingButton != null) finishViewingButton.gameObject.SetActive(false);
        if (capturePhotoButton != null) capturePhotoButton.gameObject.SetActive(false);
        if (resetPortalButton != null) resetPortalButton.gameObject.SetActive(false);
        if (portalObject != null) portalObject.SetActive(false);
        ShowReflection();
    }

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
            promptPanelController.ShowPrompt(prompt, btnText, CompleteM02);
        else
            CompleteM02();
    }

    public void CompleteM02()
    {
        currentState = FlowState.Hidden;
        if (promptPanelController != null) promptPanelController.Hide();
        if (portalObject != null) portalObject.SetActive(false);
        if (ExperimentHubController.Instance != null)
        {
            ExperimentHubController.Instance.OnPoICompleted("M2");
            Debug.Log("[M02Controller] M02 流程结束，已向 ExperimentHub 汇报完成！");
        }
    }
}
