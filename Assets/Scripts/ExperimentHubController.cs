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
        [Tooltip("POI 标识名，例如 M1, M2, M3")]
        public string poiId = "M1";

        [Tooltip("点击启动该 POI 的按钮")]
        public Button launchButton;

        [Tooltip("状态提示文字 (可选)，例如显示 Not Started 或 Done ✓")]
        public TMP_Text statusText;

        [Tooltip("完成时显示的打钩图标/小红绿点 (可选)")]
        public GameObject checkmarkIcon;

        [Tooltip("该 POI 在现场对应的 Pin 导航目标或启动 GameObject")]
        public GameObject poiTargetObject;

        [HideInInspector]
        public bool isCompleted = false;
    }

    [Header("UI Panels")]
    [Tooltip("Hub 控制台根节点 (包含 3 个按钮的 Panel)")]
    public GameObject hubPanel;

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

        BindListeners();
    }

    void Start()
    {
        RefreshAllStatuses();
    }

    private void BindListeners()
    {
        if (poi1.launchButton != null)
            poi1.launchButton.onClick.AddListener(() => LaunchPoI(poi1));

        if (poi2.launchButton != null)
            poi2.launchButton.onClick.AddListener(() => LaunchPoI(poi2));

        if (poi3.launchButton != null)
            poi3.launchButton.onClick.AddListener(() => LaunchPoI(poi3));

        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(ResetAllProgress);
    }

    /// <summary>
    /// 启动指定的 POI
    /// </summary>
    public void LaunchPoI(PoIHubItem item)
    {
        if (item == null) return;

        Debug.Log($"[ExperimentHub] 正在启动 {item.poiId}...");

        // 隐藏 Hub 面板
        if (hubPanel != null)
        {
            hubPanel.SetActive(false);
        }

        // 如果配置了对应的 GameObject (例如现场 Pin 导航标志)，将其激活以引导用户前往
        if (item.poiTargetObject != null)
        {
            item.poiTargetObject.SetActive(true);
            Debug.Log($"[ExperimentHub] 已激活 {item.poiId} 的现场目标物体：{item.poiTargetObject.name}。等待用户走近...");
        }
        else
        {
            // 仅作为调试降级方案：如果未指定现场 Pin，才直接触发故事
            if (item.poiId == "M1" && M01Controller.Instance != null)
            {
                Debug.LogWarning("[ExperimentHub] 未配置 poiTargetObject，直接启动 M1 故事！");
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
            item.statusText.text = item.isCompleted ? "Done ✓" : "Not Started";
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
                btnText.text += " (Done ✓)";
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

