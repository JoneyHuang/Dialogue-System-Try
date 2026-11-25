using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 研究者控制台中枢 (Experiment Hub Controller)。
/// 负责 Latin Square 实验中的 POI 自由派发、状态记录 (未开始 / 已完成)、重置与重新返回控制台。
/// </summary>
public class ExperimentHubController : MonoBehaviour
{
    public static ExperimentHubController Instance { get; private set; }

    [System.Serializable]
    public class PoIHubItem
    {
        [Tooltip("Module name")]
        public string poiId = "M1";

        [Tooltip("Location name")]
        public string locationName = "Cathedral West Front";

        [Tooltip("Top-left category title on the panel (e.g. 'LOCATION ARCHIVE' or 'MODULE BRIEF')")]
        public string cardHeaderTitle = "LOCATION ARCHIVE";

        [Tooltip("Location reference photos")]
        public Sprite locationPhoto;

        [Tooltip("Introduction and navigation")]
        [TextArea(3, 8)]
        public string guideAndHistory;

        [Tooltip("Launch button")]
        public Button launchButton;

        [Tooltip("Status text")]
        public TMP_Text statusText;

        [Tooltip("Checkmark icon")]
        public GameObject checkmarkIcon;

        [Tooltip("The target object for navigation or launching")]
        public GameObject poiTargetObject;

        [HideInInspector]
        public bool isCompleted = false;
    }

    [Header("UI Panels")]
    [Tooltip("Hub 控制台根节点 (包含全景图和 3 个按钮的 Panel)")]
    public GameObject hubPanel;

    [Tooltip("复用的地点档案/卡片面板 (直接将 MeetMePanel 拖入)")]
    public MeetMePanelController locationInfoPanel;

    [Tooltip("通用提示面板控制器 (直接将 PromptPanel 拖入)")]
    public PromptPanelController promptPanelController;

    [Tooltip("标题或指导说明文本 (可选)")]
    public TMP_Text headerText;

    [Header("POI Modules (M1, M2, M3)")]
    public PoIHubItem poi1 = new PoIHubItem { poiId = "M1" };
    public PoIHubItem poi2 = new PoIHubItem { poiId = "M2" };
    public PoIHubItem poi3 = new PoIHubItem { poiId = "M3" };

    [Header("Experimenter Controls")]
    [Tooltip("研究者重置所有进度的按钮 (可选)")]
    public Button resetAllButton;

    [Header("Status Feedback Style")]
    public Color notStartedColor = new Color(0.6f, 0.6f, 0.6f, 1f); // 灰色
    public Color completedColor = new Color(0.18f, 0.75f, 0.35f, 1f); // 鲜明绿色

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (hubPanel == null)
        {
            hubPanel = gameObject;
        }

        // 自动装配 locationInfoPanel 与 promptPanelController，防止 Inspector 遗漏
        if (locationInfoPanel == null)
        {
            locationInfoPanel = FindFirstObjectByType<MeetMePanelController>(FindObjectsInactive.Include);
        }
        if (promptPanelController == null)
        {
            promptPanelController = FindFirstObjectByType<PromptPanelController>(FindObjectsInactive.Include);
        }

