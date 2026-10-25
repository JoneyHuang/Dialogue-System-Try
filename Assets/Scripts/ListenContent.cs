using UnityEngine;

[CreateAssetMenu(menuName = "CCC AR/Listen Content", fileName = "ListenContent")]
public class ListenContent : ScriptableObject
{
    [Header("Recording")]
    public AudioClip recording;

    [Header("Short prompt")]
    [TextArea(1, 3)]
    public string teaser = "You are listening to the real choir recording — know more.";

    [Header("Detailed context")]
    public string detailTitle = "Choir, worship and memory";
    [TextArea(8, 20)]
    public string detailBody =
        "Choral singing has long been part of many Christian services. Choirs lead hymns, psalms and responses, giving the congregation a shared voice and helping shape the rhythm of worship.\n\n" +
        "A hymn is more than music: its words and melody can carry prayer, teaching and stories that people return to over many years. Singing together invites people to take part in a shared ritual.\n\n" +
        "These sounds can also hold personal memories. A familiar harmony may recall family, a particular service or a loved one's voice. This recording invites you to consider how a choir can connect a place, a tradition and the people who remember it.";
}