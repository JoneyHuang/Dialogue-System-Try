using UnityEngine;

/// <summary>
/// 每个 POI 的叙事内容 ScriptableObject 模板。
/// 存储角色名、所有台词、各阶段提示文本，使内容与代码完全分离。
/// 路径：Assets/StoryData/<poiName>.asset
/// </summary>
[CreateAssetMenu(fileName = "NewPoIStory", menuName = "AR Story/PoI Story Data")]
public class PoIStoryData : ScriptableObject
{
    [Header("Character")]
    [Tooltip("说话角色的显示名称")]
    public string speakerName = "Storyteller";

    [Tooltip("角色头像（可选）")]
    public Sprite characterAvatar;

    [Header("Arrival Prompt — 阶段 1")]
    [TextArea(2, 5)]
    public string arrivalPrompt = "You have arrived at the site.";
    public string arrivalButtonText = "Start";

    [Header("Dialogue Lines — 阶段 2")]
    [TextArea(2, 5)]
    public string[] dialogueLines;

    [Header("Alignment Prompt — 阶段 3 (AR 交互前站位)")]
    [TextArea(2, 5)]
    public string alignmentPrompt = "Position yourself to match the historical perspective.";
    public string alignmentButtonText = "I'm in Position";

    [Header("Interaction Instruction — 阶段 4 (可选 AR 操作说明)")]
    [TextArea(2, 5)]
    public string interactionInstruction = "";
    public string interactionButtonText = "Got it";

    [Header("Reflection Prompt — 阶段 5 (AR 交互后回顾)")]
    [TextArea(2, 5)]
    public string reflectionPrompt = "Take a moment to reflect on what you've just experienced.";
    public string reflectionButtonText = "Continue";
}