        BindListeners();
    }

    void Start()
    {
        RefreshAllStatuses();
    }

    private void BindListeners()
    {
        if (poi1.launchButton != null)
            poi1.launchButton.onClick.AddListener(() => OpenLocationCard(poi1));

        if (poi2.launchButton != null)
            poi2.launchButton.onClick.AddListener(() => OpenLocationCard(poi2));

        if (poi3.launchButton != null)
            poi3.launchButton.onClick.AddListener(() => OpenLocationCard(poi3));

        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(ResetAllProgress);
    }

    /// <summary>
    /// 点击 Hub 全景图上的地点图标：弹出地点档案卡片 (复用 MeetMePanel)
    /// </summary>
    public void OpenLocationCard(PoIHubItem item)
    {
        if (item == null) return;

        if (locationInfoPanel != null)
        {
            // Reset panel UI to clear any previous state
            locationInfoPanel.ResetPanel();

            // 1. Dynamic changing title
            string header = !string.IsNullOrEmpty(item.cardHeaderTitle) ? item.cardHeaderTitle : "LOCATION ARCHIVE";
            locationInfoPanel.SetTitle(header);

            locationInfoPanel.SetName(string.IsNullOrEmpty(item.locationName) ? item.poiId : item.locationName);
            if (item.locationPhoto != null)
            {
                locationInfoPanel.SetAvatar(item.locationPhoto);
            }
            locationInfoPanel.SetBrief(item.guideAndHistory);
            locationInfoPanel.SetButtonText("GOING THERE");

            // Confirm arrival and trigger love heart pin
            locationInfoPanel.SetAction(() =>
            {
                locationInfoPanel.Hide();
                LaunchPoI(item);
            });

            // 2. 退出机制：点击卡片右上角退出或返回时，仅关闭卡片，回到 Hub 全景图
            // Hide() is now built into SetCloseAction, no need to call it manually
            locationInfoPanel.SetCloseAction(() =>
            {
                if (hubPanel != null) hubPanel.SetActive(true);
            });

            locationInfoPanel.Show();
        }
        else
        {
            // 降级：如果未找到卡片，直接启动
            LaunchPoI(item);
        }
    }

    /// <summary>
    /// 启动指定的 POI（用户在卡片点击“确认到达”后调用）
    /// </summary>
    public void LaunchPoI(PoIHubItem item)
    {
        if (item == null) return;

        Debug.Log($"[ExperimentHub] Arrival confirmed {item.poiId}...");

        // 进入 AR / Module 前，平滑淡出背景氛围音乐
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.FadeOutBGM(1.5f);
            AudioManager.Instance.PlayConfirm();
        }

        // 隐藏 Hub 面板
        if (hubPanel != null)
        {
            hubPanel.SetActive(false);
        }

        // 切换到非 M1 模块时，安全清理 M01 的大教堂 3D 模型
        if (item.poiId != "M1" && M01Controller.Instance != null)
        {
            M01Controller.Instance.HideCathedralModel();
        }

        // 1. 如果配置了对应的 GameObject (如现场心形标记或 Pin 导航)，将其激活
        if (item.poiTargetObject != null)
        {
            item.poiTargetObject.SetActive(true);
            Debug.Log($"[ExperimentHub] 已激活 {item.poiId} 的现场目标物体：{item.poiTargetObject.name}。");
        }

        // 2. 弹出 PromptPanel 提示用户使用相机画面寻找爱心标记
        if (promptPanelController != null)
        {
            promptPanelController.ShowPrompt(
                "Look around and find the love heart. Approch or tap it to unlock the memory.",
                "Got it"
            );
        }
        else
        {
            // 调试降级：若无 promptPanel 且无实体 Pin，才直接开跑 M1
            if (item.poiId == "M1" && item.poiTargetObject == null && M01Controller.Instance != null)
            {
                Debug.LogWarning("[ExperimentHub] poiTargetObject not found. Starts it anyway.");
                M01Controller.Instance.StartStoryFlow();
            }
        }
    }

    /// <summary>
    /// 当任一 POI 流程完成时调用该方法
    /// </summary>
    /// <param name="poiId">"M1", "M2", 或 "M3"</param>
    public void OnPoICompleted(string poiId)
    {
        Debug.Log($"[ExperimentHub] 收到通知：{poiId} 已顺利完成！");

        PoIHubItem targetItem = GetItemById(poiId);
        if (targetItem != null)
        {
            targetItem.isCompleted = true;
        }

        // 重新弹出 Hub 控制台，供研究者填写问卷或点击下一个 POI
        if (hubPanel != null)
        {
            hubPanel.SetActive(true);
        }

        // 回到 Hub 控制台时，重新平滑淡入背景氛围音乐
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.FadeInBGM(duration: 2.0f);
        }

        RefreshAllStatuses();
    }

    /// <summary>
    /// 刷新所有按钮上的视觉状态（文字、颜色、打钩标记）
    /// </summary>
    public void RefreshAllStatuses()
    {
        UpdateItemStatusUI(poi1);
        UpdateItemStatusUI(poi2);
        UpdateItemStatusUI(poi3);

        if (headerText != null)
        {
            if (poi1.isCompleted && poi2.isCompleted && poi3.isCompleted)
            {
                headerText.text = "All Modules Completed!";
                headerText.color = completedColor;
            }
            else
            {
                headerText.text = "Cathedral Square Study — Select Module";
                headerText.color = Color.white;
            }
        }
    }

    private void UpdateItemStatusUI(PoIHubItem item)
    {
        if (item == null) return;

        // 1. 如果有单独的 StatusText
        if (item.statusText != null)
        {
            item.statusText.text = item.isCompleted ? "Done" : "Not Started";
            item.statusText.color = item.isCompleted ? completedColor : notStartedColor;
        }

        // 2. 如果有 CheckmarkIcon
        if (item.checkmarkIcon != null)
        {
            item.checkmarkIcon.SetActive(item.isCompleted);
        }

        // 3. 增强：若没有单独的 statusText，微调按钮上的文字或颜色
        if (item.launchButton != null && item.statusText == null && item.checkmarkIcon == null)
        {
            var btnText = item.launchButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null && item.isCompleted && !btnText.text.EndsWith("✓"))
            {
                btnText.text += " (Done)";
                btnText.color = completedColor;
            }
        }
    }

    /// <summary>
    /// 重置所有进度（用于测试下一位被试）
    /// </summary>
    public void ResetAllProgress()
    {
        poi1.isCompleted = false;
        poi2.isCompleted = false;
        poi3.isCompleted = false;

        RefreshAllStatuses();
        Debug.Log("[ExperimentHub] 已重置所有 POI 状态！");
    }

    private PoIHubItem GetItemById(string id)
    {
        if (string.Equals(id, "M1", StringComparison.OrdinalIgnoreCase)) return poi1;
        if (string.Equals(id, "M2", StringComparison.OrdinalIgnoreCase)) return poi2;
        if (string.Equals(id, "M3", StringComparison.OrdinalIgnoreCase)) return poi3;
        return null;
    }
}
